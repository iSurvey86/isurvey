# 2026-11-03 — VEMCD + hộp tiến trình, bump 1.8.17

**Phiên bản:** 1.8.17 — **đã phát hành** 2026-11-03 (`iSurvey-Setup-1.8.17.zip` + `version.json`).

## Đã làm

- **VEMCD** / `ISURVEY_PROFILE_DRAW`: vẽ đầu bảng + đường đất trên Model; gợi ý/tách mức so sánh theo A3. Ribbon, BANGIS, catalog.
- **TaskProgressWindow**: Nhập/Xuất DGN, UAV tự cắt, phát sinh cọc, duỗi tuyến, TAB, CATBD, rải khung tuyến, tile bản đồ, KML (≥20).
- Bump `1.8.16` → `1.8.17` (csproj + PackageContents). Kịch bản video `vemcd-1.8.17`, `d2c-1.8.17`.
- Đã nhìn VEMCD trên Civil 3D 2026 (bản vẽ trắng). Đã chạy Nhập DGN có log trên Civil (seed thử).

## File chính

| Khu vực | File |
|---------|------|
| VEMCD | `src/Modules/Profile/*`, `src/UI/Profile/*` |
| Tiến trình | `src/UI/TaskProgressWindow.cs`, `DgnModule` / `DgnImportService` / `DgnBatchService` / `DgnExportService`, các module UAV/Tuyến/Import/Map/Plan/Frame |
| Version | `iSurvey.csproj`, `deploy/iSurvey.bundle/PackageContents.xml` |

## Việc còn lại

- Quay video khi được yêu cầu («làm video»).
- Máy bản cũ: `UPD` hoặc cài zip Setup mới.
