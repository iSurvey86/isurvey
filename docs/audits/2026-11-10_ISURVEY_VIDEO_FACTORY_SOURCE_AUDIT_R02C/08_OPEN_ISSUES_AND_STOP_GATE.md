# 08 — Open issues and stop gate (R02C)

## DỪNG tại cổng nghiệm thu Phase 01

Không tự chuyển Phase 02 / pilot / lập trình VF.

## Đã khắc phục

1. `business_feature_id` = `BF-<LENH_CHINH>` + crosswalk AUD đợt cũ.
2. 91 XAML: parent hoặc NOT_APPLICABLE có lý do; sửa heuristic Confirm/Save/Create/Update/Start/Export.
3. STUB: SEND_BACK → SOURCE_PRESENT; stub còn 7 lệnh UAV/PC thật sự gọi `Stub("CMD")`.
4. Evidence SERVICE_CHAIN thắt chặt (side-effect + I/O).
5. Bổ sung context menu / double-click / ICommand / AI dynamic trong `04_UI_EVENT_TO_FEATURE.csv`.

## CHƯA ĐỦ BẰNG CHỨNG / chặn

- Features chưa SERVICE_CHAIN: **46** (xem `05_SOURCE_TRACE_COVERAGE.csv` cột unresolved).
- Service layer dài (DgnImportService Idle finish, ProfileDrawService band, FrameService.*): nhiều nhánh chưa đọc hết.
- Vn2000SetupWindow và một số helper UI: NOT_APPLICABLE — opener đầy đủ có thể bổ sung sau.
- Ribbon **enabled** nhưng STUB (không sửa Ribbon): [{'cmd': 'ISURVEY_UAV_CONTOUR', 'label': 'Vẽ Đường Đồng Mức', 'line': 437, 'bf': 'BF-ISURVEY_UAV_CONTOUR'}, {'cmd': 'ISURVEY_UAV_CRS', 'label': 'Hệ Tọa Độ UAV', 'line': 439, 'bf': 'BF-ISURVEY_UAV_CRS'}, {'cmd': 'ISURVEY_PC_IMPORT', 'label': 'Nhập Point Cloud', 'line': 445, 'bf': 'BF-ISURVEY_PC_IMPORT'}, {'cmd': 'ISURVEY_PC_FILTER', 'label': 'Lọc Điểm Mặt Đất', 'line': 447, 'bf': 'BF-ISURVEY_PC_FILTER'}, {'cmd': 'ISURVEY_PC_GRID', 'label': 'Rải Lưới Điểm', 'line': 449, 'bf': 'BF-ISURVEY_PC_GRID'}]
- Runtime: toàn bộ **NOT_TESTED**.

## Tiêu chí chặn nghiệm thu Phase 01 (để Chủ dự án quyết)

| Tiêu chí | Trạng thái hồ sơ R02C |
|---|---|
| BF id ổn định + crosswalk | Có |
| XAML 91 mapped/explained | Có |
| STUB không nhầm SEND_BACK | Có |
| Mọi user-facing SERVICE_CHAIN đầy đủ hậu điều kiện | **Chưa** |
| Runtime CAD | Không thuộc R02C |

## Danh sách feature cần đọc thêm (rút gọn)

- `BF-AUTOSCAN` AUTOSCAN — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được ActivityLogger.FileName trong src/Auth/ActivityLogger.cs; CHƯA ĐỦ BẰNG CHỨNG: không mở
- `BF-CATBD` CATBD — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được ActivityLogger.FileName trong src/Auth/ActivityLogger.cs; CHƯA ĐỦ BẰNG CHỨNG: không mở
- `BF-ISURVEY_ABOUT` ISURVEY_ABOUT — `HANDLER_TRACE_COMPLETE` — 
- `BF-ISURVEY_AI_ASSIST` ISURVEY_AI_ASSIST — `HANDLER_TRACE_COMPLETE` — không tìm thấy file cho type `AiChatWindow`; CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAdmin trong src/Auth/Au
- `BF-ISURVEY_AI_SETTINGS` ISURVEY_AI_SETTINGS — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAdmin trong src/Auth/AuthManager.cs; không tìm thấy file cho type `D
- `BF-ISURVEY_DELETE_GE` ISURVEY_DELETE_GE — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chư
- `BF-ISURVEY_EXPORT_KML` ISURVEY_EXPORT_KML — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được ActivityLogger.FileName trong src/Auth/ActivityLogger.cs; CHƯA ĐỦ BẰNG CHỨNG: không mở
- `BF-ISURVEY_FIT_TITLE` ISURVEY_FIT_TITLE — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chư
- `BF-ISURVEY_FRAME_EDIT` ISURVEY_FRAME_EDIT — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chư
- `BF-ISURVEY_GENOTP` ISURVEY_GENOTP — `HANDLER_TRACE_COMPLETE` — không tìm thấy file cho type `DocumentManager`; CHƯA ĐỦ BẰNG CHỨNG: không mở được OtpAuthService.ComputeAdminCode trong 
- `BF-ISURVEY_HDSD` ISURVEY_HDSD — `HANDLER_TRACE_COMPLETE` — 
- `BF-ISURVEY_IMPORT_KML` ISURVEY_IMPORT_KML — `HANDLER_TRACE_COMPLETE` — không tìm thấy file cho type `AcadWindow`; CHƯA ĐỦ BẰNG CHỨNG: không mở được ActivityLogger.FileName trong src/Auth/Acti
- `BF-ISURVEY_KHUNG` ISURVEY_KHUNG — `METHOD_BODY_SCANNED` — 
- `BF-ISURVEY_KHUNG_BV` ISURVEY_KHUNG_BV — `METHOD_BODY_SCANNED` — 
- `BF-ISURVEY_LOGIN` ISURVEY_LOGIN — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.ShowLogin trong src/Auth/AuthManager.cs
- `BF-ISURVEY_LOGO` ISURVEY_LOGO — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chư
- `BF-ISURVEY_LOGO1` ISURVEY_LOGO1 — `METHOD_BODY_SCANNED` — 
- `BF-ISURVEY_LOGO2` ISURVEY_LOGO2 — `METHOD_BODY_SCANNED` — 
- `BF-ISURVEY_LOGO_REPLACE` ISURVEY_LOGO_REPLACE — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chư
- `BF-ISURVEY_MAP` ISURVEY_MAP — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chư
- `BF-ISURVEY_MAP_SAT` ISURVEY_MAP_SAT — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chư
- `BF-ISURVEY_NUMBER_VERTICES` ISURVEY_NUMBER_VERTICES — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được ActivityLogger.FileName trong src/Auth/ActivityLogger.cs; CHƯA ĐỦ BẰNG CHỨNG: không mở
- `BF-ISURVEY_PALETTE` ISURVEY_PALETTE — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chư
- `BF-ISURVEY_PROFILE_SHEETS` ISURVEY_PROFILE_SHEETS — `METHOD_BODY_SCANNED` — 
- `BF-ISURVEY_RIBBON` ISURVEY_RIBBON — `HANDLER_TRACE_COMPLETE` — 
- `BF-XOACOC` XOACOC — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chư
- `BF-ISURVEY_ROUTE_PLOT` ISURVEY_ROUTE_PLOT — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chư
- `BF-DAODAUTUYEN` DAODAUTUYEN — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chư
- `BF-ISURVEY_ROUTE_SHEETS` ISURVEY_ROUTE_SHEETS — `METHOD_BODY_SCANNED` — 
- `BF-PHATSINHCOC` PHATSINHCOC — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chư
- `BF-THEMCOC` THEMCOC — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chư
- `BF-BANGCOC` BANGCOC — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chư
- `BF-XOACOC1` XOACOC1 — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chư
- `BF-ISURVEY_STRIP_MAP` ISURVEY_STRIP_MAP — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chư
- `BF-ISURVEY_TAB_TO_CAD` ISURVEY_TAB_TO_CAD — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được ActivityLogger.FileName trong src/Auth/ActivityLogger.cs; CHƯA ĐỦ BẰNG CHỨNG: không mở
- `BF-THEMMOHINH` THEMMOHINH — `HANDLER_TRACE_COMPLETE` — không tìm thấy file cho type `AddPointSourcesWindow`; CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated 
- `BF-XAYMOHINH` XAYMOHINH — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chư
- `BF-XOATHANHPHAN` XOATHANHPHAN — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chư
- `BF-GOMOHINH` GOMOHINH — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chư
- `BF-ANTIN` ANTIN — `HANDLER_TRACE_COMPLETE` — CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chư
- … và 6 mục nữa trong CSV 05.