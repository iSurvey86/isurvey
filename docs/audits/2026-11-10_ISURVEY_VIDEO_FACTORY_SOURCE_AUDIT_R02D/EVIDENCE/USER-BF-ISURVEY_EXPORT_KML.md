# Evidence `USER-BF-ISURVEY_EXPORT_KML`

- **file:** `src/Modules/Export/ExportModule.cs`
- **lines:** 23-125
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler ExportKmlCommand

```csharp
{
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        var ed = doc.Editor;
        var db = doc.Database;

        if (!MapModule.IsModelSpace(db))
        {
            ed.WriteMessage("\n[iSurvey] Hãy mở Model Space trước khi xuất.");
            return;
        }

        var drawingPath = string.IsNullOrWhiteSpace(db.Filename) ? null : db.Filename;

        Vn2000SetupResult? setup;
        try
        {
            setup = Vn2000SetupDialog.Show(drawingPath, Vn2000SetupMode.Export);
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] Không mở được hộp thoại: {ex.Message}");
            return;
        }

        if (setup is null)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy xuất.");
            return;
        }

        if (Math.Abs(setup.CentralMeridian - setup.EnteredMeridian) > 1e-6)
        {
            ed.WriteMessage(
                $"\n[iSurvey] Múi 6°: kinh tuyến {setup.EnteredMeridian:0.###}° được đưa về {setup.CentralMeridian:0.#}°.");
        }

        var settings = new KmlExportSettings
        {
            CentralMeridian = setup.CentralMeridian,
            ZoneWidthDegrees = setup.ZoneWidthDegrees,
            SelectionOnly = setup.SelectionOnly,
            UseKmz = setup.UseKmz,
            GroupByLayer = setup.GroupByLayer,
            UseElevationZ = setup.UseElevationZ,
            OpenAfterExport = setup.OpenAfterExport
        };

        KmlExportService.KmlCollected collected;
        try
        {
            collected = _service.Collect(doc, settings);
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] Xuất thất bại: {ex.Message}");
            iSurvey.Auth.ActivityLogger.TrackError(ex);
            return;
        }

        if (collected.Cancelled)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy xuất.");
            return;
        }

        if (collected.Features.Count == 0)
        {
            ed.WriteMessage($"\n[iSurvey] Không có hình học nào xuất được ({collected.Report}).");
            return;
        }

        var path = AskOutputPath(setup.UseKmz);
        if (path is null)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy xuất.");
            return;
        }

        settings.OutputPath = path;

        try
        {
            ed.WriteMessage(
                $"\n[iSurvey] Đang xuất (kinh tuyến {settings.CentralMeridian:0.##}°, TM-{settings.ZoneWidthDegrees})...");
            _service.Write(db, settings, collected.Features);
            ed.WriteMessage($"\n[iSurvey] Đã lưu: {settings.OutputPath}");
            ed.WriteMessage($"\n[iSurvey] {collected.Report}");
            ActivityLogger.Result("ISURVEY_EXPORT_KML",
                $"file {ActivityLogger.FileName(settings.OutputPath)}, {collected.Features.Count} đối tượng, TM-{settings.ZoneWidthDegrees} kinh tuyến {settings.CentralMeridian:0.##}°, từ bản vẽ {ActivityLogger.FileName(drawingPath)}");
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] Xuất thất bại: {ex.Message}");
            iSurvey.Auth.ActivityLogger.TrackError(ex);
        }
    }
```
