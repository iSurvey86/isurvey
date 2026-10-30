-- =====================================================================
-- VER + last seen: bản DLL mới nhất trên máy, lần cuối plugin còn chạy trên CAD.
-- Chạy trên Supabase SQL Editor (có thể chạy lại). Chạy sau schema.sql / schema_activity.sql.
-- =====================================================================

alter table public.isurvey_licenses
  add column if not exists last_seen_at timestamptz;

update public.isurvey_licenses
   set last_seen_at = last_login_at
 where last_seen_at is null;

alter table public.isurvey_licenses
  alter column last_seen_at set default now();

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
    case when p_detail is not null and length(p_detail::text) > 4000
         then jsonb_build_object('truncated', true)
         else p_detail end
  );

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
  last_seen_at,
  note,
  (now() - coalesce(last_seen_at, last_login_at)) < interval '24 hours' as seen_24h,
  (now() - coalesce(last_seen_at, last_login_at)) < interval '7 days'   as seen_7d
from public.isurvey_licenses;

revoke all on public.isurvey_v_devices from public, anon, authenticated;

notify pgrst, 'reload schema';
