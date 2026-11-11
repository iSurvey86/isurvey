# Evidence `USER-BF-ISURVEY_STRIP_MAP`

- **file:** `src/Modules/Tuyen/StripMapService.cs`
- **lines:** 28-49
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler StripMapCommand

```csharp
{
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        var db = doc.Database;
        var ed = doc.Editor;

        try
        {
            Run(doc, db, ed);
        }
        catch (System.Exception ex)
        {
            ActivityLogger.TrackError(ex, "ISURVEY_STRIP_MAP");
            ed.WriteMessage($"\n[iSurvey] Duỗi tuyến lỗi: {ex.Message}");
        }
    }
```
