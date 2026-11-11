# BF-XAYMOHINH — XAYMOHINH

- **source_status:** SOURCE_PRESENT
- **evidence:** `HANDLER_TRACE_COMPLETE`
- **runtime:** NOT_TESTED

## Call chain
XAYMOHINH CommandMethod @ src/Modules/Terrain/TerrainModule.cs:29 | effective `Build` span 165-186 | Auth gate: AuthManager.EnsureAuthenticated (trong thân handler) | type `AuthManager` file `src/Auth/AuthManager.cs` (664 dòng) — chưa khớp method call cụ thể | SurveyPaletteManager.Reload @ src/UI/Palette/SurveyPaletteManager.cs:20-20 | type `TerrainService` file `src/Modules/Terrain/TerrainService.cs` (1182 dòng) — chưa khớp method call cụ thể

## Inputs
CHƯA ĐỦ BẰNG CHỨNG đủ điều kiện tiên quyết

## Outputs / hậu điều kiện
Editor.WriteMessage phản hồi người dùng

## Errors
có catch trong handler span 165-186

## Destructive
không thấy Delete/Erase/Purge trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Unresolved
CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `AuthManager` từ handler; không tìm thấy file cho type `DocumentManager`; CHƯA ĐỦ BẰNG CHỨNG: không mở được TerrainService.Build trong src/Modules/Terrain/TerrainService.cs; CHƯA ĐỦ BẰNG CHỨNG: không mở được TerrainService.Choose trong src/Modules/Terrain/TerrainService.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `TerrainService` từ handler
