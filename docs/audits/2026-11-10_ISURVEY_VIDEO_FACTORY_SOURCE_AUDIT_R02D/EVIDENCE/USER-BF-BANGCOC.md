# Evidence `USER-BF-BANGCOC`

- **file:** `src/Modules/Tuyen/RouteModule.cs`
- **lines:** 368-415
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler ListStations

```csharp
{
        if (!AuthManager.EnsureAuthenticated("BANGCOC"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null || !ModelSpace(doc.Editor))
            return;
        var route = RouteService.Choose(doc.Editor, doc.Database);
        if (route is null)
            return;

        while (true)
        {
            StationListResult result;
            try
            {
                result = StationListWindow.Prompt(doc, route);
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage("\n[iSurvey] Không mở được danh sách cọc: " + ex.Message);
                return;
            }

            SurveyPaletteManager.Reload();
            if (!result.AddRequested)
                return;

            var point = doc.Editor.GetPoint(new PromptPointOptions("\nChọn vị trí thêm cọc (gần tim tuyến): "));
            if (point.Status != PromptStatus.OK)
            {
                doc.Editor.WriteMessage("\n[iSurvey] Đã hủy thêm cọc.");
                return;
            }

            if (!RouteService.TryAddStationAtPoint(doc, route, point.Value, replaceNear: true, out var chainage, out var missingZ))
            {
                doc.Editor.WriteMessage("\n[iSurvey] Không thêm được cọc tại điểm đã chọn.");
                continue;
            }

            var msg = "\n[iSurvey] Đã thêm cọc " + RouteService.FormatChainage(chainage) + ".";
            if (missingZ > 0)
                msg += " Chưa lấy được Z từ mô hình.";
            doc.Editor.WriteMessage(msg);
            // Mở lại bảng sau khi thêm.
        }
    }
```
