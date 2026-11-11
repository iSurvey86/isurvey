# BF-ISURVEY_TAB_TO_CAD — ISURVEY_TAB_TO_CAD

- **source_status:** SOURCE_PRESENT
- **evidence:** `HANDLER_TRACE_COMPLETE`
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_TAB_TO_CAD CommandMethod @ src/Modules/Import/TabImportModule.cs:29 | effective `TabToCadCommand` span 30-82 | Auth gate: AuthManager.EnsureAuthenticated (trong thân handler) | ActivityLogger.Result @ src/Auth/ActivityLogger.cs:57-58 | ActivityLogger.TrackError @ src/Auth/ActivityLogger.cs:81-82 | type `AuthManager` file `src/Auth/AuthManager.cs` (664 dòng) — chưa khớp method call cụ thể | type `MapModule` file `src/Modules/Map/MapModule.cs` (311 dòng) — chưa khớp method call cụ thể | type `UserSettingsStore` file `src/Core/UserSettingsStore.cs` (101 dòng) — chưa khớp method call cụ thể

## Inputs
Yêu cầu Model Space (handler)

## Outputs / hậu điều kiện
Editor.WriteMessage phản hồi người dùng

## Errors
có catch trong handler span 30-82

## Destructive
không thấy Delete/Erase/Purge trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Unresolved
CHƯA ĐỦ BẰNG CHỨNG: không mở được ActivityLogger.FileName trong src/Auth/ActivityLogger.cs; CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `AuthManager` từ handler; không tìm thấy file cho type `DocumentManager`; CHƯA ĐỦ BẰNG CHỨNG: không mở được MapModule.IsModelSpace trong src/Modules/Map/MapModule.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `MapModule` từ handler; không tìm thấy file cho type `ShowAlertDialog`; không tìm thấy file cho type `TabImportWindow`; CHƯA ĐỦ BẰNG CHỨNG: không mở được UserSettingsStore.Load trong src/Core/UserSettingsStore.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `UserSettingsStore` từ handler
