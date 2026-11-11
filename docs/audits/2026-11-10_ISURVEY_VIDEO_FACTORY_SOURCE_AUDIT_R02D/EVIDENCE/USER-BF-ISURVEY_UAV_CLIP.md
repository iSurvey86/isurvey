# Evidence `USER-BF-ISURVEY_UAV_CLIP`

- **file:** `src/Modules/Uav/UavModule.cs`
- **lines:** 11-17
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler ClipCommand

```csharp
{
        if (!AuthManager.EnsureAuthenticated(track: false)) return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null) return;
        UavImageService.ClipByBoundary(doc.Editor, doc.Database);
    }
```
