# BF-ISURVEY_LOGO2 — ISURVEY_LOGO2

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** NORMAL
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_LOGO2 @ src/Modules/Frame/FrameService.cs:79 → ToggleLogoDesignCommand → types=

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: => ToggleLogoDirect(owner: false);…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: CHƯA ĐỦ BẰNG CHỨNG / không thấy catch handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
chỉ đọc được thân handler; service chưa theo

## Evidence
EVIDENCE/USER-BF-ISURVEY_LOGO2.md#src/Modules/Frame/FrameService.cs:80-80
