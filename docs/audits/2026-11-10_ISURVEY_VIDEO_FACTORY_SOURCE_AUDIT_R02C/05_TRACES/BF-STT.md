# BF-STT — STT

- **source_status:** SOURCE_PRESENT
- **evidence:** `HANDLER_TRACE_COMPLETE`
- **runtime:** NOT_TESTED

## Call chain
STT CommandMethod @ src/Modules/Tuyen/SequenceNumberService.cs:28 | effective `SequenceCommand` span 29-116 | Auth gate: AuthManager.EnsureAuthenticated (trong thân handler) | ActivityLogger.Result @ src/Auth/ActivityLogger.cs:57-58 | type `AuthManager` file `src/Auth/AuthManager.cs` (664 dòng) — chưa khớp method call cụ thể

## Inputs
CHƯA ĐỦ BẰNG CHỨNG đủ điều kiện tiên quyết

## Outputs / hậu điều kiện
Editor.WriteMessage phản hồi người dùng

## Errors
có catch trong handler span 29-116

## Destructive
không thấy Delete/Erase/Purge trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Unresolved
CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `AuthManager` từ handler; không tìm thấy file cho type `DocumentManager`; không tìm thấy file cho type `SequenceWindow`; không tìm thấy file cho type `TransactionManager`
