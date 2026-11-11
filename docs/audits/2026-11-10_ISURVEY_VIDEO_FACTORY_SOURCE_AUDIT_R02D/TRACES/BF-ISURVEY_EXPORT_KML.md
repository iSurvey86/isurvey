# BF-ISURVEY_EXPORT_KML — ISURVEY_EXPORT_KML

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_EXPORT_KML @ src/Modules/Export/ExportModule.cs:22 → ExportKmlCommand → types=ActivityLogger;AuthManager;DocumentManager;KmlExportService;MapModule;Vn2000SetupDialog

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated(track: false))             return;          var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null)             return;          var ed = doc.Editor;         var db = doc.Database;          if (!MapModule.IsModelSpace(db))         …
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: catch trong handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
thấy type KmlExportService nhưng chưa khớp method call; CHƯA ĐỦ BẰNG CHỨNG body Vn2000SetupDialog.['Show']

## Evidence
EVIDENCE/USER-BF-ISURVEY_EXPORT_KML.md#src/Modules/Export/ExportModule.cs:23-125
