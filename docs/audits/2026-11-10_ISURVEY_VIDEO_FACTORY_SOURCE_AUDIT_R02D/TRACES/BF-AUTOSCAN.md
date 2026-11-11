# BF-AUTOSCAN — AUTOSCAN

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
AUTOSCAN @ src/Modules/Scan/ScanModule.cs:25 → AutoScanCommand → types=ActivityLogger;AuthManager;DocumentManager;ShowDialog;TransactionManager;Win32SaveFileDialog

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated(track: false))             return;          var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null)             return;          var ed = doc.Editor;         var db = doc.Database;          var pso = new PromptSelectionOptions     …
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: catch trong handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
không thấy file ShowDialog.cs; không thấy file Win32SaveFileDialog.cs

## Evidence
EVIDENCE/USER-BF-AUTOSCAN.md#src/Modules/Scan/ScanModule.cs:26-88
