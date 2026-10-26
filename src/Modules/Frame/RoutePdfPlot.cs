using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Modules.Frame;

/// <summary>
/// In gộp nhiều trang của MỘT layout ra một file PDF: mỗi trang là một cửa sổ in (Window) bằng đúng một tờ giấy
/// quanh từng khung — không dùng hộp bao của khối khung nên lề đóng tập 25 mm được giữ nguyên.
/// </summary>
internal static class RoutePdfPlot
{
    public static bool Plot(
        Document doc, string layoutName, IReadOnlyList<Extents2d> windows, string pdfPath, double paperW, double paperH, out string? issue)
    {
        issue = null;
        if (windows.Count == 0)
        {
            issue = "không có trang nào";
            return false;
        }

        if (PlotFactory.ProcessPlotState != ProcessPlotState.NotPlotting)
        {
            issue = "AutoCAD đang in tác vụ khác";
            return false;
        }

        object? oldBg = null;
        try
        {
            oldBg = AcadApp.GetSystemVariable("BACKGROUNDPLOT");
        }
        catch
        {
            // không có biến: bỏ qua
        }

        var step = "";
        try
        {
            AcadApp.SetSystemVariable("BACKGROUNDPLOT", 0);
            var dir = Path.GetDirectoryName(Path.GetFullPath(pdfPath));
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            if (File.Exists(pdfPath))
                File.Delete(pdfPath);

            using var _ = doc.LockDocument();
            var db = doc.Database;
            step = "CurrentLayout";
            LayoutManager.Current.CurrentLayout = layoutName;

            using var tr = db.TransactionManager.StartTransaction();
            var dict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
            var lay = (Layout)tr.GetObject(dict.GetAt(layoutName), OpenMode.ForRead);

            var infos = new List<PlotInfo>();
            var settingsKeep = new List<PlotSettings>();
            var psv = PlotSettingsValidator.Current;
            var piv = new PlotInfoValidator { MediaMatchingPolicy = MatchingPolicy.MatchEnabled };
            foreach (var w in windows)
            {
                step = "PlotSettings";
                var ps = new PlotSettings(lay.ModelType);
                ps.CopyFrom(lay);
                settingsKeep.Add(ps);
                step = "SetPlotWindowArea";
                psv.SetPlotWindowArea(ps, w);
                step = "SetPlotType";
                psv.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Window);
                var pi = new PlotInfo { Layout = lay.ObjectId, OverrideSettings = ps };
                step = "Validate";
                piv.Validate(pi);
                infos.Add(pi);
            }

            step = "BeginPlot";
            using var pe = PlotFactory.CreatePublishEngine();
            pe.BeginPlot(null, null);
            step = "BeginDocument";
            pe.BeginDocument(infos[0], doc.Name, null, 1, true, pdfPath);
            for (var i = 0; i < infos.Count; i++)
            {
                step = $"BeginPage {i + 1}";
                using var ppi = new PlotPageInfo();
                pe.BeginPage(ppi, infos[i], i == infos.Count - 1, null);
                pe.BeginGenerateGraphics(null);
                pe.EndGenerateGraphics(null);
                pe.EndPage(null);
            }

            step = "EndDocument";
            pe.EndDocument(null);
            pe.EndPlot(null);
            tr.Commit();
            foreach (var ps in settingsKeep)
                ps.Dispose();

            if (!File.Exists(pdfPath) || new FileInfo(pdfPath).Length == 0)
            {
                issue = "không tạo được file PDF";
                return false;
            }

            return true;
        }
        catch (System.Exception ex)
        {
            issue = $"{ex.Message} @{step}";
            return false;
        }
        finally
        {
            try
            {
                if (oldBg is not null)
                    AcadApp.SetSystemVariable("BACKGROUNDPLOT", oldBg);
            }
            catch
            {
                // không chặn
            }
        }
    }
}

