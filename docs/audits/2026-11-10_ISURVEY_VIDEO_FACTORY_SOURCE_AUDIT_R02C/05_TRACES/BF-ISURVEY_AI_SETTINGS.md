# BF-ISURVEY_AI_SETTINGS — ISURVEY_AI_SETTINGS

- **source_status:** SOURCE_PRESENT
- **evidence:** `HANDLER_TRACE_COMPLETE`
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_AI_SETTINGS CommandMethod @ src/Modules/Ai/AiAssistantModule.cs:26 | effective `SettingsCommand` span 27-58 | Auth gate: AuthManager.EnsureAuthenticated (trong thân handler) | AuthManager.Print @ src/Auth/AuthManager.cs:625-636

## Inputs
CHƯA ĐỦ BẰNG CHỨNG đủ điều kiện tiên quyết

## Outputs / hậu điều kiện
AuthManager.Print: có ghi/xuất/CAD commit (span 625-636)

## Errors
có catch trong handler span 27-58; AuthManager.Print: có catch @ src/Auth/AuthManager.cs:625-636

## Destructive
không thấy Delete/Erase/Purge trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Unresolved
CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAdmin trong src/Auth/AuthManager.cs; không tìm thấy file cho type `DarkDialog`
