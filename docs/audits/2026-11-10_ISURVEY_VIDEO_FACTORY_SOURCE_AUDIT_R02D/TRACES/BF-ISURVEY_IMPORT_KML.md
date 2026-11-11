# BF-ISURVEY_IMPORT_KML — ISURVEY_IMPORT_KML

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_IMPORT_KML @ src/Modules/Import/KmlImportModule.cs:21 → ImportKmlCommand → types=AcadWindow;ActivityLogger;AuthManager;CoordinateService;DocumentManager;KmlDrawService;KmlReader;MainWindow;MapModule;ShowDialog;Vn2000SetupDialog;WinFormsDialog

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated(track: false))             return;          var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null)             return;          var ed = doc.Editor;         if (!MapModule.IsModelSpace(doc.Database))         {             ed.Write…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: catch trong handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
không thấy file AcadWindow.cs; thấy type CoordinateService nhưng chưa khớp method call; không thấy file MainWindow.cs — chưa đủ để khóa toàn chuỗi

## Evidence
EVIDENCE/USER-BF-ISURVEY_IMPORT_KML.md#src/Modules/Import/KmlImportModule.cs:22-130
