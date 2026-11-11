# BF-TRACAOD — TRACAOD

- **source_status:** SOURCE_PRESENT
- **evidence:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** NOT_TESTED

## Call chain
TRACAOD CommandMethod @ src/Modules/Terrain/TerrainModule.cs:41 | effective `Query` span 303-344 | Auth gate: AuthManager.EnsureAuthenticated (trong thân handler) | type `AuthManager` file `src/Auth/AuthManager.cs` (664 dòng) — chưa khớp method call cụ thể | TerrainService.EraseElevationMark @ src/Modules/Terrain/TerrainService.cs:693-710

## Inputs
CHƯA ĐỦ BẰNG CHỨNG đủ điều kiện tiên quyết

## Outputs / hậu điều kiện
Editor.WriteMessage phản hồi người dùng; TerrainService.EraseElevationMark: có ghi/xuất/CAD commit (span 693-710)

## Errors
TerrainService.EraseElevationMark: có catch @ src/Modules/Terrain/TerrainService.cs:693-710

## Destructive
TerrainService.EraseElevationMark: dấu hiệu destructive @ src/Modules/Terrain/TerrainService.cs

## Unresolved
CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `AuthManager` từ handler; không tìm thấy file cho type `DocumentManager`; CHƯA ĐỦ BẰNG CHỨNG: không mở được TerrainService.Choose trong src/Modules/Terrain/TerrainService.cs; CHƯA ĐỦ BẰNG CHỨNG: không mở được TerrainService.ShowElevationMark trong src/Modules/Terrain/TerrainService.cs; CHƯA ĐỦ BẰNG CHỨNG: không mở được TerrainService.TryQuery trong src/Modules/Terrain/TerrainService.cs
