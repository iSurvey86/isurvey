# Evidence `USER-BF-ISURVEY_TAB_TO_CAD`

- **file:** `src/Modules/Import/TabImportModule.cs`
- **lines:** 30-82
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler TabToCadCommand

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
            ed.WriteMessage("\n[iSurvey] Hãy mở Model Space trước khi nhập TAB.");
            return;
        }

        var drawingPath = string.IsNullOrWhiteSpace(doc.Database.Filename) ? null : doc.Database.Filename;
        var settings = UserSettingsStore.Load(drawingPath);

        TabImportOptions? options;
        try
        {
            options = TabImportWindow.Show(settings);
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] Không mở được hộp thoại TAB: {ex.Message}");
            return;
        }

        if (options is null)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy nhập TAB.");
            return;
        }

        try
        {
            var stats = Import(doc.Database, ed, options, settings);
            ZoomExtents(ed);
            var report = BuildReport(stats, options);
            ActivityLogger.Result("ISURVEY_TAB_TO_CAD",
                $"file {ActivityLogger.FileName(options.Path)}, {stats.Hatches} hatch, {stats.Outlines} viền, {stats.Labels} nhãn, lỗi {stats.Errors}, bản vẽ {ActivityLogger.FileName(drawingPath)}");
            foreach (var line in report.Split('\n'))
                ed.WriteMessage("\n[iSurvey] " + line.TrimEnd('\r'));
            AcadApp.ShowAlertDialog(report);
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] Nhập TAB thất bại: {ex.Message}");
            ActivityLogger.TrackError(ex);
        }
    }
```
