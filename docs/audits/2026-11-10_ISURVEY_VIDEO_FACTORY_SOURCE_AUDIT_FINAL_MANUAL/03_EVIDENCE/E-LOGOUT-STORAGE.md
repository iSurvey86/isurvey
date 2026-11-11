# E-LOGOUT-STORAGE — LicenseStorage.Delete

- **file:** `src/Auth/LicenseStorage.cs`
- **lines:** 196-207
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** File.Delete license.dat

```csharp
    public void Delete()
    {
        try
        {
            if (File.Exists(_path))
                File.Delete(_path);
        }
        catch
        {
            // File bị khóa — lần sau Load vẫn xác thực lại
        }
    }
```
