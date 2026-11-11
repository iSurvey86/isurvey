# Evidence `SC10-BF-TAOTUYEN-New`

- **file:** `src/Modules/Tuyen/RouteModule.cs`
- **lines:** 92-192
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** SOURCE_BEHAVIOR_VERIFIED_STATIC

```csharp
{
        if (!AuthManager.EnsureAuthenticated("TAOTUYEN"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null || !ModelSpace(doc.Editor))
            return;

        var count = 1;
        try
        {
            count = RouteStore.Load(doc.Database).Count + 1;
        }
        catch (System.Exception)
        {
            count = 1;
        }

        var models = new List<RouteModelChoice>();
        string? preferredTerrainId = null;
        try
        {
            foreach (var model in TerrainStore.Load(doc.Database))
                models.Add(new RouteModelChoice { Id = model.Id, Name = model.Name });
            preferredTerrainId = TerrainStore.LastCreated(doc.Database)?.Id;
        }
        catch (System.Exception)
        {
            models.Clear();
            preferredTerrainId = null;
        }

        CreateRouteRequest? request;
        try
        {
            request = CreateRouteWindow.Prompt("Tuyến " + count, models, preferredTerrainId);
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Không mở được hộp thoại: " + ex.Message);
            return;
        }

        if (request is null)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy tạo tuyến.");
            return;
        }

        var picked = doc.Editor.GetEntity(new PromptEntityOptions("\nChọn polyline tim tuyến: ")
        {
            AllowNone = false
        });
        if (picked.Status != PromptStatus.OK)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy chọn tim tuyến.");
            return;
        }

        string handle;
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            if (tr.GetObject(picked.ObjectId, OpenMode.ForRead) is not Entity entity
                || entity is not (Polyline or Polyline2d or Polyline3d))
            {
                doc.Editor.WriteMessage("\n[iSurvey] Hãy chọn một polyline.");
                tr.Commit();
                return;
            }

            handle = entity.Handle.ToString();
            tr.Commit();
        }

        var reversed = RouteService.ResolveStart(doc.Editor, doc.Database, handle, out var startPt, out var endPt);
        if (reversed is null)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy chọn điểm đầu tuyến.");
            return;
        }

        var route = new RouteRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = request.Name,
            PolylineHandle = handle,
            StartChainage = request.StartChainage,
            TerrainId = request.TerrainId,
            Reversed = reversed.Value
        };
        RouteStore.Save(doc.Database, route);
        var chain = request.StartChainage.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        doc.Editor.WriteMessage(
            "\n[iSurvey] Đã tạo tuyến \"" + route.Name + "\", lý trình gốc " + chain
            + " m. Đầu (" + startPt.X.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
            + ", " + startPt.Y.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
            + ") → cuối (" + endPt.X.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
            + ", " + endPt.Y.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + ")"
            + (reversed.Value ? " (đảo so với chiều vẽ)." : "."));
        SurveyPaletteManager.Reload();
    }
```
