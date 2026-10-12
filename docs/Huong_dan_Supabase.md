# Hướng dẫn thiết lập Supabase cho iSurvey (một lần, dành cho Admin)

Mục tiêu: người dùng cài iSurvey xong là bấm **Gửi mã** → nhận OTP 6 số qua Gmail → kích hoạt, không phải cấu hình gì.
Mật khẩu SMTP và mọi bí mật nằm trên Supabase, **không** nằm trong DLL.

## 1. Tạo project
1. Vào <https://supabase.com/dashboard> → **New project**.
2. Vào **Project Settings → API**, ghi lại:
   - **Project URL** (dạng `https://<project-ref>.supabase.co`)
   - **Publishable key** (`sb_publishable_...`) hoặc **anon key** kiểu cũ — cả hai đều dùng được.
3. **Tuyệt đối không** đưa `service_role` / secret key vào DLL, `auth.config.json`, repo hay chat.

## 2. Tạo bảng và hàm
**SQL Editor → New query** → dán toàn bộ `deploy/supabase/schema.sql` → **Run**.
Script tạo bảng `isurvey_licenses`, bật RLS (client chỉ đọc được dòng của mình) và 3 hàm RPC:
`isurvey_activate`, `isurvey_check`, `isurvey_release_device`. Chạy lại nhiều lần vẫn an toàn.
Giới hạn thiết bị: hằng `c_max_devices := 2` trong `isurvey_activate`.

## 3. Bật custom SMTP (bắt buộc)
SMTP mặc định của Supabase **chỉ gửi cho thành viên team** và giới hạn **2 mail/giờ** (không dùng cho production).
**Authentication → Emails → SMTP Settings → Enable custom SMTP**, điền host/port/user/pass/From của một dịch vụ gửi mail
(Resend, Brevo, SendGrid, AWS SES... hoặc Gmail với *App Password*). Sau khi bật, giới hạn mặc định là 30 mail/giờ;
chỉnh ở **Authentication → Rate Limits**.

## 4. Sửa mẫu email để gửi mã (bắt buộc)
Mặc định Supabase gửi *magic link*. **Authentication → Emails → Templates → Magic Link**, đặt nội dung:

```html
<h2>Mã kích hoạt iSurvey</h2>
<p>Mã xác thực của bạn là: <b>{{ .Token }}</b></p>
<p>Nhập mã này vào cửa sổ kích hoạt iSurvey. Không chia sẻ mã cho người khác.</p>
```

Mẫu **Confirm signup** cũng bắt buộc đặt cùng nội dung (chứa `{{ .Token }}`). Lần kích hoạt đầu của mỗi Gmail đi qua mẫu này; nếu còn link mặc định thì người dùng nhận link thay vì mã 6 số.

## 5. Thời hạn mã
**Authentication → Sign In / Providers → Email → Email OTP expiration**: đặt **300–600** giây (mặc định 3600).
Lưu ý: giá trị này áp dụng cho cả các link email khác.

## 6. Điền khóa vào iSurvey
Cách 1 (mọi máy dùng chung, khuyến nghị): sửa hằng trong `src/Auth/AuthConfig.cs` rồi build lại:
```csharp
public const string DefaultSupabaseUrl = "https://<project-ref>.supabase.co";
public const string DefaultSupabaseKey = "sb_publishable_...";
```
Cách 2 (thử nhanh trên một máy): thêm vào `%AppData%\iSurvey\auth.config.json`
```json
{ "SupabaseUrl": "https://<project-ref>.supabase.co", "SupabaseKey": "sb_publishable_..." }
```
File này ghi đè hằng mặc định. Để trống cả hai thì iSurvey chạy chế độ cũ (SMTP/webhook/mã admin).

## 7. Quản trị hằng ngày
| Việc | Cách làm |
|---|---|
| Xem ai đang dùng, máy nào | Table Editor → `isurvey_licenses` |
| **Khóa một thiết bị** | đặt `is_active = false` → máy đó bị khóa trong vòng 24 giờ sau khi mở Civil (hoặc ngay khi đăng nhập lại) |
| Mở lại thiết bị | đặt `is_active = true` |
| Giải phóng suất thiết bị cho người dùng đổi máy | xóa dòng (người dùng cũng tự làm được bằng nút **Đăng Xuất / Đổi Máy** nếu máy cũ còn mở được) |
| **Khóa cả tài khoản** | Authentication → Users → chọn user → **Ban user**; đồng thời đặt `is_active = false` cho các dòng của user đó |
| Tăng/giảm số thiết bị | sửa `c_max_devices` trong `isurvey_activate` rồi chạy lại hàm |

Một thiết bị đang bị khóa **không** tự xóa được bằng nút Đổi Máy (hàm chỉ xóa dòng đang `is_active`), nên không né được lệnh khóa.

## 8. Cách iSurvey hoạt động với Supabase
- **Kích hoạt:** `/auth/v1/otp` → người dùng nhập 6 số → `/auth/v1/verify` → `rpc/isurvey_activate` → lưu `license.dat` (DPAPI + MachineGuid).
- **Kiểm tra định kỳ:** khi mở Civil, nếu đã quá 24 giờ kể từ lần xác minh cuối, iSurvey chạy **ngầm** (timeout 5 giây, không chặn Civil): đổi `refresh_token` → `rpc/isurvey_check`.
  - Server trả `blocked` hoặc `not_registered` → khóa lại Ribbon.
  - Mạng lỗi/timeout/lỗi khác → giữ nguyên kích hoạt.
- **Dùng offline:** tối đa **7 ngày** kể từ lần xác minh trực tuyến cuối; quá hạn phải đăng nhập lại.
- **Kích hoạt bằng mã Admin (không internet):** vẫn dùng `AdminSecret` + `ISURVEY_GENOTP`; giấy phép loại này cũng chỉ sống **7 ngày**.

## 9. Kiểm thử nhanh bằng curl (tuỳ chọn)
```bash
curl -X POST "$URL/auth/v1/otp" -H "apikey: $KEY" -H "Content-Type: application/json" \
     -d '{"email":"ban@gmail.com","create_user":true}'
```
Nhận mail, rồi:
```bash
curl -X POST "$URL/auth/v1/verify" -H "apikey: $KEY" -H "Content-Type: application/json" \
     -d '{"type":"email","email":"ban@gmail.com","token":"123456"}'
```
Kết quả có `access_token`. Gọi tiếp:
```bash
curl -X POST "$URL/rest/v1/rpc/isurvey_activate" -H "apikey: $KEY" -H "Authorization: Bearer <access_token>" \
     -H "Content-Type: application/json" -d '{"p_hwid":"PC-SURVEY-0123ABCD","p_machine_name":"TEST","p_app_version":"1.3.0"}'
```
Mong đợi `{"status":"ok", ...}`.

## 10. Lưu ý
- Giới hạn của Supabase Auth: gửi OTP 60 giây/người; verify 30 lần/5 phút/IP (văn phòng chung IP có thể gặp HTTP 429 — iSurvey không tính lượt thử khi gặp 429).
- Giấy phép cục bộ vẫn nằm trên máy người dùng: Supabase cho phép **thu hồi, kiểm kê thiết bị, nhật ký**, không phải chống vá DLL.
- Hệ thống lưu email và tên máy của người dùng; hãy thông báo điều này khi phát hành.
