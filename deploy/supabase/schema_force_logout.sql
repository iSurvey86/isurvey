-- =====================================================================
-- Đăng xuất từ xa (chạy một lần trên Supabase SQL Editor, sau schema.sql).
-- Khóa/mở (is_active) giữ nguyên. Cột này chỉ cắt phiên đang đăng nhập.
-- Có thể chạy lại.
-- =====================================================================

alter table public.isurvey_licenses
  add column if not exists force_logout boolean not null default false;

-- Đăng nhập lại trên đúng máy thì hết lệnh đăng xuất.
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

revoke all on function public.isurvey_activate(text, text, text) from public, anon;
revoke all on function public.isurvey_check(text, boolean)       from public, anon;
grant execute on function public.isurvey_activate(text, text, text) to authenticated;
grant execute on function public.isurvey_check(text, boolean)       to authenticated;

-- CREATE OR REPLACE không được chèn cột vào giữa view đã có
-- (Postgres tưởng đổi tên activated_at thành force_logout).
drop view if exists public.isurvey_v_devices;

create view public.isurvey_v_devices as
select
  id,
  email,
  hwid,
  machine_name,
  app_version,
  is_active,
  force_logout,
  activated_at,
  last_login_at,
  note,
  (now() - last_login_at) < interval '24 hours' as seen_24h,
  (now() - last_login_at) < interval '7 days'   as seen_7d
from public.isurvey_licenses;

revoke all on public.isurvey_v_devices from public, anon, authenticated;

notify pgrst, 'reload schema';
