# BF-XUATDIEM — XUATDIEM

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
XUATDIEM @ src/Modules/DiemDo/PointFileModule.cs:62 → ExportPointsCommand → types=ActivityLogger;AuthManager;DocumentManager;ExportPointsWindow

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated("XUATDIEM"))             return;          var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null)             return;          ExportPointsRequest? request;         try         {             request = ExportPointsWindow.Prompt();  …
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: catch trong handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
không thấy file ExportPointsWindow.cs

## Evidence
EVIDENCE/USER-BF-XUATDIEM.md#src/Modules/DiemDo/PointFileModule.cs:63-91
