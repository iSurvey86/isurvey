# Evidence `USER-BF-XOACOC`

- **file:** `src/Modules/Tuyen/RouteModule.cs`
- **lines:** 282-301
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler ClearStations

```csharp
{
        if (!AuthManager.EnsureAuthenticated("XOACOC"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null || !ModelSpace(doc.Editor))
            return;
        var route = RouteService.Choose(doc.Editor, doc.Database);
        if (route is null)
            return;
        if (route.Stations is null || route.Stations.Count == 0)
        {
            doc.Editor.WriteMessage("\n[iSurvey] \"" + route.Name + "\" chưa có cọc.");
            return;
        }

        var removed = RouteService.ClearStations(doc, route);
        doc.Editor.WriteMessage("\n[iSurvey] Đã xóa " + removed + " cọc của \"" + route.Name + "\".");
        SurveyPaletteManager.Reload();
    }
```
