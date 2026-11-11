# E-LOGOUT-CMD — LOGOUT command

- **file:** `src/Auth/AuthModule.cs`
- **lines:** 77-100
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** Confirm → SignOut

```csharp
    [CommandMethod("ISURVEY_LOGOUT")]
    public void LogoutCommand()
    {
        AuthManager.Refresh();
        if (!AuthManager.IsAuthenticated)
        {
            AuthManager.Print("\n[iSurvey] Chua dang nhap.");
            return;
        }

        var email = AuthManager.License!.Email;
        var ok = DarkDialog.Confirm(
            "ĐĂNG XUẤT / ĐỔI MÁY",
            $"Đăng xuất tài khoản {email} khỏi máy này?\n\nGiấy phép trên máy sẽ bị xóa và các tính năng bị khóa cho tới khi kích hoạt lại.\nNếu đang kích hoạt trực tuyến, suất thiết bị sẽ được trả lại để dùng trên máy khác.",
            "Đăng xuất");
        if (!ok)
        {
            AuthManager.Print("\n[iSurvey] Da huy dang xuat.");
            return;
        }

        AuthManager.SignOut();
        AuthManager.Print("\n[iSurvey] Da dang xuat. Cac tinh nang da bi khoa.");
    }
```
