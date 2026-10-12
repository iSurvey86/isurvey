# Changelog — iSurvey

## 1.3.0 — 2026-10-12

### Sản phẩm
- Đăng nhập Gmail bằng mã OTP 6 số (Supabase). Nhập đủ 6 số là tự xác thực. Panel 1–5 khóa cho tới khi kích hoạt.
- Tối đa 2 thiết bị mỗi Gmail. Đăng xuất / Đổi máy trả suất thiết bị. Admin khóa bằng `is_active = false` trên bảng `isurvey_licenses`.
- Giấy phép cục bộ mã hóa theo máy. Mất mạng vẫn dùng 7 ngày kể từ lần xác minh cuối. Kiểm tra lại chạy ngầm, không chặn lúc mở Civil.
- Lệnh: `ISURVEY_LOGIN` / **LGI**, `ISURVEY_UPDATE` / **UPD**, `ISURVEY_ABOUT` / **ABT**, `ISURVEY_LOGOUT` / **LGO**, `ISURVEY_GENOTP` (máy Admin).

### Docs / quy trình
- `docs/Huong_dan_Supabase.md`, `deploy/supabase/schema.sql`. HANDOFF + changelog phiên 1.3.0.

## 1.2.0 — 2026-10-12

### Sản phẩm
- Ribbon tab iSurvey: 5 panel, icon vector (nền gradient, nét trắng). Nút chưa có lệnh báo "dang duoc cap nhat".
- `CATBD` / **CBD**: cắt bình đồ theo biên khép kín. Layer khung được bảo vệ; CIRCLE ngoài whitelist bị xóa; điểm/chữ/block xét trong-ngoài; LINE và polyline cắt từng đoạn, nội suy Z, tách cung. Không dùng TRIM. Một transaction, `UNDO` một lần.
- `AUTOSCAN` / **SCN**: quét bản vẽ, xuất `CAD_AutoScan_Report.txt` (thống kê nhóm, mẫu chữ, mẫu đại diện) rồi mở Notepad.
- Build tự chọn `net8.0-windows` hoặc `net10.0-windows` theo `acdbmgd.runtimeconfig.json` của AutoCAD.

### Docs / quy trình
- HANDOFF + changelog phiên 1.2.0. README cập nhật lệnh và target framework.

## 1.1.1 — 2026-09-03

### Sản phẩm
- **Fix autoload Civil 3D 2026:** retarget `net8.0-windows` (thay net10 — host AutoCAD R25.1 dùng .NET 8).
- **Fix Export KML TM-3:** validation kinh tuyến theo múi (`IsValidCentralMeridian`); TM-3 chấp nhận 102°–117° (vd Lào Cai 104.75°).
- **TM-6 chuẩn TT 973:** kinh tuyến 105 / 111 / 117 (bỏ 99°).
- **63 tỉnh/thành** trong `isurvey_province_crs_map.json`; tương thích settings nhóm tỉnh cũ.

### Docs / quy trình
- HANDOFF + archive phiên net8 / vá KML.

## 1.1.0 — 2026-09-03

### Sản phẩm
- Xuất CAD → Google Earth: `ISURVEY_EXPORT_KML` / **EG** (KML/KMZ; mặc định KMZ; màu CAD 1:1; không explode block).
- Hỗ trợ **múi 3° (TM-3)** và **múi 6° (TM-6)** trên Map insert và Export; TM-6 snap kinh tuyến 105/111/117 theo Thông tư 973/2001/TT-TCĐC.
- Alias: **IG** (MAP), **SG** (MAP_SAT), **XG** (DELETE_GE), **EG** (EXPORT_KML).
- Target **net8.0-windows** (AutoCAD / Civil 3D 2026 R25.1); bundle deploy cập nhật path net8.

### Docs / quy trình
- Catalog lệnh `docs/Danh_sach_lenh_iSurvey.xlsx`.
- HANDOFF + changelog phiên 1.1.0.

## 1.0.0 — 2026-09-02

### Sản phẩm
- Chèn tile Google Earth (satellite / hybrid) georeference VN-2000 TM-3 (AutoCAD / Civil 3D 2026).
- Per-tile 256×256, cache, AutoRefresh, FitZoom ≤128; clip theo polyline; `ISURVEY_DELETE_GE` xóa toàn bộ tile.
- Bundle autoload + TRUSTEDPATHS (`deploy/build-bundle.ps1`).

### Docs / quy trình
- Khởi tạo `docs/phien-lam-viec` (HANDOFF), `docs/Mẫu_Tư_Vấn.md`, `docs/Mẫu_Vá_Lỗi.md`, changelog này.
