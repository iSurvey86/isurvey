# BF-ISURVEY_UAV_CLIP — ISURVEY_UAV_CLIP

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** NORMAL
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_UAV_CLIP @ src/Modules/Uav/UavModule.cs:10 → ClipCommand → types=AuthManager;DocumentManager;UavImageService

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated(track: false)) return;         var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null) return;         UavImageService.ClipByBoundary(doc.Editor, doc.Database);     }…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: CHƯA ĐỦ BẰNG CHỨNG / không thấy catch handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
(đã mở handler + ≥1 service call site)

## Evidence
EVIDENCE/USER-BF-ISURVEY_UAV_CLIP.md#src/Modules/Uav/UavModule.cs:11-17
