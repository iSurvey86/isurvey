# E-TAOTUYEN-NEW — TAOTUYEN New() start

- **file:** `src/Modules/Tuyen/RouteModule.cs`
- **lines:** 92-160
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** Dialog + pick polyline

```csharp
    private static void New()
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
```
