# BF-ISURVEY_DGN_TO_CAD — ISURVEY_DGN_TO_CAD

- **source_status:** SOURCE_PRESENT
- **evidence:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_DGN_TO_CAD CommandMethod @ src/Dgn/DgnModule.cs:21 | effective `DgnToCadCommand` span 22-84 | Auth gate: AuthManager.EnsureAuthenticated (trong thân handler) | ActivityLogger.Result @ src/Auth/ActivityLogger.cs:57-58 | type `AuthManager` file `src/Auth/AuthManager.cs` (664 dòng) — chưa khớp method call cụ thể | DgnBatchService.Start @ src/Dgn/DgnBatchService.cs:79-122 | type `DgnImportDialog` file `src/Dgn/DgnImportDialog.cs` (16 dòng) — chưa khớp method call cụ thể | DgnImportService.Start @ src/Dgn/DgnImportService.cs:112-180 | type `MapModule` file `src/Modules/Map/MapModule.cs` (311 dòng) — chưa khớp method call cụ thể | type `TaskProgressWindow` file `src/UI/TaskProgressWindow.cs` (142 dòng) — chưa khớp method call cụ thể

## Inputs
Yêu cầu Model Space (handler)

## Outputs / hậu điều kiện
Editor.WriteMessage phản hồi người dùng; DgnBatchService.Start: có ghi/xuất/CAD commit (span 79-122)

## Errors
có catch trong handler span 22-84; DgnImportService.Start: có catch @ src/Dgn/DgnImportService.cs:112-180

## Destructive
DgnImportService.Start: dấu hiệu destructive @ src/Dgn/DgnImportService.cs

## Unresolved
CHƯA ĐỦ BẰNG CHỨNG: không mở được AuthManager.EnsureAuthenticated trong src/Auth/AuthManager.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `AuthManager` từ handler; CHƯA ĐỦ BẰNG CHỨNG: không mở được DgnImportDialog.ShowImport trong src/Dgn/DgnImportDialog.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `DgnImportDialog` từ handler; không tìm thấy file cho type `DocumentManager`; CHƯA ĐỦ BẰNG CHỨNG: không mở được MapModule.IsModelSpace trong src/Modules/Map/MapModule.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `MapModule` từ handler; CHƯA ĐỦ BẰNG CHỨNG: không mở được TaskProgressWindow.ShowNew trong src/UI/TaskProgressWindow.cs; CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `TaskProgressWindow` từ handler
