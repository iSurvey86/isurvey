# BF-XOATHANHPHAN — XOATHANHPHAN

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
XOATHANHPHAN @ src/Modules/Terrain/TerrainModule.cs:53 → Clear → types=AuthManager;DarkDialog;DocumentManager;SurveyPaletteManager;TerrainService

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated("XOATHANHPHAN"))             return;         var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null || !ModelSpace(doc.Editor))             return;         var model = TerrainService.Choose(doc.Editor, doc.Database);         if (mo…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: catch trong handler
- Destructive: có dấu hiệu destructive

## Gaps
không thấy file DarkDialog.cs; CHƯA ĐỦ BẰNG CHỨNG body TerrainService.['Choose', 'CountKind', 'KindLabel', 'KindLabel', 'ClearKind']

## Evidence
EVIDENCE/USER-BF-XOATHANHPHAN.md#src/Modules/Terrain/TerrainModule.cs:220-271
