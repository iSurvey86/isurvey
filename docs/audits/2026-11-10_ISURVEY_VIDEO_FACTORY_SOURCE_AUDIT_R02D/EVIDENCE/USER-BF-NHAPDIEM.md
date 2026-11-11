# Evidence `USER-BF-NHAPDIEM`

- **file:** `src/Modules/DiemDo/PointFileModule.cs`
- **lines:** 14-60
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler ImportPointsCommand

```csharp
{
        if (!AuthManager.EnsureAuthenticated("NHAPDIEM"))
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;
        if (!IsModelSpace())
        {
            doc.Editor.WriteMessage("\n[iSurvey] Hãy mở Model Space trước khi nhập điểm.");
            return;
        }

        ImportPointsRequest? request;
        try
        {
            request = ImportPointsWindow.Prompt();
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage($"\n[iSurvey] Không mở được hộp thoại: {ex.Message}");
            return;
        }

        if (request is null)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy nhập điểm.");
            return;
        }

        try
        {
            var made = PointDrawService.Draw(
                doc, request.Points, request.TextHeight, request.PointMode, request.PointSizePercent);
            doc.Editor.WriteMessage($"\n[iSurvey] Đã tạo {made} điểm trên layer {iSurveyLayers.DiemDo}.");
            if (made > 0)
            {
                ZoomExtents(doc.Editor);
                ActivityLogger.Result("NHAPDIEM", $"đã tạo {made} điểm trên layer {iSurveyLayers.DiemDo}");
            }
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage($"\n[iSurvey] Không tạo được điểm: {ex.Message}");
        }
    }
```
