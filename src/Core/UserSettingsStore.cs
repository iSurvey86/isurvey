using System.Text.Json;
using iSurvey.Models;

namespace iSurvey.Core;

/// <summary>
/// Lưu/đọc cài đặt: ưu tiên <c>.isurvey.json</c> cạnh bản vẽ,
/// fallback <c>%AppData%\iSurvey\settings.json</c>.
/// </summary>
public static class UserSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static MapUserSettings Load(string? drawingPath)
    {
        foreach (var path in GetCandidatePaths(drawingPath))
        {
            if (!File.Exists(path))
                continue;

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<MapUserSettings>(json, JsonOptions)
                       ?? new MapUserSettings();
            }
            catch
            {
                // thử path khác
            }
        }

        return new MapUserSettings();
    }

    public static void Save(string? drawingPath, MapUserSettings settings)
    {
        var path = ResolveWritePath(drawingPath);
        if (path is null)
            return;

        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(path, json);
        }
        catch
        {
            // không chặn luồng chèn ảnh nếu ghi settings thất bại
        }
    }

    private static IEnumerable<string> GetCandidatePaths(string? drawingPath)
    {
        var besideDrawing = GetBesideDrawingPath(drawingPath);
        if (besideDrawing is not null)
            yield return besideDrawing;

        yield return GetGlobalPath();
    }

    private static string? ResolveWritePath(string? drawingPath)
    {
        var besideDrawing = GetBesideDrawingPath(drawingPath);
        if (besideDrawing is not null)
            return besideDrawing;

        return GetGlobalPath();
    }

    private static string? GetBesideDrawingPath(string? drawingPath)
    {
        if (string.IsNullOrWhiteSpace(drawingPath))
            return null;

        var full = drawingPath;
        if (!Path.IsPathRooted(full))
            return null;

        var dir = Path.GetDirectoryName(full);
        if (string.IsNullOrEmpty(dir))
            return null;

        return Path.Combine(dir, ".isurvey.json");
    }

    private static string GetGlobalPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "iSurvey", "settings.json");
    }
}
