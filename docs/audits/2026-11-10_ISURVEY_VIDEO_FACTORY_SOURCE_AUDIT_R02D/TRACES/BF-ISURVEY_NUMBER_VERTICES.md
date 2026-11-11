# BF-ISURVEY_NUMBER_VERTICES — ISURVEY_NUMBER_VERTICES

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_NUMBER_VERTICES @ src/Modules/Tuyen/VertexNumberingService.cs:37 → NumberVerticesCommand → types=ActivityLogger;AuthManager;DocumentManager;TransactionManager

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated(track: false))             return;          var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null)             return;          var db = doc.Database;         var ed = doc.Editor;          var peo = new PromptEntityOptions("\niSur…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: CHƯA ĐỦ BẰNG CHỨNG / không thấy catch handler
- Destructive: có dấu hiệu destructive

## Gaps
(đã mở handler + ≥1 service call site)

## Evidence
EVIDENCE/USER-BF-ISURVEY_NUMBER_VERTICES.md#src/Modules/Tuyen/VertexNumberingService.cs:38-238
