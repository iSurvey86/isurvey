# Evidence `USER-BF-ISURVEY_PALETTE`

- **file:** `src/UI/Palette/PaletteModule.cs`
- **lines:** 10-15
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler ShowCommand

```csharp
{
        if (!AuthManager.EnsureAuthenticated("ISURVEY_PALETTE"))
            return;
        SurveyPaletteManager.Toggle();
    }
```
