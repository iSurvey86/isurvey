# Evidence `USER-BF-ISURVEY_HDSD`

- **file:** `src/Auth/AuthModule.cs`
- **lines:** 36-49
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler HelpCommand

```csharp
{
        const string url = "https://ksnpsc.vercel.app/huong-dan/isurvey";
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            AuthManager.Print("\n[iSurvey] Da mo huong dan tren trinh duyet.");
            ActivityLogger.Enqueue("hdsd", "ISURVEY_HDSD");
        }
        catch (System.Exception ex)
        {
            AuthManager.Print("\n[iSurvey] Khong mo duoc huong dan: " + ex.Message);
        }
    }
```
