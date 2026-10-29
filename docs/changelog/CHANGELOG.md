# Changelog — iSurvey

## 1.8.13 — 2026-10-29

### Cập nhật
- Hộp hỏi lưu hiện ngay khi gói đã giải nén. Bản 1.8.12 đợi thêm khoảng 8 giây vì không thấy tín hiệu lệnh đã kết thúc.

## 1.8.12 — 2026-10-29

### Cập nhật
- Chọn không lưu khi đóng Civil thì vẫn cài. Bản 1.8.11 tưởng người dùng đã hủy vì Civil còn vài giây mới tắt hẳn.

## 1.8.11 — 2026-10-29

### Cập nhật
- Sau khi tải và giải nén, iSurvey đóng Civil như bấm nút đóng cửa sổ. Bản vẽ chưa lưu thì hiện câu hỏi lưu hay không.
- Không gọi lệnh thoát khi lệnh cập nhật còn đang chạy, vì Civil bỏ qua và đứng yên.
- Khi Civil đã tắt thì cài ngay. Không còn chờ 4 phút.

## 1.8.10 — 2026-10-29

Đã phát hành. Gói 1.8.9 trên máy chủ chưa có đánh số thứ tự và điểm đo. **UPD** từ 1.8.9 nhận các phần dưới.

### Điểm đo
- **NHAPDIEM** (`ND`): đọc CSV/TXT. Cột có 7 chữ số trước dấu thập phân là X, 6 chữ số là Y. Dấu điểm nằm trên `iSurvey_DiemDo`, tên điểm trên `iSurvey_DiemDo_Ten`, cao độ trên `iSurvey_DiemDo_CaoDo`. Tạo xong thì phóng vừa vùng điểm.
- **XUATDIEM** (`XD`): ghi cột cách nhau bằng chấm phẩy, thập phân là dấu chấm, mặc định 3 số lẻ. Tên xếp từ số nhỏ đến lớn, rồi A–Z.

### Số thứ tự
- **STT**: đánh số tại từng điểm click. Vòng tròn và chữ nằm trên layer `iSurvey_STT`. Số đã vẽ trên layer cũ `KS-STT` không tự chuyển.

### Lệnh tắt
- Thêm `UG`, `AK`, `AD`, `SK`, `AIC` cho các lệnh đã có. Bỏ hai lệnh rải khung dùng để kiểm thử, không có trên Ribbon.

## 1.8.9 — 2026-10-28

Đã phát hành. Máy đang 1.8.7 cập nhật bằng **UPD** nhận luôn phần 1.8.8 (nét mỏng, chữ đọc ngang, nhớ logo và chữ ký).

### Hộp thoại rải khung
- RAIKHUNG và RAIMCD dùng một cửa sổ hai cột: bên trái là bố cục in, bên phải là đủ 25 ô khung tên, không thanh cuộn. Ô nội dung dài có dấu `...`; rê chuột vào thì thấy đủ câu.
- **Nhập Excel/CSV**: cột STT là mã, cột Nội dung là giá trị. Ngày kiểu tháng/năm đọc thành `10/2026`. Ô Tờ vẫn tự điền.

### Kiểu in
- **In màu** gắn `acad.ctb`. **In đen trắng** gắn `monochrome.ctb`. Độ dày nét lấy theo file trên máy. Không sửa hai file này.
- INTUYEN in lại đúng bảng nét đã gắn trên layout.

### Mặt cắt dọc
- Trục đứng lùi 2,5 m so với điểm chọn để thước cao độ và nét đứng không dính mép viewport.
- Từ góc dưới-trái điểm chọn, layout che phần thừa bên trái và bên dưới. Hình trên Model giữ nguyên.

## 1.8.8 — 2026-10-28

Chưa phát hành. Máy cài bằng UPD vẫn đang 1.8.7 cho đến khi phát hành bản này.

### In PDF
- RAIKHUNG, RAIMCD và INTUYEN in bằng bảng `iSurvey_005.ctb`: cả 255 màu, nét 0.05 mm, màu và kiểu nét vẫn theo đối tượng. Không sửa `acad.ctb` của máy.

### Chữ trên tờ in
- Chữ, dòng chữ và thuộc tính nằm trong vùng tờ RAIKHUNG đọc ngang trên layout. Góc và chiều cao bên Model giữ nguyên. Sửa chữ bên Model thì tờ in cập nhật theo. Nhãn mảnh và chữ ngoài tờ không đổi.

### Logo và chữ ký
- Trong một file, logo và chữ ký đã chèn được dùng lại cho lần RAIKHUNG hoặc RAIMCD sau. Đổi ảnh, đổi file chữ ký, hoặc bấm Xóa chữ ký thì lựa chọn mới có hiệu lực. File khác bắt đầu trống.

### Rải khung theo tuyến
- Lệnh đổi tên **RAIKHUNG**. `DANTRANG` và `CATTUYEN` không còn.
- Hộp thoại gọn một hàng khổ giấy và lưới. Tiêu đề mục khung tên là «2. THÔNG TIN KHUNG TÊN». Cột STT và đầu bảng căn giữa. Đầu cột «Nội dung» không có dấu hai chấm.
- Tick «Tự động gộp, xuất pdf sau khi tạo» bật sẵn khi tạo trang. Lệnh không hộp thoại không tự in.
- Đã chọn file chữ ký thì nút thành **Xóa chữ ký**. Nhấp đúp khung để sửa thông tin.
- **INTUYEN** hỏi in khung đang chọn (`1`) hay tất cả (`2`), có cửa sổ tiến trình, và in được layout mặt cắt dọc.

### Rải khung mặt cắt dọc
- **RAIMCD**: chọn góc dưới-trái của hàng cuối cần in. Tỷ lệ trên khung là `1/500; 1/5000` hoặc `1/200; 1/2000`. Viewport khóa theo mẫu số lớn (1:5000 hoặc 1:2000).
- Dải cao hơn khổ giấy thì tăng khổ A3 → A2 → A1 → A0, không đổi tỷ lệ. Layout tên `MatCat_Doc`.
- Mặt đất đặt ở khoảng trên-giữa khung. Góc trái sát mép trong của khung.

## 1.8.7 — 2026-10-27

### Dàn trang in
- Hộp DANTRANG chia hai cột: khổ giấy / tỷ lệ / chồng gối, và Hàng / Cột / Cách.
- **Nhập TXT/CSV**: mỗi dòng `MÃ;nội dung` (hoặc tab, hoặc dấu phẩy đầu tiên). Bỏ dòng `#` và `//`. Không nhập ô Tờ và Tỷ lệ vì hai ô đó tự điền.
- Khi chọn Họ tên 1–5 có nút **Chèn chữ ký**. File DWG được đặt vào ô trống bên phải tên, trên mọi tờ.
- Sau khi tạo layout có hộp “Dàn trang in đã xong”, kèm tên layout, số tờ, và đường PDF nếu có in.
- Tên PDF gợi ý: `tên bản vẽ-yyyyMMdd-HHmm.pdf`.
- **INTUYEN** in lại layout `BanDo_Tuyen` đã có, không dựng trang mới. Nhiều layout thì chọn bằng số `1`, `2`, …
- Hỏi ghi đè layout cũng bằng số `1` / `2`, để bấm chọn không bị gãy vì dấu cách.

### Đăng xuất từ xa
- Admin bật cờ trên máy (không khóa). Bản này hỏi khoảng mỗi 2 phút. Có cờ thì xóa giấy phép trên máy và bảo đăng nhập lại. Suất thiết bị vẫn giữ.
- Bản 1.8.6 trở xuống không nghe cờ này.

## 1.8.6 — 2026-10-27

### Dàn trang in (DANTRANG)
- `ISURVEY_ROUTE_SHEETS` / **DANTRANG** (tên cũ `CATTUYEN` vẫn nhận): chọn tuyến → hộp thoại khổ giấy, tỷ lệ (có 1/10000), chồng gối, lưới Hàng × Cột. Một layout `BanDo_Tuyen` chứa mọi tờ; ô Tờ ghi `i/N`. Đường bao mảnh chỉ vẽ trên Model (không in).
- Kích đúp (hoặc F2) sửa ô khung tên. Bỏ khoảng trắng thừa. Chọn STT 1 hoặc 5: **Thay Logo...** / **Ẩn logo**. Tick in PDF gộp (`DWG To PDF.pc3`, mỗi trang đúng một tờ giấy).

### Cập nhật (UPD)
- Hộp thoại mới: phiên bản hiện tại → bản mới, tính năng mới (Chi tiết nếu dài), **Cập nhật ngay** / **Đóng**.
- Tải gói có thanh tiến trình; đóng hộp thoại rồi mới thoát Civil (tránh treo). Helper đợi DLL được nhả khóa, cài, ghi kết quả, mở lại Civil.
- Nhật ký: `update_check` → `update_download` → `update_quit` → `update_result` (thành công/thất bại, mã lỗi). File theo dõi `%LocalAppData%\iSurvey\updates\update.log`.

## 1.8.5 — 2026-10-26

### Khung tên
- `ANLOGO` hỏi ẩn logo nào: gõ `1` cho chủ đầu tư, `2` cho đơn vị thiết kế. `ANLOGO1` và `ANLOGO2` làm thẳng việc đó. Hai logo không còn tắt cùng lúc.
- `CODAN` co chữ ô chủ đầu tư và đại diện chủ đầu tư về một dòng. Ô tên bản vẽ vẫn xuống dòng trong khung.

## 1.8.4 — 2026-10-23

### Hộp nhắc F12
- CAD 2025/2026 coi chữ trong ngoặc vuông là một mục chọn. Câu nhắc không còn `[iSurvey]` hay danh sách viết tay trong `[...]`.
- Chèn ảnh vệ tinh hiện **Xem lướt** và **Ghép ảnh HD**. Gõ `1` hoặc `2` vẫn được. Mức zoom, cắt ảnh UAV, xoay khung và lưới tọa độ cũng hiện tên tiếng Việt.

### Nhật ký hoạt động
- Khi lệnh xong, nhật ký ghi thêm việc vừa làm: tên file, tên bản vẽ, mức zoom, số tile, số đối tượng. Chế độ xem lướt ghi số tile; chế độ ghép HD ghi tên file `.jpg`.
- Tên lệnh gửi lên được viết dạng «SG (chèn ảnh vệ tinh)». Dòng đã ghi trước bản này không có thêm số liệu.

## 1.8.3 — 2026-10-23

### CAD 2022
- AutoCAD / Civil 3D **2022** dùng chung DLL .NET Framework 4.8 với 2023 và 2024. DLL build từ API 2022 (`AutoCAD.NET` 24.1); 2023 và 2024 nạp được plugin này. `PackageContents` nhận series R24.1–R24.3.

### Xuất DGN (C2D)
- Bộ cài kèm đủ bốn seed: V7 2D, V7 3D, V8 2D, V8 3D. Lệnh xuất không cần file template của từng năm CAD.

### Nhập DGN (D2C)
- Nhãn chế độ: «Ghép thành một file .dwg» và «Rời từng file .dwg». Tiền tố layer mặc định `iSurvey_` (cấu hình cũ `DGN_` được đổi sang `iSurvey_`). Bảng tệp có lưới dòng/cột mờ khi danh sách trống.

### Khung tên
- Ô DỰ ÁN (`DU_AN`) mặc định: «Đường dây và TBA 110KV KCN Minh Châu» trên khổ A0–A4.

### Cài đặt
- `CAI_DAT.txt` liệt kê CAD 2022–2027. Bỏ dòng hướng dẫn admin quản lý giấy phép.

## 1.8.2 — 2026-10-22

### Nhập DGN (D2C)
- Hộp thoại nền sáng: bảng nhiều tệp (thêm, lên, xuống, xóa), tổng số tệp, cột STT / Tên tệp / Đường dẫn / Kích thước.
- **Ghép tọa độ** (mặc định): nhập lần lượt vào Model Space của bản vẽ đang mở, giữ tọa độ gốc.
- **Chuyển đổi rời:** mỗi tệp một `.dwg` trong cùng thư mục (nếu trùng tên thì đánh số `_1`, `_2`…).
- Layer: giữ Level gốc hoặc thêm tiền tố (mặc định `DGN_`). Không còn mục ánh xạ JSON trên hộp thoại.
- TCVN3 ➔ Unicode mặc định bật; Explode DGN Cell mặc định tắt. Dọn linetype/layer rác luôn chạy. Zoom Extents một lần ở tệp cuối khi ghép.

## 1.8.1 — 2026-10-22

### Nhập MapInfo TAB (T2C)
- `ISURVEY_TAB_TO_CAD` / **T2C**: tô đặc theo màu MapInfo, layer theo MDSD, nhãn tại điểm trong thửa. Đổi tọa độ TM ➔ TM bằng ProjNET khi kinh tuyến trục khác bản vẽ.

### GDAL dùng chung
- Native GDAL một bản tại `Contents/SharedDependencies/GdalNative` cho cả 2023, 2025 và 2027 (không nhân ba trong từng năm).

## 1.8.0 — 2026-10-21

### Khung bản đồ / khung bản vẽ kỹ thuật (mới)
- `ISURVEY_KHUNG` (**KHUNG**) và `ISURVEY_KHUNG_BV` (**KHUNGBV**): chèn khung chuẩn TCVN 7285:2003 khổ A0–A4, tỷ lệ 1/200…1/5000 (khung tên đọc trên khung; kích thước ngoài bản vẽ = giấy mm × N/1000, theo `INSUNITS`; đơn vị unitless coi như mét). Xem trước bằng jig, xoay khung (0 / góc / theo tuyến), UCS đúng, nhóm ẩn danh, một `UNDO` hoàn tác cả lượt chèn.
- Khung bản đồ vẽ thêm: lưới thập mỗi 100 mm giấy theo **tọa độ CAD (WCS)**, nhãn `X=`/`Y=` ở cạnh trái và trên, mũi tên Bắc, thước tỷ lệ. Khung bản vẽ kỹ thuật không có phần này.
- Mẫu `KhungBanDo_Chuan.dwg` (`src/Templates`, vào bundle `Contents\Templates`): 5 khổ, khung tên điền sẵn từ `INFOR.txt`, **logo EVN NPC vector** (ngôi sao/vòng tròn dựng từ ảnh; chữ EVN = HelveticaBlackVU, NPC = HelveticaBlackOblicVU lấy từ đường viền font — không phụ thuộc font hay file ảnh trên máy khác). Tên chủ đầu tư một dòng.
- Ribbon: 2 nút mới trong panel "Khung & Cắt Bình Đồ".

### DUOITUYEN (Duỗi tuyến)
- Chọn tim tuyến ở vị trí bất kỳ; bắt buộc chọn điểm đầu (DD ➔ G1…Gn ➔ DC); dải cắt chữ nhật liền mạch, số liệu chính xác; bảng góc `.txt` dạng `Tên Điểm,X,Y,Khoảng Cách Cạnh Trước,Góc Rẽ` (`P 43 34 53`, X/Y 2 số lẻ).

### Trợ lý AI (thử nghiệm, chỉ Admin)
- `ISURVEY_AI_ASSIST` (**AIA**), công cụ `isurvey_execute_cad` chạy chuỗi Command Line (blocklist `SHELL`/`NETLOAD`/`QUIT`…, lệnh phá hủy cần xác nhận). `AuthManager.IsAdmin()` / `EnsureAdmin()`. Xem `docs/Huong_dan_Tro_ly_AI.md`.

### Khác
- Khi nạp add-in, nếu `FILEDIA` hoặc `CMDDIA` đang = 0 (do script bỏ quên) → đặt lại 1 và báo trên command line.
- `build-bundle.ps1` / `iSurvey.csproj`: đóng gói `src\Templates\*.dwg`; loại `deploy\tools` khỏi build add-in.

## 1.7.2 — 2026-10-19

### Cập nhật tự cài (UPD)
- `ISURVEY_UPDATE` / **UPD**: khi có bản mới → **Cập nhật ngay** → tải zip Setup về `%LocalAppData%\iSurvey\updates\` → xác nhận **Thoát & cài**.
- Helper `Apply-iSurveyUpdate.ps1` chờ AutoCAD/Civil thoát (tránh khóa DLL) rồi chạy `Install-iSurvey.ps1` (per-user), hiện thông báo hoàn tất.
- Bắt buộc thoát CAD trước khi cài; không cài đè trong lúc acad đang mở. Lỗi tải vẫn có tùy chọn mở link thủ công.
- `UpdateApplyService.cs`; zip Setup kèm `Apply-iSurveyUpdate.ps1`; cập nhật `CAI_DAT.txt` / `Huong_dan_Phat_hanh.md`.

## 1.7.1 — 2026-10-19

Bản vá sau rà soát 1.7.0. **Nên cài thay 1.7.0.**

### Sửa lỗi nghiêm trọng (đăng nhập / nhật ký)
- `ActivityLogger` refresh token cho MỖI sự kiện nhưng không lưu refresh token mới. Supabase đổi refresh token mỗi lần dùng nên token trong `license.dat` nhanh chóng hết hiệu lực: nhật ký ngừng ghi, kiểm tra 24 giờ thất bại, giấy phép hết hạn sau 7 ngày không xác minh (người dùng phải đăng nhập lại).
- Nay `AuthManager.AcquireAccessToken` cấp access token dùng chung (cache ~50 phút), lưu refresh token mới ngay, qua một `TokenGate` chung với kiểm tra định kỳ; lỗi refresh nghỉ 5 phút (không dồn dập). `Refresh()` / `ApplyRecheck` không còn ghi đè token mới bằng bản cũ.
- Sự kiện `logout` trước đây không bao giờ tới server (giấy phép bị xóa trước khi gửi) — nay gửi cùng bước trả suất thiết bị.
- Sự kiện `error` nay được ghi từ các lệnh Map / Xuất / Nhập KML / CATBD / SCN / UAVCLIP / DGN khi lỗi. Nhật ký dùng proxy hệ thống (mạng công ty).

### Sửa lỗi khác
- `GOC`: đỉnh trùng nhau làm lệnh ném lỗi (`GetNormal` vector 0); tuyến khép kín nay tính phân giác đúng ở đỉnh đầu/cuối và không đánh số trùng đỉnh cuối; đỉnh có Z ≠ 0 vẫn tô được chấm (Hatch cùng cao độ).
- `UAVCLIP`: ảnh trên layer khóa không còn làm hỏng cả lô; biên suy biến chỉ bỏ qua ảnh đó; lỗi bất ngờ báo rõ và hoàn tác; `DrawOrder` dùng đúng block chứa ảnh (Layout cũng được).
- `ISURVEY_UPDATE`: `min_version` / `released_at` trong `version.json` nay được đọc đúng (trước đó bị bỏ qua); chấp nhận BOM của PowerShell 5.1; chỉ mở link http/https.
- `build-release.ps1`: tham số `-Notes` thay cho ghi chú cố định; sửa lỗi cú pháp `$stageName.zip`; `version.json` ghi UTF-8 không BOM.
- Mới `deploy/publish-release.ps1`: build + upload zip + `version.json` lên bucket `isurvey-releases` bằng một lệnh (khóa `service_role` đọc từ biến môi trường `ISURVEY_SERVICE_ROLE_KEY`, không lưu file); kiểm tra zip trước khi đổi manifest, chặn hạ version, `-DryRun` / `-SkipBuild` / `-Force`.
- `schema_activity.sql`: giới hạn kích thước `detail` (chạy lại file trên Supabase).

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
