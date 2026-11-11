# BF-NHAPDIEM — NHAPDIEM

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
NHAPDIEM @ src/Modules/DiemDo/PointFileModule.cs:13 → ImportPointsCommand → types=ActivityLogger;AuthManager;DocumentManager;ImportPointsWindow;PointDrawService

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated("NHAPDIEM"))             return;          var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null)             return;         if (!IsModelSpace())         {             doc.Editor.WriteMessage("\n[iSurvey] Hãy mở Model Space trước …
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: catch trong handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
không thấy file ImportPointsWindow.cs; CHƯA ĐỦ BẰNG CHỨNG body PointDrawService.['Draw']

## Evidence
EVIDENCE/USER-BF-NHAPDIEM.md#src/Modules/DiemDo/PointFileModule.cs:14-60
