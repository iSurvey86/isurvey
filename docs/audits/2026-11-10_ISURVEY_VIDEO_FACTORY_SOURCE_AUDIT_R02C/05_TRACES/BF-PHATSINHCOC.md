# BF-PHATSINHCOC — PHATSINHCOC

- **source_status:** SOURCE_PRESENT
- **evidence:** `HANDLER_TRACE_COMPLETE`
- **runtime:** NOT_TESTED

## Call chain
PHATSINHCOC CommandMethod @ src/Modules/Tuyen/RouteModule.cs:23 | effective `Stations` span 194-262 | Auth gate: AuthManager.EnsureAuthenticated (trong thân handler) | type `AuthManager` file `src/Auth/AuthManager.cs` (664 dòng) — chưa khớp method call cụ thể | type `RouteService` file `src/Modules/Tuyen/RouteService.cs` (806 dòng) — chưa khớp method call cụ thể | SurveyPaletteManager.Reload @ src/UI/Palette/SurveyPaletteManager.cs:20-20

## Inputs
CHƯA ĐỦ BẰNG CHỨNG đủ điều kiện tiên quyết

## Outputs / hậu điều kiện
Editor.WriteMessage phản hồi người dùng

## Errors
có catch trong handler span 194-262

## Destructive
không thấy Delete/Erase/Purge trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Unresolved
CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `AuthManager` từ handler; không tìm thấy file cho type `CreateStationsWindow`; không tìm thấy file cho type `DocumentManager`; CHƯA ĐỦ BẰNG CHỨNG: không mở được RouteService.BuildStations trong src/Modules/Tuyen/RouteService.cs; CHƯA ĐỦ BẰNG CHỨNG: không mở được RouteService.Choose trong src/Modules/Tuyen/RouteService.cs; CHƯA ĐỦ BẰNG CHỨNG: không mở được RouteService.TryLength trong src/Modules/Tuyen/RouteService.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `RouteService` từ handler
