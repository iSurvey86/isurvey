# BF-ISURVEY_UPDATE_GE — ISURVEY_UPDATE_GE

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_UPDATE_GE @ src/Modules/Map/MapModule.cs:123 → UpdateGeCommand → types=ActivityLogger;AuthManager;DocumentManager;UserSettingsStore

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated(track: false)) return;          var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null) return;          if (!IsModelSpace(doc.Database))         {             doc.Editor.WriteMessage("\n[iSurvey] Hãy mở Model Space.");            …
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: catch trong handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
CHƯA ĐỦ BẰNG CHỨNG body UserSettingsStore.['Load']

## Evidence
EVIDENCE/USER-BF-ISURVEY_UPDATE_GE.md#src/Modules/Map/MapModule.cs:124-167
