# BF-ISURVEY_ROUTE_SHEETS — ISURVEY_ROUTE_SHEETS

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_ROUTE_SHEETS @ src/Modules/Frame/FrameService.RouteLayout.cs:29 → RouteLayoutCommand → types=

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: => RunRouteLayout("ISURVEY_ROUTE_SHEETS");…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: CHƯA ĐỦ BẰNG CHỨNG / không thấy catch handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
chỉ đọc được thân handler; service chưa theo

## Evidence
EVIDENCE/USER-BF-ISURVEY_ROUTE_SHEETS.md#src/Modules/Frame/FrameService.RouteLayout.cs:30-30
