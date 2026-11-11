# BF-ISURVEY_LOGIN — ISURVEY_LOGIN

- **source_status:** SOURCE_PRESENT
- **evidence:** `HANDLER_TRACE_COMPLETE`
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_LOGIN CommandMethod @ src/Auth/AuthModule.cs:24 | effective `LoginCommand` span 25-32 | Auth gate: AuthManager.EnsureAuthenticated (trong thân handler) | AuthManager.Refresh @ src/Auth/AuthManager.cs:125-147

## Inputs
CHƯA ĐỦ BẰNG CHỨNG đủ điều kiện tiên quyết

## Outputs / hậu điều kiện
CHƯA ĐỦ BẰNG CHỨNG hậu điều kiện đo được

## Errors
CHƯA ĐỦ BẰNG CHỨNG nhánh lỗi đầy đủ

## Destructive
không thấy Delete/Erase/Purge trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Unresolved
CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.ShowLogin trong src/Auth/AuthManager.cs
