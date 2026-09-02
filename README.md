# iSurvey — Add-in AutoCAD / Civil 3D

Add-in .NET 8 chèn ảnh vệ tinh Google Satellite vào Model Space, georeference theo hệ VN2000 (múi chiếu 3°).

**Phiên bản bundle:** `1.0.0` (`deploy/iSurvey.bundle/PackageContents.xml`)

**Đồng bộ phiên làm việc (Cursor / đa máy):** đọc [docs/phien-lam-viec/HANDOFF.md](docs/phien-lam-viec/HANDOFF.md). Quy tắc cuối phiên: [docs/phien-lam-viec/README.md](docs/phien-lam-viec/README.md). Changelog: [docs/changelog/CHANGELOG.md](docs/changelog/CHANGELOG.md).

## Yêu cầu

- Windows x64
- AutoCAD hoặc Civil 3D 2026 (đường dẫn mặc định trong `iSurvey.csproj`: `C:\Program Files\Autodesk\AutoCAD 2026\`)
- .NET SDK 8.0

## Biên dịch

```powershell
cd D:\AIPoject\isurvey
dotnet build iSurvey.sln -c Release
```

Nếu AutoCAD cài ở thư mục khác:

```powershell
dotnet build iSurvey.csproj -c Release "-p:AcadDir=D:\Autodesk\AutoCAD 2026"
```

File đầu ra:

`bin\Release\net8.0-windows\iSurvey.dll`

(kèm `Data\isurvey_vn2000_tm3.json`, `Data\isurvey_map_sources.json` và các dependency ProjNet)

## Cài tự nạp (khuyên dùng — không cần NETLOAD)

### Bước 1: Tạo bundle

```powershell
cd D:\AIPoject\isurvey
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
   `%AppData%\Autodesk\ApplicationPlugins\iSurvey.bundle\Contents\Win64\2026\...`

Sau đó **mở lại** Civil 3D.

### Bước 3: Mở Civil 3D 2026

Tab **iSurvey** xuất hiện tự động. Gõ **ISURVEY_MAP** hoặc bấm **Chèn Google Earth**.

### Cập nhật bản mới

1. **Thoát** Civil 3D
2. Chạy lại `.\deploy\build-bundle.ps1 -Install` (hoặc thay file trong `ApplicationPlugins\iSurvey.bundle\Contents\Win64\2026\`)
3. Mở lại Civil 3D

> Bundle hiện target **AutoCAD / Civil 3D 2026** (R25.1, .NET 8).

---

## Nạp thủ công (NETLOAD — dự phòng)

1. Mở AutoCAD / Civil 3D.
2. Gõ lệnh **NETLOAD**.
3. Chọn file `iSurvey.dll` trong thư mục `bin\Release\net8.0-windows\`.
4. Tab **iSurvey** → **Chèn Google Earth**, hoặc gõ **ISURVEY_MAP**.
5. Hộp thoại: chọn **Tỉnh** → **Kinh tuyến trục** → **Loại ảnh** → **Áp dụng** (tự tải full khung nhìn).

**Xóa ảnh:** Nút **Xóa GE** hoặc lệnh **ISURVEY_DELETE_GE** → xác nhận → xóa toàn bộ tile iSurvey.

## Cài đặt được nhớ

- Bản vẽ đã lưu: file `.isurvey.json` cùng thư mục với bản vẽ
- Bản vẽ chưa lưu: `%AppData%\iSurvey\settings.json`

## Lưu ý

- Chỉ hỗ trợ Model Space (`TILEMODE=1` hoặc `MSPACE=1` trong viewport Layout).
- Mỗi tile Google 256×256 = một `RasterImage`, cache tại `%LocalAppData%\iSurvey\tiles\`.
- **ChooseZoom** theo kích thước màn hình; tự hạ zoom đến ≤ **128 tile** (không báo lỗi “vùng quá rộng”).
- **AutoRefresh** khi pan/zoom — zoom in sẽ tự nét hơn.
- **Clip đường bao:** popup → *Theo đường bao* → chọn Polyline đóng; tile gốc 256px clip CAD (không mosaic/resize).
- Hỗ trợ **Google Satellite Hybrid** (`lyrs=y`).
- **Layout / in ấn:** phủ đủ vùng cần in ở Model rồi mới sang Layout (tile chỉ tồn tại vùng đã refresh).
- **Xóa GE:** xác nhận Yes/No → xóa toàn bộ tile iSurvey.

## Phát triển — reload DLL không cần tắt Civil 3D

**Civil 3D / AutoCAD không có lệnh `NETUNLOAD`.** Đây là giới hạn của .NET (assembly đã nạp không thể gỡ khỏi AppDomain mặc định), không phải riêng bản 2026. Vì vậy `NETLOAD` lại cùng file `iSurvey.dll` **không** cập nhật code mới — phải thoát app hoặc dùng công cụ hot-reload.

### Cách nhanh nhất: DevReload (khuyên dùng khi dev)

[DevReload](https://github.com/shtirlitsDva/DevReload) dùng `AssemblyLoadContext` để gỡ/nạp lại plugin trong cùng phiên Civil 3D.

1. Cài DevReload vào Civil 3D (một lần):
   - Clone repo, build bundle Release, copy `DevReload.bundle` vào  
     `%APPDATA%\Autodesk\ApplicationPlugins\`
   - Hoặc `NETLOAD` → chọn `DevReload.dll`
2. Trong Civil 3D gõ **DEVRELOAD** → **+ Add Plugin** → chọn `D:\AIPoject\isurvey\iSurvey.csproj`
3. Prefix gợi ý: `ISURVEY` → lệnh **ISURVEYDEV** (build + reload), **ISURVEYLOAD**, **ISURVEYUNLOAD**
4. Sửa code → `dotnet build -c Debug` hoặc gõ **ISURVEYDEV** → test ngay, **không tắt Civil 3D**

Build **Debug** của iSurvey đã bật cờ `DEVRELOAD` (dùng `NoCommands` thay vì tự đăng ký lệnh). Build **Release** giữ cách nạp thường qua `NETLOAD` / bundle.

### Nếu chưa cài DevReload

| Tình huống | Cách xử lý |
|------------|------------|
| Build báo không copy được DLL (file bị khóa) | DLL vẫn có tại `obj\Release\net8.0-windows\iSurvey.dll` nhưng **không reload được** trong phiên hiện tại |
| Cần test bản mới | Thoát Civil 3D → build → mở lại → `NETLOAD` |
| Sửa nhỏ, đang debug VS | Attach debugger vào `acad.exe`, bật **Edit and Continue** |

### Mẹo giảm thời gian chờ khi phải restart

- Lưu bản vẽ (`QSAVE`) trước khi thoát — mở lại nhanh hơn
- Pin shortcut Civil 3D với `/nologo` nếu không cần splash screen
- Chỉ restart khi thật sự cần test DLL mới; giữ một phiên dev riêng với DevReload
