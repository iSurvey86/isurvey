# Evidence `USER-BF-ISURVEY_ROUTE_PLOT`

- **file:** `src/Modules/Frame/FrameService.RouteLayout.cs`
- **lines:** 34-50
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler RoutePlotCommand

```csharp
{
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;
        try
        {
            PlotExistingRoute(doc);
        }
        catch (System.Exception ex)
        {
            ActivityLogger.TrackError(ex, "ISURVEY_ROUTE_PLOT");
            doc.Editor.WriteMessage($"\n[iSurvey] In layout lỗi: {ex.Message}");
        }
    }
```
