using System.Globalization;
using System.Text.Json;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using iSurvey.Auth;
using iSurvey.UI.Frame;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Modules.Frame;

/// <summary>
/// RAIMCD: rải khung mặt cắt dọc đã vẽ sẵn. Người dùng chọn góc dưới-trái của hàng cuối cần in.
/// Mỗi tờ là khung kỹ thuật, hai viewport cùng tỷ lệ: trái cố định tên hàng, phải trượt theo lý trình.
/// Tỷ lệ in mặc định 1/500; 1/5000 (viewport 1:5000). Không sửa hình trên Model.
/// </summary>
public sealed partial class FrameService
{
    private const string ProfileLayoutName = "MatCat_Doc";
    private const double ProfileBottomMargin = 15;
    private const double ProfileTopMargin = 15;

    /// <summary>Mặt đất nằm ở khoảng này, tính từ đáy vùng vẽ (0 = đáy, 1 = đỉnh).</summary>
    private const double ProfileGroundFraction = 0.64;

    /// <summary>
    /// Mép trái vùng vẽ của khung mẫu nằm ở 20 mm (cột đóng tập). Đặt góc chọn ngay trong mép đó.
    /// </summary>
    private const double ProfileLeftInsetMm = 22;
    private static readonly int[] ProfileScales = [5000, 2000];
    private static readonly string[] ProfileScaleLabels = ["1/500; 1/5000", "1/200; 1/2000"];
    private static readonly string[] ProfileSizes = ["A3", "A2", "A1", "A0"];

    [CommandMethod("ISURVEY_PROFILE_SHEETS")]
    public void ProfileLayoutCommand() => RunProfile(withDialog: true, "ISURVEY_PROFILE_SHEETS");

    /// <summary>Không hộp thoại. Dùng cho kiểm thử: điểm chọn vẫn lấy từ dòng lệnh.</summary>
    [CommandMethod("ISURVEY_PROFILE_SHEETS_QUICK")]
    public void ProfileLayoutQuickCommand() => RunProfile(withDialog: false, "ISURVEY_PROFILE_SHEETS_QUICK");

    private static void RunProfile(bool withDialog, string commandName)
    {
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;
        try
        {
            ProfileRun(doc, withDialog, commandName);
        }
        catch (System.Exception ex)
        {
            ActivityLogger.TrackError(ex, commandName);
            doc.Editor.WriteMessage($"\n[iSurvey] Rải khung mặt cắt dọc lỗi: {ex.Message}");
        }
    }

    private static void ProfileRun(Document doc, bool withDialog, string commandName)
    {
        var db = doc.Database;
        var ed = doc.Editor;
        var template = ResolveTemplate();
        if (template is null)
        {
            ed.WriteMessage($"\n[iSurvey] Không thấy file khung mẫu {TemplateName} (Contents\\Templates của bundle).");
            return;
        }

        if (!LayoutManager.Current.CurrentLayout.Equals("Model", StringComparison.OrdinalIgnoreCase))
            LayoutManager.Current.CurrentLayout = "Model";

        var pick = ed.GetPoint("\niSurvey: Chọn góc dưới-trái của hàng cuối cần in: ");
        if (pick.Status != PromptStatus.OK)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy.");
            return;
        }

        if (!TryMeasureProfile(db, pick.Value, out var band, out var why))
        {
            ed.WriteMessage("\n[iSurvey] " + why);
            return;
        }

        var unitM = UnitMeters(db.Insunits, out var unitNote);
        if (unitNote is not null)
            ed.WriteMessage("\n[iSurvey] " + unitNote);

        var stationM = Math.Max(0, (band.XRight - band.XSplit) * unitM);
        ed.WriteMessage(
            $"\n[iSurvey] Mặt cắt dài {stationM:0.#} m, cao {band.YTop - band.YCut:0.#} đơn vị. Cột tên từ X {band.XLeft:0.#} đến {band.XSplit:0.#}.");

        var saved = ProfileSheetSettings.Load();
        var routeSaved = RouteLayoutSettings.Load();
        var scaleN = Array.IndexOf(ProfileScales, saved.Scale) >= 0 ? saved.Scale : 5000;
        var sizeName = FirstFittingSize(band, unitM, scaleN, saved.Size);
        var overlapM = saved.OverlapM >= 0 ? saved.OverlapM : 100;

        var defaults = ReadFrameDefaults(template, sizeName);
        var attrs = new Dictionary<string, string>(defaults, StringComparer.OrdinalIgnoreCase);
        foreach (var kv in routeSaved.Attributes)
            attrs[kv.Key] = FrameFields.Clean(kv.Value);

        var opts = new RouteLayoutOptions
        {
            Size = sizeName,
            Scale = scaleN,
            OverlapM = overlapM,
            GapMm = saved.GapMm > 0 ? saved.GapMm : 20,
            PlotPdf = false,
            ScaleText = ProfileScaleText(scaleN),
            Attributes = attrs,
            HideOwnerLogo = TitleBlockFit.IsLogoHidden(db, owner: true),
            HideDesignLogo = TitleBlockFit.IsLogoHidden(db, owner: false)
        };

        int CountPieces(string size, int scale, double ovM) =>
            SliceStations(band, size, scale, ovM, unitM).Count;

        string? Reject(string size, int scale) => FitIssue(band, size, scale, unitM);

        SuggestGrid(CountPieces(opts.Size, opts.Scale, opts.OverlapM), out var rows0, out var cols0);
        opts.Rows = rows0;
        opts.Cols = cols0;

        if (withDialog)
        {
            opts.PlotPdf = true;
            var input = new RouteLayoutDialogInput
            {
                RouteLengthM = stationM,
                Sizes = ProfileSizes,
                Scales = ProfileScales,
                ScaleLabels = ProfileScaleLabels,
                KeepOverlap = true,
                WindowTitle = "iSurvey — Rải khung mặt cắt dọc",
                RejectPlan = Reject,
                Initial = opts,
                TemplateDefaults = defaults,
                CountPieces = CountPieces,
                MapWidthM = (size, scale) => StationWidthM(band, size, scale, unitM),
                SuggestGrid = (n) => { SuggestGrid(n, out var r, out var c); return (r, c); },
                ExistingSignatures = LiveSignatureTags(db),
                KeptLogoOwner = KeptLogoFileName(db, owner: true),
                KeptLogoDesign = KeptLogoFileName(db, owner: false)
            };
            var result = RouteLayoutWindow.Show(input);
            if (result is null)
            {
                ed.WriteMessage("\n[iSurvey] Đã hủy.");
                return;
            }

            opts = result;
            opts.ScaleText = ProfileScaleText(opts.Scale);
        }

        if (FitIssue(band, opts.Size, opts.Scale, unitM) is { } unfit)
        {
            ed.WriteMessage("\n[iSurvey] " + unfit);
            return;
        }

        var spec = Specs.First(s => s.Name.Equals(opts.Size, StringComparison.OrdinalIgnoreCase));
        var slices = SliceStations(band, opts.Size, opts.Scale, opts.OverlapM, unitM);
        if (opts.Rows * opts.Cols < slices.Count)
        {
            SuggestGrid(slices.Count, out var r, out var c);
            opts.Rows = r;
            opts.Cols = c;
        }

        ed.WriteMessage(
            $"\n[iSurvey] {slices.Count} tờ {spec.Name}, tỷ lệ {opts.ScaleText}, chồng gối {opts.OverlapM:0.#} m, lưới {opts.Rows}×{opts.Cols}.");

        SaveProfileChoices(opts, defaults);

        var overwrite = false;
        var layoutName = ProfileLayoutName;
        if (LayoutExists(db, layoutName))
        {
            if (withDialog)
            {
                var pk = new PromptKeywordOptions($"\niSurvey: Layout {layoutName} đã có. Gõ 1 để ghi đè, 2 để tạo layout mới")
                {
                    AllowNone = true,
                    AppendKeywordsToMessage = true
                };
                pk.Keywords.Add("1", "1", "1");
                pk.Keywords.Add("2", "2", "2");
                pk.Keywords.Default = "2";
                var rk = ed.GetKeywords(pk);
                if (rk.Status is not (PromptStatus.OK or PromptStatus.None))
                {
                    ed.WriteMessage("\n[iSurvey] Đã hủy — chưa tạo layout.");
                    return;
                }

                overwrite = rk.Status == PromptStatus.OK && rk.StringResult == "1";
            }

            if (!overwrite)
            {
                var n = 2;
                while (LayoutExists(db, $"{ProfileLayoutName}-{n}"))
                    n++;
                layoutName = $"{ProfileLayoutName}-{n}";
            }
        }

        string? pdfPath = null;
        if (opts.PlotPdf)
        {
            pdfPath = DefaultPdfPath(db, layoutName);
            if (withDialog)
            {
                using var dlg = new System.Windows.Forms.SaveFileDialog
                {
                    Title = "Lưu file PDF gộp",
                    Filter = "PDF (*.pdf)|*.pdf",
                    FileName = Path.GetFileName(pdfPath),
                    InitialDirectory = Path.GetDirectoryName(pdfPath),
                    OverwritePrompt = true
                };
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    pdfPath = dlg.FileName;
                else
                {
                    pdfPath = null;
                    ed.WriteMessage("\n[iSurvey] Không chọn nơi lưu PDF — bỏ qua bước in.");
                }
            }
        }

        _pageSetupIssue = null;
        var built = BuildProfileLayout(doc, spec, template, opts, unitM, band, slices, layoutName, overwrite, out var windows);
        ed.Regen();

        if (_pageSetupIssue is not null)
            ed.WriteMessage($"\n[iSurvey] Chưa đặt được khổ giấy cho layout ({_pageSetupIssue}) — chọn khổ giấy {spec.Name}, tỷ lệ in 1:1 trong Page Setup.");

        ed.WriteMessage(
            $"\n[iSurvey] Đã tạo layout {layoutName}: {built} tờ {spec.Name}, tỷ lệ {opts.ScaleText}. Viewport đã khóa.");

        string? pdfNote = null;
        string? pdfIssue = null;
        if (pdfPath is not null && _pageSetupIssue is null)
        {
            var (pw, ph) = PaperSizeMm(spec);
            var pdf = RoutePdfPlot.Plot(doc, layoutName, windows, pdfPath, pw, ph, out pdfIssue);
            if (pdf)
            {
                ed.WriteMessage($"\n[iSurvey] Đã in {windows.Count} trang vào {pdfPath}");
                pdfNote = ", PDF " + windows.Count + " trang";
            }
            else
            {
                ed.WriteMessage($"\n[iSurvey] In PDF thất bại: {pdfIssue}");
            }
        }
        else if (pdfPath is not null)
        {
            pdfIssue = _pageSetupIssue;
            ed.WriteMessage("\n[iSurvey] Bỏ qua in PDF vì chưa đặt được máy in/khổ giấy cho layout.");
        }

        if (withDialog)
        {
            var done = $"Dàn trang mặt cắt dọc đã xong.\n\nLayout {layoutName}: {built} tờ {spec.Name}, tỷ lệ {opts.ScaleText}.";
            if (pdfNote is not null)
                done += $"\nĐã in {windows.Count} trang:\n{pdfPath}";
            else if (pdfIssue is not null)
                done += "\nChưa in được PDF: " + pdfIssue;
            else
                done += "\nChưa in PDF. Gõ INTUYEN khi muốn in layout này.";
            System.Windows.MessageBox.Show(done, "iSurvey", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        ActivityLogger.Enqueue("raimcd", commandName, new
        {
            size = spec.Name,
            scale = opts.ScaleText,
            pieces = built,
            rows = opts.Rows,
            cols = opts.Cols,
            lengthM = Math.Round(stationM, 1),
            overlapM = opts.OverlapM,
            pdf = pdfNote is not null,
            drawing = ActivityLogger.FileName(db.Filename)
        });
        ActivityLogger.Result(commandName, $"layout {layoutName}: {built} tờ {spec.Name} {opts.ScaleText}, dài {stationM:0.#} m{pdfNote}");
    }

    private readonly record struct ProfileBand(double XLeft, double XSplit, double XRight, double YCut, double YTop, double YGround);

    private readonly record struct Seg(double X0, double Y0, double X1, double Y1)
    {
        public double MinX => Math.Min(X0, X1);
        public double MaxX => Math.Max(X0, X1);
        public double MinY => Math.Min(Y0, Y1);
        public double MaxY => Math.Max(Y0, Y1);
        public bool Horizontal => Math.Abs(Y1 - Y0) < 0.8 && Math.Abs(X1 - X0) > 20;
        public bool Vertical => Math.Abs(X1 - X0) < 0.8 && Math.Abs(Y1 - Y0) > 20;
    }

    private readonly record struct Box(double X0, double Y0, double X1, double Y1);

    private static bool TryMeasureProfile(Database db, Point3d pick, out ProfileBand band, out string why)
    {
        band = default;
        why = "";
        var segs = new List<Seg>();
        var boxes = new List<Box>();
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            foreach (ObjectId id in ms)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is not Entity ent)
                    continue;
                try
                {
                    CollectSegs(ent, segs);
                    var ext = ent.GeometricExtents;
                    var b = new Box(ext.MinPoint.X, ext.MinPoint.Y, ext.MaxPoint.X, ext.MaxPoint.Y);
                    if (b.X1 - b.X0 > 250000 || b.Y1 - b.Y0 > 20000)
                        continue;
                    boxes.Add(b);
                }
                catch
                {
                    // proxy không có khung
                }
            }

            tr.Commit();
        }

        Seg? cutLine = null;
        var bestDy = double.MaxValue;
        foreach (var s in segs)
        {
            if (!s.Horizontal || s.MinX > pick.X + 8 || s.MaxX < pick.X - 8)
                continue;
            if (s.MinY < pick.Y - 80 || s.MinY > pick.Y + 6)
                continue;
            var dy = Math.Abs(s.MinY - pick.Y);
            if (s.MinY <= pick.Y + 1 && dy <= bestDy)
            {
                bestDy = dy;
                cutLine = s;
            }
        }

        var yCut = cutLine?.MinY ?? pick.Y;
        Seg? leftLine = null;
        var bestDx = double.MaxValue;
        foreach (var s in segs)
        {
            if (!s.Vertical || s.MinY > yCut + 2 || s.MaxY < yCut - 2)
                continue;
            if (s.MinX < pick.X - 60 || s.MinX > pick.X + 25)
                continue;
            var dx = Math.Abs(s.MinX - pick.X);
            if (dx < bestDx)
            {
                bestDx = dx;
                leftLine = s;
            }
        }

        if (leftLine is null)
        {
            why = "Không thấy góc bảng tại điểm này. Bấm giao nét ngang và nét dọc mép trái của hàng cuối.";
            return false;
        }

        var xLeft = leftLine.Value.MinX;
        double? split = null;
        foreach (var s in segs)
        {
            if (!s.Vertical || s.MinY > yCut + 2 || s.MaxY < yCut - 2)
                continue;
            if (s.MinX <= xLeft + 40)
                continue;
            if (split is null || s.MinX < split)
                split = s.MinX;
        }

        var xSplit = split ?? xLeft + 250;
        if (xSplit - xLeft < 60 || xSplit - xLeft > 800)
            xSplit = xLeft + 250;

        var top = yCut;
        var right = xSplit;
        var grew = true;
        var guard = 0;
        while (grew && guard++ < 30)
        {
            grew = false;
            foreach (var b in boxes)
            {
                if (b.X1 < xLeft - 8 || b.X0 < xLeft - 80)
                    continue;
                if (b.Y1 < yCut - 1 || b.Y0 > top + 300)
                    continue;
                if (b.Y1 > top + 0.5)
                {
                    top = b.Y1;
                    grew = true;
                }

                if (b.X1 > right)
                    right = b.X1;
            }
        }

        if (right - xSplit < 20 || top - yCut < 40)
        {
            why = "Điểm này không nằm trên bảng mặt cắt. Chọn góc dưới-trái của hàng Khoảng cách cộng dồn.";
            return false;
        }

        var tops = new List<double>();
        foreach (var s in segs)
        {
            if (!s.Vertical || s.MaxY - s.MinY < 30)
                continue;
            if (s.MinX < xSplit - 2 || s.MinX > right + 2 || s.MaxY < yCut + 60)
                continue;
            tops.Add(s.MaxY);
        }

        tops.Sort();
        var yGround = tops.Count > 0 ? tops[tops.Count / 2] : yCut + (top - yCut) * 0.72;
        band = new ProfileBand(xLeft, xSplit, right, yCut, top, yGround);
        return true;
    }

    private static void CollectSegs(Entity ent, List<Seg> segs)
    {
        switch (ent)
        {
            case Line ln:
                segs.Add(new Seg(ln.StartPoint.X, ln.StartPoint.Y, ln.EndPoint.X, ln.EndPoint.Y));
                break;
            case Polyline pl:
                var n = pl.NumberOfVertices;
                for (var i = 0; i < n - 1; i++)
                {
                    var a = pl.GetPoint2dAt(i);
                    var b = pl.GetPoint2dAt(i + 1);
                    segs.Add(new Seg(a.X, a.Y, b.X, b.Y));
                }

                if (pl.Closed && n > 1)
                {
                    var a = pl.GetPoint2dAt(n - 1);
                    var b = pl.GetPoint2dAt(0);
                    segs.Add(new Seg(a.X, a.Y, b.X, b.Y));
                }

                break;
        }
    }

    private static string ProfileScaleText(int scaleN) =>
        scaleN == 2000 ? "1/200; 1/2000" : "1/500; 1/5000";

    private static double ProfileMapWidth(Spec spec) =>
        Math.Max(80, spec.Mx1 - spec.Mx0 - ProfileLeftInsetMm);

    /// <summary>Đưa mặt đất lên khoảng trên-giữa, vẫn giữ đáy bảng và đỉnh lý trình trong khung.</summary>
    private static double ProfileCenterY(ProfileBand band, double viewH)
    {
        var wanted = band.YGround - (ProfileGroundFraction - 0.5) * viewH;
        var minCenter = band.YTop + ProfileTopMargin - viewH / 2.0;
        var maxCenter = band.YCut - ProfileBottomMargin + viewH / 2.0;
        if (minCenter > maxCenter)
            return maxCenter;
        if (wanted < minCenter)
            return minCenter;
        if (wanted > maxCenter)
            return maxCenter;
        return wanted;
    }

    private static string FirstFittingSize(ProfileBand band, double unitM, int scale, string preferred)
    {
        if (Array.IndexOf(ProfileSizes, preferred) >= 0 && FitIssue(band, preferred, scale, unitM) is null)
            return preferred;
        foreach (var name in ProfileSizes)
        {
            if (FitIssue(band, name, scale, unitM) is null)
                return name;
        }

        return "A0";
    }

    private static string? FitIssue(ProfileBand band, string size, int scale, double unitM)
    {
        var spec = Specs.FirstOrDefault(s => s.Name.Equals(size, StringComparison.OrdinalIgnoreCase));
        if (spec is null)
            return "Khổ giấy không dùng cho mặt cắt dọc.";
        var custom = 1000.0 * unitM / scale;
        if (custom <= 0)
            return "Tỷ lệ không hợp lệ.";
        var need = (band.YTop - band.YCut) + ProfileBottomMargin + ProfileTopMargin;
        var capacity = (spec.My1 - spec.My0) / custom;
        if (need > capacity + 1)
            return $"Khổ {size} ở tỷ lệ này cao {capacity:0} đơn vị, dải mặt cắt cần {need:0}. Chọn khổ lớn hơn.";
        var labelMm = (band.XSplit - band.XLeft) * custom;
        var mapW = ProfileMapWidth(spec);
        if (labelMm > mapW * 0.45)
            return "Cột tên hàng chiếm quá nhiều chiều ngang của khổ này.";
        return null;
    }

    private static double StationWidthM(ProfileBand band, string size, int scale, double unitM)
    {
        var spec = Specs.First(s => s.Name.Equals(size, StringComparison.OrdinalIgnoreCase));
        var custom = 1000.0 * unitM / scale;
        var labelMm = (band.XSplit - band.XLeft) * custom;
        var stationMm = Math.Max(40, ProfileMapWidth(spec) - labelMm);
        return stationMm / custom * unitM;
    }

    private static List<(double X0, double X1)> SliceStations(ProfileBand band, string size, int scale, double overlapM, double unitM)
    {
        var spec = Specs.First(s => s.Name.Equals(size, StringComparison.OrdinalIgnoreCase));
        var custom = 1000.0 * unitM / scale;
        var labelMm = (band.XSplit - band.XLeft) * custom;
        var stationMm = Math.Max(40, ProfileMapWidth(spec) - labelMm);
        var usable = stationMm / custom;
        var overlap = overlapM / unitM;
        if (overlap > usable * 0.5)
            overlap = usable * 0.5;
        if (overlap < 0)
            overlap = 0;
        var x0 = band.XSplit;
        var x1 = band.XRight;
        var list = new List<(double X0, double X1)>();
        if (x1 - x0 <= usable)
        {
            list.Add((x0, x0 + usable));
            return list;
        }

        var step = Math.Max(usable * 0.25, usable - overlap);
        var x = x0;
        var guard = 0;
        while (guard++ < 500)
        {
            if (x + usable >= x1 - 0.5)
            {
                var start = Math.Max(x0, x1 - usable);
                list.Add((start, start + usable));
                break;
            }

            list.Add((x, x + usable));
            var next = x + step;
            if (next <= x + 0.1)
                break;
            x = next;
        }

        return list;
    }

    private static int BuildProfileLayout(
        Document doc, Spec spec, string template, RouteLayoutOptions opts, double unitM, ProfileBand band,
        List<(double X0, double X1)> slices, string layoutName, bool overwrite, out List<Extents2d> windows)
    {
        var db = doc.Database;
        var ed = doc.Editor;
        windows = [];
        using var _ = doc.LockDocument();
        var lm = LayoutManager.Current;
        if (overwrite)
        {
            lm.CurrentLayout = "Model";
            lm.DeleteLayout(layoutName);
        }

        var (paperW, paperH) = PaperSizeMm(spec);
        var mapW = ProfileMapWidth(spec);
        var mapH = spec.My1 - spec.My0;
        var originX = paperW - spec.Fw - 5.0;
        var originY = (paperH - spec.Fh) / 2.0;
        var custom = 1000.0 * unitM / opts.Scale;
        var labelMm = (band.XSplit - band.XLeft) * custom;
        var stationMm = mapW - labelMm;
        var viewH = mapH / custom;
        var centerY = ProfileCenterY(band, viewH);

        ObjectId blockId;
        string frameLayer;
        using (var trS = db.TransactionManager.StartTransaction())
        {
            blockId = EnsureBlock(db, trS, spec.Block, template);
            EnsureLayer(trS, db, LayerViewport);
            var lt = (LayerTable)trS.GetObject(db.LayerTableId, OpenMode.ForRead);
            var ltr = (LayerTableRecord)trS.GetObject(lt[LayerViewport], OpenMode.ForWrite);
            ltr.IsPlottable = false;
            frameLayer = lt.Has("KB_KHUNG") ? "KB_KHUNG" : "0";
            trS.Commit();
        }

        ApplyPendingLogos(doc, opts);
        ApplyLogoVisibility(doc, opts);
        var signBlocks = CollectSignatureBlocks(doc, opts);

        var layoutId = lm.CreateLayout(layoutName);
        lm.CurrentLayout = layoutName;
        lm.CurrentLayout = "Model";

        var viewportIds = new List<ObjectId>();
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var lay = (Layout)tr.GetObject(layoutId, OpenMode.ForWrite);
            ApplyPageSetup(lay, spec, paperW, paperH);
            var ps = (BlockTableRecord)tr.GetObject(lay.BlockTableRecordId, OpenMode.ForWrite);
            foreach (ObjectId id in ps)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is Viewport old && old.Number != 1)
                {
                    old.UpgradeOpen();
                    old.Erase();
                }
            }

            var def = (BlockTableRecord)tr.GetObject(blockId, OpenMode.ForRead);
            var attDefs = new List<AttributeDefinition>();
            foreach (ObjectId id in def)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is AttributeDefinition ad && !ad.Constant)
                    attDefs.Add(ad);
            }

            for (var i = 0; i < slices.Count; i++)
            {
                var slice = slices[i];
                var row = i / opts.Cols;
                var col = i % opts.Cols;
                var cx = col * (paperW + opts.GapMm);
                var cy = (opts.Rows - 1 - row) * (paperH + opts.GapMm);
                windows.Add(new Extents2d(cx, cy, cx + paperW, cy + paperH));

                var br = new BlockReference(new Point3d(cx + originX, cy + originY, 0), blockId) { Layer = frameLayer };
                ps.AppendEntity(br);
                tr.AddNewlyCreatedDBObject(br, true);
                foreach (var ad in attDefs)
                {
                    var ar = new AttributeReference();
                    ar.SetAttributeFromBlock(ad, br.BlockTransform);
                    ar.TextString = SheetAttributeText(ad.Tag, opts, i + 1, slices.Count, opts.Scale);
                    br.AttributeCollection.AppendAttribute(ar);
                    tr.AddNewlyCreatedDBObject(ar, true);
                    TitleBlockFit.Fit(tr, ar, br);
                }

                PlaceSignatures(tr, ps, br, signBlocks);

                viewportIds.Add(AddProfileViewport(tr, ps,
                    cx + originX + spec.Mx0 + ProfileLeftInsetMm + labelMm / 2.0,
                    cy + originY + spec.Cy,
                    labelMm, mapH,
                    (band.XLeft + band.XSplit) / 2.0, centerY, viewH, custom));
                viewportIds.Add(AddProfileViewport(tr, ps,
                    cx + originX + spec.Mx0 + ProfileLeftInsetMm + labelMm + stationMm / 2.0,
                    cy + originY + spec.Cy,
                    stationMm, mapH,
                    (slice.X0 + slice.X1) / 2.0, centerY, viewH, custom));
            }

            tr.Commit();
        }

        lm.CurrentLayout = layoutName;
        using (var tr = db.TransactionManager.StartTransaction())
        {
            foreach (var vid in viewportIds)
            {
                var vp = (Viewport)tr.GetObject(vid, OpenMode.ForWrite);
                vp.On = true;
                vp.Locked = true;
            }

            tr.Commit();
        }

        try
        {
            ed.Command("_.ZOOM", "_E");
        }
        catch
        {
            // chỉ để nhìn thấy cả lưới
        }

        return slices.Count;
    }

    private static ObjectId AddProfileViewport(
        Transaction tr, BlockTableRecord ps, double paperX, double paperY, double width, double height,
        double modelX, double modelY, double viewH, double custom)
    {
        var vp = new Viewport
        {
            CenterPoint = new Point3d(paperX, paperY, 0),
            Width = width,
            Height = height,
            Layer = LayerViewport
        };
        ps.AppendEntity(vp);
        tr.AddNewlyCreatedDBObject(vp, true);
        vp.ViewDirection = Vector3d.ZAxis;
        vp.ViewTarget = new Point3d(modelX, modelY, 0);
        vp.ViewCenter = Point2d.Origin;
        vp.TwistAngle = 0;
        vp.ViewHeight = viewH;
        vp.CustomScale = custom;
        return vp.ObjectId;
    }

    private static void SaveProfileChoices(RouteLayoutOptions opts, Dictionary<string, string> defaults)
    {
        try
        {
            var route = RouteLayoutSettings.Load();
            foreach (var f in FrameFields.All)
            {
                if (f.Auto || !opts.Attributes.TryGetValue(f.Tag, out var v))
                    continue;
                defaults.TryGetValue(f.Tag, out var d);
                if (!string.Equals(v, d ?? "", StringComparison.Ordinal))
                    route.Attributes[f.Tag] = v;
            }

            route.Save();
        }
        catch
        {
            // không chặn lệnh
        }

        new ProfileSheetSettings
        {
            Size = opts.Size,
            Scale = opts.Scale,
            OverlapM = opts.OverlapM,
            GapMm = opts.GapMm
        }.Save();
    }

    private sealed class ProfileSheetSettings
    {
        public string Size { get; set; } = "A3";
        public int Scale { get; set; } = 5000;
        public double OverlapM { get; set; } = 100;
        public double GapMm { get; set; } = 20;

        private static string FilePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "iSurvey", "profile_layout.json");

        public static ProfileSheetSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var s = JsonSerializer.Deserialize<ProfileSheetSettings>(File.ReadAllText(FilePath));
                    if (s is not null)
                        return s;
                }
            }
            catch
            {
                // file hỏng
            }

            return new ProfileSheetSettings();
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(FilePath)!;
                Directory.CreateDirectory(dir);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch
            {
                // không chặn lệnh
            }
        }
    }
}
