# BF-ISURVEY_UPDATE — ISURVEY_UPDATE

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** NORMAL
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_UPDATE @ src/Auth/AuthModule.cs:74 → UpdateCommand → types=UpdateFlow

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: => UpdateFlow.RunManualCheck();…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: CHƯA ĐỦ BẰNG CHỨNG / không thấy catch handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
(đã mở handler + ≥1 service call site)

## Evidence
EVIDENCE/USER-BF-ISURVEY_UPDATE.md#src/Auth/AuthModule.cs:75-75
