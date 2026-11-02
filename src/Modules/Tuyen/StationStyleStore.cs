using System.Text.Json;

namespace iSurvey.Modules.Tuyen;

/// <summary>Lưu kiểu cọc tại %AppData%\iSurvey\station-style.json.</summary>
public static class StationStyleStore
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static StationStyleSettings Load()
    {
        try
        {
            var path = FilePath();
            if (!File.Exists(path))
                return new StationStyleSettings();
            var settings = JsonSerializer.Deserialize<StationStyleSettings>(File.ReadAllText(path), Json);
            return settings ?? new StationStyleSettings();
        }
        catch
        {
            return new StationStyleSettings();
        }
    }

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
}
