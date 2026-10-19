# Changelog — iSurvey

## 1.7.0 — 2026-10-19

### Phát hành / vận hành
- **Bản cài gửi người dùng:** `deploy/build-release.ps1` → zip `iSurvey-Setup-1.7.0.zip` (bundle + `Install-iSurvey.bat` / `Uninstall-iSurvey.bat` + `CAI_DAT.txt`). Người dùng giải nén → chạy `Install-iSurvey.bat` (đóng Civil trước).
- **Update online:** lệnh `ISURVEY_UPDATE` đọc `version.json` công khai trên Supabase Storage bucket `isurvey-releases`, so semver, mở link tải zip Setup nếu có bản mới. Bucket + file đã kiểm tra public OK.
- **Nhật ký hoạt động:** bảng `isurvey_activity` + RPC `isurvey_log_event` (`schema_activity.sql`). Client ghi login/logout/command/update_check; thêm `heartbeat` (login + recheck 24h) và helper `TrackError`. Chỉ giấy phép Online.
- **Admin UI (ksnpsc):** tab **Quản lý hệ thống → iSurvey** (repo ksnpsc, Admin only) — tổng quan máy/lệnh/lỗi, khóa thiết bị, nhật ký. Env `ISURVEY_SUPABASE_URL` + `ISURVEY_SERVICE_ROLE_KEY`. Views SQL: `schema_admin.sql`.

### Sản phẩm
- Panel **UAV & LiDAR**, `UAVCLIP` / `ISURVEY_UAV_CLIP` (clip polyline + Smart Filter / Auto-Clip nền trắng-đen); user xác nhận Auto-Clip đạt.
- `GOC` / `ISURVEY_NUMBER_VERTICES` đánh số góc tuyến (layer `ISURVEY_GOC_TUYEN`, cỡ theo tỷ lệ bản đồ mặc định 1:1000); user xác nhận rất tốt.

### Docs
- `docs/Huong_dan_Phat_hanh.md`, `docs/Huong_dan_Admin_ksnpsc.md`; cập nhật `Huong_dan_Supabase.md`.

## 1.6.0 — 2026-10-15

### Sản phẩm (CAD ➔ DGN, `C2D`)
- `C2D` / `ISURVEY_CAD_TO_DGN` xuất bản vẽ ra MicroStation **V8 hoặc V7** bằng `-DGNEXPORT` của Civil 3D. Hộp thoại `DgnExportWindow` tối màu: phạm vi (toàn bộ / đối tượng đang chọn, chụp PICKFIRST ngay khi gõ lệnh), phiên bản, Purge, ghi đè. Bấm xuất ➔ hộp lưu file ➔ hộp thoại báo kết quả (có nút mở thư mục).
- Trình tự prompt `-DGNEXPORT` (đã xác minh Civil 3D 2026): định dạng `[V7/V8]` ➔ tên file ➔ đơn vị `[Master/Sub]` ➔ mapping setup ➔ seed. Prompt seed chỉ nhận keyword hoặc `~` (hộp thoại modal, không nhận đường dẫn gõ) ➔ `SeedDialogDriver` (luồng nền) tự điền đường dẫn seed và bấm Open (nhận diện theo ComboBox 13006 + IDOK).
- **Seed tự động**, không bắt người dùng chọn: seed trong bundle `Contents\Seeds\VN2000_Seed{2D|3D}_{V7|V8}.dgn` ➔ seed metric của Civil (`V7/V8-Metric-Seed2D/3D.dgn`). Bản vẽ có Z ≠ 0 (hoặc Solid3d / Polyline3d / mesh...) bắt buộc seed 3D ("A 3D seed file is required"); `Detect3D` quét trước. Kẹt ở bước seed quá 90 giây ➔ tự hủy và báo.
- `VN2000_Seed2D_V7.dgn` tạo từ 5 element đầu (TCB...) của `DC2.dgn` rồi thêm marker kết thúc (1 UOR = 1 cm, tọa độ VN-2000 vừa giới hạn V7); `VN2000_Seed3D_V7.dgn` = bản 2D vá các byte TCB theo khác biệt giữa seed 2D/3D của Civil. Chưa có seed V8 chuẩn VN-2000 (xem HANDOFF).
- `DgnExportService`: xuất từ **bản sao tạm** (Wblock đối tượng ➔ DWG tạm ➔ mở ➔ xuất ➔ đóng bỏ) khi chọn "đối tượng đang chọn" hoặc bật Purge ➔ bản vẽ gốc không đổi; xuất toàn bộ + tắt Purge thì xuất thẳng bản vẽ hiện hành. Log `%LocalAppData%\iSurvey\logs\c2d_*.log`.
- `deploy/build-bundle.ps1` copy `src\Seeds\*.dgn` vào `Contents\Seeds`.
- Đã kiểm chứng (Civil 3D 2026): V7/V8, chọn/toàn bộ, 2D/3D, Purge bật/tắt; nhập lại DGN ra đúng X=530254, Y=2378229 và Z; trên DWG thật (Hải Dương, 1182 đối tượng, có Z) xuất 128 KB trong 5,2 giây.

### Sản phẩm (DGN ➔ CAD, `D2C`)
- **Batch** trong hộp thoại D2C: nhiều file `.dgn`, **ghép** vào MỘT bản vẽ mới chưa lưu (các mảnh liền nhau theo tọa độ) hoặc **không ghép** (mỗi file một `.dwg` cùng thư mục, trùng tên: đánh số / bỏ qua / ghi đè). Tuần tự trong bản vẽ trống từ `QNEWTEMPLATE`, đợi kích hoạt (`WaitUntilActive`) trước khi ra lệnh.
- Nhập bằng `DGNIMPORTMODE=2` (thẳng vào bản vẽ hiện hành, tọa độ tuyệt đối đúng). Chế độ 0 tạo bản vẽ mới và lệch +5×10⁸ (X), +10⁹ (Y) nên không dùng.
- `-DGNIMPORT` hoàn tất bất đồng bộ sau khi `Editor.Command` trả về ➔ hậu xử lý chờ `Handseed` ngừng đổi (Idle). `UNDO` 2 lần.
- DGN V8: layer `<file> - Model$0$Level N` ➔ bỏ tiền tố về `Level N` (gộp các mảnh); font V8 giải mã artifact `N/64`, `∅`, `±` rồi qua bảng TCVN3. Giới hạn: chữ hoa `.VnTimeH` ra chữ thường.

### Sản phẩm (chung)
- Panel **Bản đồ** chia hai nhóm ngăn cách bằng `RibbonSeparator`: nhóm Google Earth/KML và nhóm **DGN**. Nhãn ngắn hơn cho màn 14 inch; mỗi nút DGN có tooltip riêng.
- Module **DGN ➔ CAD** (Giai đoạn 1): `D2C` / `ISURVEY_DGN_TO_CAD` nhập một file `.dgn` (V7/V8) vào Model Space bằng engine DGN của Civil 3D (`-DGNIMPORT`, `FILEDIA=0`, `CMDDIA=0`), không cần MicroStation.
  - Lọc đối tượng mới theo `Database.Handseed`; bọc `UNDO _BEGIN/_END` (một lần `UNDO` hoàn tác cả lần nhập); khôi phục biến hệ thống.
  - Hậu xử lý: Explode cell (có giới hạn), đổi tên/gộp layer (giữ Level / tiền tố / bảng JSON địa chính), TCVN3 ➔ Unicode (Arial) nhận diện theo nội dung chuỗi, dọn linetype/layer/block rác, kiểm tra vùng bao VN-2000, zoom.
  - Hộp thoại `DgnImportWindow` tối màu; log chi tiết ra Command Line và `%LocalAppData%\iSurvey\logs\dgn_*.log`.
- `DGNCFG` / `ISURVEY_DGN_CONFIG`: lưu mặc định đơn vị, layer, font; mở bảng ánh xạ `dgn_layer_mapping.json` (bản sửa được trong `%AppData%\iSurvey`).
- Ba nút DGN bị khóa khi chưa đăng nhập.

### Chưa làm / chưa kiểm chứng
- Chuẩn hóa theo quy định (Phụ lục 21 – 63 Level, tên layer, màu RGB Phụ lục 22, font, chữ hoa V8) — hoãn theo yêu cầu, xem HANDOFF.
- Seed V8 chuẩn VN-2000 (đang dùng seed metric của Civil); chưa mở file xuất trong MicroStation; mapping Level của `-DGNEXPORT` mặc định (layer `0` ➔ level 63); font text tiếng Việt khi xuất chưa kiểm.
- Batch xuất nhiều bản vẽ ➔ DGN chưa có. VNI chưa được chuyển mã (chỉ TCVN3).

## 1.5.2 — 2026-10-13

### Sản phẩm
- `ISURVEY_MAP_SAT` hỏi chế độ: **1** xem lướt (nhiều tile, AutoRefresh) hoặc **2** ghép một ảnh HD.
- Ảnh HD: chọn đường bao, zoom 18/19/20, tối đa 400 ô. Tải Google Satellite `scale=2` (512×512), ghép một JPEG, clip đúng đường bao, gửi xuống dưới cùng. AutoRefresh không thay ảnh này. **Xóa ảnh** vẫn xóa được.

### Docs / quy trình
- HANDOFF + changelog phiên 1.5.2. README mô tả hai chế độ chèn ảnh.

## 1.5.1 — 2026-10-13

### Sản phẩm
- Hộp thoại **Chọn kinh tuyến trục** mở lại. Bản 1.5.0 gắn cả `DisplayMemberPath` và `ItemTemplate` trên ô tỉnh, nên WPF ném lỗi và lệnh `INK` / `EXK` dừng trước khi cửa sổ hiện.

### Docs / quy trình
- HANDOFF + changelog phiên 1.5.1.

## 1.5.0 — 2026-10-13

### Sản phẩm
- Panel Bản đồ: nhập KML/KMZ (`INK`), xuất KML/KMZ (`EXK`, giữ `EG`), và ba nút ảnh (chèn vệ tinh, cập nhật, xóa).
- Hộp thoại VN-2000 dùng chung cho nhập, xuất và chèn ảnh. Nhãn **Chọn kinh tuyến trục**. Nhớ tỉnh, múi và tùy chọn trong `.isurvey.json`.
- Nhập: chọn file trước, rồi thiết lập. Vẽ `Point` / `LineString` / `Polygon` vào layer `KML_IMPORT`, phóng vừa vùng vừa nhập. Vòng trong và `gx:Track` bỏ qua.
- Lọc đỉnh Douglas-Peucker tắt mặc định (sai số 0,2 m). Bật khi nhập đường nền dày đỉnh.
- Xuất: thiết lập → chọn đối tượng hoặc cả bản vẽ → lưu file → mở Google Earth nếu đã chọn.

### Docs / quy trình
- HANDOFF + changelog phiên 1.5.0. README cập nhật lệnh nhập/xuất.

## 1.4.0 — 2026-10-12

### Sản phẩm
- Một bundle cho AutoCAD / Civil 3D **2023–2027**. CAD tự nạp đúng DLL: `Contents/Win64/2023` (.NET Framework 4.8, 2023–2024), `2025` (.NET 8, 2025–2026), `2027` (.NET 10).
- API biên dịch từ gói `AutoCAD.NET` (24.2.0 / 25.0.1 / 26.0.0). Bản 2023–2024 nạp DLL phụ (ProjNET, System.Text.Json) từ thư mục plugin.

### Docs / quy trình
- Catalog lệnh bổ sung `CATBD`/`CBD`, `AUTOSCAN`/`SCN` và nhóm Tài khoản. HANDOFF + changelog phiên 1.4.0.

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
