# BF-ISURVEY_EXPORT_KML — ISURVEY_EXPORT_KML

- **source_status:** SOURCE_PRESENT
- **evidence:** `HANDLER_TRACE_COMPLETE`
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_EXPORT_KML CommandMethod @ src/Modules/Export/ExportModule.cs:22 | effective `ExportKmlCommand` span 23-125 | Auth gate: AuthManager.EnsureAuthenticated (trong thân handler) | ActivityLogger.Result @ src/Auth/ActivityLogger.cs:57-58 | ActivityLogger.TrackError @ src/Auth/ActivityLogger.cs:81-82 | type `AuthManager` file `src/Auth/AuthManager.cs` (664 dòng) — chưa khớp method call cụ thể | type `KmlExportService` file `src/Modules/Export/KmlExportService.cs` (104 dòng) — chưa khớp method call cụ thể | type `MapModule` file `src/Modules/Map/MapModule.cs` (311 dòng) — chưa khớp method call cụ thể | type `Vn2000SetupDialog` file `src/UI/Vn2000/Vn2000SetupDialog.cs` (17 dòng) — chưa khớp method call cụ thể

## Inputs
Yêu cầu Model Space (handler)

## Outputs / hậu điều kiện
Editor.WriteMessage phản hồi người dùng

## Errors
có catch trong handler span 23-125

## Destructive
không thấy Delete/Erase/Purge trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Unresolved
CHƯA ĐỦ BẰNG CHỨNG: không mở được ActivityLogger.FileName trong src/Auth/ActivityLogger.cs; CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `AuthManager` từ handler; không tìm thấy file cho type `DocumentManager`; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `KmlExportService` từ handler; CHƯA ĐỦ BẰNG CHỨNG: không mở được MapModule.IsModelSpace trong src/Modules/Map/MapModule.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `MapModule` từ handler; CHƯA ĐỦ BẰNG CHỨNG: không mở được Vn2000SetupDialog.Show trong src/UI/Vn2000/Vn2000SetupDialog.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `Vn2000SetupDialog` từ handler
