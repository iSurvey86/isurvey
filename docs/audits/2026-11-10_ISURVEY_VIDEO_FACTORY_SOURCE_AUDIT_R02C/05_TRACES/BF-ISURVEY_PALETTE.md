# BF-ISURVEY_PALETTE — ISURVEY_PALETTE

- **source_status:** SOURCE_PRESENT
- **evidence:** `HANDLER_TRACE_COMPLETE`
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_PALETTE CommandMethod @ src/UI/Palette/PaletteModule.cs:9 | effective `ShowCommand` span 10-15 | Auth gate: AuthManager.EnsureAuthenticated (trong thân handler) | type `AuthManager` file `src/Auth/AuthManager.cs` (664 dòng) — chưa khớp method call cụ thể | SurveyPaletteManager.Toggle @ src/UI/Palette/SurveyPaletteManager.cs:22-30

## Inputs
CHƯA ĐỦ BẰNG CHỨNG đủ điều kiện tiên quyết

## Outputs / hậu điều kiện
CHƯA ĐỦ BẰNG CHỨNG hậu điều kiện đo được

## Errors
CHƯA ĐỦ BẰNG CHỨNG nhánh lỗi đầy đủ

## Destructive
không thấy Delete/Erase/Purge trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Unresolved
CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `AuthManager` từ handler
