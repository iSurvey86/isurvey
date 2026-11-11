# BF-ISURVEY_HDSD — ISURVEY_HDSD

- **source_status:** SOURCE_PRESENT
- **evidence:** `HANDLER_TRACE_COMPLETE`
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_HDSD CommandMethod @ src/Auth/AuthModule.cs:35 | effective `HelpCommand` span 36-49 | Auth gate: AuthManager.EnsureAuthenticated (trong thân handler) | ActivityLogger.Enqueue @ src/Auth/ActivityLogger.cs:32-48 | AuthManager.Print @ src/Auth/AuthManager.cs:625-636

## Inputs
CHƯA ĐỦ BẰNG CHỨNG đủ điều kiện tiên quyết

## Outputs / hậu điều kiện
AuthManager.Print: có ghi/xuất/CAD commit (span 625-636)

## Errors
có catch trong handler span 36-49; ActivityLogger.Enqueue: có catch @ src/Auth/ActivityLogger.cs:32-48; AuthManager.Print: có catch @ src/Auth/AuthManager.cs:625-636

## Destructive
không thấy Delete/Erase/Purge trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Unresolved
(không)
