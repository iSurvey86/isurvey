# 01 — Kết luận đối soát thủ công cuối Phase 01

**Ngày:** 2026-11-10  
**HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`  
**Kế thừa:** R01–R02D (không sao chép lại).  
**Runtime:** NOT_TESTED toàn bộ.

## P0

### 1. ISURVEY_DGN_TO_CAD — PARTIAL_BLOCKED
Chuỗi: `DgnModule` → `DgnImportService.Start` → `-DGNIMPORT` async → hậu xử lý.  
Tác động: PERSISTENT_CAD_MUTATION.  
Đủ: entry/Start/lỗi file. Thiếu: toàn bộ Idle/Finish/explode.  
Chứng cứ: `E-D2C-MODULE`, `E-D2C-START`.

### 2. ISURVEY_CAD_TO_DGN — SOURCE_SUPPORTED (FILE_WRITE)
**Không** lấy WriteMessage làm bằng chứng tạo file.  
Đã thấy: `File.Exists`/`File.Delete` ghi đè → `Editor.Command("_.-DGNEXPORT", …, OutputPath)` → `OnIdle` chờ `File.Exists && size>0`.  
Chứng cứ: `E-C2D-MODULE`, `E-C2D-OVERWRITE`, `E-C2D-RUNEXPORT`.

### 3. VEMCD / ISURVEY_PROFILE_DRAW — PARTIAL_BLOCKED
Đã chứng minh: eraseHandles cũ → `ProfileDrawService.Draw` tạo entity + Commit → `ProfileStore.Save`.  
Thiếu: geometry band còn lại. Chứng cứ: `E-VEMCD-DRAW`, `E-VEMCD-REDRAW`, `E-VEMCD-SERVICE`.

### 4. TRACAOD — SOURCE_SUPPORTED (TEMPORARY_CAD_MUTATION)
**Không read-only.** `ShowElevationMark` tạo DBText; `EraseElevationMark` cuối vòng. Crash giữa vòng có thể sót nhãn.  
Chứng cứ: `E-TRACAOD-QUERY`, `E-TRACAOD-MARK`.

### 5. ISURVEY_LOGOUT — SOURCE_SUPPORTED (AUTH_STATE)
`SignOut` → `LicenseStorage.Delete()` xóa `license.dat`; online release nền. Không trích token.  
Chứng cứ: `E-LOGOUT-CMD`, `E-LOGOUT-SIGNOUT`, `E-LOGOUT-STORAGE`.

### 6. Store/Delete
| Lệnh | Impact | Disposition | Evidence |
|---|---|---|---|
| TAOTUYEN | PERSISTENT_CAD_MUTATION | SOURCE_SUPPORTED | E-TAOTUYEN-* |
| TAOMOHINH | PERSISTENT_CAD_MUTATION | SOURCE_SUPPORTED | E-TAOMOHINH-* |
| THIETLAPCOC | FILE_WRITE | SOURCE_SUPPORTED | E-THIETLAPCOC-* |
| DELETE_GE | PERSISTENT_CAD_MUTATION | SOURCE_SUPPORTED | E-DELETE-GE |

### 7. Bằng chứng từng bị cắt
- VEMCD Draw: khôi phục `E-VEMCD-*` (không cắt giữa EraseHandles→Commit).
- MAP_SAT `HdMosaicWorkflow.Run`: `E-MAPSAT-HD` — PARTIAL_BLOCKED phần sau L100.
- NUMBER_VERTICES: `E-NUMBER-VERTICES`, `E-NUMBER-TXT` — SOURCE_SUPPORTED.

## P1

### 8. XAML 10 binding / 9 handler
Hai `Close_Click` StationList = một handler. MapInsert Delete gọi `GeDeleteWorkflow` (không CLOSE thuần). StationList Delete = `RemoveStation`. Vn2000 = helper đa lệnh. Evidence: `E-XAML-*`.

### 9. 10 USER thiếu evidence_refs R02D
Chính SC10 — đã gắn `E-*` trong CSV disposition.

### 10. Ma trận 52 USER
`02_FEATURE_DISPOSITION_52_USER.csv`: SOURCE_SUPPORTED=10; PARTIAL_BLOCKED=42. Không nâng hồ sơ chưa đối soát thủ công.

## Đề xuất
**GO_FOR_PROMPT_MASTER_DESIGN_WITH_BLOCKED_FEATURES**  
Tính năng `PARTIAL_BLOCKED` cấm đưa vào kế hoạch quay đến cổng xác minh riêng. Đóng kiểm toán nguồn ≠ runtime CAD.
