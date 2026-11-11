# BF-ISURVEY_UAV_SEND_BACK — ISURVEY_UAV_SEND_BACK

- **source_status:** SOURCE_PRESENT
- **evidence:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_UAV_SEND_BACK CommandMethod @ src/Modules/Uav/UavModule.cs:28 | effective `SendBackCommand` span 29-35 | Auth gate: AuthManager.EnsureAuthenticated (trong thân handler) | type `AuthManager` file `src/Auth/AuthManager.cs` (664 dòng) — chưa khớp method call cụ thể | UavImageService.SendImageToBack @ src/Modules/Uav/UavImageService.cs:468-489

## Inputs
UavImageService.SendImageToBack: có đọc/prompt (span 468-489)

## Outputs / hậu điều kiện
UavImageService.SendImageToBack: có ghi/xuất/CAD commit (span 468-489)

## Errors
CHƯA ĐỦ BẰNG CHỨNG nhánh lỗi đầy đủ

## Destructive
không thấy Delete/Erase/Purge trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Unresolved
CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `AuthManager` từ handler; không tìm thấy file cho type `DocumentManager`
