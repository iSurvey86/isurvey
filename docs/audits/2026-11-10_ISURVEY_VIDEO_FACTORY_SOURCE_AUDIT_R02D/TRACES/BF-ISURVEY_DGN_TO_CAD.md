# BF-ISURVEY_DGN_TO_CAD — ISURVEY_DGN_TO_CAD

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** SC10_RECHECK
- **runtime:** NOT_TESTED

## Call chain
DgnModule.DgnToCadCommand @ DgnModule.cs:21-84 | DgnImportDialog / DgnBatchService.Start hoặc DgnImportService.Start (async Idle) | Engine -DGNIMPORT + hậu xử lý explode/rename/TCVN3 (DgnImportService)

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: File .dgn V7/V8; Model Space; options dialog; batch optional
- Algorithm: Start async; theo dõi Handseed/Idle; timeout/quiet windows; post-process
- Outputs: Entities CAD (+report ImportedEntities/Warnings/LogPath)
- Errors: dialog catch; hủy; report.Error; timeout 300s
- Destructive: Explode cell, purge block/linetype/layer, rename layer trên DWG đích

## Gaps
CHƯA ĐỦ BẰNG CHỨNG: toàn bộ nhánh Idle/Finish trong DgnImportService (>1000 dòng) chưa đọc hết từng dòng

## Evidence

