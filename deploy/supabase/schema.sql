-- =====================================================================
-- iSurvey — quản lý giấy phép/thiết bị trên Supabase
-- Chạy toàn bộ file này trong Supabase Dashboard > SQL Editor (có thể chạy lại nhiều lần).
--
-- Nguyên tắc bảo mật:
--   * Client (DLL) chỉ giữ publishable/anon key. KHÔNG bao giờ đưa service_role / secret key vào DLL.
--   * Client KHÔNG có quyền INSERT/UPDATE/DELETE trực tiếp lên bảng. Mọi thao tác ghi đi qua các hàm
--     SECURITY DEFINER bên dưới; email và user lấy từ JWT (client không tự khai được), giờ lấy từ server.
--   * Admin quản lý (khóa/mở thiết bị, xóa, sửa giới hạn) bằng Table Editor / SQL Editor (service role bỏ qua RLS).
-- =====================================================================

create table if not exists public.isurvey_licenses (
  id            uuid primary key default gen_random_uuid(),
  user_id       uuid not null references auth.users(id) on delete cascade,
  email         text not null,
  hwid          text not null,
  machine_name  text,
  app_version   text,
  is_active     boolean not null default true,      -- Admin đặt false để khóa/thu hồi thiết bị
  force_logout  boolean not null default false,     -- Admin bật để cắt phiên hiện tại; đăng nhập lại thì xóa
  activated_at  timestamptz not null default now(),
  last_login_at timestamptz not null default now(),
  note          text,
  unique (email, hwid)
);

create index if not exists isurvey_licenses_user_id_idx on public.isurvey_licenses (user_id);
create index if not exists isurvey_licenses_email_idx   on public.isurvey_licenses (email);

-- Bảng đã tạo trước khi có đăng xuất từ xa: thêm cột, không đụng dữ liệu cũ.
alter table public.isurvey_licenses
  add column if not exists force_logout boolean not null default false;

alter table public.isurvey_licenses enable row level security;

-- Client chỉ ĐỌC được thiết bị của chính mình. Không có policy insert/update/delete cho client.
drop policy if exists "Users can view own device" on public.isurvey_licenses;
create policy "Users can view own device"
  on public.isurvey_licenses for select
  to authenticated
  using (auth.uid() = user_id);

revoke all on table public.isurvey_licenses from anon, authenticated;
grant select on table public.isurvey_licenses to authenticated;

-- ---------------------------------------------------------------------
-- isurvey_activate: đăng ký/cập nhật thiết bị sau khi xác thực OTP.
-- Trả jsonb: { status: ok | blocked | device_limit | invalid_email | invalid_hwid | unauthenticated, ... }
-- Giới hạn tối đa 2 thiết bị / Gmail (đổi hằng c_max_devices nếu cần).
-- ---------------------------------------------------------------------
create or replace function public.isurvey_activate(
  p_hwid         text,
  p_machine_name text default null,
  p_app_version  text default null
) returns jsonb
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  c_max_devices constant int := 2;
  v_uid   uuid := auth.uid();
  v_email text := lower(coalesce(auth.jwt() ->> 'email', ''));
  v_row   public.isurvey_licenses%rowtype;
  v_count int;
begin
  if v_uid is null then
    return jsonb_build_object('status', 'unauthenticated');
  end if;

  if v_email !~ '^[^@[:space:]]+@(gmail|googlemail)\.com$' then
    return jsonb_build_object('status', 'invalid_email');
  end if;

  if p_hwid is null or p_hwid !~ '^PC-SURVEY-[0-9A-F]{8}$' then
    return jsonb_build_object('status', 'invalid_hwid');
  end if;

  -- Tuần tự hóa theo email để hai máy cùng đăng ký một lúc không vượt giới hạn.
  perform pg_advisory_xact_lock(hashtext(v_email));

  select * into v_row
    from public.isurvey_licenses
   where email = v_email and hwid = p_hwid
   for update;

  if found then
    if not v_row.is_active then
      return jsonb_build_object('status', 'blocked', 'is_active', false);
    end if;

    update public.isurvey_licenses
       set user_id       = v_uid,
           machine_name  = coalesce(left(p_machine_name, 120), machine_name),
           app_version   = coalesce(left(p_app_version, 40), app_version),
           last_login_at = now(),
           force_logout  = false
     where id = v_row.id;

    select count(*) into v_count from public.isurvey_licenses where email = v_email;
    return jsonb_build_object('status', 'ok', 'is_active', true,
                              'device_count', v_count, 'max_devices', c_max_devices);
  end if;

  -- Thiết bị mới: tính cả thiết bị đang bị khóa để không né giới hạn bằng cách khóa/mở.
  select count(*) into v_count from public.isurvey_licenses where email = v_email;
  if v_count >= c_max_devices then
    return jsonb_build_object('status', 'device_limit',
                              'device_count', v_count, 'max_devices', c_max_devices);
  end if;

  insert into public.isurvey_licenses (user_id, email, hwid, machine_name, app_version)
  values (v_uid, v_email, p_hwid, left(p_machine_name, 120), left(p_app_version, 40));

  return jsonb_build_object('status', 'ok', 'is_active', true,
                            'device_count', v_count + 1, 'max_devices', c_max_devices);
end;
$$;

-- ---------------------------------------------------------------------
-- isurvey_check: kiểm tra định kỳ và khi admin đăng xuất từ xa.
-- Trả: ok | blocked | logout | not_registered | unauthenticated. KHÔNG tự đăng ký thiết bị.
-- p_touch: bản 24 giờ cập nhật last_login_at. Lần hỏi nhanh (khoảng 2 phút) truyền false
-- để không biến "lần thấy" thành nhịp hỏi.
-- Chữ ký cũ (1 tham số) phải xóa trước: thêm tham số mặc định là overload mới, không phải replace.
-- ---------------------------------------------------------------------
drop function if exists public.isurvey_check(text);

create or replace function public.isurvey_check(p_hwid text, p_touch boolean default true)
returns jsonb
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  v_uid   uuid := auth.uid();
  v_email text := lower(coalesce(auth.jwt() ->> 'email', ''));
  v_row   public.isurvey_licenses%rowtype;
begin
  if v_uid is null then
    return jsonb_build_object('status', 'unauthenticated');
  end if;

  select * into v_row
    from public.isurvey_licenses
   where email = v_email and hwid = p_hwid;

  if not found then
    return jsonb_build_object('status', 'not_registered');
  end if;

  if not v_row.is_active then
    return jsonb_build_object('status', 'blocked', 'is_active', false);
  end if;

  if v_row.force_logout then
    return jsonb_build_object('status', 'logout', 'is_active', true);
  end if;

  if p_touch then
    update public.isurvey_licenses set last_login_at = now() where id = v_row.id;
  end if;

  return jsonb_build_object('status', 'ok', 'is_active', true);
end;
$$;

-- ---------------------------------------------------------------------
-- isurvey_release_device: nút "Đăng Xuất / Đổi Máy" giải phóng 1 suất thiết bị.
-- Chỉ xóa khi thiết bị đang active — thiết bị bị Admin khóa KHÔNG xóa được (không né được lệnh khóa).
-- ---------------------------------------------------------------------
create or replace function public.isurvey_release_device(p_hwid text)
returns jsonb
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  v_uid   uuid := auth.uid();
  v_email text := lower(coalesce(auth.jwt() ->> 'email', ''));
  v_n     int;
begin
  if v_uid is null then
    return jsonb_build_object('status', 'unauthenticated');
  end if;

  delete from public.isurvey_licenses
   where email = v_email and hwid = p_hwid and is_active;
  get diagnostics v_n = row_count;

  if v_n > 0 then
    return jsonb_build_object('status', 'released');
  end if;
  return jsonb_build_object('status', 'not_found_or_blocked');
end;
$$;

-- Chỉ người đã đăng nhập (authenticated) gọi được các hàm.
revoke all on function public.isurvey_activate(text, text, text)  from public, anon;
revoke all on function public.isurvey_check(text, boolean)        from public, anon;
revoke all on function public.isurvey_release_device(text)        from public, anon;
grant execute on function public.isurvey_activate(text, text, text) to authenticated;
grant execute on function public.isurvey_check(text, boolean)       to authenticated;
grant execute on function public.isurvey_release_device(text)       to authenticated;

-- Nhật ký hoạt động (login / lệnh / update): chạy thêm schema_activity.sql
-- (bảng isurvey_activity + RPC isurvey_log_event).
-- View tổng hợp Admin: schema_admin.sql. UI: tab iSurvey trong ksnpsc.
