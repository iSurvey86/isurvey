# Evidence `SC10-BF-ISURVEY_UAV_FRAME_TOGGLE-FrameToggleCommand`

- **file:** `src/Modules/Uav/UavModule.cs`
- **lines:** 20-26
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** SOURCE_BEHAVIOR_VERIFIED_STATIC

```csharp
{
        if (!AuthManager.EnsureAuthenticated(track: false)) return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null) return;
        UavImageService.ToggleImageFrames(doc.Editor, doc.Database);
    }
```
