using System.Reflection;

namespace iSurvey.Dgn;

/// <summary>
/// Tìm file seed DGN cho -DGNEXPORT, không bắt người dùng chọn thủ công. Thứ tự ưu tiên:
/// 1) seed người dùng chỉ định trong cấu hình;
/// 2) seed VN-2000 đi kèm bundle: <c>Contents\Seeds\VN2000_Seed{2D|3D}_{V8|V7}.dgn</c>;
/// 3) seed metric do chính Civil 3D / AutoCAD cài sẵn (<c>V8-Metric-Seed2D.dgn</c>, <c>V7-Metric-Seed3D.dgn</c>...).
/// Bản vẽ có Z ≠ 0 bắt buộc dùng seed 3D.
/// </summary>
internal static class DgnSeedLocator
{
    public sealed record SeedInfo(string Path, string Source);

    /// <param name="is3D">Bản vẽ có dữ liệu 3D (Z ≠ 0): -DGNEXPORT bắt buộc dùng seed 3D ("A 3D seed file is required").</param>
    public static SeedInfo? Resolve(DgnExportVersion version, string? userOverride, bool is3D = false)
    {
        // Seed tự chỉ định trong cấu hình là seed 2D; bản vẽ 3D phải dùng seed 3D nên bỏ qua override.
        if (!is3D && !string.IsNullOrWhiteSpace(userOverride) && File.Exists(userOverride))
            return new SeedInfo(userOverride, "cấu hình người dùng");

        var dim = is3D ? "3D" : "2D";
        var bundleName = $"VN2000_Seed{dim}_{(version == DgnExportVersion.V7 ? "V7" : "V8")}.dgn";
        foreach (var dir in BundleSeedDirs())
        {
            var candidate = System.IO.Path.Combine(dir, bundleName);
            if (File.Exists(candidate))
                return new SeedInfo(candidate, $"seed {dim} VN-2000 đi kèm iSurvey");
        }

        var civilName = $"{(version == DgnExportVersion.V7 ? "V7" : "V8")}-Metric-Seed{dim}.dgn";
        var civil = FindCivilSeed(civilName);
        return civil is null ? null : new SeedInfo(civil, $"seed metric {dim} của Civil 3D");
    }

    /// <summary>Thư mục có thể chứa Seeds: bundle (Contents\Seeds), cạnh DLL, hoặc thư mục dev.</summary>
    private static IEnumerable<string> BundleSeedDirs()
    {
        string? asmDir = null;
        try
        {
            asmDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }
        catch
        {
            // bỏ qua
        }

        if (string.IsNullOrEmpty(asmDir))
            yield break;

        // …\iSurvey.bundle\Contents\Win64\<năm>\iSurvey.dll  ➔  …\iSurvey.bundle\Contents\Seeds
        var contents = System.IO.Path.GetFullPath(System.IO.Path.Combine(asmDir, "..", ".."));
        yield return System.IO.Path.Combine(contents, "Seeds");
        yield return System.IO.Path.Combine(asmDir, "Seeds");
    }

    private static string? FindCivilSeed(string fileName)
    {
        try
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var autodesk = System.IO.Path.Combine(local, "Autodesk");
            if (!Directory.Exists(autodesk))
                return null;

            // %LocalAppData%\Autodesk\C3D 2026\enu\Template\AutoCAD Template\V8-Metric-Seed2D.dgn
            string? best = null;
            foreach (var product in Directory.EnumerateDirectories(autodesk))
            {
                foreach (var lang in SafeDirs(product))
                {
                    var candidate = System.IO.Path.Combine(lang, "Template", "AutoCAD Template", fileName);
                    if (!File.Exists(candidate))
                        continue;

                    // Chọn bản của phiên bản mới nhất (tên thư mục sản phẩm lớn nhất).
                    if (best is null || string.Compare(candidate, best, StringComparison.OrdinalIgnoreCase) > 0)
                        best = candidate;
                }
            }

            return best;
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> SafeDirs(string dir)
    {
        try
        {
            return Directory.EnumerateDirectories(dir).ToList();
        }
        catch
        {
            return [];
        }
    }
}
