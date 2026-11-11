# BF-ISURVEY_PALETTE — ISURVEY_PALETTE

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** NORMAL
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_PALETTE @ src/UI/Palette/PaletteModule.cs:9 → ShowCommand → types=AuthManager;SurveyPaletteManager

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated("ISURVEY_PALETTE"))             return;         SurveyPaletteManager.Toggle();     }…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: CHƯA ĐỦ BẰNG CHỨNG / không thấy catch handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
(đã mở handler + ≥1 service call site)

## Evidence
EVIDENCE/USER-BF-ISURVEY_PALETTE.md#src/UI/Palette/PaletteModule.cs:10-15
