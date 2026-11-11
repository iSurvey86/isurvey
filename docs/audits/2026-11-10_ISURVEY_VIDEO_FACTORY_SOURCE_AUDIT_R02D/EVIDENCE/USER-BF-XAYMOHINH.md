# Evidence `USER-BF-XAYMOHINH`

- **file:** `src/Modules/Terrain/TerrainModule.cs`
- **lines:** 165-186
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler Build

```csharp
{
        if (!AuthManager.EnsureAuthenticated("XAYMOHINH"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null || !ModelSpace(doc.Editor))
            return;
        var model = TerrainService.Choose(doc.Editor, doc.Database);
        if (model is null)
            return;
        try
        {
            var count = TerrainService.Build(doc, model);
            doc.Editor.WriteMessage("\n[iSurvey] Đã xây dựng " + count + " tam giác cho \"" + model.Name + "\".");
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Không xây dựng được: " + ex.Message);
        }

        SurveyPaletteManager.Reload();
    }
```
