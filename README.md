# iSurvey — Add-in AutoCAD / Civil 3D

Add-in cho AutoCAD / Civil 3D **2023–2027**: chèn tile Google Earth georeference **VN-2000** (TM-3 / TM-6), nhập và xuất **KML/KMZ**, cắt bình đồ theo biên (`CATBD`), quét cấu trúc bản vẽ (`AUTOSCAN`). Các lệnh nghiệp vụ khóa cho tới khi kích hoạt bằng Gmail + mã OTP.

**Phiên bản bundle:** `1.5.1` (`deploy/iSurvey.bundle/PackageContents.xml`)

**Đồng bộ phiên làm việc (Cursor / đa máy):** đọc [docs/phien-lam-viec/HANDOFF.md](docs/phien-lam-viec/HANDOFF.md). Quy tắc cuối phiên: [docs/phien-lam-viec/README.md](docs/phien-lam-viec/README.md). Changelog: [docs/changelog/CHANGELOG.md](docs/changelog/CHANGELOG.md).

## Yêu cầu

- Windows x64
- AutoCAD hoặc Civil 3D **2023, 2024, 2025, 2026 hoặc 2027** (64-bit). Một bundle chứa đúng DLL cho từng nhóm:
  - 2023–2024: .NET Framework 4.8, build từ API 2023 (2024 tương thích nhị phân)
  - 2025–2026: .NET 8, build từ API 2025 (2026 nạp được plugin 2025)
  - 2027: .NET 10, build riêng (không nạp DLL của 2025/2026)
- Máy build cần .NET SDK 8 và 10. API AutoCAD lấy từ gói `AutoCAD.NET` (không cần cài đủ các năm CAD để biên dịch).

## Lệnh chính

| Lệnh | Alias | Mô tả |
|------|-------|--------|
| `ISURVEY_MAP` | **IG** | Chèn basemap Google Earth (hộp thoại tỉnh / múi / loại ảnh) |
| `ISURVEY_MAP_SAT` | **SG** | Chèn nhanh Google Satellite |
| `ISURVEY_UPDATE_GE` |  | Làm mới ảnh GE theo khung nhìn hiện tại |
| `ISURVEY_DELETE_GE` | **XG** | Xóa toàn bộ tile GE (xác nhận) |
| `ISURVEY_IMPORT_KML` | **INK** | Nhập KML/KMZ vào layer `KML_IMPORT` (VN-2000), rồi phóng vừa vùng vừa nhập |
| `ISURVEY_EXPORT_KML` | **EXK**, **EG** | Xuất CAD → KML/KMZ. Thiết lập → chọn đối tượng → lưu file → mở Google Earth nếu đã chọn |
| `CATBD` | **CBD** | Cắt bình đồ theo đường biên (giữ Z, không dùng TRIM) |
| `AUTOSCAN` | **SCN** | Quét cấu trúc bản vẽ, xuất `CAD_AutoScan_Report.txt` |
| `ISURVEY_LOGIN` | **LGI** | Đăng nhập / kích hoạt (Gmail + OTP 6 số) |
| `ISURVEY_ABOUT` | **ABT** | Thông tin tài khoản và giấy phép trên máy |
| `ISURVEY_LOGOUT` | **LGO** | Đăng xuất / đổi máy (trả suất thiết bị nếu đang trực tuyến) |
| `ISURVEY_UPDATE` | **UPD** | Kiểm tra cập nhật |
| `ISURVEY_GENOTP` |  | In mã kích hoạt trong ngày (chỉ máy Admin có `AdminSecret`) |

Catalog đầy đủ: [docs/Danh_sach_lenh_iSurvey.xlsx](docs/Danh_sach_lenh_iSurvey.xlsx). Thiết lập Supabase (một lần, Admin): [docs/Huong_dan_Supabase.md](docs/Huong_dan_Supabase.md).

## Biên dịch

```powershell
cd D:\AIProject\isurvey
dotnet build iSurvey.sln -c Release
```

Lệnh trên build cả ba bản. File đầu ra:

`bin\Release\net48\iSurvey.dll` (2023–2024), `bin\Release\net8.0-windows\iSurvey.dll` (2025–2026), `bin\Release\net10.0-windows\iSurvey.dll` (2027)

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

### Bước 3: Mở AutoCAD hoặc Civil 3D (2023–2027)

Tab **iSurvey** xuất hiện tự động. Gõ **IG** / **ISURVEY_MAP** hoặc bấm ribbon tương ứng.

### Cập nhật bản mới

1. **Thoát** Civil 3D
2. Chạy lại `.\deploy\build-bundle.ps1 -Install` (hoặc thay cả thư mục `ApplicationPlugins\iSurvey.bundle`)
3. Mở lại Civil 3D

> Bundle hỗ trợ **AutoCAD / Civil 3D 2023–2027**. CAD tự chọn DLL trong `Contents\Win64\2023`, `2025` hoặc `2027` theo phiên bản.

Tab **iSurvey** có 6 panel: Bản đồ, Khung & Cắt Bình Đồ, Biên tập Địa hình, Tuyến & Cột Điện, Hồ sơ & QA/QC, Tài khoản & Hệ thống. Năm panel đầu chỉ bật sau khi kích hoạt. Nút chưa có lệnh in thông báo đang cập nhật trên dòng lệnh.

Kích hoạt: **LGI** → Gmail → **Gửi mã** → nhập 6 số. Giấy phép lưu tại `%AppData%\iSurvey\license.dat` và gắn với máy này; máy khác phải kích hoạt lại (tối đa 2 máy mỗi Gmail).

---

## Nạp thủ công (NETLOAD — dự phòng)

1. Mở AutoCAD / Civil 3D.
2. Gõ lệnh **NETLOAD**.
3. Chọn `iSurvey.dll` đúng năm: `bin\Release\net48\` (2023–2024), `net8.0-windows\` (2025–2026), `net10.0-windows\` (2027).
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
- **AutoRefresh** khi pan/zoom.
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
