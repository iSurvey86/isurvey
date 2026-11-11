# BF-THEMMOHINH — THEMMOHINH

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
THEMMOHINH @ src/Modules/Terrain/TerrainModule.cs:23 → Add → types=AddPointSourcesWindow;AuthManager;DocumentManager;SurveyPaletteManager;TerrainService

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated("THEMMOHINH"))             return;         var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null || !ModelSpace(doc.Editor))             return;         var model = TerrainService.Choose(doc.Editor, doc.Database);         if (mode…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: catch trong handler
- Destructive: có dấu hiệu destructive

## Gaps
không thấy file AddPointSourcesWindow.cs; CHƯA ĐỦ BẰNG CHỨNG body TerrainService.['Choose', 'AddPoints', 'Add', 'DropMesh']

## Evidence
EVIDENCE/USER-BF-THEMMOHINH.md#src/Modules/Terrain/TerrainModule.cs:102-163
