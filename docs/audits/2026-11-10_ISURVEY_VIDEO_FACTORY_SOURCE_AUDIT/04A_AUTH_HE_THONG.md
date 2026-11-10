# 04A — Auth & hệ thống

**Nhóm CSV:** `He_thong_Auth`  
**Mức:** `SOURCE_REVIEWED` (điểm vào + cổng auth); runtime login/OTP: `CHƯA KIỂM CHỨNG THỰC TẾ`

## Điểm vào chung

| Lệnh | Alias | Source | Ghi chú |
|---|---|---|---|
| ISURVEY_LOGIN | LGI | `AuthModule.cs:24` / `CommandAliases.cs:170` | Dialog đăng nhập |
| ISURVEY_LOGOUT | LGO | `AuthModule.cs:149` / `:182` | Xóa phiên cục bộ |
| ISURVEY_ABOUT | ABT | `AuthModule.cs:67` / `:176` | Thông tin giấy phép |
| ISURVEY_UPDATE | UPD | `AuthModule.cs:74` / `:173` | Cập nhật add-in |
| ISURVEY_HDSD | HD | `AuthModule.cs:35` / `:179` | Mở HDSD |
| ISURVEY_RIBBON | MENUISURVEY | `AuthModule.cs:52` / `:185` | Rebuild Ribbon |
| ISURVEY_GENOTP | — | `AuthModule.cs:178` | **ADMIN** — cần `AdminSecret` trong `auth.config.json` (không đọc nội dung tệp) |

## Cổng bảo vệ

- `AuthManager.Initialize` đọc license cục bộ, không dialog — `AuthManager.cs` (~109+), gọi từ `Main.cs:53`.
- `EnsureAuthenticated` được gọi từ hầu hết module (grep count theo file trong inventory đợt này).
- `EnsureAdmin` — `AuthManager.cs:85+`; AI dùng cổng này (`AiAssistantModule`).
- Recheck nền / `force_logout`: `AuthManager.StartBackgroundRecheck`, `LicenseRechecker.cs` — có nhánh Admin cắt phiên (`CHƯA KIỂM CHỨNG THỰC TẾ` hành vi mạng).

## I/O & rủi ro video

| Mục | Bằng chứng source | Rủi ro quay |
|---|---|---|
| File license cục bộ | tồn tại path kiểu `%AppData%\iSurvey\license.dat` (chỉ ghi nhận tồn tại) | Không quay màn OTP/email |
| `auth.config.json` | docs/`YOUTUBE` không; code tham chiếu config — **không** mở nội dung trong đợt này | Bí mật AdminSecret |
| Supabase / HTTP | `SupabaseAuthClient`, `ActivityLogger` | Cần mạng; không đưa token vào video |

## Video

**Không** đưa `ISURVEY_GENOTP` vào catalog video công khai (Mục 04 yêu cầu audit).  
`LGI/ABT/UPD/MENUISURVEY` có thể là Tips/Support, ưu tiên thấp hơn nghiệp vụ bản vẽ.

## Chưa đủ

Luồng OTP từng bước UI, URL endpoint, schema RPC: không trích trong báo cáo; đánh dấu `BỊ CHẶN DO THIẾU DỮ LIỆU` / chủ đích bảo mật.
