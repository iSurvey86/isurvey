# BF-THEMCOC — THEMCOC

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
THEMCOC @ src/Modules/Tuyen/RouteModule.cs:53 → AddOne → types=AuthManager;DocumentManager;RouteService;SurveyPaletteManager

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated("THEMCOC"))             return;         var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null || !ModelSpace(doc.Editor))             return;         var route = RouteService.Choose(doc.Editor, doc.Database);         if (route is …
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: CHƯA ĐỦ BẰNG CHỨNG / không thấy catch handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
CHƯA ĐỦ BẰNG CHỨNG body RouteService.['Choose', 'TryAddStationAtPoint', 'FormatChainage']

## Evidence
EVIDENCE/USER-BF-THEMCOC.md#src/Modules/Tuyen/RouteModule.cs:336-366
