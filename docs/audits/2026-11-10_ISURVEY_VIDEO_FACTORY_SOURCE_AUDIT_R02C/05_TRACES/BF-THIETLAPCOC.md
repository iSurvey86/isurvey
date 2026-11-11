# BF-THIETLAPCOC — THIETLAPCOC

- **source_status:** SOURCE_PRESENT
- **evidence:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** NOT_TESTED

## Call chain
THIETLAPCOC CommandMethod @ src/Modules/Tuyen/RouteModule.cs:41 | effective `StationStyle` span 62-90 | Auth gate: AuthManager.EnsureAuthenticated (trong thân handler) | type `AuthManager` file `src/Auth/AuthManager.cs` (664 dòng) — chưa khớp method call cụ thể | StationStyleStore.Save @ src/Modules/Tuyen/StationStyleStore.cs:30-44

## Inputs
CHƯA ĐỦ BẰNG CHỨNG đủ điều kiện tiên quyết

## Outputs / hậu điều kiện
Editor.WriteMessage phản hồi người dùng

## Errors
có catch trong handler span 62-90; StationStyleStore.Save: có catch @ src/Modules/Tuyen/StationStyleStore.cs:30-44

## Destructive
không thấy Delete/Erase/Purge trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Unresolved
CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `AuthManager` từ handler; không tìm thấy file cho type `DocumentManager`; CHƯA ĐỦ BẰNG CHỨNG: không mở được StationStyleStore.Load trong src/Modules/Tuyen/StationStyleStore.cs; không tìm thấy file cho type `StationStyleWindow`
