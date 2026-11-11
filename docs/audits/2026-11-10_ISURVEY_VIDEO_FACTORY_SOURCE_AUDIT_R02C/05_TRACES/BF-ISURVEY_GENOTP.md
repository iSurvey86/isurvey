# BF-ISURVEY_GENOTP — ISURVEY_GENOTP

- **source_status:** SOURCE_PRESENT
- **evidence:** `HANDLER_TRACE_COMPLETE`
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_GENOTP CommandMethod @ src/Auth/AuthModule.cs:106 | effective `GenerateOtpCommand` span 107-136 | type `OtpAuthService` file `src/Auth/OtpAuthService.cs` (498 dòng) — chưa khớp method call cụ thể

## Inputs
CHƯA ĐỦ BẰNG CHỨNG đủ điều kiện tiên quyết

## Outputs / hậu điều kiện
Editor.WriteMessage phản hồi người dùng

## Errors
CHƯA ĐỦ BẰNG CHỨNG nhánh lỗi đầy đủ

## Destructive
không thấy Delete/Erase/Purge trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Unresolved
không tìm thấy file cho type `DocumentManager`; CHƯA ĐỦ BẰNG CHỨNG: không mở được OtpAuthService.ComputeAdminCode trong src/Auth/OtpAuthService.cs; CHƯA ĐỦ BẰNG CHỨNG: không mở được OtpAuthService.TryNormalizeGmail trong src/Auth/OtpAuthService.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `OtpAuthService` từ handler
