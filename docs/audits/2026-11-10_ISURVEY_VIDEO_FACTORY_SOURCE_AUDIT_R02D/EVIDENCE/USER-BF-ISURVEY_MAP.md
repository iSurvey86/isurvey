# Evidence `USER-BF-ISURVEY_MAP`

- **file:** `src/Modules/Map/MapModule.cs`
- **lines:** 26-30
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler InsertMapCommand

```csharp
{
        if (!AuthManager.EnsureAuthenticated(track: false)) return;
        RunMapWorkflow(MapInsertAction.Insert);
    }
```
