using Autodesk.AutoCAD.DatabaseServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Modules.Frame;

/// <summary>
/// Bảng in màu của iSurvey: 255 màu ACI, màu và kiểu nét giữ theo đối tượng, nét in 0.05 mm.
/// Không sửa acad.ctb hay file pc3 của máy.
/// </summary>
internal static class ThinPlotStyle
{
    public const string FileName = "iSurvey_005.ctb";

    /// <summary>Gắn bảng nét 0.05 mm vào thiết lập in. Bản vẽ STB thì báo lỗi — CTB không dùng được.</summary>
    public static void Apply(PlotSettings ps, Database db)
    {
        if (!db.PlotStyleMode)
            throw new InvalidOperationException("bản vẽ dùng kiểu in đặt tên (STB), không gắn được bảng 255 màu");

        Install();
        var v = PlotSettingsValidator.Current;
        v.RefreshLists(ps);
        v.SetCurrentStyleSheet(ps, FileName);
        ps.PlotPlotStyles = true;
        ps.ShowPlotStyles = true;
    }

    private static void Install()
    {
        var dir = StylesDirectory();
        Directory.CreateDirectory(dir);
        var dest = Path.Combine(dir, FileName);
        var bytes = Embedded();
        if (File.Exists(dest))
        {
            var have = File.ReadAllBytes(dest);
            if (Same(have, bytes))
                return;
        }

        File.WriteAllBytes(dest, bytes);
    }

    private static bool Same(byte[] have, byte[] bytes)
    {
        if (have.Length != bytes.Length)
            return false;
        for (var i = 0; i < have.Length; i++)
        {
            if (have[i] != bytes[i])
                return false;
        }

        return true;
    }

    private static string StylesDirectory()
    {
        var root = Convert.ToString(AcadApp.GetSystemVariable("ROAMABLEROOTPREFIX")) ?? "";
        return Path.Combine(root.TrimEnd('\\', '/'), "Plotters", "Plot Styles");
    }

    private static byte[] Embedded()
    {
        using var src = typeof(ThinPlotStyle).Assembly.GetManifestResourceStream("iSurvey.iSurvey_005.ctb")
            ?? throw new InvalidOperationException("thiếu bảng in iSurvey_005.ctb trong plugin");
        using var ms = new MemoryStream();
        src.CopyTo(ms);
        return ms.ToArray();
    }
}
