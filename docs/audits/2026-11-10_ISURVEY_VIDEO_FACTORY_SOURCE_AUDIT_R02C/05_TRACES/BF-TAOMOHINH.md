# BF-TAOMOHINH — TAOMOHINH

- **source_status:** SOURCE_PRESENT
- **evidence:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** NOT_TESTED

## Call chain
TAOMOHINH CommandMethod @ src/Modules/Terrain/TerrainModule.cs:17 | effective `New` span 56-100 | Auth gate: AuthManager.EnsureAuthenticated (trong thân handler) | type `AuthManager` file `src/Auth/AuthManager.cs` (664 dòng) — chưa khớp method call cụ thể | SurveyPaletteManager.Reload @ src/UI/Palette/SurveyPaletteManager.cs:20-20 | TerrainStore.Save @ src/Modules/Terrain/TerrainStore.cs:73-100

## Inputs
CHƯA ĐỦ BẰNG CHỨNG đủ điều kiện tiên quyết

## Outputs / hậu điều kiện
Editor.WriteMessage phản hồi người dùng; TerrainStore.Save: có ghi/xuất/CAD commit (span 73-100)

## Errors
có catch trong handler span 56-100

## Destructive
không thấy Delete/Erase/Purge trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Unresolved
CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `AuthManager` từ handler; không tìm thấy file cho type `CreateTerrainWindow`; không tìm thấy file cho type `DocumentManager`; CHƯA ĐỦ BẰNG CHỨNG: không mở được TerrainStore.Load trong src/Modules/Terrain/TerrainStore.cs
