# BF-GOMOHINH — GOMOHINH

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
GOMOHINH @ src/Modules/Terrain/TerrainModule.cs:47 → Detach → types=AuthManager;DocumentManager;SurveyPaletteManager;TerrainService

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated("GOMOHINH"))             return;         var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null || !ModelSpace(doc.Editor))             return;         var model = TerrainService.Choose(doc.Editor, doc.Database);         if (model …
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: CHƯA ĐỦ BẰNG CHỨNG / không thấy catch handler
- Destructive: có dấu hiệu destructive

## Gaps
CHƯA ĐỦ BẰNG CHỨNG body TerrainService.['Choose', 'CountKind', 'KindLabel', 'Detach', 'KindLabel']

## Evidence
EVIDENCE/USER-BF-GOMOHINH.md#src/Modules/Terrain/TerrainModule.cs:188-218
