# E-LOGOUT-SIGNOUT — AuthManager.SignOut

- **file:** `src/Auth/AuthManager.cs`
- **lines:** 204-215
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** Storage.Delete; clear license

```csharp
    public static void SignOut()
    {
        var old = _license;
        Storage.Delete();
        _license = null;
        RaiseChanged();

        // "Đổi máy": ghi nhật ký đăng xuất + trả lại suất thiết bị trên server
        // (nền, không chặn; lỗi thì Admin giải phóng thủ công).
        if (old is { Mode: LicenseMode.Online, RefreshToken: not null })
            _ = Task.Run(() => ReleaseDeviceAsync(old));
    }
```
