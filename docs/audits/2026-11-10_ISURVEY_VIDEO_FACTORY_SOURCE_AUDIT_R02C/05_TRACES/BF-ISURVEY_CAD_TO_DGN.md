# BF-ISURVEY_CAD_TO_DGN — ISURVEY_CAD_TO_DGN

- **source_status:** SOURCE_PRESENT
- **evidence:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_CAD_TO_DGN CommandMethod @ src/Dgn/DgnModule.cs:87 | effective `CadToDgnCommand` span 88-146 | Auth gate: AuthManager.EnsureAuthenticated (trong thân handler) | ActivityLogger.Result @ src/Auth/ActivityLogger.cs:57-58 | type `AuthManager` file `src/Auth/AuthManager.cs` (664 dòng) — chưa khớp method call cụ thể | type `DgnExportDialog` file `src/Dgn/DgnExportDialog.cs` (16 dòng) — chưa khớp method call cụ thể | DgnExportService.Start @ src/Dgn/DgnExportService.cs:71-165 | type `MapModule` file `src/Modules/Map/MapModule.cs` (311 dòng) — chưa khớp method call cụ thể | type `TaskProgressWindow` file `src/UI/TaskProgressWindow.cs` (142 dòng) — chưa khớp method call cụ thể

## Inputs
Yêu cầu Model Space (handler)

## Outputs / hậu điều kiện
Editor.WriteMessage phản hồi người dùng

## Errors
có catch trong handler span 88-146; DgnExportService.Start: có catch @ src/Dgn/DgnExportService.cs:71-165

## Destructive
DgnExportService.Start: dấu hiệu destructive @ src/Dgn/DgnExportService.cs

## Unresolved
CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `AuthManager` từ handler; CHƯA ĐỦ BẰNG CHỨNG: không mở được DgnExportDialog.ShowExport trong src/Dgn/DgnExportDialog.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `DgnExportDialog` từ handler; không tìm thấy file cho type `DocumentManager`; CHƯA ĐỦ BẰNG CHỨNG: không mở được MapModule.IsModelSpace trong src/Modules/Map/MapModule.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `MapModule` từ handler; CHƯA ĐỦ BẰNG CHỨNG: không mở được TaskProgressWindow.ShowNew trong src/UI/TaskProgressWindow.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `TaskProgressWindow` từ handler
