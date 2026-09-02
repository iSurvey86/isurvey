# HANDOFF — Phiên làm việc (mới nhất ở trên)

> **Máy khác:** `git pull` → đọc block **đầu tiên** dưới đây → tiếp tục chat.  
> **Cuối phiên:** `làm cuối phiên đầy đủ` (= HANDOFF → bump version + docs + changelog → commit + push). Chi tiết: [README](./README.md).

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
