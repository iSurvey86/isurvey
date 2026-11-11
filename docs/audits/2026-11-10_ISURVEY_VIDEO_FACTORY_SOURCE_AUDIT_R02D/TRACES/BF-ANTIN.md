# BF-ANTIN — ANTIN

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
ANTIN @ src/Modules/Terrain/TerrainModule.cs:35 → Toggle → types=AuthManager;DocumentManager;SurveyPaletteManager;TerrainService;TerrainStore

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated("ANTIN"))             return;         var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null)             return;         var pending = TerrainContext.ModelId;         if (string.IsNullOrWhiteSpace(pending))         {             v…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: CHƯA ĐỦ BẰNG CHỨNG / không thấy catch handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
CHƯA ĐỦ BẰNG CHỨNG body TerrainService.['Choose']; CHƯA ĐỦ BẰNG CHỨNG body TerrainStore.['Load']

## Evidence
EVIDENCE/USER-BF-ANTIN.md#src/Modules/Terrain/TerrainModule.cs:273-301
