# BF-ISURVEY_UAV_SEND_BACK — ISURVEY_UAV_SEND_BACK

- **verdict_source:** `SOURCE_BEHAVIOR_VERIFIED_STATIC`
- **priority:** SC10_RECHECK
- **runtime:** NOT_TESTED

## Call chain
UavModule.SendBackCommand @ src/Modules/Uav/UavModule.cs:28-35 | AuthManager.EnsureAuthenticated(track:false) | UavImageService.SendImageToBack @ UavImageService.cs:468-489 | PromptEntity RasterImage → Transaction → SendImageToBack(tr,…) → Commit → ActivityLogger

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: User pick 1 RasterImage; active Document/Database
- Algorithm: GetEntity chỉ RasterImage; reorder DrawOrder xuống đáy trong transaction
- Outputs: Ảnh dưới cùng draw order; message; ActivityLogger.Result
- Errors: hủy pick → WriteMessage Đã hủy; không catch bao ngoài ngoài module
- Destructive: Không xóa entity — chỉ đổi thứ tự vẽ

## Gaps
(không)

## Evidence

