# E-THIETLAPCOC-STORE — StationStyleStore.Save

- **file:** `src/Modules/Tuyen/StationStyleStore.cs`
- **lines:** 30-50
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** AppData JSON WriteAllText

```csharp
    public static void Save(StationStyleSettings settings)
    {
        try
        {
            var path = FilePath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonSerializer.Serialize(settings, Json));
        }
        catch
        {
            // Không chặn lệnh nếu ghi file thất bại.
        }
    }

    private static string FilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "iSurvey", "station-style.json");
    }
```
