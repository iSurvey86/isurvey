# Evidence `USER-BF-AUTOSCAN`

- **file:** `src/Modules/Scan/ScanModule.cs`
- **lines:** 26-88
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler AutoScanCommand

```csharp
{
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        var ed = doc.Editor;
        var db = doc.Database;

        var pso = new PromptSelectionOptions
        {
            MessageForAdding = "\niSurvey: Quét chọn vùng cần phân tích (gõ ALL để chọn hết): "
        };
        var ps = ed.GetSelection(pso);
        if (ps.Status != PromptStatus.OK)
        {
            ed.WriteMessage("\n[iSurvey] Da huy AUTOSCAN.");
            return;
        }

        var dialog = new Win32SaveFileDialog
        {
            Title = "Luu bao cao AutoScan",
            Filter = "Text (*.txt)|*.txt",
            DefaultExt = ".txt",
            AddExtension = true,
            FileName = "CAD_AutoScan_Report.txt"
        };
        if (!string.IsNullOrWhiteSpace(db.Filename) && Path.IsPathRooted(db.Filename))
            dialog.InitialDirectory = Path.GetDirectoryName(db.Filename);

        if (dialog.ShowDialog() != true)
        {
            ed.WriteMessage("\n[iSurvey] Da huy AUTOSCAN.");
            return;
        }

        try
        {
            string text;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var name = string.IsNullOrWhiteSpace(db.Filename) ? "(chua luu)" : db.Filename;
                text = new ScanReport().Build(db, tr, ps.Value.GetObjectIds(), name);
                tr.Commit();
            }

            File.WriteAllText(dialog.FileName, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            ed.WriteMessage($"\n[iSurvey] Da luu bao cao: {dialog.FileName}");
            ActivityLogger.Result("AUTOSCAN",
                $"báo cáo {ActivityLogger.FileName(dialog.FileName)}, {ps.Value.Count} đối tượng, bản vẽ {ActivityLogger.FileName(db.Filename)}");

            Process.Start(new ProcessStartInfo("notepad.exe", $"\"{dialog.FileName}\"") { UseShellExecute = true });
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] AUTOSCAN that bai: {ex.Message}");
            iSurvey.Auth.ActivityLogger.TrackError(ex);
        }
    }
```
