# Evidence `USER-BF-ISURVEY_LOGO`

- **file:** `src/Modules/Frame/FrameService.cs`
- **lines:** 63-74
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler ToggleLogoCommand

```csharp
{
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;
        var which = PromptLogo(doc.Editor);
        if (which is null)
            return;
        ApplyLogo(doc, which.Value);
    }
```
