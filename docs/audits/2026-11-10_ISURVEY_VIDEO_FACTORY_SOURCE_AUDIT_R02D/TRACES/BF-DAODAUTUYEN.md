# BF-DAODAUTUYEN — DAODAUTUYEN

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
DAODAUTUYEN @ src/Modules/Tuyen/RouteModule.cs:35 → Reverse → types=AuthManager;DocumentManager;RouteService;SurveyPaletteManager

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated("DAODAUTUYEN"))             return;         var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null || !ModelSpace(doc.Editor))             return;         var route = RouteService.Choose(doc.Editor, doc.Database);         if (route…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: CHƯA ĐỦ BẰNG CHỨNG / không thấy catch handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
CHƯA ĐỦ BẰNG CHỨNG body RouteService.['Choose', 'Reverse']

## Evidence
EVIDENCE/USER-BF-DAODAUTUYEN.md#src/Modules/Tuyen/RouteModule.cs:264-280
