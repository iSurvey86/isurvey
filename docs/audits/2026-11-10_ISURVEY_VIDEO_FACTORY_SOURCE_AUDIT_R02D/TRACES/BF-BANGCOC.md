# BF-BANGCOC — BANGCOC

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
BANGCOC @ src/Modules/Tuyen/RouteModule.cs:59 → ListStations → types=AuthManager;DocumentManager;RouteService;StationListWindow;SurveyPaletteManager

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated("BANGCOC"))             return;         var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null || !ModelSpace(doc.Editor))             return;         var route = RouteService.Choose(doc.Editor, doc.Database);         if (route is …
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: catch trong handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
CHƯA ĐỦ BẰNG CHỨNG body RouteService.['Choose', 'TryAddStationAtPoint', 'FormatChainage']; không thấy file StationListWindow.cs

## Evidence
EVIDENCE/USER-BF-BANGCOC.md#src/Modules/Tuyen/RouteModule.cs:368-415
