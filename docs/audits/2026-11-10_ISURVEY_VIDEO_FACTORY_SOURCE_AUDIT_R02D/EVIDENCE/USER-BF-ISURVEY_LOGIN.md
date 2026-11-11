# Evidence `USER-BF-ISURVEY_LOGIN`

- **file:** `src/Auth/AuthModule.cs`
- **lines:** 25-32
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler LoginCommand

```csharp
{
        AuthManager.Refresh();
        if (AuthManager.IsAuthenticated)
            ShowAbout();
        else
            AuthManager.ShowLogin();
    }
```
