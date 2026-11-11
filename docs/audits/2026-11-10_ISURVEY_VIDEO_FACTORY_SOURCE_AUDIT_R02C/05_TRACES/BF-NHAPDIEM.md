# BF-NHAPDIEM — NHAPDIEM

- **source_status:** SOURCE_PRESENT
- **evidence:** `HANDLER_TRACE_COMPLETE`
- **runtime:** NOT_TESTED

## Call chain
NHAPDIEM CommandMethod @ src/Modules/DiemDo/PointFileModule.cs:13 | effective `ImportPointsCommand` span 14-60 | Auth gate: AuthManager.EnsureAuthenticated (trong thân handler) | ActivityLogger.Result @ src/Auth/ActivityLogger.cs:57-58 | type `AuthManager` file `src/Auth/AuthManager.cs` (664 dòng) — chưa khớp method call cụ thể | type `PointDrawService` file `src/Modules/DiemDo/PointDrawService.cs` (155 dòng) — chưa khớp method call cụ thể

## Inputs
Yêu cầu Model Space (handler)

## Outputs / hậu điều kiện
Editor.WriteMessage phản hồi người dùng

## Errors
có catch trong handler span 14-60

## Destructive
không thấy Delete/Erase/Purge trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Unresolved
CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `AuthManager` từ handler; không tìm thấy file cho type `DocumentManager`; không tìm thấy file cho type `ImportPointsWindow`; CHƯA ĐỦ BẰNG CHỨNG: không mở được PointDrawService.Draw trong src/Modules/DiemDo/PointDrawService.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `PointDrawService` từ handler
