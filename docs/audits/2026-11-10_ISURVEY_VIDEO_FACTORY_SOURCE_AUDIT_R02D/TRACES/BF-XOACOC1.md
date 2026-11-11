# BF-XOACOC1 — XOACOC1

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
XOACOC1 @ src/Modules/Tuyen/RouteModule.cs:47 → RemoveOne → types=AuthManager;DocumentManager;RouteService;SurveyPaletteManager

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated("XOACOC1"))             return;         var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null || !ModelSpace(doc.Editor))             return;          var options = new PromptEntityOptions("\nChọn gạch cọc hoặc chữ cọc cần xóa: ")…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: CHƯA ĐỦ BẰNG CHỨNG / không thấy catch handler
- Destructive: có dấu hiệu destructive

## Gaps
CHƯA ĐỦ BẰNG CHỨNG body RouteService.['RemoveStationByEntity']

## Evidence
EVIDENCE/USER-BF-XOACOC1.md#src/Modules/Tuyen/RouteModule.cs:303-334
