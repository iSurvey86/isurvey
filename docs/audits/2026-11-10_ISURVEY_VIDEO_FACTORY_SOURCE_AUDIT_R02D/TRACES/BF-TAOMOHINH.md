# BF-TAOMOHINH — TAOMOHINH

- **verdict_source:** `SOURCE_BEHAVIOR_VERIFIED_STATIC`
- **priority:** SC10_RECHECK
- **runtime:** NOT_TESTED

## Call chain
TAOMOHINH → New @ TerrainModule.cs:56-100 | CreateTerrainWindow.Prompt → TerrainStore.Save → SurveyPaletteManager.Reload

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: Model Space; tên/layer mô hình
- Algorithm: Tạo TerrainRecord Guid; Save; chưa build mesh
- Outputs: Model record; message hướng dẫn thêm điểm/xây
- Errors: dialog catch; hủy
- Destructive: Không xóa CAD

## Gaps
(không)

## Evidence

