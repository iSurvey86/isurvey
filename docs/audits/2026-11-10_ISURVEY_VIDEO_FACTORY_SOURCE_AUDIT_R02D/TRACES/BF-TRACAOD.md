# BF-TRACAOD — TRACAOD

- **verdict_source:** `SOURCE_BEHAVIOR_VERIFIED_STATIC`
- **priority:** SC10_RECHECK
- **runtime:** NOT_TESTED

## Call chain
TRACAOD → Query @ TerrainModule.cs:303-344 | EnsureAuthenticated; ModelSpace; TerrainService.Choose; GetPoint; TerrainService elevation | TerrainService.Choose, EraseElevationMark, ShowElevationMark, TryQuery

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: Model Space; chọn mô hình; pick điểm XY
- Algorithm: Tra cao độ từ mesh/model
- Outputs: WriteMessage cao độ
- Errors: CHƯA ĐỦ BẰNG CHỨNG đầy đủ mọi nhánh Query cho tới khi đọc hết TerrainService
- Destructive: Không ghi CAD (kỳ vọng read-only)

## Gaps
(không)

## Evidence

