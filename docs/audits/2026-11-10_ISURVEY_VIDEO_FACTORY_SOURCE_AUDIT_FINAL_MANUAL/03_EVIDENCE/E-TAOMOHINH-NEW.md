# E-TAOMOHINH-NEW — TAOMOHINH New()

- **file:** `src/Modules/Terrain/TerrainModule.cs`
- **lines:** 56-100
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** Dialog → Save

```csharp
    private static void New()
    {
        if (!AuthManager.EnsureAuthenticated("TAOMOHINH"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null || !ModelSpace(doc.Editor))
            return;

        var count = 1;
        try
        {
            count = TerrainStore.Load(doc.Database).Count + 1;
        }
        catch (System.Exception)
        {
            count = 1;
        }

        CreateTerrainRequest? request;
        try
        {
            request = CreateTerrainWindow.Prompt("Mô hình " + count);
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Không mở được hộp thoại: " + ex.Message);
            return;
        }

        if (request is null)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy tạo mô hình.");
            return;
        }

        var model = new TerrainRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = request.Name,
            Layer = request.Layer
        };
        TerrainStore.Save(doc.Database, model);
        doc.Editor.WriteMessage("\n[iSurvey] Đã tạo mô hình \"" + model.Name + "\". Hãy thêm điểm, đứt gãy hoặc đồng mức, và đường bao, rồi xây dựng.");
        SurveyPaletteManager.Reload();
    }
```
