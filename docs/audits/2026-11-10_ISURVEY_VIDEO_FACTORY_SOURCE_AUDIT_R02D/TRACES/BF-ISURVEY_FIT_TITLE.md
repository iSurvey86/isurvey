# BF-ISURVEY_FIT_TITLE — ISURVEY_FIT_TITLE

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** NORMAL
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_FIT_TITLE @ src/Modules/Frame/FrameService.cs:296 → FitTitleCommand → types=AuthManager;DocumentManager

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated(track: false))             return;         var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null)             return;         var n = TitleBlockFit.FitDocument(doc);         doc.Editor.WriteMessage(n > 0             ? $"\n[iSurvey…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: CHƯA ĐỦ BẰNG CHỨNG / không thấy catch handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
(đã mở handler + ≥1 service call site)

## Evidence
EVIDENCE/USER-BF-ISURVEY_FIT_TITLE.md#src/Modules/Frame/FrameService.cs:297-308
