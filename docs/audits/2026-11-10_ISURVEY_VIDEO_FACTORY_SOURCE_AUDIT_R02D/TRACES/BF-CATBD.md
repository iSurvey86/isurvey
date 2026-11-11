# BF-CATBD — CATBD

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
CATBD @ src/Modules/Plan/PlanModule.cs:46 → CutPlanCommand → types=ActivityLogger;AuthManager;DocumentManager;MapModule

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated(track: false))             return;          var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null)             return;          var ed = doc.Editor;         var db = doc.Database;          if (!MapModule.IsModelSpace(db))         …
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: catch trong handler
- Destructive: có dấu hiệu destructive

## Gaps
(đã mở handler + ≥1 service call site)

## Evidence
EVIDENCE/USER-BF-CATBD.md#src/Modules/Plan/PlanModule.cs:47-112
