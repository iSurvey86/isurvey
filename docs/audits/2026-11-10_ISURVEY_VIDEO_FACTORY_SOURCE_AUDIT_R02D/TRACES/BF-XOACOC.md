# BF-XOACOC — XOACOC

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
XOACOC @ src/Modules/Tuyen/RouteModule.cs:29 → ClearStations → types=AuthManager;DocumentManager;RouteService;SurveyPaletteManager

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated("XOACOC"))             return;         var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null || !ModelSpace(doc.Editor))             return;         var route = RouteService.Choose(doc.Editor, doc.Database);         if (route is n…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: CHƯA ĐỦ BẰNG CHỨNG / không thấy catch handler
- Destructive: có dấu hiệu destructive

## Gaps
CHƯA ĐỦ BẰNG CHỨNG body RouteService.['Choose', 'ClearStations']

## Evidence
EVIDENCE/USER-BF-XOACOC.md#src/Modules/Tuyen/RouteModule.cs:282-301
