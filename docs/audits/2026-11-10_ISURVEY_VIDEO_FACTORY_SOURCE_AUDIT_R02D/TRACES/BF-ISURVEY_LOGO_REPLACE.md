# BF-ISURVEY_LOGO_REPLACE — ISURVEY_LOGO_REPLACE

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** NORMAL
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_LOGO_REPLACE @ src/Modules/Frame/FrameService.cs:130 → ReplaceLogoCommand → types=ActivityLogger;AuthManager;DocumentManager

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated(track: false))             return;         var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null)             return;          try         {             ReplaceLogoCore(doc);         }         catch (System.Exception ex)         {…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: catch trong handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
(đã mở handler + ≥1 service call site)

## Evidence
EVIDENCE/USER-BF-ISURVEY_LOGO_REPLACE.md#src/Modules/Frame/FrameService.cs:131-148
