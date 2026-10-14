using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace iSurvey.Dgn;

/// <summary>Bảng ánh xạ Level MicroStation ➔ Layer AutoCAD, đọc từ dgn_layer_mapping.json.</summary>
internal sealed class DgnLayerMap
{
    private const string FileName = "dgn_layer_mapping.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    // "Level 10", "Level10", "DC2$0$Level 10", "10", "Level_10" ➔ 10. Không bắt "ABC12".
    private static readonly Regex LevelRegex = new(
        @"^(?:.*[^A-Za-z0-9])?(?:level|lvl|lv)?\s*0*(\d{1,4})$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public Dictionary<int, string> Levels { get; } = [];

    public string? SourcePath { get; private set; }

    /// <summary>Bản người dùng sửa được: %AppData%\iSurvey\dgn_layer_mapping.json.</summary>
    public static string UserPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "iSurvey", FileName);

    public static string BundledPath
    {
        get
        {
            var asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppContext.BaseDirectory;
            var path = Path.Combine(asmDir, "Data", FileName);
            return File.Exists(path) ? path : Path.Combine(AppContext.BaseDirectory, "Data", FileName);
        }
    }

    /// <summary>Ưu tiên bản người dùng; nếu không có thì dùng bản đi kèm.</summary>
    public static DgnLayerMap Load()
    {
        var map = new DgnLayerMap();
        foreach (var path in new[] { UserPath, BundledPath })
        {
            if (!File.Exists(path))
                continue;

            try
            {
                var file = JsonSerializer.Deserialize<MapFile>(File.ReadAllText(path), JsonOptions);
                if (file?.Levels is null)
                    continue;

                foreach (var rule in file.Levels)
                {
                    if (rule.Level >= 0 && !string.IsNullOrWhiteSpace(rule.Layer))
                        map.Levels[rule.Level] = rule.Layer.Trim();
                }

                map.SourcePath = path;
                return map;
            }
            catch
            {
                // file hỏng ➔ thử nguồn kế tiếp
            }
        }

        return map;
    }

    /// <summary>Tạo bản sao người dùng từ bản đi kèm (nếu chưa có) rồi trả về đường dẫn.</summary>
    public static string EnsureUserCopy()
    {
        var user = UserPath;
        if (File.Exists(user))
            return user;

        Directory.CreateDirectory(Path.GetDirectoryName(user)!);
        File.Copy(BundledPath, user, overwrite: false);
        return user;
    }

    public static bool TryExtractLevel(string layerName, out int level)
    {
        level = 0;
        var m = LevelRegex.Match(layerName.Trim());
        return m.Success && int.TryParse(m.Groups[1].Value, out level);
    }

    private sealed class MapFile
    {
        public List<Rule>? Levels { get; set; }
    }

    private sealed class Rule
    {
        public int Level { get; set; }

        public string Layer { get; set; } = string.Empty;
    }
}
