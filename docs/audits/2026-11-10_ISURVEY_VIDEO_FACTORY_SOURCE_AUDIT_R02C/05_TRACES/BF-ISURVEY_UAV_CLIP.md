# BF-ISURVEY_UAV_CLIP — ISURVEY_UAV_CLIP

- **source_status:** SOURCE_PRESENT
- **evidence:** `HANDLER_TRACE_COMPLETE`
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_UAV_CLIP CommandMethod @ src/Modules/Uav/UavModule.cs:10 | effective `ClipCommand` span 11-17 | Auth gate: AuthManager.EnsureAuthenticated (trong thân handler) | type `AuthManager` file `src/Auth/AuthManager.cs` (664 dòng) — chưa khớp method call cụ thể | UavImageService.ClipByBoundary @ src/Modules/Uav/UavImageService.cs:23-53

## Inputs
CHƯA ĐỦ BẰNG CHỨNG đủ điều kiện tiên quyết

## Outputs / hậu điều kiện
UavImageService.ClipByBoundary: có ghi/xuất/CAD commit (span 23-53)

## Errors
UavImageService.ClipByBoundary: có catch @ src/Modules/Uav/UavImageService.cs:23-53

## Destructive
không thấy Delete/Erase/Purge trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Unresolved
CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `AuthManager` từ handler; không tìm thấy file cho type `DocumentManager`
