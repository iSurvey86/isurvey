# BF-ISURVEY_MAP_SAT — ISURVEY_MAP_SAT

- **source_status:** SOURCE_PRESENT
- **evidence:** `HANDLER_TRACE_COMPLETE`
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_MAP_SAT CommandMethod @ src/Modules/Map/MapModule.cs:32 | effective `InsertSatelliteCommand` span 33-64 | Auth gate: AuthManager.EnsureAuthenticated (trong thân handler) | ActivityLogger.Result @ src/Auth/ActivityLogger.cs:57-58 | type `AuthManager` file `src/Auth/AuthManager.cs` (664 dòng) — chưa khớp method call cụ thể

## Inputs
Yêu cầu Model Space (handler)

## Outputs / hậu điều kiện
Editor.WriteMessage phản hồi người dùng

## Errors
CHƯA ĐỦ BẰNG CHỨNG nhánh lỗi đầy đủ

## Destructive
không thấy Delete/Erase/Purge trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Unresolved
CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `AuthManager` từ handler; không tìm thấy file cho type `DocumentManager`
