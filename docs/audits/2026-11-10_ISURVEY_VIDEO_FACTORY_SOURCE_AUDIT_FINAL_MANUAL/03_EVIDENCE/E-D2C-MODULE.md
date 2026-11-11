# E-D2C-MODULE — D2C command entry

- **file:** `src/Dgn/DgnModule.cs`
- **lines:** 21-84
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** Auth, Model Space, dialog, Start/Batch

```csharp
    [CommandMethod("ISURVEY_DGN_TO_CAD")]
    public void DgnToCadCommand()
    {
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        var ed = doc.Editor;
        if (!MapModule.IsModelSpace(doc.Database))
        {
            ed.WriteMessage("\n[iSurvey] Hãy mở Model Space trước khi nhập DGN.");
            return;
        }

        DgnImportOptions? options = TryLoadProgressSeed();
        if (options is null)
        {
            try
            {
                var drawingPath = string.IsNullOrWhiteSpace(doc.Database.Filename) ? null : doc.Database.Filename;
                options = DgnImportDialog.ShowImport(drawingPath);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[iSurvey] Không mở được hộp thoại DGN: {ex.Message}");
                return;
            }
        }

        if (options is null)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy nhập DGN.");
            ActivityLogger.Result("ISURVEY_DGN_TO_CAD", "đã hủy, chưa chọn file");
            return;
        }

        if (options.IsBatch)
        {
            var batchProgress = TaskProgressWindow.ShowNew(
                "iSurvey — Nhập DGN",
                options.BatchCombine ? "Đang ghép nhiều file DGN" : "Đang nhập DGN (rời từng file)",
                $"Chuẩn bị {options.BatchFiles.Count} tệp…");
            DgnBatchService.Start(doc, options, (results, combined) =>
            {
                batchProgress.ForceClose();
                WriteBatchSummary(AcadApp.DocumentManager.MdiActiveDocument?.Editor ?? ed, results, combined);
            }, batchProgress);
            return;
        }

        // DGNIMPORT hoàn tất bất đồng bộ: Start trả về ngay, báo cáo in ra khi hậu xử lý xong.
        var progress = TaskProgressWindow.ShowNew(
            "iSurvey — Nhập DGN",
            "Đang nhập DGN",
            "Chuẩn bị — " + Path.GetFileName(options.FilePath));
        DgnImportService.Start(doc, options, report =>
        {
            progress.ForceClose();
            WriteSummary(AcadApp.DocumentManager.MdiActiveDocument?.Editor ?? ed, report, options.FilePath);
        }, progress);
    }
```
