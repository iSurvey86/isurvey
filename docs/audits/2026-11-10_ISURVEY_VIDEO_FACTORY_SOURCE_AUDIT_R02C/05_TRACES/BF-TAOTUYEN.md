# BF-TAOTUYEN — TAOTUYEN

- **source_status:** SOURCE_PRESENT
- **evidence:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** NOT_TESTED

## Call chain
TAOTUYEN CommandMethod @ src/Modules/Tuyen/RouteModule.cs:17 | effective `New` span 92-192 | Auth gate: AuthManager.EnsureAuthenticated (trong thân handler) | type `AuthManager` file `src/Auth/AuthManager.cs` (664 dòng) — chưa khớp method call cụ thể | type `RouteService` file `src/Modules/Tuyen/RouteService.cs` (806 dòng) — chưa khớp method call cụ thể | RouteStore.Save @ src/Modules/Tuyen/RouteStore.cs:60-87 | SurveyPaletteManager.Reload @ src/UI/Palette/SurveyPaletteManager.cs:20-20 | type `TerrainStore` file `src/Modules/Terrain/TerrainStore.cs` (137 dòng) — chưa khớp method call cụ thể

## Inputs
CHƯA ĐỦ BẰNG CHỨNG đủ điều kiện tiên quyết

## Outputs / hậu điều kiện
Editor.WriteMessage phản hồi người dùng; RouteStore.Save: có ghi/xuất/CAD commit (span 60-87)

## Errors
có catch trong handler span 92-192

## Destructive
không thấy Delete/Erase/Purge trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Unresolved
CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `AuthManager` từ handler; không tìm thấy file cho type `CreateRouteWindow`; không tìm thấy file cho type `DocumentManager`; CHƯA ĐỦ BẰNG CHỨNG: không mở được RouteService.ResolveStart trong src/Modules/Tuyen/RouteService.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `RouteService` từ handler; CHƯA ĐỦ BẰNG CHỨNG: không mở được RouteStore.Load trong src/Modules/Tuyen/RouteStore.cs; CHƯA ĐỦ BẰNG CHỨNG: không mở được TerrainStore.LastCreated trong src/Modules/Terrain/TerrainStore.cs; CHƯA ĐỦ BẰNG CHỨNG: không mở được TerrainStore.Load trong src/Modules/Terrain/TerrainStore.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `TerrainStore` từ handler; không tìm thấy file cho type `TransactionManager`
