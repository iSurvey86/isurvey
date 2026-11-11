# BF-ISURVEY_KHUNG — ISURVEY_KHUNG

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** NORMAL
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_KHUNG @ src/Modules/Frame/FrameService.cs:54 → FrameCommand → types=

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: => Execute(mapMode: true, "ISURVEY_KHUNG");…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: CHƯA ĐỦ BẰNG CHỨNG / không thấy catch handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
chỉ đọc được thân handler; service chưa theo

## Evidence
EVIDENCE/USER-BF-ISURVEY_KHUNG.md#src/Modules/Frame/FrameService.cs:55-55
