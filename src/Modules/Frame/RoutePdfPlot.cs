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
        Document doc, string layoutName, IReadOnlyList<Extents2d> windows, string pdfPath, double paperW, double paperH,
        bool monochrome, string? stampPath, out string? issue)
    {
        _ = paperW;
        _ = paperH;
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
        object? oldFrame = null;
        var stampUsed = false;
        try
        {
            oldBg = AcadApp.GetSystemVariable("BACKGROUNDPLOT");
        }
        catch
        {
            // không có biến: bỏ qua
        }

        var step = "";
        PlotProgressWindow? progress = null;
        try
        {
            AcadApp.SetSystemVariable("BACKGROUNDPLOT", 0);
            var dir = Path.GetDirectoryName(Path.GetFullPath(pdfPath));
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            if (File.Exists(pdfPath))
                File.Delete(pdfPath);

            using var _ = doc.LockDocument();
            progress = PlotProgressWindow.ShowNew();
            var db = doc.Database;
            if (!string.IsNullOrEmpty(stampPath) && !File.Exists(stampPath))
                stampPath = null;
            step = "CurrentLayout";
            progress.Report(0, windows.Count, "Đang mở layout " + layoutName + "...");
            LayoutManager.Current.CurrentLayout = layoutName;

            using var tr = db.TransactionManager.StartTransaction();
            var dict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
            var lay = (Layout)tr.GetObject(dict.GetAt(layoutName), OpenMode.ForRead);

            var infos = new List<PlotInfo>();
            var settingsKeep = new List<PlotSettings>();
            var psv = PlotSettingsValidator.Current;
            var piv = new PlotInfoValidator { MediaMatchingPolicy = MatchingPolicy.MatchEnabled };
            for (var i = 0; i < windows.Count; i++)
            {
                if (progress.Cancelled)
                {
                    DisposeSettings(settingsKeep);
                    issue = "đã hủy";
                    return false;
                }

                step = "PlotSettings";
                progress.Report(i, windows.Count, $"Đang chuẩn bị trang {i + 1} / {windows.Count}...");
                var ps = new PlotSettings(lay.ModelType);
                ps.CopyFrom(lay);
                settingsKeep.Add(ps);
                step = "SetPlotWindowArea";
                psv.SetPlotWindowArea(ps, windows[i]);
                step = "SetPlotType";
                psv.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Window);
                step = "StyleSheet";
                NamedPlotStyle.Apply(ps, db, monochrome ? NamedPlotStyle.MonoFile : NamedPlotStyle.ColorFile);
                var pi = new PlotInfo { Layout = lay.ObjectId, OverrideSettings = ps };
                step = "Validate";
                piv.Validate(pi);
                infos.Add(pi);
            }

            if (progress.Cancelled || infos.Count == 0)
            {
                DisposeSettings(settingsKeep);
                issue = "đã hủy";
                return false;
            }

            step = "Stamp";
            var stamps = string.IsNullOrEmpty(stampPath)
                ? new FrameService.PlotStamp(ObjectId.Null, [])
                : FrameService.PlacePlotStamps(tr, db, lay.BlockTableRecordId, stampPath, windows);
            if (stamps.Images.Count > 0)
            {
                stampUsed = true;
                doc.Editor.WriteMessage("\n[iSurvey] Đóng mộc đỏ trên PDF.");
                try
                {
                    oldFrame = AcadApp.GetSystemVariable("IMAGEFRAME");
                    AcadApp.SetSystemVariable("IMAGEFRAME", (short)0);
                }
                catch
                {
                    oldFrame = null;
                }
            }

            step = "BeginPlot";
            using var pe = PlotFactory.CreatePublishEngine();
            pe.BeginPlot(null, null);
            step = "BeginDocument";
            pe.BeginDocument(infos[0], doc.Name, null, 1, true, pdfPath);
            var printed = 0;
            var cancelled = false;
            for (var i = 0; i < infos.Count; i++)
            {
                if (progress.Cancelled)
                {
                    cancelled = true;
                    break;
                }

                step = $"BeginPage {i + 1}";
                progress.Report(printed, infos.Count, $"Đang in trang {i + 1} / {infos.Count}...");
                using var ppi = new PlotPageInfo();
                pe.BeginPage(ppi, infos[i], i == infos.Count - 1, null);
                pe.BeginGenerateGraphics(null);
                pe.EndGenerateGraphics(null);
                pe.EndPage(null);
                printed++;
                progress.Report(printed, infos.Count, $"Đã in {printed} / {infos.Count} trang");
            }

            step = "EndDocument";
            pe.EndDocument(null);
            pe.EndPlot(null);
            RestoreImageFrame(oldFrame);
            oldFrame = null;
            if (FrameService.ErasePlotStamps(tr, stamps))
                tr.Commit();
            DisposeSettings(settingsKeep);

            if (cancelled)
            {
                TryDelete(pdfPath);
                issue = "đã hủy";
                return false;
            }

            if (!File.Exists(pdfPath) || new FileInfo(pdfPath).Length == 0)
            {
                issue = "không tạo được file PDF";
                return false;
            }

            progress.Report(infos.Count, infos.Count, "Đã xuất xong.");
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
                progress?.ForceClose();
            }
            catch
            {
                // không chặn
            }

            try
            {
                RestoreImageFrame(oldFrame);
                if (oldBg is not null)
                    AcadApp.SetSystemVariable("BACKGROUNDPLOT", oldBg);
            }
            catch
            {
                // không chặn
            }

            if (stampUsed)
            {
                try
                {
                    var open = AcadApp.DocumentManager.MdiActiveDocument;
                    if (open is not null)
                        FrameService.RemoveLeftoverStamps(open.Database);
                }
                catch
                {
                    // không chặn
                }
            }
        }
    }

    private static void DisposeSettings(List<PlotSettings> settings)
    {
        foreach (var ps in settings)
            ps.Dispose();
        settings.Clear();
    }

    private static void RestoreImageFrame(object? oldFrame)
    {
        if (oldFrame is null)
            return;
        try
        {
            AcadApp.SetSystemVariable("IMAGEFRAME", oldFrame);
        }
        catch
        {
            // không chặn
        }
    }

    private static void TryDelete(string pdfPath)
    {
        try
        {
            if (File.Exists(pdfPath))
                File.Delete(pdfPath);
        }
        catch
        {
            // file đang mở: bỏ qua
        }
    }
}
