# HANDOFF — Phiên làm việc (mới nhất ở trên)

> **Máy khác:** `git pull` → đọc block **đầu tiên** dưới đây → tiếp tục chat.  
> **Cuối phiên:** `làm cuối phiên đầy đủ` (= HANDOFF → bump version + docs + changelog → commit + push). Chi tiết: [README](./README.md).

---

## 2026-09-03 — Export KML/KMZ + TM-6 + .NET 10 (1.1.0)

**Máy / ngữ cảnh:** Cursor — máy văn phòng (Civil 3D 2026 / .NET 10); cuối phiên đầy đủ; bump **1.1.0**.

### Đã chốt / đã làm

**Sản phẩm (AppVersion 1.1.0):**
- **Xuất Google Earth:** lệnh `ISURVEY_EXPORT_KML` (alias **EG**); module `src/Modules/Export/*` + UI `src/UI/KmlExport/*`.
  - Mặc định KMZ; phạm vi toàn bản vẽ / selection; group-by-layer; Use Z tắt mặc định; mở file sau xuất.
  - Không Dim / layer ẩn-đóng băng; block = điểm insert (không explode).
  - Style: màu CAD **1:1 RGB** → KML `aabbggrr` (không remap đen→trắng); outline không fill; strip MText; ẩn pin vàng; LineWeight → width.
- **Múi chiếu TM-3 / TM-6** (Map + Export): radio UI; mặc định TM-3; TM-6 snap kinh tuyến **99 / 105 / 111**; cùng TOWGS84; `ZoneWidthDegrees` trong settings / session / pipeline CRS.
- **Alias lệnh:** `IG`=MAP, `SG`=MAP_SAT, `XG`=DELETE_GE, `EG`=EXPORT_KML (`CommandAliases.cs` + PackageContents).
- **Ribbon:** nút xuất KML/KMZ.
- **Target:** `net10.0-windows` (Civil R25.1 .NET 10); `deploy/build-bundle.ps1` path net10; `System.Drawing.Common` optional/framework.
- **Catalog lệnh:** `docs/Danh_sach_lenh_iSurvey.xlsx` (shortcut 2 ký tự ưu tiên, tránh xung đột acad.pgp / Telex).

**Nghiên cứu (không copy code):** HHMaps `C:\Hhmaps2019` — style KMZ tách loại, province/zone ini.

**Hoãn:** palette UI dọc (chờ thêm module).

### File chính

| Khu vực | File |
|---------|------|
| Export KML | `src/Modules/Export/*`, `src/UI/KmlExport/*`, `src/Models/KmlExportSettings.cs` |
| CRS / múi | `CoordinateService.cs`, `BasemapSession.cs`, Map + Export UI |
| Alias / ribbon | `src/CommandAliases.cs`, `RibbonBuilder.cs`, `PackageContents.xml` |
| Deploy | `iSurvey.csproj` (net10), `deploy/build-bundle.ps1` |
| Catalog | `docs/Danh_sach_lenh_iSurvey.xlsx` |

### Việc tiếp

- [ ] (Tuỳ) Mosaic lớn phục vụ Layout / in.
- [ ] (Tuỳ) Palette dọc khi có thêm module.
- [ ] (Tuỳ) Lưu settings trong DWG; toggle AutoRefresh trên UI.
- [ ] Bổ sung HDSD / workflow Map + Export.

### Câu mở phiên sau

```text
Đọc docs/phien-lam-viec/HANDOFF.md (block đầu). iSurvey 1.1.0 — Export KML/KMZ (EG), TM-3/TM-6 Map+Export, alias IG/SG/XG/EG, net10. Tiếp: tính năng mới theo Mẫu_Tư_Vấn hoặc vá lỗi theo Mẫu_Vá_Lỗi.
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
