# BF-ISURVEY_IMPORT_KML — ISURVEY_IMPORT_KML

- **source_status:** SOURCE_PRESENT
- **evidence:** `HANDLER_TRACE_COMPLETE`
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_IMPORT_KML CommandMethod @ src/Modules/Import/KmlImportModule.cs:21 | effective `ImportKmlCommand` span 22-130 | Auth gate: AuthManager.EnsureAuthenticated (trong thân handler) | ActivityLogger.Result @ src/Auth/ActivityLogger.cs:57-58 | ActivityLogger.TrackError @ src/Auth/ActivityLogger.cs:81-82 | type `AuthManager` file `src/Auth/AuthManager.cs` (664 dòng) — chưa khớp method call cụ thể | type `CoordinateService` file `src/Modules/Map/CoordinateService.cs` (234 dòng) — chưa khớp method call cụ thể | KmlDrawService.FitImported @ src/Modules/Import/KmlDrawService.cs:261-288 | type `KmlReader` file `src/Modules/Import/KmlReader.cs` (140 dòng) — chưa khớp method call cụ thể

## Inputs
Yêu cầu Model Space (handler)

## Outputs / hậu điều kiện
Editor.WriteMessage phản hồi người dùng

## Errors
có catch trong handler span 22-130; KmlDrawService.FitImported: có catch @ src/Modules/Import/KmlDrawService.cs:261-288

## Destructive
không thấy Delete/Erase/Purge trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Unresolved
không tìm thấy file cho type `AcadWindow`; CHƯA ĐỦ BẰNG CHỨNG: không mở được ActivityLogger.FileName trong src/Auth/ActivityLogger.cs; CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `AuthManager` từ handler; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `CoordinateService` từ handler; không tìm thấy file cho type `DocumentManager`; CHƯA ĐỦ BẰNG CHỨNG: không mở được KmlDrawService.Draw trong src/Modules/Import/KmlDrawService.cs; CHƯA ĐỦ BẰNG CHỨNG: không mở được KmlReader.Read trong src/Modules/Import/KmlReader.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `KmlReader` từ handler; không tìm thấy file cho type `MainWindow`
