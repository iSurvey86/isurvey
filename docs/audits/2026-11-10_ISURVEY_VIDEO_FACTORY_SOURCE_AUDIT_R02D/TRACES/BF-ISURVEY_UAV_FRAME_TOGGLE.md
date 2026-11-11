# BF-ISURVEY_UAV_FRAME_TOGGLE — ISURVEY_UAV_FRAME_TOGGLE

- **verdict_source:** `SOURCE_BEHAVIOR_VERIFIED_STATIC`
- **priority:** SC10_RECHECK
- **runtime:** NOT_TESTED

## Call chain
UavModule.FrameToggleCommand @ UavModule.cs:19-26 | UavImageService.ToggleImageFrames @ UavImageService.cs:439-465 | ACAD_IMAGE_VARS RasterVariables.ImageFrame toggle Off↔Above → Commit

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: Database hiện hành (không pick)
- Algorithm: Đọc/tạo RasterVariables trong NOD; đảo ImageFrame
- Outputs: Khung ảnh bật/tắt toàn bản vẽ; ActivityLogger
- Errors: không có try/catch riêng trong ToggleImageFrames — exception bubble
- Destructive: Không xóa ảnh; đổi biến hệ thống IMAGEFRAME

## Gaps
(không)

## Evidence

