# BF-PHATSINHCOC — PHATSINHCOC

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
PHATSINHCOC @ src/Modules/Tuyen/RouteModule.cs:23 → Stations → types=AuthManager;CreateStationsWindow;DocumentManager;RouteService;SurveyPaletteManager

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated("PHATSINHCOC"))             return;         var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null || !ModelSpace(doc.Editor))             return;         var route = RouteService.Choose(doc.Editor, doc.Database);         if (route…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: catch trong handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
không thấy file CreateStationsWindow.cs; CHƯA ĐỦ BẰNG CHỨNG body RouteService.['Choose', 'TryLength', 'BuildStations']

## Evidence
EVIDENCE/USER-BF-PHATSINHCOC.md#src/Modules/Tuyen/RouteModule.cs:194-262
