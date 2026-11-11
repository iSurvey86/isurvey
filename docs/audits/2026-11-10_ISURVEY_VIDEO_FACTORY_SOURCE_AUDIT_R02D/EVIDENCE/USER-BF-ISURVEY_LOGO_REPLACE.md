# Evidence `USER-BF-ISURVEY_LOGO_REPLACE`

- **file:** `src/Modules/Frame/FrameService.cs`
- **lines:** 131-148
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler ReplaceLogoCommand

```csharp
{
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        try
        {
            ReplaceLogoCore(doc);
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage($"\n[iSurvey] Thay logo không thực hiện được: {ex.Message}");
            ActivityLogger.TrackError(ex);
        }
    }
```
