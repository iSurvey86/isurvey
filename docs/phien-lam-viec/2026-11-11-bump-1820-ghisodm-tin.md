# 2026-11-11 — Bump 1.8.20 (GHISODM + TIN/cọc)

Máy văn phòng. Civil 3D 2026 Metric. Cuối phiên đầy đủ. **Bump → 1.8.20.** Chưa phát hành UPD.

## Đã làm

- **Bump** `iSurvey.csproj` + `deploy/iSurvey.bundle/PackageContents.xml` → **1.8.20**.
- **GHISODM**: WPF «Ghi Số Đồng Mức» — 3 phương án, khóa ô động, Elevation/Magenta/nhãn `iSurvey_NhanDDM`, Z gốc + bước cùng hàng. Alias `ISURVEY_GAN_CAO_DO_PLINE`. Đã nhìn hộp trên Civil (Drawing1 thử, không lưu).
- **XAYMOHINH**: tối ưu CDT/ép breakline; tiến trình thân thiện; hộp Thành công; không lưu Mesh vào NOD (`TerrainStore` strip/`StringBuilder`); cảnh báo thiếu Z / Z phẳng.
- **THEMMOHINH**: cảnh báo đồng mức thiếu cao độ khi thêm.
- **PHATSINHCOC**: `TinElevationIndex` (một lần nạp Face); hộp thoại gọn.
- **TAOMOHINH**: bỏ ghi chú dưới ô lớp; Width 360.
- Catalog Excel + `docs/video/CATALOG.md` + kịch bản `ghisodm` / `xaymohinh` / `phatsinhcoc` / `themmohinh` **1.8.20** (Chưa quay). Giữ dòng kịch bản 1.8.19 cũ.
- Changelog mục **1.8.20**; README phiên bản bundle.

## Chưa làm

- `publish-release.ps1` / upload Setup + `version.json`.
- Quay / đăng video.
- HDSD web ksnpsc theo 1.8.20.
