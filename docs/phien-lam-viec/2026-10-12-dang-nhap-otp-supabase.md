# 2026-10-12 — Đăng nhập OTP Supabase

## Ngữ cảnh

Máy văn phòng, Civil 3D 2026 host **.NET 10**. User **XÁC NHẬN** kiến trúc Supabase (RPC `SECURITY DEFINER`, tối đa 2 thiết bị, fallback ngoại tuyến 7 ngày, kiểm tra lại ngầm không chặn `Initialize`). `DefaultSupabaseUrl` / `DefaultSupabaseKey` để trống lúc code, user điền sau khi tạo project.

## Trạng thái đã chốt

- Panel 6 **Tài khoản & Hệ thống**. Panel 1–5 `IsEnabled` theo giấy phép. Alias **LGI / UPD / ABT / LGO**. Nhãn Panel 2 giữ **Cắt Bình Đồ**.
- OTP: Gmail, 6 số, tự xác thực khi đủ số, cooldown gửi lại 60 giây, sai 5 lần khóa 5 phút. HTTP 429 và lỗi mạng không trừ lượt thử.
- Supabase REST: `/auth/v1/otp`, `/auth/v1/verify`, refresh token, RPC `isurvey_activate`, `isurvey_check`, `isurvey_release_device`.
- `license.dat` v2: DPAPI + entropy MachineGuid + HMAC. Hết hạn = lần xác minh cuối + 7 ngày. Kiểm tra lại sau 24 giờ, timeout 5 giây. Chỉ khóa Ribbon khi server trả `blocked` hoặc `not_registered`.
- Mã Admin (`ISURVEY_GENOTP` + `AdminSecret`) và mã Debug `839205` (chỉ bản Debug) tạo giấy phép ngoại tuyến 7 ngày.
- User đã nhận mail OTP từ `iSurveyEVN@gmail.com` và Ribbon hiện **Đã kích hoạt**. SMTP App Password nằm trên Supabase, không có trong repo. Publishable key nằm trong `AuthConfig.cs`.

## Việc mở

- Test thu hồi, hết 2 thiết bị, đăng xuất trả suất, và kiểm tra lại sau 24 giờ trên Civil.
- Bổ sung lệnh Auth vào catalog Excel.
- Việc mở từ 1.2.0: test `CBD`, `SCN`, EG Lào Cai TM-3 trên DWG thật.
