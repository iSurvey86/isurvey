# BF-THIETLAPCOC — THIETLAPCOC

- **verdict_source:** `SOURCE_BEHAVIOR_VERIFIED_STATIC`
- **priority:** SC10_RECHECK
- **runtime:** NOT_TESTED

## Call chain
THIETLAPCOC → StationStyle @ RouteModule.cs:62-90 | StationStyleWindow.Prompt(StationStyleStore.Load) → StationStyleStore.Save

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: Settings kiểu cọc hiện có
- Algorithm: UI chỉnh thông số; lưu store; nhắc chạy PHATSINHCOC để vẽ lại
- Outputs: Settings persisted; không vẽ ngay
- Errors: dialog catch; hủy
- Destructive: Không xóa CAD; ghi đè settings

## Gaps
(không)

## Evidence

