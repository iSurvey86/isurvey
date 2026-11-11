# BF-ISURVEY_MAP — ISURVEY_MAP

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** NORMAL
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_MAP @ src/Modules/Map/MapModule.cs:25 → InsertMapCommand → types=AuthManager

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated(track: false)) return;         RunMapWorkflow(MapInsertAction.Insert);     }…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: CHƯA ĐỦ BẰNG CHỨNG / không thấy catch handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
không thấy file RunMapWorkflow.cs

## Evidence
EVIDENCE/USER-BF-ISURVEY_MAP.md#src/Modules/Map/MapModule.cs:26-30
