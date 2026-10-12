# HANDOFF — Phiên làm việc (mới nhất ở trên)

> **Máy khác:** `git pull` → đọc block **đầu tiên** dưới đây → tiếp tục chat.  
> **Cuối phiên:** `làm cuối phiên đầy đủ` (= HANDOFF → bump version + docs + changelog → commit + push). Chi tiết: [README](./README.md).

---

## 2026-10-12 — Đăng nhập OTP Supabase (1.3.0)

**Máy / ngữ cảnh:** Cursor — máy văn phòng (Civil 3D 2026 host .NET 10); cuối phiên đầy đủ; bump **1.3.0**. Project Supabase đã chạy; user nhận mail OTP và Ribbon hiện **Đã kích hoạt**.

### Đã chốt / đã làm

**Sản phẩm (AppVersion 1.3.0):**
- **Panel 6 Tài khoản & Hệ thống.** Panel 1–5 khóa khi chưa kích hoạt. Nút tài khoản: "Đăng Nhập / Kích Hoạt" hoặc email + "Đã kích hoạt".
- **Đăng nhập Gmail + OTP 6 số**, tự xác thực khi đủ 6 số. Sai: "Mã không đúng hoặc đã hết hạn. Còn N lần thử." Sai 5 lần thì khóa 5 phút.
- **Supabase** (HttpClient, không NuGet): gửi OTP, verify, RPC `isurvey_activate` / `isurvey_check` / `isurvey_release_device`. Tối đa **2 thiết bị / Gmail**.
- **Giấy phép cục bộ** `license.dat` (DPAPI + MachineGuid). Trực tuyến: kiểm tra ngầm mỗi 24 giờ (timeout 5 giây, không chặn lúc mở Civil); mất mạng vẫn dùng tới 7 ngày. Mã Admin / Debug: giấy phép ngoại tuyến 7 ngày.
- **Đăng xuất / Đổi máy** trả suất thiết bị (dòng đang active). Dòng Admin đã khóa không tự xóa được.
- Alias: **LGI**, **UPD**, **ABT**, **LGO**. Lệnh admin `ISURVEY_GENOTP` (cần `AdminSecret` trên máy đó, không nằm trong repo).
- URL + publishable key nằm trong `AuthConfig.cs` để máy khác pull về là kích hoạt được. SMTP App Password chỉ nằm trên Supabase.

**Đã kiểm:** test lõi offline (Debug 114, Release 118) và harness cửa sổ đăng nhập. Trên Civil thật: gửi mã, nhận mail, kích hoạt, Ribbon mở khóa.

**Chưa test trên Civil:** thu hồi (`is_active = false`), đủ 2 thiết bị, đăng xuất trả suất, kiểm tra lại sau 24 giờ.

### File chính

| Khu vực | File |
|---------|------|
| Auth | `src/Auth/` (`AuthManager`, `OtpAuthService`, `SupabaseAuthClient`, `LicenseStorage`, `LicenseRechecker`) |
| UI | `src/UI/Account/LoginWindow`, `DarkDialog`, `src/UI/RibbonBuilder.cs` |
| Supabase | `deploy/supabase/schema.sql`, `docs/Huong_dan_Supabase.md` |
| Lệnh / deploy | `src/Auth/AuthModule.cs`, `CommandAliases.cs`, `PackageContents.xml`, `iSurvey.csproj` |

### Việc tiếp

- [ ] Test khóa thiết bị, hết suất 2 máy, và `LGO` trả suất trên Supabase.
- [ ] Bổ sung lệnh Auth vào `docs/Danh_sach_lenh_iSurvey.xlsx`.
- [ ] Test `CBD` trên bản sao DWG có TIN và đồng mức Z>0.
- [ ] Test `SCN` trên vùng nhỏ, đọc file `.txt`.
- [ ] Xác nhận EG Lào Cai TM-3 xuất KMZ được.

### Câu mở phiên sau

```text
Đọc docs/phien-lam-viec/HANDOFF.md (block đầu). iSurvey 1.3.0 — đăng nhập Gmail OTP qua Supabase, tối đa 2 thiết bị, Ribbon panel Tài khoản. Đã kích hoạt thành công trên Civil máy văn phòng. Tiếp: test thu hồi / hết suất / đăng xuất, hoặc tính năng mới theo Mẫu_Tư_Vấn / vá lỗi theo Mẫu_Vá_Lỗi.
```

**Lưu trữ ngày:** [2026-10-12-dang-nhap-otp-supabase.md](./2026-10-12-dang-nhap-otp-supabase.md)

---

## 2026-10-12 — Ribbon 5 panel + CATBD + AUTOSCAN (1.2.0)

**Máy / ngữ cảnh:** Cursor — máy văn phòng (Civil 3D 2026 host .NET 10); cuối phiên đầy đủ; bump **1.2.0**.

### Đã chốt / đã làm

**Sản phẩm (AppVersion 1.2.0):**
- **Ribbon 5 panel:** Bản đồ, Khung & Cắt Bình Đồ, Biên tập Địa hình, Tuyến & Cột Điện, Hồ sơ & QA/QC. Icon vector (gradient, nét trắng). Nhãn đã chỉnh: **Chèn GE**, **Cắt Bình Đồ**. User đã xem trên Civil.
- **`CATBD` / alias `CBD`:** cắt bình đồ theo biên khép kín. Whitelist layer khung; CIRCLE ngoài whitelist xóa; TEXT dùng AlignmentPoint khi căn lề; INSERT dùng tâm GeometricExtents; LINE/polyline cắt từng đoạn, nội suy Z; cung LWPOLYLINE tách bulge; Polyline3d spline-fit bỏ ControlVertex rồi tạo SimplePoly. Không dùng TRIM. Một transaction — `UNDO` một lần.
- **`AUTOSCAN` / alias `SCN`:** quét vùng, xuất `CAD_AutoScan_Report.txt` (3 phần), mở Notepad. Mã DXF mô phỏng từ thuộc tính .NET (không EntGet). Alias không dùng `ASC` (Telex ra "ác").
- **Nút chưa có lệnh:** `RibbonPlaceholderHandler` in ASCII, không đăng ký lệnh giả.
- **Target tự chọn:** `iSurvey.csproj` và `build-bundle.ps1` đọc `acdbmgd.runtimeconfig.json` → net8 hoặc net10. Máy này host net10; máy host net8 vẫn build net8.

**Đã kiểm:** lõi cắt hình học 18 ca (dự án thử tạm, không nằm trong repo). Ribbon đã cài và user xác nhận giao diện.

**Chưa test trên DWG thật:** `CBD` với TIN/đồng mức; `SCN` file báo cáo; EG Lào Cai xuất KMZ sau bundle mới.

### File chính

| Khu vực | File |
|---------|------|
| Ribbon | `src/UI/RibbonBuilder.cs`, `RibbonIcons.cs`, `RibbonPlaceholderHandler.cs` |
| Cắt bình đồ | `src/Modules/Plan/PlanModule.cs`, `PlanGeometry.cs`, `PlanPaths.cs` |
| Quét cấu trúc | `src/Modules/Scan/ScanModule.cs`, `ScanReport.cs` |
| Lệnh / deploy | `src/Main.cs`, `CommandAliases.cs`, `PackageContents.xml`, `iSurvey.csproj`, `deploy/build-bundle.ps1` |

### Việc tiếp

- [ ] Test `CBD` trên bản sao DWG có TIN và đồng mức Z>0.
- [ ] Test `SCN` trên vùng nhỏ, đọc file `.txt`.
- [ ] Xác nhận EG Lào Cai TM-3 xuất KMZ được (bundle đã cài lại trong phiên).
- [ ] (Tuỳ) Mosaic lớn phục vụ Layout / in.
- [ ] (Tuỳ) Lưu settings trong DWG; toggle AutoRefresh trên UI.
- [ ] Bổ sung HDSD / workflow.

### Câu mở phiên sau

```text
Đọc docs/phien-lam-viec/HANDOFF.md (block đầu). iSurvey 1.2.0 — ribbon 5 panel, CATBD/CBD, AUTOSCAN/SCN, target net8 hoặc net10 theo host. Tiếp: test CBD và SCN trên DWG thật, hoặc tính năng mới theo Mẫu_Tư_Vấn / vá lỗi theo Mẫu_Vá_Lỗi.
```

**Lưu trữ ngày:** [2026-10-12-ribbon-catbd-autoscan.md](./2026-10-12-ribbon-catbd-autoscan.md)

---

## 2026-09-03 — net8 autoload + vá KML TM-3 + TM-6 TT973 (1.1.1)

**Máy / ngữ cảnh:** Cursor — máy văn phòng (Civil 3D 2026 / .NET 8); cuối phiên đầy đủ; bump **1.1.1**.

### Đã chốt / đã làm

**Sản phẩm (AppVersion 1.1.1):**
- **Retarget `net8.0-windows`:** plugin autoload trên Civil 3D 2026 (tab **iSurvey** trên Ribbon); `build-bundle.ps1` path net8.
- **TM-6 kinh tuyến chuẩn TT 973:** 105 / 111 / 117 — `CoordinateService`, Map + Export UI.
- **63 tỉnh/thành:** `isurvey_province_crs_map.json`; `FindSavedGroup` tương thích settings nhóm tỉnh cũ.
- **Vá Export KML TM-3:** `IsValidCentralMeridian(cm, zone)` — TM-3 102°–117° (vd Lào Cai 104.75°); TM-6 chỉ 105/111/117. Map Insert dùng chung.

**Debug phiên:** NETLOAD im lặng (evtx không có lỗi .NET) → xác định mismatch net10 vs host net8. Video + Event Viewer đã phân tích.

**Hoãn:** test EG Lào Cai sau cài lại bundle (Civil 3D phải thoát khi `-Install`).

### File chính

| Khu vực | File |
|---------|------|
| Deploy / runtime | `iSurvey.csproj`, `deploy/build-bundle.ps1`, `PackageContents.xml` |
| CRS / validation | `CoordinateService.cs` (`Tm6CentralMeridians`, `IsValidCentralMeridian`) |
| UI Map + Export | `MapInsertWindow.*`, `KmlExportWindow.xaml.cs` |
| Dữ liệu tỉnh | `src/Data/isurvey_province_crs_map.json` |
| Docs | `README.md`, `docs/changelog/CHANGELOG.md` |

### Việc tiếp

- [ ] Cài bundle 1.1.1 (`build-bundle.ps1 -Install`) và xác nhận EG Lào Cai TM-3 xuất được.
- [ ] (Tuỳ) Mosaic lớn phục vụ Layout / in.
- [ ] (Tuỳ) Palette dọc khi có thêm module.
- [ ] (Tuỳ) Lưu settings trong DWG; toggle AutoRefresh trên UI.
- [ ] Bổ sung HDSD / workflow Map + Export.

### Câu mở phiên sau

```text
Đọc docs/phien-lam-viec/HANDOFF.md (block đầu). iSurvey 1.1.1 — net8 autoload Civil 3D 2026, TM-6 105/111/117, 63 tỉnh, vá EG TM-3. Tiếp: test EG sau cài bundle, hoặc tính năng mới theo Mẫu_Tư_Vấn / vá lỗi theo Mẫu_Vá_Lỗi.
```

**Lưu trữ ngày:** [2026-09-03-net8-load-va-kml-tm3.md](./2026-09-03-net8-load-va-kml-tm3.md)

---

## 2026-09-03 — Export KML/KMZ + TM-6 + .NET 8 (1.1.0)

**Máy / ngữ cảnh:** Cursor — máy văn phòng (Civil 3D 2026 / .NET 8); cuối phiên đầy đủ; bump **1.1.0**.

### Đã chốt / đã làm

**Sản phẩm (AppVersion 1.1.0):**
- **Xuất Google Earth:** lệnh `ISURVEY_EXPORT_KML` (alias **EG**); module `src/Modules/Export/*` + UI `src/UI/KmlExport/*`.
  - Mặc định KMZ; phạm vi toàn bản vẽ / selection; group-by-layer; Use Z tắt mặc định; mở file sau xuất.
  - Không Dim / layer ẩn-đóng băng; block = điểm insert (không explode).
  - Style: màu CAD **1:1 RGB** → KML `aabbggrr` (không remap đen→trắng); outline không fill; strip MText; ẩn pin vàng; LineWeight → width.
- **Múi chiếu TM-3 / TM-6** (Map + Export): radio UI; mặc định TM-3; TM-6 snap kinh tuyến **105 / 111 / 117** theo Thông tư 973/2001/TT-TCĐC; cùng TOWGS84; `ZoneWidthDegrees` trong settings / session / pipeline CRS.
- **Alias lệnh:** `IG`=MAP, `SG`=MAP_SAT, `XG`=DELETE_GE, `EG`=EXPORT_KML (`CommandAliases.cs` + PackageContents).
- **Ribbon:** nút xuất KML/KMZ.
- **Target:** `net8.0-windows` (Civil R25.1 .NET 8); `deploy/build-bundle.ps1` path net8; `System.Drawing.Common` optional/framework.
- **Catalog lệnh:** `docs/Danh_sach_lenh_iSurvey.xlsx` (shortcut 2 ký tự ưu tiên, tránh xung đột acad.pgp / Telex).

**Nghiên cứu (không copy code):** HHMaps `C:\Hhmaps2019` — style KMZ tách loại, province/zone ini.

**Hoãn:** palette UI dọc (chờ thêm module).

### File chính

| Khu vực | File |
|---------|------|
| Export KML | `src/Modules/Export/*`, `src/UI/KmlExport/*`, `src/Models/KmlExportSettings.cs` |
| CRS / múi | `CoordinateService.cs`, `BasemapSession.cs`, Map + Export UI |
| Alias / ribbon | `src/CommandAliases.cs`, `RibbonBuilder.cs`, `PackageContents.xml` |
| Deploy | `iSurvey.csproj` (net8), `deploy/build-bundle.ps1` |
| Catalog | `docs/Danh_sach_lenh_iSurvey.xlsx` |

### Việc tiếp

- [ ] (Tuỳ) Mosaic lớn phục vụ Layout / in.
- [ ] (Tuỳ) Palette dọc khi có thêm module.
- [ ] (Tuỳ) Lưu settings trong DWG; toggle AutoRefresh trên UI.
- [ ] Bổ sung HDSD / workflow Map + Export.

### Câu mở phiên sau

```text
Đọc docs/phien-lam-viec/HANDOFF.md (block đầu). iSurvey 1.1.0 — Export KML/KMZ (EG), TM-3/TM-6 Map+Export, alias IG/SG/XG/EG. Tiếp: tính năng mới theo Mẫu_Tư_Vấn hoặc vá lỗi theo Mẫu_Vá_Lỗi.
```

**Lưu trữ ngày:** [2026-09-03-export-kml-tm6.md](./2026-09-03-export-kml-tm6.md)

---

## 2026-09-02 — Nền tảng Map tile GE + VN-2000 + handoff (1.0.0)

**Máy / ngữ cảnh:** Cursor — cuối phiên đầy đủ; dọn HANDOFF ksnpsc; khởi tạo handoff riêng repo **iSurvey**.

### Đã chốt / đã làm

**Sản phẩm (giữ AppVersion 1.0.0 — không bump, phiên chỉ docs):**
- Add-in **.NET 8 x64** cho AutoCAD / Civil 3D **2026**: chèn tile Google Earth (satellite / hybrid) georeference **VN-2000 TM-3**.
- Pipeline **per-tile 256×256**, disk cache, `ChooseZoom` / `FitZoom` (≤128 tiles), **AutoRefresh** theo pan/zoom, ẩn `IMAGEFRAME`.
- Lệnh: `ISURVEY_MAP`, `ISURVEY_MAP_SAT`, `ISURVEY_DELETE_GE`.
- Bundle autoload: `deploy/build-bundle.ps1` → `%AppData%\Autodesk\ApplicationPlugins\iSurvey.bundle` + TRUSTEDPATHS.
- **Clip theo biên** khi chèn (CAD clip, không mosaic/resize).
- **Xóa GE:** xác nhận Yes/No → xóa **toàn bộ** tile GE (không xóa theo trong/ngoài biên; không dùng Wipeout).

**Quy trình / docs:**
- `docs/phien-lam-viec/` — HANDOFF + README (cụm「làm cuối phiên đầy đủ」); mẫu tư vấn / vá lỗi.
- `docs/changelog/CHANGELOG.md` — khởi tạo.
- Root `README.md` — trỏ tới handoff.

### File chính

| Khu vực | File |
|---------|------|
| Lệnh / module | `src/Modules/Map/MapModule.cs`, `GeDeleteWorkflow.cs` |
| Tile / refresh | `TileAttachService.cs`, `BasemapRefreshService.cs`, `AutoRefreshController.cs`, `TileCacheService.cs` |
| Clip / xóa | `RasterClipService.cs`, `RasterDeleteService.cs`, `PolygonClipHelper.cs` |
| Deploy | `deploy/build-bundle.ps1`, `deploy/iSurvey.bundle/PackageContents.xml` |
| Docs quy trình | `docs/Mẫu_Tư_Vấn.md`, `docs/Mẫu_Vá_Lỗi.md`, `docs/phien-lam-viec/*`, `docs/changelog/CHANGELOG.md` |

### Việc tiếp

- [ ] (Tuỳ) Xuất một mosaic lớn phục vụ Layout / in (đã thảo luận, chưa làm).
- [ ] (Tuỳ) Lưu settings trong DWG; toggle AutoRefresh trên UI.
- [ ] Bổ sung HDSD / workflow module Map khi cần user cuối.

### Câu mở phiên sau

```text
Đọc docs/phien-lam-viec/HANDOFF.md (block đầu). iSurvey 1.0.0 — Map tile GE + VN-2000 + clip biên + xóa toàn bộ GE. Tiếp: tính năng mới theo Mẫu_Tư_Vấn hoặc vá lỗi theo Mẫu_Vá_Lỗi.
```

**Lưu trữ ngày:** [2026-09-02-nen-tang-map-tile.md](./2026-09-02-nen-tang-map-tile.md)

---
