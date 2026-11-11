# Evidence `USER-BF-ISURVEY_UPDATE-UpdateFlow-RunManualCheck`

- **file:** `src/Auth/UpdateFlow.cs`
- **lines:** 14-40
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** called from BF-ISURVEY_UPDATE

```csharp
{
        AuthManager.Print("\n[iSurvey] Đang kiểm tra bản cập nhật...");
        var result = UpdateCheckService.Check();
        LogCheck(result, source: "upd");

        if (!result.Ok)
        {
            DarkDialog.ShowInfo(
                "KIỂM TRA CẬP NHẬT",
                "Không kiểm tra được",
                [("Phiên bản hiện tại", AuthManager.AppVersion)],
                note: result.Message);
            return;
        }

        if (!result.UpdateAvailable || result.Manifest is null)
        {
            DarkDialog.ShowInfo(
                "KIỂM TRA CẬP NHẬT",
                "Bạn đang dùng phiên bản mới nhất",
                [("Phiên bản hiện tại", AuthManager.AppVersion)]);
            return;
        }

        PromptAndApply(result.Manifest, result.ForceUpdate, source: "upd");
    }
```
