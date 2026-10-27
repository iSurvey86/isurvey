-- =====================================================================
-- iSurvey — view tổng hợp cho Admin (Table Editor / ksnpsc qua service role)
-- Chạy sau schema.sql + schema_activity.sql. Có thể chạy lại.
-- =====================================================================

-- Máy / giấy phép đang active (toàn bộ đăng ký còn hiệu lực).
-- Xóa rồi tạo lại: CREATE OR REPLACE không cho chèn cột force_logout vào giữa view cũ.
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

-- Top lệnh (30 ngày gần nhất)
create or replace view public.isurvey_v_command_stats_30d as
select
  command_name,
  count(*)::bigint as times,
  count(distinct email)::bigint as users,
  count(distinct hwid)::bigint as machines,
  max(created_at) as last_at
from public.isurvey_activity
where event_type = 'command'
  and created_at >= now() - interval '30 days'
  and command_name is not null
group by command_name
order by times desc;

-- Lỗi gần đây (view lọc; Admin vẫn query bảng gốc)
create or replace view public.isurvey_v_recent_errors as
select
  id, email, hwid, machine_name, app_version, command_name, detail, created_at
from public.isurvey_activity
where event_type = 'error'
order by created_at desc;

comment on view public.isurvey_v_devices is 'iSurvey admin: thiết bị + cờ seen 24h/7d';
comment on view public.isurvey_v_command_stats_30d is 'iSurvey admin: thống kê lệnh 30 ngày';
comment on view public.isurvey_v_recent_errors is 'iSurvey admin: sự kiện error';

-- Views chỉ để service role / dashboard đọc (không grant cho anon/authenticated)
revoke all on public.isurvey_v_devices from public, anon, authenticated;
revoke all on public.isurvey_v_command_stats_30d from public, anon, authenticated;
revoke all on public.isurvey_v_recent_errors from public, anon, authenticated;
