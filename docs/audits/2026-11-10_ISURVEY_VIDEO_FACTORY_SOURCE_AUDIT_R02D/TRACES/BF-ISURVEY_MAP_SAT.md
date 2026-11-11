# BF-ISURVEY_MAP_SAT — ISURVEY_MAP_SAT

- **verdict_source:** `SOURCE_BEHAVIOR_VERIFIED_STATIC`
- **priority:** NORMAL
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_MAP_SAT @ src/Modules/Map/MapModule.cs:32 → InsertSatelliteCommand → types=ActivityLogger;AuthManager;DocumentManager

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated(track: false)) return;          var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null) return;          if (!IsModelSpace(doc.Database))         {             doc.Editor.WriteMessage("\n[iSurvey] Hãy mở Model Space.");            …
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: CHƯA ĐỦ BẰNG CHỨNG / không thấy catch handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
(đã mở handler + ≥1 service call site)

## Evidence
EVIDENCE/USER-BF-ISURVEY_MAP_SAT.md#src/Modules/Map/MapModule.cs:33-64
