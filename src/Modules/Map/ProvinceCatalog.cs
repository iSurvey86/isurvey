using System.Reflection;
using System.Text.Json;
using iSurvey.Models;

namespace iSurvey.Modules.Map;

/// <summary>Đọc danh mục tỉnh và tra kinh tuyến trục từ dữ liệu JSON.</summary>
public sealed class ProvinceCatalog
{
    private readonly List<ProvinceCrsGroup> _groups;
    private readonly Dictionary<string, Vn2000CrsEntry> _crsByKey;

    public ProvinceCatalog()
    {
        _groups = LoadGroups();
        _crsByKey = LoadCrsByKey();
    }

    public IReadOnlyList<ProvinceCrsGroup> Groups => _groups;

    public double GetCentralMeridian(string sourceProvinceKey)
    {
        if (_crsByKey.TryGetValue(sourceProvinceKey, out var entry))
            return entry.CentralMeridian;

        throw new InvalidOperationException($"Không tìm thấy kinh tuyến trục cho '{sourceProvinceKey}'.");
    }

    private static string DataPath(string fileName)
    {
        var asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                     ?? AppContext.BaseDirectory;
        var path = Path.Combine(asmDir, "Data", fileName);
        if (!File.Exists(path))
            path = Path.Combine(AppContext.BaseDirectory, "Data", fileName);
        return path;
    }

    private static List<ProvinceCrsGroup> LoadGroups()
    {
        var path = DataPath("isurvey_province_crs_map.json");
        if (!File.Exists(path))
            throw new FileNotFoundException("Thiếu isurvey_province_crs_map.json", path);

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<ProvinceCrsGroup>>(json)
               ?? throw new InvalidOperationException("Không parse được isurvey_province_crs_map.json");
    }

    private static Dictionary<string, Vn2000CrsEntry> LoadCrsByKey()
    {
        var path = DataPath("isurvey_vn2000_tm3.json");
        var json = File.ReadAllText(path);
        var entries = JsonSerializer.Deserialize<List<Vn2000CrsEntry>>(json)
                      ?? throw new InvalidOperationException("Không parse được isurvey_vn2000_tm3.json");

        return entries
            .GroupBy(e => e.SourceProvinceKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }
}
