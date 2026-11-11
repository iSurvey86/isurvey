# Evidence `SC10-BF-ISURVEY_CAD_TO_DGN-CadToDgnCommand`

- **file:** `src/Dgn/DgnModule.cs`
- **lines:** 88-146
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** SOURCE_PARTIAL_VERIFIED

```csharp
{
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        var ed = doc.Editor;
        if (!MapModule.IsModelSpace(doc.Database))
        {
            ed.WriteMessage("\n[iSurvey] Hãy mở Model Space trước khi xuất DGN.");
            return;
        }

        // Chụp vùng chọn (PICKFIRST) NGAY, trước khi mở hộp thoại.
        ObjectId[] selected = [];
        try
        {
            var picked = ed.SelectImplied();
            if (picked.Status == PromptStatus.OK)
                selected = picked.Value.GetObjectIds();
        }
        catch
        {
            // không có vùng chọn
        }

        DgnExportOptions? options;
        try
        {
            var drawingPath = string.IsNullOrWhiteSpace(doc.Database.Filename) ? null : doc.Database.Filename;
            options = DgnExportDialog.ShowExport(drawingPath, selected.Length);
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] Không mở được hộp thoại xuất DGN: {ex.Message}");
            return;
        }

        if (options is null)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy xuất DGN.");
            ActivityLogger.Result("ISURVEY_CAD_TO_DGN", "đã hủy, chưa xuất");
            return;
        }

        options.SelectedIds = selected;
        var exportProgress = TaskProgressWindow.ShowNew(
            "iSurvey — Xuất DGN",
            "Đang xuất CAD ➔ DGN",
            "Chuẩn bị — " + Path.GetFileName(options.OutputPath));
        DgnExportService.Start(doc, options, report =>
        {
            exportProgress.ForceClose();
            WriteExportSummary(AcadApp.DocumentManager.MdiActiveDocument?.Editor ?? ed, report);
        }, exportProgress);
    }
```
