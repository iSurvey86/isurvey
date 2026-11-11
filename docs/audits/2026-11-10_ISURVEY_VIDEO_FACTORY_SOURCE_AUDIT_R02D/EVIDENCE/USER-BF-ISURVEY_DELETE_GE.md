# Evidence `USER-BF-ISURVEY_DELETE_GE`

- **file:** `src/Modules/Map/MapModule.cs`
- **lines:** 170-176
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler DeleteGeCommand

```csharp
{
        if (!AuthManager.EnsureAuthenticated(track: false)) return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null) return;
        GeDeleteWorkflow.Run(doc);
    }
```
