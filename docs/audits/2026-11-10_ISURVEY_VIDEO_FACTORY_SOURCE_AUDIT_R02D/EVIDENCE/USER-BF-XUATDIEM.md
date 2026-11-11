# Evidence `USER-BF-XUATDIEM`

- **file:** `src/Modules/DiemDo/PointFileModule.cs`
- **lines:** 63-91
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler ExportPointsCommand

```csharp
{
        if (!AuthManager.EnsureAuthenticated("XUATDIEM"))
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        ExportPointsRequest? request;
        try
        {
            request = ExportPointsWindow.Prompt();
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage($"\n[iSurvey] Không mở được hộp thoại: {ex.Message}");
            return;
        }

        if (request is null)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy xuất điểm.");
            return;
        }

        doc.Editor.WriteMessage($"\n[iSurvey] Đã ghi {request.Count} điểm ra {request.FileName}.");
        ActivityLogger.Result("XUATDIEM", $"đã ghi {request.Count} điểm ra {request.FileName}");
    }
```
