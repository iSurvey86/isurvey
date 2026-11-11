# BF-ISURVEY_DELETE_GE — ISURVEY_DELETE_GE

- **verdict_source:** `SOURCE_BEHAVIOR_VERIFIED_STATIC`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_DELETE_GE @ src/Modules/Map/MapModule.cs:169 → DeleteGeCommand → types=AuthManager;DocumentManager

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated(track: false)) return;         var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null) return;         GeDeleteWorkflow.Run(doc);     }…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: CHƯA ĐỦ BẰNG CHỨNG / không thấy catch handler
- Destructive: có dấu hiệu destructive

## Gaps
(đã mở handler + ≥1 service call site)

## Evidence
EVIDENCE/USER-BF-ISURVEY_DELETE_GE.md#src/Modules/Map/MapModule.cs:170-176
