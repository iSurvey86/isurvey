# Evidence `SC10-BF-ISURVEY_UAV_SEND_BACK-SendBackCommand`

- **file:** `src/Modules/Uav/UavModule.cs`
- **lines:** 29-35
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** SOURCE_BEHAVIOR_VERIFIED_STATIC

```csharp
{
        if (!AuthManager.EnsureAuthenticated(track: false)) return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null) return;
        UavImageService.SendImageToBack(doc.Editor, doc.Database);
    }
```
