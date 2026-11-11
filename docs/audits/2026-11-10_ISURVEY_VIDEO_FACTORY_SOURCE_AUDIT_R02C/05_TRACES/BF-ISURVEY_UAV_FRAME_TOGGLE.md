# BF-ISURVEY_UAV_FRAME_TOGGLE — ISURVEY_UAV_FRAME_TOGGLE

- **source_status:** SOURCE_PRESENT
- **evidence:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_UAV_FRAME_TOGGLE CommandMethod @ src/Modules/Uav/UavModule.cs:19 | effective `FrameToggleCommand` span 20-26 | Auth gate: AuthManager.EnsureAuthenticated (trong thân handler) | type `AuthManager` file `src/Auth/AuthManager.cs` (664 dòng) — chưa khớp method call cụ thể | UavImageService.ToggleImageFrames @ src/Modules/Uav/UavImageService.cs:439-465

## Inputs
CHƯA ĐỦ BẰNG CHỨNG đủ điều kiện tiên quyết

## Outputs / hậu điều kiện
UavImageService.ToggleImageFrames: có ghi/xuất/CAD commit (span 439-465)

## Errors
CHƯA ĐỦ BẰNG CHỨNG nhánh lỗi đầy đủ

## Destructive
không thấy Delete/Erase/Purge trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Unresolved
CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `AuthManager` từ handler; không tìm thấy file cho type `DocumentManager`
