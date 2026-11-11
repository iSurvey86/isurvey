# BF-ISURVEY_ROUTE_PLOT — ISURVEY_ROUTE_PLOT

- **source_status:** SOURCE_PRESENT
- **evidence:** `HANDLER_TRACE_COMPLETE`
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_ROUTE_PLOT CommandMethod @ src/Modules/Frame/FrameService.RouteLayout.cs:33 | effective `RoutePlotCommand` span 34-50 | Auth gate: AuthManager.EnsureAuthenticated (trong thân handler) | ActivityLogger.TrackError @ src/Auth/ActivityLogger.cs:81-82 | type `AuthManager` file `src/Auth/AuthManager.cs` (664 dòng) — chưa khớp method call cụ thể

## Inputs
CHƯA ĐỦ BẰNG CHỨNG đủ điều kiện tiên quyết

## Outputs / hậu điều kiện
Editor.WriteMessage phản hồi người dùng

## Errors
có catch trong handler span 34-50

## Destructive
không thấy Delete/Erase/Purge trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Unresolved
CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `AuthManager` từ handler; không tìm thấy file cho type `DocumentManager`
