-- =====================================================================
-- iSurvey — nhật ký hoạt động (chạy thêm sau schema.sql)
-- Supabase SQL Editor → Run toàn bộ file này.
-- =====================================================================

create table if not exists public.isurvey_activity (
  id            uuid primary key default gen_random_uuid(),
  user_id       uuid references auth.users(id) on delete set null,
  email         text,
  hwid          text,
  machine_name  text,
  app_version   text,
  event_type    text not null,          -- login | logout | command | update_check | error
  command_name  text,
  detail        jsonb,
  created_at    timestamptz not null default now()
);

create index if not exists isurvey_activity_created_at_idx on public.isurvey_activity (created_at desc);
create index if not exists isurvey_activity_email_idx on public.isurvey_activity (email);
create index if not exists isurvey_activity_event_idx on public.isurvey_activity (event_type);

alter table public.isurvey_activity enable row level security;

-- Client không đọc trực tiếp (Admin xem bằng Table Editor / service role).
drop policy if exists "Users insert own activity via rpc only" on public.isurvey_activity;

revoke all on table public.isurvey_activity from anon, authenticated;

-- ---------------------------------------------------------------------
-- isurvey_log_event: ghi 1 dòng nhật ký. Email/user lấy từ JWT.
-- ---------------------------------------------------------------------
create or replace function public.isurvey_log_event(
  p_event_type   text,
  p_command      text default null,
  p_detail       jsonb default null,
  p_app_version  text default null,
  p_hwid         text default null,
  p_machine_name text default null
) returns jsonb
language plpgsql
security definer
set search_path = public, pg_temp
as $$
declare
  v_uid   uuid := auth.uid();
  v_email text := lower(coalesce(auth.jwt() ->> 'email', ''));
begin
  if v_uid is null then
    return jsonb_build_object('status', 'unauthenticated');
  end if;

  if p_event_type is null or length(trim(p_event_type)) = 0 then
    return jsonb_build_object('status', 'invalid_event');
  end if;

  insert into public.isurvey_activity (
    user_id, email, hwid, machine_name, app_version, event_type, command_name, detail
  ) values (
    v_uid,
    nullif(v_email, ''),
    left(p_hwid, 64),
    left(p_machine_name, 120),
    left(p_app_version, 40),
    left(trim(p_event_type), 40),
    left(p_command, 80),
    -- chặn detail quá lớn (client có thể bị lạm dụng ghi rác)
    case when p_detail is not null and length(p_detail::text) > 4000
         then jsonb_build_object('truncated', true)
         else p_detail end
  );

  -- VER + last seen: lần cuối plugin còn chạy trên máy đó (không tính lệnh Đăng xuất trên web).
  if left(trim(p_event_type), 40) <> 'remote_logout' then
    update public.isurvey_licenses
       set last_seen_at = now(),
           app_version  = coalesce(nullif(left(p_app_version, 40), ''), app_version),
           machine_name = coalesce(nullif(left(p_machine_name, 120), ''), machine_name)
     where email = v_email
       and hwid  = left(p_hwid, 64);
  end if;

  return jsonb_build_object('status', 'ok');
end;
$$;

revoke all on function public.isurvey_log_event(text, text, jsonb, text, text, text) from public, anon;
grant execute on function public.isurvey_log_event(text, text, jsonb, text, text, text) to authenticated;
