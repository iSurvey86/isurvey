# BF-ISURVEY_LOGO — ISURVEY_LOGO

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** NORMAL
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_LOGO @ src/Modules/Frame/FrameService.cs:62 → ToggleLogoCommand → types=AuthManager;DocumentManager

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated(track: false))             return;         var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null)             return;         var which = PromptLogo(doc.Editor);         if (which is null)             return;         ApplyLogo(doc…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: CHƯA ĐỦ BẰNG CHỨNG / không thấy catch handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
(đã mở handler + ≥1 service call site)

## Evidence
EVIDENCE/USER-BF-ISURVEY_LOGO.md#src/Modules/Frame/FrameService.cs:63-74
