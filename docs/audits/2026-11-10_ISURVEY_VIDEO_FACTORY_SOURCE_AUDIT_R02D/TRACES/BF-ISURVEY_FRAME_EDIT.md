# BF-ISURVEY_FRAME_EDIT — ISURVEY_FRAME_EDIT

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_FRAME_EDIT @ src/Modules/Frame/FrameService.FrameEdit.cs:16 → EditFrameInfoCommand → types=ActivityLogger;AuthManager;DocumentManager

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated(track: false))             return;         var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null)             return;          var id = FrameDoubleClick.TakePending();         if (id.IsNull)         {             var opt = new Pro…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: catch trong handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
(đã mở handler + ≥1 service call site)

## Evidence
EVIDENCE/USER-BF-ISURVEY_FRAME_EDIT.md#src/Modules/Frame/FrameService.FrameEdit.cs:17-53
