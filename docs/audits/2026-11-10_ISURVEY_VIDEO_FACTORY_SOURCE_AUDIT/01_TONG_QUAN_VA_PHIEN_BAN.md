# 01 — Tổng quan và phiên bản

**Ngày giờ đọc:** 2026-11-10 08:35:26 +07:00  
**Root đã xác nhận:** `D:\AIProject\isurvey` (có `.git`, là repository root)  
**Nhánh:** `main`  
**HEAD:** `53c5eb4644f2dbc0f736e03098c749197a81a93e`  
**Thông điệp commit HEAD:** `Giữ 1.8.17: VEMCD Zero-Clutter — chữ thước 12, CTA 2×2, lưới không Layer.`  
**Submodule:** không phát hiện (`git submodule status` trống / không dùng)

## Mục tiêu đợt kiểm toán

Kiểm toán **tĩnh** toàn bộ mã nguồn/tài liệu kỹ thuật trong phạm vi đọc được tại root trên, phục vụ chuẩn bị kiến trúc iSurvey Video Factory (KT-01–KT-07, KT-09). **Không** chạy AutoCAD/Civil, **không** build, **không** sửa mã nghiệp vụ, **không** triển khai Video Factory.

## Giới hạn bắt buộc đã áp dụng

| Được phép | Không được |
|---|---|
| Đọc mã/tài liệu; `git status/log/rev-parse`; tạo báo cáo mới trong thư mục đợt này | Sửa C#/XAML/config; build/test/CAD; commit/push; lộ bí mật; tuyên bố runtime PASS |

Nhãn bằng chứng dùng trong bộ hồ sơ: `CÓ BẰNG CHỨNG TỪ MÃ NGUỒN`, `SUY LUẬN CẦN XÁC MINH`, `CHƯA TÌM THẤY`, `CHƯA KIỂM CHỨNG THỰC TẾ`, `BỊ CHẶN DO THIẾU DỮ LIỆU`.

## Working tree lúc bắt đầu (không chỉnh)

Untracked (không thuộc đợt kiểm toán, **không** xóa/đụng):

- `docs/MCD/`
- `docs/SEED 1000/`
- `docs/dau/`
- `src/Templates/KhungBanDo_Chuan.bak`
- `tmp/`

Không có tệp tracked đang modified tại thời điểm snapshot trước kiểm toán.

## Phiên bản sản phẩm suy ra từ source

| Nguồn | Giá trị | Mức |
|---|---|---|
| `iSurvey.csproj` `<Version>` | `1.8.17` | `CÓ BẰNG CHỨNG TỪ MÃ NGUỒN` |
| `deploy/iSurvey.bundle/PackageContents.xml` `AppVersion` | `1.8.17` | `CÓ BẰNG CHỨNG TỪ MÃ NGUỒN` |
| `ComponentEntry Version` (cùng PackageContents) | `1.8.17` | `CÓ BẰNG CHỨNG TỪ MÃ NGUỒN` |
| Bundle đã cài `%AppData%\...\iSurvey.bundle` | Tồn tại `PackageContents.xml` | Chỉ ghi nhận tồn tại — **không** chứng minh đang được AutoCAD nạp hay khớp bit-for-bit với HEAD (`CHƯA KIỂM CHỨNG THỰC TẾ`) |

## Dự án / framework

| Mục | Bằng chứng |
|---|---|
| Solution | `iSurvey.sln` |
| Add-in chính | `iSurvey.csproj` — TFMs `net48;net8.0-windows;net10.0-windows` |
| Smoke tool riêng | `deploy/tools/McpSmokeTest/McpSmokeTest.csproj` (bị `Compile Remove` khỏi add-in) |
| AutoCAD.NET | 24.1 (net48), 25.0.1 (net8), 26.0.0 (net10); `ExcludeAssets=runtime` |
| Khác | ProjNet 2.0.0; MaxRev.Gdal.*; WPF + WinForms |

## Mức độ đầy đủ đợt này

- Quét toàn bộ `src/**/*.cs` cho `[CommandMethod(` → **125** khai báo / **125** chuỗi lệnh duy nhất.
- Ribbon: `src/UI/RibbonBuilder.cs` → **61** nút `Btn` (36 enabled, 25 disabled/placeholder).
- Hồ sơ chi tiết: theo nhóm module trong `04*` + chỉ mục khớp CSV `03`.
- **Không** tuyên bố 100% bao phủ semantic mọi dòng nghiệp vụ bên trong từng service (một số file lớn chỉ đọc cấu trúc/điểm vào + mẫu nhánh chính). Phần chưa đọc sâu được liệt kê ở `10` và `11`.

## Kết luận phiên bản cho Video Factory

Số phiên bản source/bundle template hiện tại là **1.8.17**. Việc máy người dùng đang chạy đúng bản đó: `CHƯA KIỂM CHỨNG THỰC TẾ`.
