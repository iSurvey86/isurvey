# Admin iSurvey trong ksnpsc (tab Quản lý hệ thống)

## Đã có gì
- Tab **iSurvey** (chỉ Admin hệ thống: `Q_ADMIN` hoặc `q_system_log`)
- Tổng quan: máy active, seen 24h / N ngày, email, lỗi, top lệnh
- Thiết bị: khóa / mở (`is_active` trên `isurvey_licenses`)
- Nhật ký: lọc `login` / `command` / `heartbeat` / `error` / …

API server (service role iSurvey, không lộ ra browser):
- `GET /api/isurvey/summary?admin_email=`
- `GET /api/isurvey/activity?admin_email=`
- `GET|PATCH /api/isurvey/licenses`

## Cấu hình `.env.local` (ksnpsc)

```env
ISURVEY_SUPABASE_URL=https://lmraewcashlnfdawrvmg.supabase.co
ISURVEY_SERVICE_ROLE_KEY=<service_role từ Project Settings → API của project iSurvey>
```

Lấy **service_role** (secret) trên dashboard Supabase iSurvey — **không** commit, **không** đưa vào DLL.

Cũng cần `NEXT_PUBLIC_SUPABASE_URL` + key của **ksnpsc** để API xác minh email Admin qua `NHAN_SU` / `PHAN_QUYEN`.

## SQL trên project iSurvey (một lần)

1. `schema.sql` (nếu chưa)
2. `schema_activity.sql`
3. `schema_admin.sql` (views tổng hợp — tuỳ chọn cho Table Editor)

## Dùng

1. Thêm env → restart `npm run dev` (hoặc deploy Vercel + env production).
2. Đăng nhập ksnpsc bằng tài khoản Admin.
3. **Quản lý hệ thống → iSurvey** (hoặc `?tab=ISURVEY`).

## Lưu ý
- User thường không thấy tab (cùng gate với QL hệ thống).
- Heartbeat / error đầy đủ hơn sau khi user chạy bản DLL có `ActivityLogger.TrackHeartbeat` / `TrackError`.
- “Máy đang dùng” ≈ `last_login_at` trong 24h (cập nhật khi `isurvey_check` / login), không phải socket realtime.
