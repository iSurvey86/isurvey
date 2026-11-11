# Evidence `USER-BF-ANTIN`

- **file:** `src/Modules/Terrain/TerrainModule.cs`
- **lines:** 273-301
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler Toggle

```csharp
{
        if (!AuthManager.EnsureAuthenticated("ANTIN"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;
        var pending = TerrainContext.ModelId;
        if (string.IsNullOrWhiteSpace(pending))
        {
            var models = TerrainStore.Load(doc.Database);
            if (models.Count == 0)
            {
                doc.Editor.WriteMessage("\n[iSurvey] Chưa có mô hình để ẩn hoặc hiện lưới.");
                return;
            }

            foreach (var model in models)
                Flip(doc, model);
            SurveyPaletteManager.Reload();
            return;
        }

        var one = TerrainService.Choose(doc.Editor, doc.Database);
        if (one is null)
            return;
        Flip(doc, one);
        SurveyPaletteManager.Reload();
    }
```
