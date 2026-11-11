# BF-ISURVEY_PROFILE_SHEETS — ISURVEY_PROFILE_SHEETS

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_PROFILE_SHEETS @ src/Modules/Frame/FrameService.ProfileLayout.cs:28 → ProfileLayoutCommand → types=

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: => RunProfile("ISURVEY_PROFILE_SHEETS");…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: CHƯA ĐỦ BẰNG CHỨNG / không thấy catch handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
chỉ đọc được thân handler; service chưa theo

## Evidence
EVIDENCE/USER-BF-ISURVEY_PROFILE_SHEETS.md#src/Modules/Frame/FrameService.ProfileLayout.cs:29-29
