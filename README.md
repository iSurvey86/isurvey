# iSurvey — Add-in AutoCAD / Civil 3D

Add-in cho AutoCAD / Civil 3D **2022–2027**: chèn tile Google Earth georeference **VN-2000** (TM-3 / TM-6), nhập và xuất **KML/KMZ**, cắt bình đồ theo biên (`CATBD`), quét cấu trúc bản vẽ (`AUTOSCAN`). Các lệnh nghiệp vụ khóa cho tới khi kích hoạt bằng Gmail + mã OTP.

**Phiên bản bundle:** `1.8.11` (`deploy/iSurvey.bundle/PackageContents.xml`)

**Đồng bộ phiên làm việc (Cursor / đa máy):** đọc [docs/phien-lam-viec/HANDOFF.md](docs/phien-lam-viec/HANDOFF.md). Quy tắc cuối phiên: [docs/phien-lam-viec/README.md](docs/phien-lam-viec/README.md). Changelog: [docs/changelog/CHANGELOG.md](docs/changelog/CHANGELOG.md).

## Yêu cầu

- Windows x64
- AutoCAD hoặc Civil 3D **2022, 2023, 2024, 2025, 2026 hoặc 2027** (64-bit). Một bundle chứa đúng DLL cho từng nhóm:
  - 2022–2024: .NET Framework 4.8, build từ API 2022 (2023 và 2024 nạp được plugin này)
  - 2025–2026: .NET 8, build từ API 2025 (2026 nạp được plugin 2025)
  - 2027: .NET 10, build riêng (không nạp DLL của 2025/2026)
- Máy build cần .NET SDK 8 và 10. API AutoCAD lấy từ gói `AutoCAD.NET` (không cần cài đủ các năm CAD để biên dịch).

## Lệnh chính

| Lệnh | Alias | Mô tả |
|------|-------|--------|
| `ISURVEY_MAP` | **IG** | Chèn basemap Google Earth (hộp thoại tỉnh / múi / loại ảnh) |
| `ISURVEY_MAP_SAT` | **SG** | Chèn Google Satellite: 1 xem lướt (AutoRefresh) hoặc 2 ghép một ảnh HD theo đường bao |
| `ISURVEY_UPDATE_GE` | **UG** | Làm mới ảnh GE theo khung nhìn hiện tại |
| `ISURVEY_DELETE_GE` | **XG** | Xóa toàn bộ tile GE (xác nhận) |
| `ISURVEY_IMPORT_KML` | **INK** | Nhập KML/KMZ vào layer `KML_IMPORT` (VN-2000), rồi phóng vừa vùng vừa nhập |
| `ISURVEY_EXPORT_KML` | **EXK**, **EG** | Xuất CAD → KML/KMZ. Thiết lập → chọn đối tượng → lưu file → mở Google Earth nếu đã chọn |
| `ISURVEY_DGN_TO_CAD` | **D2C** | Nhập nhiều file MicroStation `.dgn` (V7/V8). **Ghép thành một file .dwg:** chèn tất cả vào Model Space bản vẽ đang mở. **Rời từng file .dwg:** mỗi file một `.dwg` cùng thư mục. Layer giữ Level gốc hoặc thêm tiền tố (mặc định `iSurvey_`); TCVN3 ➔ Unicode và Explode Cell theo tùy chọn. Dọn linetype/layer rác luôn chạy; Zoom Extents một lần khi ghép xong. `UNDO` hai lần để hoàn tác (hậu xử lý + DGNIMPORT) |
| `ISURVEY_CAD_TO_DGN` | **C2D** | Xuất CAD ➔ MicroStation DGN V8/V7: toàn bộ Model Space hoặc đối tượng đang chọn; Purge trên bản sao tạm (bản vẽ gốc không đổi); tự chọn seed 2D/3D đi kèm bộ cài (V7 và V8), tự điền hộp thoại seed |
| `ISURVEY_DGN_CONFIG` | **DGNCFG** | Lưu mặc định hộp thoại nhập DGN: ghép/rời, layer giữ nguyên hoặc tiền tố, TCVN3, Explode Cell |
| `ISURVEY_TAB_TO_CAD` | **T2C** | Nhập MapInfo TAB: tô đặc đúng màu MapInfo, layer theo MDSD, nhãn tại điểm trong thửa |
| `CATBD` | **CBD** | Cắt bình đồ theo đường biên (giữ Z, không dùng TRIM) |
| `AUTOSCAN` | **SCN** | Quét cấu trúc bản vẽ, xuất `CAD_AutoScan_Report.txt` |
| `ISURVEY_LOGIN` | **LGI** | Đăng nhập / kích hoạt (Gmail + OTP 6 số) |
| `ISURVEY_ABOUT` | **ABT** | Thông tin tài khoản và giấy phép trên máy |
| `ISURVEY_LOGOUT` | **LGO** | Đăng xuất / đổi máy (trả suất thiết bị nếu đang trực tuyến) |
| `ISURVEY_UPDATE` | **UPD** | Kiểm tra cập nhật: tải gói và giải nén, rồi đóng Civil như bấm nút đóng. Bản vẽ chưa lưu thì hỏi lưu. Đóng xong thì cài ngay và mở lại |
| `ISURVEY_GENOTP` |  | In mã kích hoạt trong ngày (chỉ máy Admin có `AdminSecret`) |
| `ISURVEY_UAV_CLIP` | **UAVCLIP** | Clip biên ảnh UAV (polyline hoặc Auto-Clip nền trắng/đen) |
| `ISURVEY_NUMBER_VERTICES` | **GOC** | Đánh số góc tuyến theo tỷ lệ bản đồ |
| `STT` | **STT** | Đánh số thứ tự tại từng điểm click (nút «Đánh Số Thứ Tự»). Tiền tố + số + hậu tố, vòng tròn và wipeout trên layer `iSurvey_STT`. Esc hoặc chuột phải để dừng |
| `NHAPDIEM` | **ND** | Nhập điểm đo từ CSV/TXT. Tọa độ X có 7 chữ số trước dấu thập phân, Y có 6. Dấu điểm trên `iSurvey_DiemDo`, tên trên `iSurvey_DiemDo_Ten`, cao độ trên `iSurvey_DiemDo_CaoDo`. Tạo xong thì phóng vừa vùng điểm |
| `XUATDIEM` | **XD** | Xuất điểm đo ra CSV: cột cách nhau bằng chấm phẩy, thập phân là dấu chấm, mặc định 3 số lẻ. Tên xếp từ số nhỏ đến lớn, rồi A–Z |
| `ISURVEY_STRIP_MAP` | **DUOITUYEN** | Duỗi thẳng dải bản đồ dọc tuyến: chọn tim tuyến (click chỗ nào cũng được) + bắt buộc chọn điểm đầu (DD, G1…Gn, DC); cắt dải chữ nhật liền mạch; xuất bảng góc `.txt` (`Tên Điểm,X,Y,Khoảng Cách Cạnh Trước,Góc Rẽ` với `P/T độ phút giây`, X/Y 2 số lẻ) |
| `ISURVEY_KHUNG` | **KHUNG** | Chèn **khung bản đồ** chuẩn TCVN 7285:2003 (ISO 5457) khổ A0–A4, tỷ lệ 1/200…1/5000: khung tên (ô DỰ ÁN mặc định «Đường dây và TBA 110KV KCN Minh Châu», logo EVN NPC vector), lưới tọa độ theo tọa độ CAD, mũi tên Bắc, thước tỷ lệ. Xoay: không / góc / theo tuyến |
| `ISURVEY_KHUNG_BV` | **KHUNGBV** | Chèn **khung bản vẽ kỹ thuật** (cùng khung tên, không lưới tọa độ). Mẫu: `Contents\Templates\KhungBanDo_Chuan.dwg`; ghi đè bằng `%AppData%\iSurvey\KhungBanDo.dwg` |
| `ISURVEY_LOGO` | **ANLOGO** | Ẩn hoặc hiện một logo trên khung tên. Gõ `1` logo chủ đầu tư, `2` logo đơn vị thiết kế. `ANLOGO1` và `ANLOGO2` chọn thẳng, không hỏi |
| `ISURVEY_LOGO_REPLACE` | **THAYLOGO** | Thay logo EVN bằng ảnh (chủ đầu tư hoặc đơn vị thiết kế); gõ `K` để khôi phục |
| `ISURVEY_ROUTE_SHEETS` | **RAIKHUNG** | Rải khung in dọc tuyến (nút Ribbon «Rải Khung Theo Tuyến»): một layout lưới khung + viewport xoay. Chữ trong vùng tờ đọc ngang trên layout, góc và cao chữ bên Model không đổi. Hộp thoại hai cột: bố cục in bên trái, 25 ô khung tên bên phải. Nhập Excel theo cột STT và Nội dung, có file mẫu .xlsx. Kiểu in: In màu gắn `acad.ctb`, In đen trắng gắn `monochrome.ctb`. Logo và chữ ký đã chèn được nhớ trong file cho lần rải sau; nút chuyển **Xóa chữ ký** khi ô đã có chữ ký. Tick xuất PDF bật sẵn. Nhấp đúp khung để sửa thông tin |
| `ISURVEY_PROFILE_SHEETS` | **RAIMCD** | Rải khung mặt cắt dọc đã vẽ: chọn góc dưới-trái hàng cuối. Tỷ lệ trên khung `1/500; 1/5000` (hoặc `1/200; 1/2000`). Cao quá khổ thì tăng khổ giấy, giữ tỷ lệ. Layout `MatCat_Doc`. Trục đứng lùi 2,5 m so với điểm chọn. Phần thừa bên trái và bên dưới được che trên layout, Model không bị xóa. Cùng hộp thoại, kiểu in và cách nhớ logo, chữ ký như RAIKHUNG |
| `ISURVEY_ROUTE_PLOT` | **INTUYEN** | In lại layout `BanDo_Tuyen` hoặc `MatCat_Doc`. Hỏi `1` khung đang chọn, `2` tất cả. Có cửa sổ tiến trình. PDF dùng đúng bảng nét đã gắn lúc tạo layout. Không dựng trang mới |
| `ISURVEY_FIT_TITLE` | **CODAN** | Co ô chủ đầu tư và đại diện chủ đầu tư về một dòng. Ô tên bản vẽ vẫn xuống dòng trong khung |
| `ISURVEY_AI_ASSIST` | **AIA** | Trợ lý AI điều khiển Civil/CAD bằng Command Line (**chỉ Admin**, thử nghiệm). Xem [docs/Huong_dan_Tro_ly_AI.md](docs/Huong_dan_Tro_ly_AI.md) |

Catalog đầy đủ: [docs/Danh_sach_lenh_iSurvey.xlsx](docs/Danh_sach_lenh_iSurvey.xlsx). Supabase: [docs/Huong_dan_Supabase.md](docs/Huong_dan_Supabase.md). Phát hành Setup/update: [docs/Huong_dan_Phat_hanh.md](docs/Huong_dan_Phat_hanh.md). Admin nhật ký (ksnpsc): [docs/Huong_dan_Admin_ksnpsc.md](docs/Huong_dan_Admin_ksnpsc.md).

## Biên dịch

```powershell
cd D:\AIProject\isurvey
dotnet build iSurvey.sln -c Release
```

Lệnh trên build cả ba bản. File đầu ra:

`bin\Release\net48\iSurvey.dll` (2022–2024), `bin\Release\net8.0-windows\iSurvey.dll` (2025–2026), `bin\Release\net10.0-windows\iSurvey.dll` (2027)

(kèm `Data\isurvey_vn2000_tm3.json`, `Data\isurvey_map_sources.json` và các dependency ProjNet)

## Cài tự nạp (khuyên dùng — không cần NETLOAD)

### Bước 1: Tạo bundle

```powershell
cd D:\AIProject\isurvey
.\deploy\build-bundle.ps1
```

Kết quả: `deploy\output\iSurvey.bundle\`

> **Lưu ý:** Nếu Civil 3D đang mở và đã NETLOAD `iSurvey.dll`, build Release có thể báo lỗi copy file bị khóa. Script `build-bundle.ps1` vẫn đóng gói được từ `obj\Release` — hoặc thoát Civil 3D rồi build lại.

### Bước 2: Cài vào Civil 3D (một lần)

**Cách A — script tự cài (khuyên dùng, tự thêm Trusted Location):**

```powershell
.\deploy\build-bundle.ps1 -Install
```

Script sẽ copy bundle vào `%AppData%\Autodesk\ApplicationPlugins\` và ghi **TRUSTEDPATHS** vào registry — Civil 3D 2026 **không hỏi xác nhận DLL** nữa.

**Cách A2 — cài cho tất cả user (cần quyền Admin):**

```powershell
.\deploy\build-bundle.ps1 -Install -InstallAllUsers
```

Copy vào `C:\Program Files\Autodesk\ApplicationPlugins\` — thư mục này AutoCAD 2026 tin cậy mặc định.

**Cách B — copy tay (mang USB sang máy cơ quan):**

Copy cả thư mục `iSurvey.bundle` vào `%AppData%\Autodesk\ApplicationPlugins\`, rồi chạy thêm:

```powershell
.\deploy\Set-iSurveyTrustedPath.ps1
```

### Hộp thoại "Unsigned Executable File"

Từ **AutoCAD 2026**, `%AppData%\ApplicationPlugins` **không còn được tin cậy mặc định** (Autodesk siết bảo mật). Lần đầu bạn bấm **Always Load** vẫn chạy được, nhưng mỗi máy nên:

1. Chạy `Set-iSurveyTrustedPath.ps1` (hoặc `build-bundle.ps1 -Install`), **hoặc**
2. Vào **OPTIONS → Files → Trusted Locations** → thêm:
   `%AppData%\Autodesk\ApplicationPlugins\iSurvey.bundle\Contents\Win64\...`

Sau đó **mở lại** Civil 3D.

### Bước 3: Mở AutoCAD hoặc Civil 3D (2022–2027)

Tab **iSurvey** xuất hiện tự động. Gõ **IG** / **ISURVEY_MAP** hoặc bấm ribbon tương ứng.

### Cập nhật bản mới

1. **Thoát** Civil 3D
2. Chạy lại `.\deploy\build-bundle.ps1 -Install` (hoặc thay cả thư mục `ApplicationPlugins\iSurvey.bundle`)
3. Mở lại Civil 3D

> Bundle hỗ trợ **AutoCAD / Civil 3D 2022–2027**. CAD tự chọn DLL trong `Contents\Win64\2023` (cho 2022–2024), `2025` hoặc `2027` theo phiên bản.

Tab **iSurvey** có 6 panel: Bản đồ, Khung & Cắt Bình Đồ, Biên tập Địa hình, Tuyến & Cột Điện, Hồ sơ & QA/QC, Tài khoản & Hệ thống. Năm panel đầu chỉ bật sau khi kích hoạt. Nút chưa có lệnh in thông báo đang cập nhật trên dòng lệnh.

Kích hoạt: **LGI** → Gmail → **Gửi mã** → nhập 6 số. Giấy phép lưu tại `%AppData%\iSurvey\license.dat` và gắn với máy này; máy khác phải kích hoạt lại (tối đa 2 máy mỗi Gmail).

---

## Nạp thủ công (NETLOAD — dự phòng)

1. Mở AutoCAD / Civil 3D.
2. Gõ lệnh **NETLOAD**.
3. Chọn `iSurvey.dll` đúng năm: `bin\Release\net48\` (2022–2024), `net8.0-windows\` (2025–2026), `net10.0-windows\` (2027).
4. Tab **iSurvey** → **Nhập File KML/KMZ**, hoặc gõ **INK** / **IG**.
5. Hộp thoại: chọn **Tỉnh** → **Múi 3°/6°** → **Loại ảnh** → **Áp dụng**.

**Xóa ảnh:** **XG** / **ISURVEY_DELETE_GE** → xác nhận → xóa toàn bộ tile iSurvey.

**Nhập KML/KMZ:** **INK** → chọn file → hộp thoại **Chọn kinh tuyến trục** → vẽ layer `KML_IMPORT` → phóng vừa vùng vừa nhập. Lọc đỉnh Douglas-Peucker tắt mặc định.

**Xuất Google Earth:** **EXK** hoặc **EG** → thiết lập tỉnh / múi / phạm vi → chọn đối tượng (nếu không xuất cả bản vẽ) → lưu file → mở Google Earth nếu đã tick.

**Cắt bình đồ:** **CBD** / **CATBD** → chọn đường biên khép kín → quét vùng cần cắt. Một lần `UNDO` hoàn tác cả lần cắt.

**Quét cấu trúc:** **SCN** / **AUTOSCAN** → quét vùng → lưu `.txt` → Notepad mở file.

## Cài đặt được nhớ

- Bản vẽ đã lưu: file `.isurvey.json` cùng thư mục với bản vẽ
- Bản vẽ chưa lưu: `%AppData%\iSurvey\settings.json`
- Gồm tỉnh, kinh tuyến, **múi (3/6)**, loại basemap, clip biên.

## Lưu ý

- Chỉ hỗ trợ Model Space (`TILEMODE=1` hoặc `MSPACE=1` trong viewport Layout).
- Mỗi tile Google 256×256 = một `RasterImage`, cache tại `%LocalAppData%\iSurvey\tiles\`.
- **ChooseZoom** theo kích thước màn hình; tự hạ zoom đến ≤ **128 tile**.
- **AutoRefresh** khi pan/zoom (chế độ xem lướt).
- **Ảnh HD** (`ISURVEY_MAP_SAT` chọn 2): một JPEG ghép từ ô 512×512 (`scale=2`), zoom 18/19/20, tối đa 400 ô, clip theo đường bao. Không bị AutoRefresh thay. File nằm ở `%LocalAppData%\iSurvey\mosaic\`.
- **Clip đường bao:** popup → *Theo đường bao* → chọn Polyline đóng.
- **TM-6:** kinh tuyến trục snap **105 / 111 / 117** theo Thông tư 973/2001/TT-TCĐC (cùng TOWGS84 với TM-3).
- **Export KML:** màu CAD 1:1; không Dim / layer ẩn; block = điểm insert.
- **Layout / in ấn:** phủ đủ vùng cần in ở Model rồi mới sang Layout.
- **Xóa GE:** xác nhận Yes/No → xóa toàn bộ tile iSurvey.

## Phát triển — reload DLL không cần tắt Civil 3D

**Civil 3D / AutoCAD không có lệnh `NETUNLOAD`.** Đây là giới hạn của .NET (assembly đã nạp không thể gỡ khỏi AppDomain mặc định), không phải riêng bản 2026. Vì vậy `NETLOAD` lại cùng file `iSurvey.dll` **không** cập nhật code mới — phải thoát app hoặc dùng công cụ hot-reload.

### Cách nhanh nhất: DevReload (khuyên dùng khi dev)

[DevReload](https://github.com/shtirlitsDva/DevReload) dùng `AssemblyLoadContext` để gỡ/nạp lại plugin trong cùng phiên Civil 3D.

1. Cài DevReload vào Civil 3D (một lần):
   - Clone repo, build bundle Release, copy `DevReload.bundle` vào  
     `%APPDATA%\Autodesk\ApplicationPlugins\`
   - Hoặc `NETLOAD` → chọn `DevReload.dll`
2. Trong Civil 3D gõ **DEVRELOAD** → **+ Add Plugin** → chọn `D:\AIProject\isurvey\iSurvey.csproj`
3. Prefix gợi ý: `ISURVEY` → lệnh **ISURVEYDEV** (build + reload), **ISURVEYLOAD**, **ISURVEYUNLOAD**
4. Sửa code → `dotnet build -c Debug` hoặc gõ **ISURVEYDEV** → test ngay, **không tắt Civil 3D**

Build **Debug** của iSurvey đã bật cờ `DEVRELOAD` (dùng `NoCommands` thay vì tự đăng ký lệnh). Build **Release** giữ cách nạp thường qua `NETLOAD` / bundle.

### Nếu chưa cài DevReload

| Tình huống | Cách xử lý |
|------------|------------|
| Build báo không copy được DLL (file bị khóa) | DLL vẫn có tại `obj\Release\net8.0-windows\` hoặc `net10.0-windows\` nhưng **không reload được** trong phiên hiện tại |
| Cần test bản mới | Thoát Civil 3D → build → mở lại → `NETLOAD` |
| Sửa nhỏ, đang debug VS | Attach debugger vào `acad.exe`, bật **Edit and Continue** |

### Mẹo giảm thời gian chờ khi phải restart

- Lưu bản vẽ (`QSAVE`) trước khi thoát — mở lại nhanh hơn
- Pin shortcut Civil 3D với `/nologo` nếu không cần splash screen
- Chỉ restart khi thật sự cần test DLL mới; giữ một phiên dev riêng với DevReload
