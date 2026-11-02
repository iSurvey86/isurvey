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
/// Mặt cắt ghi 1/500 và 1/5000 thì áp đúng khung 1/5000 (viewport 1:5000), khổ nhỏ nhất còn chứa dải.
/// Mỗi tờ là khung kỹ thuật, hai viewport cùng tỷ lệ: trái cố định tên hàng, phải trượt theo lý trình.
/// Không sửa hình trên Model.
/// </summary>
public sealed partial class FrameService
{
    private const string ProfileLayoutName = "MatCat_Doc";

    private static readonly int[] ProfileScales = [5000, 2000];
    private static readonly string[] ProfileScaleLabels = ["1/500; 1/5000", "1/200; 1/2000"];
    private static readonly string[] ProfileSizes = ["A3", "A2", "A1", "A0"];

    [CommandMethod("ISURVEY_PROFILE_SHEETS")]
    public void ProfileLayoutCommand() => RunProfile("ISURVEY_PROFILE_SHEETS");

    private static void RunProfile(string commandName)
    {
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;
        try
        {
            ProfileRun(doc, commandName);
        }
        catch (System.Exception ex)
        {
            ActivityLogger.TrackError(ex, commandName);
            doc.Editor.WriteMessage($"\n[iSurvey] Rải khung mặt cắt dọc lỗi: {ex.Message}");
        }
        finally
        {
            EnableScreenTyping();
        }
    }

    /// <summary>
    /// F12: gõ trên màn hình. Gán trong một lệnh thì AutoCAD trả lại giá trị cũ
    /// ngay khi lệnh đó thoát. Phải gán ở ngữ cảnh ứng dụng, lúc không còn lệnh nào.
    /// </summary>
    private static void EnableScreenTyping()
    {
        try
        {
            AcadApp.DocumentManager.ExecuteInApplicationContext(_ =>
            {
                try
                {
                    AcadApp.SetSystemVariable("DYNMODE", 3);
                }
                catch
                {
                    // không chặn lệnh
                }
            }, null);
        }
        catch
        {
            // không chặn lệnh
        }
    }

    private static void ProfileRun(Document doc, string commandName)
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

        if (!TryMeasureProfile(db, pick.Value, out var band, out var segs, out var why))
        {
            ed.WriteMessage("\n[iSurvey] " + why);
            return;
        }

        // Mặt cắt vẽ theo mét. Khung 1/5000 luôn coi 1 đơn vị = 1 m, kể cả khi INSUNITS để Feet.
        var unitM = 1.0;
        if (db.Insunits is not (UnitsValue.Meters or UnitsValue.Undefined))
            ed.WriteMessage("\n[iSurvey] Khung 1/5000 tính 1 đơn vị bản vẽ = 1 m.");

        var stationM = Math.Max(0, (band.XRight - band.XSplit) * unitM);
        ed.WriteMessage(
            $"\n[iSurvey] Mặt cắt dài {stationM:0.#} m, cao {band.YTop - band.YCut:0.#} đơn vị. Cột tên từ X {band.XLeft:0.#} đến {band.XSplit:0.#}.");

        var saved = ProfileSheetSettings.Load();
        var routeSaved = RouteLayoutSettings.Load();
        var scaleN = Array.IndexOf(ProfileScales, saved.Scale) >= 0 ? saved.Scale : 5000;
        var sizeName = FirstFittingSize(band, unitM, scaleN);
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
            Monochrome = saved.Monochrome,
            ScaleText = ProfileScaleText(scaleN),
            Attributes = attrs,
            HideOwnerLogo = TitleBlockFit.IsLogoHidden(db, owner: true),
            HideDesignLogo = TitleBlockFit.IsLogoHidden(db, owner: false)
        };

        int CountPieces(string size, int scale, double ovM) =>
            BuildFrames(segs, band, size, scale, ovM, unitM).Sum(g => g.Count);

        string? Reject(string size, int scale) => FitIssue(band, size, scale, unitM);

        SuggestGrid(CountPieces(opts.Size, opts.Scale, opts.OverlapM), out var rows0, out var cols0);
        opts.Rows = rows0;
        opts.Cols = cols0;

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

        if (FitIssue(band, opts.Size, opts.Scale, unitM) is { } unfit)
        {
            ed.WriteMessage("\n[iSurvey] " + unfit);
            return;
        }

        var spec = Specs.First(s => s.Name.Equals(opts.Size, StringComparison.OrdinalIgnoreCase));
        var groups = BuildFrames(segs, band, opts.Size, opts.Scale, opts.OverlapM, unitM);
        var sheetCount = groups.Sum(g => g.Count);
        var extra = sheetCount - groups.Count;
        if (opts.Cols < 1)
            opts.Cols = 1;
        if (extra > 0)
            ed.WriteMessage($"\n[iSurvey] {extra} tờ thêm phía trên các đoạn cao hơn khung. Tỷ lệ giữ nguyên, tờ dưới vẫn ghim dòng đã chọn.");

        ed.WriteMessage(
            $"\n[iSurvey] {sheetCount} tờ {spec.Name}, tỷ lệ {opts.ScaleText}, chồng gối {opts.OverlapM:0.#} m, {groups.Count} đoạn lý trình.");

        SaveProfileChoices(opts, defaults);

        var overwrite = false;
        var layoutName = ProfileLayoutName;
        if (LayoutExists(db, layoutName))
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

        _pageSetupIssue = null;
        var built = BuildProfileLayout(doc, spec, template, opts, unitM, band, groups, layoutName, overwrite, out var windows);
        ed.Regen();

        if (_pageSetupIssue is not null)
            ed.WriteMessage($"\n[iSurvey] Chưa đặt được khổ giấy cho layout ({_pageSetupIssue}) — chọn khổ giấy {spec.Name}, tỷ lệ in 1:1 trong Page Setup.");

        ed.WriteMessage(
            $"\n[iSurvey] Đã tạo layout {layoutName}: {built} tờ {spec.Name}, tỷ lệ {opts.ScaleText}. Viewport đã khóa. Gõ INTUYEN khi muốn in.");

        string? pdfNote = null;
        string? pdfIssue = null;
        if (pdfPath is not null && _pageSetupIssue is null)
        {
            var (pw, ph) = PaperSizeMm(spec);
            var stamp = opts.StampOnPdf && !string.IsNullOrEmpty(opts.StampPath) && File.Exists(opts.StampPath) ? opts.StampPath : null;
            var pdf = RoutePdfPlot.Plot(doc, layoutName, windows, pdfPath, pw, ph, opts.Monochrome, stamp, out pdfIssue);
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

        var done = $"Dàn trang mặt cắt dọc đã xong.\n\nLayout {layoutName}: {built} tờ {spec.Name}, tỷ lệ {opts.ScaleText}.";
        if (pdfNote is not null)
            done += $"\nĐã in {windows.Count} trang:\n{pdfPath}";
        else if (pdfIssue is not null)
            done += "\nChưa in được PDF: " + pdfIssue;
        else
            done += "\nChưa in PDF. Gõ INTUYEN khi muốn in layout này.";
        PdfResultDialog.Show(done, pdfNote is not null ? pdfPath : null);

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

    private static bool TryMeasureProfile(Database db, Point3d pick, out ProfileBand band, out List<Seg> segs, out string why)
    {
        band = default;
        segs = [];
        why = "";
        var found = new List<Seg>();
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
                    CollectSegs(ent, found);
                }
                catch
                {
                    // proxy không có khung
                }
            }

            tr.Commit();
        }

        segs = found;
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

        // Bề ngang theo các nét của bảng. Không đi theo tuyến kéo dài lên bản đồ.
        var right = xSplit;
        var grew = true;
        var guard = 0;
        while (grew && guard++ < 40)
        {
            grew = false;
            foreach (var s in segs)
            {
                if (s.MaxY - s.MinY > 8000)
                    continue;
                if (s.MaxX < xLeft - 8 || s.MinX < xLeft - 80 || s.MinX > right + 80)
                    continue;
                if (s.MinY > yCut + 700 || s.MaxY < yCut - 1)
                    continue;
                if (s.MaxX > right)
                {
                    right = s.MaxX;
                    grew = true;
                }
            }
        }

        var top = ProfileCeiling(segs, xLeft, right, yCut);

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

    /// <summary>
    /// Trần lưới mặt cắt: mức cao của các nét chạm bảng.
    /// Không lấy nét tuyến kéo từ bảng lên bản đồ hàng cây số.
    /// </summary>
    private static double ProfileCeiling(List<Seg> segs, double xLeft, double xRight, double yCut)
    {
        var tops = new List<double>();
        foreach (var s in segs)
        {
            if (s.MaxX < xLeft - 8 || s.MinX > xRight + 20)
                continue;
            if (s.MinY > yCut + 800 || s.MaxY < yCut + 200)
                continue;
            if (s.MaxY - s.MinY > 8000)
                continue;
            tops.Add(s.MaxY);
        }

        if (tops.Count == 0)
            return yCut + 800;
        tops.Sort();
        return tops[(int)((tops.Count - 1) * 0.7)];
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

    /// <summary>
    /// Vùng được vẽ, mm, gốc là điểm chèn khối KB_A*.
    /// Khối không có gốc ở góc tờ giấy: A3 chèn tại (0,0) thì khung tên bắt đầu ở x = 340.
    /// Đo trên bản rải: viewport tới x = chèn + 360 thì đè chữ khung tên (chữ nằm ở chèn + 341).
    /// A3 là (0, 0, 340, 277). Khổ khác lấy đúng ô Mx, My của khung.
    /// </summary>
    private static (double Left, double Bottom, double Width, double Height) ProfileDrawRect(Spec spec) =>
        (spec.Mx0, spec.My0, spec.Mx1 - spec.Mx0, spec.My1 - spec.My0);

    private static double ProfileMapWidth(Spec spec) => ProfileDrawRect(spec).Width;

    private static string FirstFittingSize(ProfileBand band, double unitM, int scale)
    {
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
        // Cao hơn tờ giấy thì thêm tờ phía trên, không ép sang khổ lớn.
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

    /// <summary>Một tờ: đoạn lý trình và mức cao độ neo ở lề dưới.</summary>
    private readonly record struct ProfileFrame(double X0, double X1, double YAnchor, bool PinTable);

    /// <summary>
    /// Chia theo lý trình, rồi mỗi đoạn cao hơn khung được tách thêm tờ phía trên.
    /// Tờ dưới ghim dòng đã chọn. Tờ trên giữ cùng tỷ lệ, không đổi khổ giấy.
    /// </summary>
    private static List<List<ProfileFrame>> BuildFrames(
        List<Seg> segs, ProfileBand band, string size, int scale, double overlapM, double unitM)
    {
        var slices = SliceStations(band, size, scale, overlapM, unitM);
        var groups = new List<List<ProfileFrame>>();
        foreach (var slice in slices)
            groups.Add(SplitHeight(segs, band.YCut, slice.X0, slice.X1, size, scale, unitM));
        return groups;
    }

    private static List<ProfileFrame> SplitHeight(
        List<Seg> segs, double yCut, double x0, double x1, string size, int scale, double unitM)
    {
        var spec = Specs.First(s => s.Name.Equals(size, StringComparison.OrdinalIgnoreCase));
        var custom = 1000.0 * unitM / scale;
        var room = ProfileDrawRect(spec).Height / custom;
        var overlap = Math.Min(room * 0.08, 20.0 / custom);
        var localTop = LocalTop(segs, x0, x1, yCut);
        var frames = new List<ProfileFrame>();
        var anchor = yCut;
        var pin = true;
        var guard = 0;
        while (guard++ < 16)
        {
            frames.Add(new ProfileFrame(x0, x1, anchor, pin));
            var top = anchor + room;
            if (localTop <= top + 0.5)
                break;
            var next = top - overlap;
            if (next <= anchor + 1)
                break;
            anchor = next;
            pin = false;
        }

        if (frames.Count > 1)
        {
            var last = frames[^1];
            var top = last.YAnchor + room;
            if (localTop > top + 0.5)
                frames[^1] = new ProfileFrame(last.X0, last.X1, Math.Max(frames[^2].YAnchor + 1, localTop - room), false);
        }

        return frames;
    }

    /// <summary>
    /// Đỉnh mặt cắt trong một đoạn lý trình.
    /// Lấy phân vị cao, không lấy một nét lạc lên bản đồ.
    /// </summary>
    private static double LocalTop(List<Seg> segs, double x0, double x1, double yCut)
    {
        var ys = new List<double>();
        foreach (var s in segs)
        {
            if (s.MaxY - s.MinY > 8000)
                continue;
            if (s.MaxX < x0 - 2 || s.MinX > x1 + 2)
                continue;
            if (s.MaxY < yCut + 5 || s.MinY > yCut + 20000)
                continue;
            ys.Add(s.MaxY);
        }

        if (ys.Count == 0)
            return yCut;
        ys.Sort();
        return ys[(int)((ys.Count - 1) * 0.99)];
    }

    private static int BuildProfileLayout(
        Document doc, Spec spec, string template, RouteLayoutOptions opts, double unitM, ProfileBand band,
        List<List<ProfileFrame>> groups, string layoutName, bool overwrite, out List<Extents2d> windows)
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
        var custom = 1000.0 * unitM / opts.Scale;
        var labelMm = (band.XSplit - band.XLeft) * custom;
        var sheetTotal = groups.Sum(g => g.Count);
        var sheetNo = 0;

        ObjectId blockId;
        string frameLayer;
        using (var trS = db.TransactionManager.StartTransaction())
        {
            blockId = EnsureBlock(db, trS, spec.Block, template);
            EnsureLayer(trS, db, LayerViewport);
            var lt = (LayerTable)trS.GetObject(db.LayerTableId, OpenMode.ForRead);
            var ltr = (LayerTableRecord)trS.GetObject(lt[LayerViewport], OpenMode.ForWrite);
            ltr.IsPlottable = true;
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
        var viewportFit = new List<ProfileVp>();
        var frameIds = new List<ObjectId>();
        var signIds = new List<ObjectId>();
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var lay = (Layout)tr.GetObject(layoutId, OpenMode.ForWrite);
            ApplyPageSetup(lay, spec, paperW, paperH, opts.Monochrome);
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

            var cols = Math.Max(1, opts.Cols);
            var stationRows = (int)Math.Ceiling(groups.Count / (double)cols);
            var yBase = 0.0;
            for (var sr = 0; sr < stationRows; sr++)
            {
                var rowGroups = groups.Skip(sr * cols).Take(cols).ToList();
                var bandCount = rowGroups.Max(g => g.Count);
                for (var col = 0; col < rowGroups.Count; col++)
                {
                    var bands = rowGroups[col];
                    for (var b = 0; b < bands.Count; b++)
                    {
                        var frame = bands[b];
                        var cx = col * (paperW + opts.GapMm);
                        var cy = yBase + b * (paperH + opts.GapMm);
                        windows.Add(new Extents2d(cx, cy, cx + paperW, cy + paperH));
                        sheetNo++;

                        var br = new BlockReference(new Point3d(cx, cy, 0), blockId) { Layer = frameLayer };
                        ps.AppendEntity(br);
                        tr.AddNewlyCreatedDBObject(br, true);
                        frameIds.Add(br.ObjectId);
                        foreach (var ad in attDefs)
                        {
                            var ar = new AttributeReference();
                            ar.SetAttributeFromBlock(ad, br.BlockTransform);
                            ar.TextString = SheetAttributeText(ad.Tag, opts, sheetNo, sheetTotal, opts.Scale);
                            br.AttributeCollection.AppendAttribute(ar);
                            tr.AddNewlyCreatedDBObject(ar, true);
                            TitleBlockFit.Fit(tr, ar, br);
                        }

                        PlaceSignatures(tr, ps, br, signBlocks, signIds);

                        // Chỉ vẽ trong ô bản đồ của khung. A3: 340 × 277 mm. Không tràn khung tên, không thấp hơn ô này.
                        var draw = ProfileDrawRect(spec);
                        var mapLeft = cx + draw.Left;
                        var mapBottom = cy + draw.Bottom;
                        var mapW = draw.Width;
                        var mapH = draw.Height;
                        var labelPaper = Math.Min(Math.Max(8, labelMm), mapW * 0.42);
                        var labelModelW = labelPaper / custom;
                        var rightPaperW = mapW - labelPaper;
                        var paperY = mapBottom + mapH / 2.0;
                        var shownH = mapH / custom;
                        var centerY = frame.YAnchor + shownH / 2.0;

                        var leftFit = new ProfileVp(ObjectId.Null,
                            mapLeft + labelPaper / 2.0, paperY, labelPaper, mapH,
                            band.XLeft + labelModelW / 2.0, centerY, shownH, custom);
                        var leftId = AddProfileViewport(tr, ps, leftFit);
                        viewportIds.Add(leftId);
                        viewportFit.Add(leftFit with { Id = leftId });

                        var rightLeft = mapLeft + labelPaper;
                        var rightFit = new ProfileVp(ObjectId.Null,
                            rightLeft + rightPaperW / 2.0, paperY, rightPaperW, mapH,
                            frame.X0 + (rightPaperW / custom) / 2.0, centerY, shownH, custom);
                        var rightId = AddProfileViewport(tr, ps, rightFit);
                        viewportIds.Add(rightId);
                        viewportFit.Add(rightFit with { Id = rightId });
                    }
                }

                yBase += bandCount * (paperH + opts.GapMm);
            }

            tr.Commit();
        }

        lm.CurrentLayout = layoutName;

        var mismatched = 0;
        foreach (var fit in viewportFit)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var vp = (Viewport)tr.GetObject(fit.Id, OpenMode.ForWrite);
                vp.On = true;
                tr.Commit();
            }

            try
            {
                ed.Regen();
            }
            catch
            {
                // regen không chặn lệnh
            }

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var vp = (Viewport)tr.GetObject(fit.Id, OpenMode.ForWrite);
                if (vp.Locked)
                    vp.Locked = false;
                ApplyProfileView(vp, fit);
                if (!ProfilePaperMatches(vp, fit))
                    ApplyProfileView(vp, fit);
                var lay = (Layout)tr.GetObject(layoutId, OpenMode.ForRead);
                var space = (BlockTableRecord)tr.GetObject(lay.BlockTableRecordId, OpenMode.ForWrite);
                ClipProfileViewport(tr, space, vp, fit);
                ApplyProfileView(vp, fit);
                tr.Commit();
            }
        }

        using (var tr = db.TransactionManager.StartTransaction())
        {
            if (frameIds.Count > 0 || viewportIds.Count > 0)
            {
                var lay = (Layout)tr.GetObject(layoutId, OpenMode.ForRead);
                var space = (BlockTableRecord)tr.GetObject(lay.BlockTableRecordId, OpenMode.ForRead);
                var order = (DrawOrderTable)tr.GetObject(space.DrawOrderTableId, OpenMode.ForWrite);
                if (viewportIds.Count > 0)
                {
                    var under = new ObjectIdCollection();
                    foreach (var id in viewportIds)
                        under.Add(id);
                    order.MoveToBottom(under);
                }

                if (frameIds.Count > 0)
                {
                    var frames = new ObjectIdCollection();
                    foreach (var id in frameIds)
                        frames.Add(id);
                    order.MoveToTop(frames);
                }

                if (signIds.Count > 0)
                {
                    var signs = new ObjectIdCollection();
                    foreach (var id in signIds)
                        signs.Add(id);
                    order.MoveToTop(signs);
                }
            }

            foreach (var fit in viewportFit)
            {
                var vp = (Viewport)tr.GetObject(fit.Id, OpenMode.ForWrite);
                if (!ProfilePaperMatches(vp, fit))
                {
                    if (vp.Locked)
                        vp.Locked = false;
                    ApplyProfileView(vp, fit);
                }

                if (!ProfilePaperMatches(vp, fit))
                    mismatched++;
                vp.Locked = true;
            }

            tr.Commit();
        }

        if (mismatched > 0)
            ed.WriteMessage($"\n[iSurvey] {mismatched} cửa sổ chưa khớp vùng vẽ của khung.");

        try
        {
            ed.Command("_.PSPACE");
        }
        catch
        {
            // đã ở paper space
        }

        try
        {
            ed.Command("_.ZOOM", "_E");
        }
        catch
        {
            // chỉ để nhìn thấy cả lưới
        }

        return sheetNo;
    }

    private static ObjectId AddProfileViewport(Transaction tr, BlockTableRecord ps, ProfileVp fit)
    {
        var vp = new Viewport
        {
            CenterPoint = new Point3d(fit.X, fit.Y, 0),
            Width = fit.W,
            Height = fit.H,
            Layer = LayerViewport
        };
        ps.AppendEntity(vp);
        tr.AddNewlyCreatedDBObject(vp, true);
        vp.ViewDirection = Vector3d.ZAxis;
        vp.ViewTarget = new Point3d(fit.ModelX, fit.ModelY, 0);
        vp.ViewCenter = Point2d.Origin;
        vp.TwistAngle = 0;
        vp.ViewHeight = fit.ViewH;
        vp.CustomScale = fit.Scale;
        return vp.ObjectId;
    }

    private readonly record struct ProfileVp(
        ObjectId Id, double X, double Y, double W, double H, double ModelX, double ModelY, double ViewH, double Scale);

    /// <summary>
    /// Gán khung nhìn rồi gán lại hình chữ nhật giấy. Bật viewport và gán tỷ lệ hay nới cửa sổ ra ngoài tờ.
    /// </summary>
    private static void ApplyProfileView(Viewport vp, ProfileVp fit)
    {
        vp.ViewDirection = Vector3d.ZAxis;
        vp.TwistAngle = 0;
        vp.ViewTarget = new Point3d(fit.ModelX, fit.ModelY, 0);
        vp.ViewCenter = Point2d.Origin;
        vp.ViewHeight = fit.ViewH;
        vp.CustomScale = fit.Scale;
        vp.CenterPoint = new Point3d(fit.X, fit.Y, 0);
        vp.Width = fit.W;
        vp.Height = fit.H;
    }

    private static bool ProfilePaperMatches(Viewport vp, ProfileVp fit) =>
        Math.Abs(vp.Width - fit.W) < 0.8
        && Math.Abs(vp.Height - fit.H) < 0.8
        && Math.Abs(vp.CenterPoint.X - fit.X) < 0.8
        && Math.Abs(vp.CenterPoint.Y - fit.Y) < 0.8
        && vp.CustomScale > 1e-9
        && Math.Abs(vp.CustomScale - fit.Scale) / fit.Scale < 0.02;

    /// <summary>Cắt cửa sổ đúng hình chữ nhật vùng vẽ, để hình không tràn ra ngoài khung dù AutoCAD nới viewport.</summary>
    private static void ClipProfileViewport(Transaction tr, BlockTableRecord ps, Viewport vp, ProfileVp fit)
    {
        var x0 = fit.X - fit.W / 2.0;
        var y0 = fit.Y - fit.H / 2.0;
        var x1 = fit.X + fit.W / 2.0;
        var y1 = fit.Y + fit.H / 2.0;
        var pl = new Polyline { Layer = LayerViewport, Closed = true };
        pl.AddVertexAt(0, new Point2d(x0, y0), 0, 0, 0);
        pl.AddVertexAt(1, new Point2d(x1, y0), 0, 0, 0);
        pl.AddVertexAt(2, new Point2d(x1, y1), 0, 0, 0);
        pl.AddVertexAt(3, new Point2d(x0, y1), 0, 0, 0);
        ps.AppendEntity(pl);
        tr.AddNewlyCreatedDBObject(pl, true);
        vp.NonRectClipEntityId = pl.ObjectId;
        vp.NonRectClipOn = true;
    }

    /// <summary>Lớp viewport phải in được, nếu không PDF chỉ còn nền trắng.</summary>
    internal static void MakeViewportsPlottable(Database db)
    {
        using var tr = db.TransactionManager.StartTransaction();
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (!lt.Has(LayerViewport))
        {
            tr.Commit();
            return;
        }

        var ltr = (LayerTableRecord)tr.GetObject(lt[LayerViewport], OpenMode.ForWrite);
        ltr.IsPlottable = true;
        tr.Commit();
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
            GapMm = opts.GapMm,
            Monochrome = opts.Monochrome
        }.Save();
    }

    private sealed class ProfileSheetSettings
    {
        public string Size { get; set; } = "A3";
        public int Scale { get; set; } = 5000;
        public double OverlapM { get; set; } = 100;
        public double GapMm { get; set; } = 20;
        public bool Monochrome { get; set; }

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
