# Evidence `USER-BF-PHATSINHCOC`

- **file:** `src/Modules/Tuyen/RouteModule.cs`
- **lines:** 194-262
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler Stations

```csharp
{
        if (!AuthManager.EnsureAuthenticated("PHATSINHCOC"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null || !ModelSpace(doc.Editor))
            return;
        var route = RouteService.Choose(doc.Editor, doc.Database);
        if (route is null)
            return;
        if (!RouteService.TryLength(doc.Database, route, out var length))
        {
            doc.Editor.WriteMessage("\n[iSurvey] Không đọc được polyline tim tuyến.");
            return;
        }

        var from = route.StartChainage;
        var to = route.StartChainage + length;
        CreateStationsRequest? request;
        try
        {
            request = CreateStationsWindow.Prompt(route.Name, from, to);
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Không mở được hộp thoại: " + ex.Message);
            return;
        }

        if (request is null)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy phát sinh cọc.");
            return;
        }

        try
        {
            var count = RouteService.BuildStations(
                doc,
                route,
                request.FromChainage,
                request.ToChainage,
                request.Step,
                out var missingZ);
            if (count == 0)
            {
                doc.Editor.WriteMessage("\n[iSurvey] Không phát sinh được cọc trong khoảng lý trình đã chọn.");
                return;
            }

            var msg = "\n[iSurvey] Đã phát sinh " + count + " cọc trên \"" + route.Name + "\".";
            if (missingZ > 0)
            {
                msg += " " + missingZ + " cọc chưa lấy được Z từ mô hình.";
                if (string.IsNullOrWhiteSpace(route.TerrainId))
                    msg += " Hãy gắn mô hình khi tạo tuyến hoặc chỉ còn một mô hình đã dựng TIN.";
                else
                    msg += " Kiểm tra mô hình đã Xây dựng TIN và tuyến nằm trong đường bao.";
            }

            doc.Editor.WriteMessage(msg);
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Không phát sinh được cọc: " + ex.Message);
        }

        SurveyPaletteManager.Reload();
    }
```
