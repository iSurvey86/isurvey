# BF-ISURVEY_CAD_TO_DGN — ISURVEY_CAD_TO_DGN

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** SC10_RECHECK
- **runtime:** NOT_TESTED

## Call chain
DgnModule.CadToDgnCommand @ DgnModule.cs:87-146 | SelectImplied pickfirst → DgnExportDialog → DgnExportService.Start → WriteExportSummary

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: Model Space; selection set optional; output path options
- Algorithm: Xuất async qua DgnExportService; progress window
- Outputs: File .dgn; bytes; entities count; ActivityLogger
- Errors: dialog catch; hủy; report.Error + log path
- Destructive: Không xóa CAD; ghi file output (có thể ghi đè theo dialog)

## Gaps
CHƯA ĐỦ BẰNG CHỨNG: toàn bộ DgnExportService pipeline

## Evidence

