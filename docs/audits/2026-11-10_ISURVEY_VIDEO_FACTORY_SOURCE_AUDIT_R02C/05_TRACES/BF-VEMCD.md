# BF-VEMCD — VEMCD

- **source_status:** SOURCE_PRESENT
- **evidence:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** NOT_TESTED

## Call chain
VEMCD CommandMethod @ src/Modules/Profile/ProfileModule.cs:17 | effective `Draw` span 20-147 | Auth gate: AuthManager.EnsureAuthenticated (trong thân handler) | ActivityLogger.TrackError @ src/Auth/ActivityLogger.cs:81-82 | type `AuthManager` file `src/Auth/AuthManager.cs` (664 dòng) — chưa khớp method call cụ thể | type `ProfileDrawService` file `src/Modules/Profile/ProfileDrawService.cs` (1143 dòng) — chưa khớp method call cụ thể | type `ProfileSampleService` file `src/Modules/Profile/ProfileSampleService.cs` (278 dòng) — chưa khớp method call cụ thể | ProfileSettingsStore.Save @ src/Modules/Profile/ProfileSettingsStore.cs:61-68 | type `RouteStore` file `src/Modules/Tuyen/RouteStore.cs` (113 dòng) — chưa khớp method call cụ thể

## Inputs
Yêu cầu Model Space (handler)

## Outputs / hậu điều kiện
Editor.WriteMessage phản hồi người dùng

## Errors
có catch trong handler span 20-147

## Destructive
không thấy Delete/Erase/Purge trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Unresolved
CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `AuthManager` từ handler; không tìm thấy file cho type `DocumentManager`; không tìm thấy file cho type `DrawProfileWindow`; CHƯA ĐỦ BẰNG CHỨNG: không mở được ProfileDrawService.SuggestDatum trong src/Modules/Profile/ProfileDrawService.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `ProfileDrawService` từ handler; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `ProfileSampleService` từ handler; CHƯA ĐỦ BẰNG CHỨNG: không mở được ProfileSettingsStore.Load trong src/Modules/Profile/ProfileSettingsStore.cs; CHƯA ĐỦ BẰNG CHỨNG: không mở được RouteStore.Load trong src/Modules/Tuyen/RouteStore.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `RouteStore` từ handler
