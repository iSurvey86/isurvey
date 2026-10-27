using System.Globalization;
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
/// DANTRANG (ISURVEY_ROUTE_SHEETS): chia tuyến thành các mảnh in (thuật toán tham lam dọc tuyến, chồng gối), vẽ đường bao
/// mảnh trên Model, rồi dàn TẤT CẢ các khung lên MỘT layout theo lưới Hàng × Cột. Mỗi khung là block KB_A* 1:1 giấy kèm
/// một Viewport đã khóa tỷ lệ và xoay (Twist) để đoạn tuyến nằm ngang. Không sửa tọa độ thật trên Model.
/// Tùy chọn in gộp một file PDF nhiều trang ngay sau khi tạo.
/// </summary>
public sealed partial class FrameService
{
    private const string LayerPiece = "ISURVEY_MANH";
    private const string LayerViewport = "KB_VIEWPORT";
    private const string RouteLayoutName = "BanDo_Tuyen";

    /// <summary>Tỷ lệ cho DANTRANG (thêm 1/10000 so với lệnh KHUNG).</summary>
    private static readonly int[] RouteScales = [200, 500, 1000, 2000, 5000, 10000];

    [CommandMethod("ISURVEY_ROUTE_SHEETS")]
    public void RouteLayoutCommand() => RunRouteLayout(withDialog: true, "ISURVEY_ROUTE_SHEETS");

    /// <summary>Như DANTRANG nhưng không hộp thoại: dùng thiết lập đã lưu (route_layout.json); layout trùng tên thì tạo tên mới. Dùng kiểm thử/tự động hóa.</summary>
    [CommandMethod("ISURVEY_ROUTE_SHEETS_QUICK")]
    public void RouteLayoutQuickCommand() => RunRouteLayout(withDialog: false, "ISURVEY_ROUTE_SHEETS_QUICK");

    /// <summary>In lại layout BanDo_Tuyen đã có, không dựng trang mới.</summary>
    [CommandMethod("ISURVEY_ROUTE_PLOT")]
    public void RoutePlotCommand()
    {
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;
        try
        {
            PlotExistingRoute(doc);
        }
        catch (System.Exception ex)
        {
            ActivityLogger.TrackError(ex, "ISURVEY_ROUTE_PLOT");
            doc.Editor.WriteMessage($"\n[iSurvey] In layout lỗi: {ex.Message}");
        }
    }

    private static void RunRouteLayout(bool withDialog, string commandName)
    {
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        try
        {
            RouteLayoutRun(doc, withDialog, commandName);
        }
        catch (System.Exception ex)
        {
            ActivityLogger.TrackError(ex, commandName);
            doc.Editor.WriteMessage($"\n[iSurvey] Cắt tuyến thành layout lỗi: {ex.Message}");
        }
    }

    private static void RouteLayoutRun(Document doc, bool withDialog, string commandName)
    {
        var db = doc.Database;
        var ed = doc.Editor;

        var template = ResolveTemplate();
        if (template is null)
        {
            ed.WriteMessage($"\n[iSurvey] Không thấy file khung mẫu {TemplateName} (Contents\\Templates của bundle).");
            return;
        }

        // Đang ở layout (sau lần chạy trước) thì không chọn được tuyến trên Model: về Model trước.
        if (!LayoutManager.Current.CurrentLayout.Equals("Model", StringComparison.OrdinalIgnoreCase))
            LayoutManager.Current.CurrentLayout = "Model";

        // 1) Chọn tuyến + điểm đầu
        if (!PickRoute(doc, out var route))
            return;

        var unitM = UnitMeters(db.Insunits, out var unitNote);
        if (unitNote is not null)
            ed.WriteMessage("\n[iSurvey] " + unitNote);
        var routeLenM = route.Length * unitM;

        // 2) Thiết lập: hộp thoại hoặc thiết lập đã lưu
        var settings = RouteLayoutSettings.Load();
        var sizeName = Specs.Any(s => s.Name.Equals(settings.Size, StringComparison.OrdinalIgnoreCase)) ? settings.Size.ToUpperInvariant() : "A3";
        var scaleN = Array.IndexOf(RouteScales, settings.Scale) >= 0 ? settings.Scale : 1000;

        double MapWidthM(string size, int scale) =>
            (Specs.First(s => s.Name.Equals(size, StringComparison.OrdinalIgnoreCase)) is var sp ? (sp.Mx1 - sp.Mx0) : 0) * scale / 1000.0;

        var overlapM = settings.OverlapM >= 0 ? settings.OverlapM : Math.Max(1, Math.Round(MapWidthM(sizeName, scaleN) * 0.1));

        int CountPieces(string size, int scale, double ovM)
        {
            var sp = Specs.First(s => s.Name.Equals(size, StringComparison.OrdinalIgnoreCase));
            var k = scale / (1000.0 * unitM);
            var w = (sp.Mx1 - sp.Mx0) * k;
            var h = (sp.My1 - sp.My0) * k;
            return Segment(route, w, h, Math.Min(ovM / unitM, w * 0.5)).Count;
        }

        var defaults = ReadFrameDefaults(template, sizeName);
        var attrs = new Dictionary<string, string>(defaults, StringComparer.OrdinalIgnoreCase);
        foreach (var kv in settings.Attributes)
            attrs[kv.Key] = FrameFields.Clean(kv.Value);

        var opts = new RouteLayoutOptions
        {
            Size = sizeName,
            Scale = scaleN,
            OverlapM = overlapM,
            GapMm = settings.GapMm,
            PlotPdf = false,
            Attributes = attrs,
            HideOwnerLogo = TitleBlockFit.IsLogoHidden(db, owner: true),
            HideDesignLogo = TitleBlockFit.IsLogoHidden(db, owner: false)
        };
        SuggestGrid(CountPieces(opts.Size, opts.Scale, opts.OverlapM), out var rows0, out var cols0);
        opts.Rows = rows0;
        opts.Cols = cols0;

        if (withDialog)
        {
            var input = new RouteLayoutDialogInput
            {
                RouteLengthM = routeLenM,
                Sizes = Specs.Select(s => s.Name).ToArray(),
                Scales = RouteScales,
                Initial = opts,
                TemplateDefaults = defaults,
                CountPieces = CountPieces,
                MapWidthM = MapWidthM,
                SuggestGrid = (n) => { SuggestGrid(n, out var r, out var c); return (r, c); }
            };
            var result = RouteLayoutWindow.Show(input);
            if (result is null)
            {
                ed.WriteMessage("\n[iSurvey] Đã hủy.");
                return;
            }

            opts = result;
        }

        var spec = Specs.First(s => s.Name.Equals(opts.Size, StringComparison.OrdinalIgnoreCase));
        scaleN = opts.Scale;
        var s0 = scaleN / (1000.0 * unitM);
        var wm = (spec.Mx1 - spec.Mx0) * s0;
        var hm = (spec.My1 - spec.My0) * s0;
        var overlap = opts.OverlapM / unitM;
        if (overlap > wm * 0.5)
        {
            overlap = wm * 0.5;
            ed.WriteMessage($"\n[iSurvey] Chồng gối lớn hơn nửa chiều dài bản đồ — dùng {overlap * unitM:0.#} m.");
        }

        var pieces = Segment(route, wm, hm, overlap);
        if (opts.Rows * opts.Cols < pieces.Count)
        {
            ed.WriteMessage($"\n[iSurvey] Lưới {opts.Rows}×{opts.Cols} không đủ chỗ cho {pieces.Count} mảnh — tự mở rộng.");
            SuggestGrid(pieces.Count, out var r, out var c);
            opts.Rows = r;
            opts.Cols = c;
        }

        ed.WriteMessage(
            $"\n[iSurvey] Tuyến dài {routeLenM:0.#} m → {pieces.Count} mảnh khổ {spec.Name} 1/{scaleN}, chồng gối {opts.OverlapM:0.#} m, lưới {opts.Rows}×{opts.Cols}.");

        SaveSettings(opts, defaults);

        // 3) Tên layout: có sẵn thì hỏi ghi đè hay tạo tên mới
        var overwrite = false;
        var layoutName = RouteLayoutName;
        var exists = LayoutExists(db, layoutName);
        if (exists)
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
                while (LayoutExists(db, $"{RouteLayoutName}-{n}"))
                    n++;
                layoutName = $"{RouteLayoutName}-{n}";
            }
        }

        // 4) Đường dẫn PDF
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

        // 5) Tạo layout (+ logo, outline trên Model)
        _pageSetupIssue = null;
        var built = BuildGridLayout(doc, spec, template, opts, s0, unitM, pieces, layoutName, overwrite, out var windows);
        ed.Regen();

        if (_pageSetupIssue is not null)
            ed.WriteMessage($"\n[iSurvey] Chưa đặt được khổ giấy cho layout ({_pageSetupIssue}) — chọn khổ giấy {spec.Name}, tỷ lệ in 1:1 trong Page Setup.");

        ed.WriteMessage(
            $"\n[iSurvey] Đã tạo layout {layoutName}: {built} khung {spec.Name} 1/{scaleN} (lưới {opts.Rows}×{opts.Cols}, cách {opts.GapMm:0.#} mm). " +
            $"Đường bao mảnh vẽ trên Model (layer {LayerPiece}, không in). Viewport đã khóa tỷ lệ/hướng.");

        // 6) In PDF gộp
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
            var done = $"Dàn trang in đã xong.\n\nLayout {layoutName}: {built} tờ {spec.Name}, tỷ lệ 1/{scaleN}.";
            if (pdfNote is not null)
                done += $"\nĐã in {windows.Count} trang:\n{pdfPath}";
            else if (pdfIssue is not null)
                done += "\nChưa in được PDF: " + pdfIssue;
            else
                done += "\nChưa in PDF. Gõ INTUYEN khi muốn in layout này.";
            System.Windows.MessageBox.Show(done, "iSurvey", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        var note = $"layout {layoutName}: {built} khung {spec.Name} 1/{scaleN}, tuyến {routeLenM:0.#} m, chồng gối {opts.OverlapM:0.#} m{pdfNote}";
        ActivityLogger.Enqueue("dantrang", commandName, new
        {
            size = spec.Name,
            scale = scaleN,
            pieces = built,
            rows = opts.Rows,
            cols = opts.Cols,
            lengthM = Math.Round(routeLenM, 1),
            overlapM = opts.OverlapM,
            pdf = pdfNote is not null,
            drawing = ActivityLogger.FileName(db.Filename)
        });
        ActivityLogger.Result(commandName, note);
    }

    // -------------------------------------------------------------------------------------------------
    //  Chọn tuyến, gợi ý lưới, thiết lập
    // -------------------------------------------------------------------------------------------------
    private static bool PickRoute(Document doc, out RouteGeom route)
    {
        route = null!;
        var db = doc.Database;
        var ed = doc.Editor;
        List<Point2d> pts;
        var closed = false;
        while (true)
        {
            var peo = new PromptEntityOptions("\niSurvey: Chọn tuyến (Polyline, Line, Arc, Spline hoặc Alignment): ") { AllowNone = false };
            var per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\n[iSurvey] Đã hủy.");
                return false;
            }

            using var trRead = db.TransactionManager.StartTransaction();
            var ent = (Entity)trRead.GetObject(per.ObjectId, OpenMode.ForRead);
            var flat = FlattenRoute(ent, trRead, out closed);
            if (flat is null)
            {
                ed.WriteMessage($"\n[iSurvey] Đối tượng {ent.GetType().Name} không phải đường tuyến. Chọn lại.");
                continue;
            }

            pts = flat;
            trRead.Commit();
            break;
        }

        // Điểm đầu tuyến (hướng đánh số mảnh). Enter = theo chiều vẽ.
        var ppStart = new PromptPointOptions("\niSurvey: Chọn điểm đầu tuyến (Enter = theo chiều vẽ): ") { AllowNone = true };
        var prStart = ed.GetPoint(ppStart);
        if (prStart.Status == PromptStatus.Cancel)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy.");
            return false;
        }

        if (prStart.Status == PromptStatus.OK && !closed)
        {
            var sp = prStart.Value.TransformBy(ed.CurrentUserCoordinateSystem);
            var dFirst = Math.Sqrt(Math.Pow(sp.X - pts[0].X, 2) + Math.Pow(sp.Y - pts[0].Y, 2));
            var dLast = Math.Sqrt(Math.Pow(sp.X - pts[^1].X, 2) + Math.Pow(sp.Y - pts[^1].Y, 2));
            if (dLast < dFirst)
                pts.Reverse();
        }

        route = new RouteGeom(pts);
        if (route.Length < 1e-6)
        {
            ed.WriteMessage("\n[iSurvey] Tuyến quá ngắn.");
            return false;
        }

        return true;
    }

    /// <summary>Gợi ý lưới gần dạng ngang (khoảng 1,5 lần): cột = ⌈√(1,5·N)⌉, hàng = ⌈N / cột⌉, tối đa 99.</summary>
    private static void SuggestGrid(int n, out int rows, out int cols)
    {
        n = Math.Max(1, n);
        cols = Math.Min(99, Math.Max(1, (int)Math.Ceiling(Math.Sqrt(1.5 * n))));
        rows = Math.Min(99, (int)Math.Ceiling(n / (double)cols));
    }

    private static bool LayoutExists(Database db, string name)
    {
        using var tr = db.TransactionManager.StartTransaction();
        var dict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
        return dict.Contains(name);
    }

    /// <summary>Giá trị mặc định của các ô khung tên: lấy từ định nghĩa thuộc tính (ATTDEF) trong khung mẫu.</summary>
    private static Dictionary<string, string> ReadFrameDefaults(string template, string sizeName)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var src = new Database(false, true);
            src.ReadDwgFile(template, FileOpenMode.OpenForReadAndAllShare, true, null);
            using var tr = src.TransactionManager.StartTransaction();
            var bt = (BlockTable)tr.GetObject(src.BlockTableId, OpenMode.ForRead);
            var blockName = "KB_" + sizeName;
            if (bt.Has(blockName))
            {
                var def = (BlockTableRecord)tr.GetObject(bt[blockName], OpenMode.ForRead);
                foreach (ObjectId id in def)
                {
                    if (tr.GetObject(id, OpenMode.ForRead) is AttributeDefinition ad && !ad.Constant)
                        result[ad.Tag] = FrameFields.Clean(AttributeText(ad.Tag, ad.TextString, 1000));
                }
            }

            tr.Commit();
        }
        catch
        {
            // không đọc được mẫu: ô để trống
        }

        foreach (var f in FrameFields.All)
            if (!result.ContainsKey(f.Tag))
                result[f.Tag] = "";
        return result;
    }

    /// <summary>Lưu lại thiết lập; ô khung tên chỉ lưu những ô khác mặc định của khung mẫu.</summary>
    private static void SaveSettings(RouteLayoutOptions o, Dictionary<string, string> defaults)
    {
        var s = new RouteLayoutSettings
        {
            Size = o.Size,
            Scale = o.Scale,
            OverlapM = o.OverlapM,
            GapMm = o.GapMm,
            PlotPdf = o.PlotPdf
        };
        foreach (var f in FrameFields.All)
        {
            if (f.Auto || !o.Attributes.TryGetValue(f.Tag, out var v))
                continue;
            defaults.TryGetValue(f.Tag, out var d);
            if (!string.Equals(v, d ?? "", StringComparison.Ordinal))
                s.Attributes[f.Tag] = v;
        }

        s.Save();
    }

    private static string DefaultPdfPath(Database db, string layoutName)
    {
        string dir;
        string stem;
        if (!string.IsNullOrEmpty(db.Filename) && File.Exists(db.Filename))
        {
            dir = Path.GetDirectoryName(db.Filename)!;
            stem = Path.GetFileNameWithoutExtension(db.Filename);
        }
        else
        {
            dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            stem = "BanVe";
        }

        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
        return Path.Combine(dir, $"{stem}-{stamp}.pdf");
    }

    // -------------------------------------------------------------------------------------------------
    //  Layout lưới + Viewport

    // -------------------------------------------------------------------------------------------------
    private static int BuildGridLayout(
        Document doc, Spec spec, string template, RouteLayoutOptions opts, double s0, double unitM, List<Piece> pieces,
        string layoutName, bool overwrite, out List<Extents2d> windows)
    {
        var db = doc.Database;
        var ed = doc.Editor;
        var scaleN = opts.Scale;
        windows = [];
        using var _ = doc.LockDocument();
        var lm = LayoutManager.Current;

        // Ghi đè: bỏ layout cũ + đường bao mảnh cũ trên Model
        if (overwrite)
        {
            lm.CurrentLayout = "Model";
            lm.DeleteLayout(layoutName);
            using var trC = db.TransactionManager.StartTransaction();
            var bt0 = (BlockTable)trC.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)trC.GetObject(bt0[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            foreach (ObjectId id in ms)
            {
                if (trC.GetObject(id, OpenMode.ForRead) is Entity e && e.Layer.Equals(LayerPiece, StringComparison.OrdinalIgnoreCase))
                {
                    e.UpgradeOpen();
                    e.Erase();
                }
            }

            trC.Commit();
        }

        var (paperW, paperH) = PaperSizeMm(spec);
        var mapW = spec.Mx1 - spec.Mx0;
        var mapH = spec.My1 - spec.My0;
        // gốc khối khung trên giấy: lề trái 25 mm (đóng tập), dưới 10 mm (khớp khung mẫu KB_A0…KB_A4)
        var originX = paperW - spec.Fw - 5.0;
        var originY = (paperH - spec.Fh) / 2.0;
        var customScale = 1000.0 * unitM / scaleN;

        ObjectId blockId;
        string frameLayer;
        ObjectId style;
        using (var trS = db.TransactionManager.StartTransaction())
        {
            blockId = EnsureBlock(db, trS, spec.Block, template);
            EnsureLayer(trS, db, LayerPiece);
            EnsureLayer(trS, db, LayerViewport);
            var lt = (LayerTable)trS.GetObject(db.LayerTableId, OpenMode.ForRead);
            foreach (var ln in new[] { LayerPiece, LayerViewport })
            {
                var ltr = (LayerTableRecord)trS.GetObject(lt[ln], OpenMode.ForWrite);
                ltr.IsPlottable = false;
            }

            frameLayer = lt.Has("KB_KHUNG") ? "KB_KHUNG" : "0";
            style = TextStyleId(trS, db);
            trS.Commit();
        }

        // Logo thay thế (áp cho cả bản vẽ, trước khi chèn khung để khung nhận đúng logo)
        ApplyPendingLogos(doc, opts);
        ApplyLogoVisibility(doc, opts);
        var signBlocks = ImportSignatureBlocks(doc, opts);

        // Pha 0: tạo layout, chuyển sang nó một lần để AutoCAD khởi tạo vùng giấy riêng, rồi quay về Model.
        var layoutId = lm.CreateLayout(layoutName);
        lm.CurrentLayout = layoutName;
        lm.CurrentLayout = "Model";

        // Pha 1: không chuyển layout — khung + khung tên, viewport (còn tắt), đường bao mảnh trên Model.
        // (Tạo chữ/đo chữ khi layout đang là layout hiện hành làm Civil/AutoCAD sập ngẫu nhiên, nên tách ra.)
        var viewportIds = new List<ObjectId>();
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var model = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
            var lay = (Layout)tr.GetObject(layoutId, OpenMode.ForWrite);
            ApplyPageSetup(lay, spec, paperW, paperH);

            var ps = (BlockTableRecord)tr.GetObject(lay.BlockTableRecordId, OpenMode.ForWrite);
            // bỏ viewport phụ có sẵn (nếu có); giữ viewport số 1 = chính tờ giấy
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

            for (var i = 0; i < pieces.Count; i++)
            {
                var pc = pieces[i];
                var row = i / opts.Cols;
                var col = i % opts.Cols;
                // ô (row, col): hàng 0 ở trên cùng, mỗi ô đúng một tờ giấy
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
                    ar.TextString = SheetAttributeText(ad.Tag, opts, pc.Index, pieces.Count, scaleN);
                    br.AttributeCollection.AppendAttribute(ar);
                    tr.AddNewlyCreatedDBObject(ar, true);
                    TitleBlockFit.Fit(tr, ar, br);
                }

                PlaceSignatures(tr, ps, br, signBlocks);

                // Viewport: tâm vùng nhìn = tâm mảnh, xoay ngược góc tuyến → tuyến nằm ngang
                var twist = Math.PI * 2 - NormalizeAngle(pc.Phi);
                if (twist >= Math.PI * 2 - 1e-12)
                    twist = 0;
                var vp = new Viewport
                {
                    CenterPoint = new Point3d(cx + originX + spec.Cx, cy + originY + spec.Cy, 0),
                    Width = mapW,
                    Height = mapH,
                    Layer = LayerViewport
                };
                ps.AppendEntity(vp);
                tr.AddNewlyCreatedDBObject(vp, true);
                vp.ViewDirection = Vector3d.ZAxis;
                vp.ViewTarget = new Point3d(pc.Center.X, pc.Center.Y, 0);
                vp.ViewCenter = Point2d.Origin;
                vp.TwistAngle = twist;
                vp.ViewHeight = mapH / customScale;
                vp.CustomScale = customScale;
                viewportIds.Add(vp.ObjectId);

                DrawPieceOutline(tr, db, model, style, pc, mapW * s0, mapH * s0, s0, pieces.Count, spec.Name);
            }

            tr.Commit();
        }

        // Pha 2: viewport chỉ bật (On) và khóa được khi layout của nó đang là layout hiện hành.
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

        return pieces.Count;
    }

    private static void ApplyPendingLogos(Document doc, RouteLayoutOptions opts)
    {
        if (string.IsNullOrEmpty(opts.LogoOwnerPath) && string.IsNullOrEmpty(opts.LogoDesignPath))
            return;

        var db = doc.Database;
        foreach (var (path, owner) in new[] { (opts.LogoOwnerPath, true), (opts.LogoDesignPath, false) })
        {
            if (string.IsNullOrEmpty(path))
                continue;
            try
            {
                var image = LogoReplaceService.PrepareImage(db, path, owner);
                using var tr = db.TransactionManager.StartTransaction();
                var res = LogoReplaceService.Replace(tr, db, owner, image);
                if (res.Logos > 0)
                    tr.Commit();
                doc.Editor.WriteMessage("\n[iSurvey] " + res.Message + ".");
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage($"\n[iSurvey] Không thay được logo ({ex.Message}).");
            }
        }
    }

    /// <summary>Ẩn/hiện logo từng bên theo lựa chọn trong hộp thoại (tắt/bật layer logo của khung, giống ANLOGO1/ANLOGO2).</summary>
    private static void ApplyLogoVisibility(Document doc, RouteLayoutOptions opts)
    {
        var db = doc.Database;
        try
        {
            using var tr = db.TransactionManager.StartTransaction();
            var split = TitleBlockFit.SeparateLogos(tr, db);
            if (split.Count == 0)
                return;
            TitleBlockFit.SetLogoHidden(tr, db, owner: true, opts.HideOwnerLogo);
            TitleBlockFit.SetLogoHidden(tr, db, owner: false, opts.HideDesignLogo);
            tr.Commit();
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage($"\n[iSurvey] Không đặt được ẩn/hiện logo ({ex.Message}).");
        }
    }
    private static void DrawPieceOutline(
        Transaction tr, Database db, BlockTableRecord model, ObjectId style, Piece pc, double w, double h, double s0, int total,
        string size)
    {
        var u = new Vector3d(Math.Cos(pc.Phi), Math.Sin(pc.Phi), 0);
        var v = new Vector3d(-Math.Sin(pc.Phi), Math.Cos(pc.Phi), 0);
        var c = new Point3d(pc.Center.X, pc.Center.Y, 0);
        Point3d P(double a, double b) => c + u * (a * w / 2) + v * (b * h / 2);
        AddPoly(tr, model, LayerPiece, [P(-1, -1), P(1, -1), P(1, 1), P(-1, 1)]);
        var label = $"MẢNH {pc.Index:00}/{total:00} ({size})";
        AddText(tr, db, model, style, LayerPiece, label, P(-1, 1) + u * (3 * s0) - v * (3 * s0), 3.5 * s0, pc.Phi,
            TextHorizontalMode.TextLeft, TextVerticalMode.TextTop);
    }

    private static double NormalizeAngle(double a)
    {
        var tau = Math.PI * 2;
        a %= tau;
        if (a < 0)
            a += tau;
        return a;
    }

    /// <summary>Kích thước giấy (mm) theo khổ, đúng hướng khung mẫu (A4 đứng, còn lại ngang).</summary>
    private static (double w, double h) PaperSizeMm(Spec s) => s.Name switch
    {
        "A0" => (1189, 841),
        "A1" => (841, 594),
        "A2" => (594, 420),
        "A3" => (420, 297),
        _ => (210, 297)
    };

    private static string? _pageSetupIssue;
    private static string _step = "";

    /// <summary>Thiết lập khổ giấy (ưu tiên khổ không lề), tỷ lệ in 1:1 (mm), máy in PDF. Lỗi thì ghi chú lại — layout vẫn dùng được.</summary>
    private static void ApplyPageSetup(Layout lay, Spec spec, double paperW, double paperH)
    {
        try
        {
            using var ps = new PlotSettings(lay.ModelType);
            ps.CopyFrom(lay);
            var v = PlotSettingsValidator.Current;
            try
            {
                v.SetPlotConfigurationName(ps, "DWG To PDF.pc3", null);
            }
            catch
            {
                // dùng máy in mặc định của bản vẽ
            }

            v.RefreshLists(ps);

            string? media = null;
            var rotated = false;
            var bestScore = 0;
            foreach (var name in v.GetCanonicalMediaNameList(ps))
            {
                v.SetCanonicalMediaName(ps, name);
                var size = ps.PlotPaperSize;
                var straight = Math.Abs(size.X - paperW) < 0.6 && Math.Abs(size.Y - paperH) < 0.6;
                var turned = Math.Abs(size.X - paperH) < 0.6 && Math.Abs(size.Y - paperW) < 0.6;
                if (!straight && !turned)
                    continue;
                var isBleed = name.Contains("full_bleed", StringComparison.OrdinalIgnoreCase);
                var isOther = name.Contains("rotated", StringComparison.OrdinalIgnoreCase) || name.Contains("expand", StringComparison.OrdinalIgnoreCase);
                var score = isBleed && !isOther ? 3 : !isOther ? 2 : 1;
                if (score <= bestScore)
                    continue;
                bestScore = score;
                media = name;
                rotated = !straight;
            }

            if (media is null)
            {
                _pageSetupIssue = $"không thấy khổ giấy {paperW:0}×{paperH:0} mm trong máy in của bản vẽ";
                return;
            }

            _step = "SetCanonicalMediaName";
            v.SetCanonicalMediaName(ps, media);
            _step = "SetPlotPaperUnits";
            v.SetPlotPaperUnits(ps, PlotPaperUnit.Millimeters);
            _step = "SetPlotRotation";
            v.SetPlotRotation(ps, rotated ? PlotRotation.Degrees090 : PlotRotation.Degrees000);
            _step = "SetPlotType";
            v.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Layout);
            _step = "SetUseStandardScale";
            v.SetUseStandardScale(ps, false);
            _step = "SetCustomPrintScale";
            v.SetCustomPrintScale(ps, new CustomScale(1, 1));
            _step = "SetPlotOrigin";
            v.SetPlotOrigin(ps, new Point2d(0, 0));
            lay.CopyFrom(ps);
        }
        catch (System.Exception ex)
        {
            _pageSetupIssue = ex.Message + " @" + _step;
        }
    }

    /// <summary>Giá trị một thuộc tính khung tên của tờ thứ <paramref name="index"/>/<paramref name="total"/>.</summary>
    private static string SheetAttributeText(string tag, RouteLayoutOptions opts, int index, int total, int scaleN)
    {
        if (tag.Equals(FrameFields.SheetTag, StringComparison.OrdinalIgnoreCase))
            return $"{index}/{total}";
        if (tag.Equals(FrameFields.ScaleTag, StringComparison.OrdinalIgnoreCase))
            return $"1/{scaleN}";
        return opts.Attributes.TryGetValue(tag, out var v) ? v : "";
    }

    private static Dictionary<string, ObjectId> ImportSignatureBlocks(Document doc, RouteLayoutOptions opts)
    {
        var map = new Dictionary<string, ObjectId>(StringComparer.OrdinalIgnoreCase);
        if (opts.Signatures is null || opts.Signatures.Count == 0)
            return map;
        var db = doc.Database;
        foreach (var kv in opts.Signatures)
        {
            var path = kv.Value;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                doc.Editor.WriteMessage($"\n[iSurvey] Không thấy file chữ ký {kv.Key}.");
                continue;
            }

            try
            {
                var info = new FileInfo(path);
                var name = $"ISURVEY_CK_{kv.Key.ToUpperInvariant()}_{info.Length:X}";
                using var src = new Database(false, true);
                src.ReadDwgFile(path, FileOpenMode.OpenForReadAndAllShare, true, "");
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    if (bt.Has(name))
                    {
                        map[kv.Key] = bt[name];
                        tr.Commit();
                        continue;
                    }

                    tr.Commit();
                }

                map[kv.Key] = db.Insert(name, src, false);
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage($"\n[iSurvey] Không đưa được chữ ký {kv.Key} vào bản vẽ ({ex.Message}).");
            }
        }

        return map;
    }

    /// <summary>
    /// Ô ký là phần trống bên phải họ tên: hình chữ nhật của hàng TEN_n, cắt bởi đường đứng ngay sau chữ.
    /// Không thấy ô thì đặt giữa chữ, cao khoảng 10 mm.
    /// </summary>
    private static bool TrySignatureCell(Transaction tr, BlockReference frame, Point3d attrPaper, out Point3d center, out double cellW, out double cellH)
    {
        center = attrPaper;
        cellW = 0;
        cellH = 0;
        var local = attrPaper.TransformBy(frame.BlockTransform.Inverse());
        var def = (BlockTableRecord)tr.GetObject(frame.BlockTableRecord, OpenMode.ForRead);
        Extents3d? row = null;
        var bestArea = double.MaxValue;
        var dividers = new List<(double X, double Y0, double Y1)>();
        foreach (ObjectId id in def)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead);
            if (ent is Polyline pl && pl.Closed)
            {
                Extents3d ext;
                try
                {
                    ext = pl.GeometricExtents;
                }
                catch
                {
                    continue;
                }

                if (local.X < ext.MinPoint.X - 0.5 || local.X > ext.MaxPoint.X + 0.5
                    || local.Y < ext.MinPoint.Y - 0.5 || local.Y > ext.MaxPoint.Y + 0.5)
                    continue;
                var area = (ext.MaxPoint.X - ext.MinPoint.X) * (ext.MaxPoint.Y - ext.MinPoint.Y);
                if (area < 4 || area >= bestArea)
                    continue;
                bestArea = area;
                row = ext;
            }
            else if (ent is Line ln && Math.Abs(ln.StartPoint.X - ln.EndPoint.X) <= 0.2)
            {
                var y0 = Math.Min(ln.StartPoint.Y, ln.EndPoint.Y);
                var y1 = Math.Max(ln.StartPoint.Y, ln.EndPoint.Y);
                if (y1 - y0 < 4)
                    continue;
                dividers.Add(((ln.StartPoint.X + ln.EndPoint.X) / 2.0, y0, y1));
            }
        }

        if (row is null)
            return false;
        var box = row.Value;
        double? split = null;
        foreach (var d in dividers)
        {
            if (d.X < local.X + 1.0 || d.X > box.MaxPoint.X - 0.5)
                continue;
            var overlap = Math.Min(d.Y1, box.MaxPoint.Y) - Math.Max(d.Y0, box.MinPoint.Y);
            if (overlap < (box.MaxPoint.Y - box.MinPoint.Y) * 0.5)
                continue;
            if (split is null || d.X < split)
                split = d.X;
        }

        if (split is null)
            return false;
        cellW = box.MaxPoint.X - split.Value;
        cellH = box.MaxPoint.Y - box.MinPoint.Y;
        if (cellW < 4 || cellH < 4)
            return false;
        center = new Point3d((split.Value + box.MaxPoint.X) / 2.0, (box.MinPoint.Y + box.MaxPoint.Y) / 2.0, 0)
            .TransformBy(frame.BlockTransform);
        return true;
    }

    /// <summary>Đặt block chữ ký vào ô ký bên phải họ tên trên một tờ.</summary>
    private static void PlaceSignatures(Transaction tr, BlockTableRecord space, BlockReference frame, Dictionary<string, ObjectId> blocks)
    {
        if (blocks.Count == 0)
            return;
        foreach (ObjectId aid in frame.AttributeCollection)
        {
            if (tr.GetObject(aid, OpenMode.ForRead) is not AttributeReference ar)
                continue;
            if (!blocks.TryGetValue(ar.Tag, out var blockId))
                continue;
            var ins = new BlockReference(Point3d.Origin, blockId) { Layer = frame.Layer };
            space.AppendEntity(ins);
            tr.AddNewlyCreatedDBObject(ins, true);
            Extents3d ext;
            try
            {
                ext = ins.GeometricExtents;
            }
            catch
            {
                ins.Erase();
                continue;
            }

            var bw = Math.Max(ext.MaxPoint.X - ext.MinPoint.X, 0.01);
            var bh = Math.Max(ext.MaxPoint.Y - ext.MinPoint.Y, 0.01);
            var paper = ar.AlignmentPoint;
            if (paper.DistanceTo(Point3d.Origin) < 1e-6)
                paper = ar.Position;
            var target = paper;
            var maxW = 40.0;
            var maxH = 10.0;
            if (TrySignatureCell(tr, frame, paper, out var cellCenter, out var cellW, out var cellH))
            {
                target = cellCenter;
                maxW = Math.Max(cellW - 2.0, cellW * 0.85);
                maxH = Math.Max(cellH - 2.0, cellH * 0.85);
            }

            var sc = maxH / bh;
            if (bw * sc > maxW)
                sc = maxW / bw;
            ins.ScaleFactors = new Scale3d(sc);
            var sized = ins.GeometricExtents;
            var mid = new Point3d(
                (sized.MinPoint.X + sized.MaxPoint.X) / 2,
                (sized.MinPoint.Y + sized.MaxPoint.Y) / 2,
                0);
            ins.Position = target - (mid - ins.Position);
        }
    }

    private static void PlotExistingRoute(Document doc)
    {
        var db = doc.Database;
        var ed = doc.Editor;
        List<string> names;
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var dict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
            names = [];
            foreach (DBDictionaryEntry entry in dict)
            {
                if (entry.Key.StartsWith(RouteLayoutName, StringComparison.OrdinalIgnoreCase))
                    names.Add(entry.Key);
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            tr.Commit();
        }

        if (names.Count == 0)
        {
            ed.WriteMessage("\n[iSurvey] Bản vẽ chưa có layout dàn trang. Gõ DANTRANG trước.");
            return;
        }

        var layoutName = names[0];
        if (names.Count > 1)
        {
            var pk = new PromptKeywordOptions("\niSurvey: Chọn layout cần in. " + string.Join(", ", names.Select((n, i) => $"{i + 1} = {n}")))
            {
                AllowNone = false,
                AppendKeywordsToMessage = true
            };
            for (var i = 0; i < names.Count && i < 9; i++)
            {
                var token = (i + 1).ToString(CultureInfo.InvariantCulture);
                pk.Keywords.Add(token, token, token);
            }

            pk.Keywords.Default = "1";
            var rk = ed.GetKeywords(pk);
            if (rk.Status != PromptStatus.OK || !int.TryParse(rk.StringResult, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pick) || pick < 1 || pick > names.Count)
            {
                ed.WriteMessage("\n[iSurvey] Đã hủy in.");
                return;
            }

            layoutName = names[pick - 1];
        }

        List<Extents2d> windows;
        double paperW;
        double paperH;
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var dict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
            var lay = (Layout)tr.GetObject(dict.GetAt(layoutName), OpenMode.ForRead);
            var ps = (BlockTableRecord)tr.GetObject(lay.BlockTableRecordId, OpenMode.ForRead);
            var found = new List<(double x, double y, Extents2d window)>();
            foreach (ObjectId id in ps)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is not BlockReference br)
                    continue;
                var def = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
                var spec = Specs.FirstOrDefault(s => s.Block.Equals(def.Name, StringComparison.OrdinalIgnoreCase));
                if (spec is null)
                    continue;
                var (pw, ph) = PaperSizeMm(spec);
                paperW = pw;
                paperH = ph;
                var originX = pw - spec.Fw - 5.0;
                var originY = (ph - spec.Fh) / 2.0;
                var cx = br.Position.X - originX;
                var cy = br.Position.Y - originY;
                found.Add((cx, cy, new Extents2d(cx, cy, cx + pw, cy + ph)));
            }

            tr.Commit();
            if (found.Count == 0)
            {
                ed.WriteMessage($"\n[iSurvey] Layout {layoutName} không có khung tờ in.");
                return;
            }

            windows = found
                .OrderByDescending(f => f.y)
                .ThenBy(f => f.x)
                .Select(f => f.window)
                .ToList();
            var first = Specs.First(s => s.Block.Equals("KB_A3", StringComparison.OrdinalIgnoreCase));
            (paperW, paperH) = PaperSizeMm(first);
            foreach (var spec in Specs)
            {
                if (found.Count > 0)
                    break;
            }

            // Khổ lấy từ khung đầu tiên đã gom ở trên. Tính lại cho đúng khổ của các tờ.
            using var tr2 = db.TransactionManager.StartTransaction();
            var dict2 = (DBDictionary)tr2.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
            var lay2 = (Layout)tr2.GetObject(dict2.GetAt(layoutName), OpenMode.ForRead);
            var ps2 = (BlockTableRecord)tr2.GetObject(lay2.BlockTableRecordId, OpenMode.ForRead);
            foreach (ObjectId id in ps2)
            {
                if (tr2.GetObject(id, OpenMode.ForRead) is not BlockReference br)
                    continue;
                var def = (BlockTableRecord)tr2.GetObject(br.BlockTableRecord, OpenMode.ForRead);
                var spec = Specs.FirstOrDefault(s => s.Block.Equals(def.Name, StringComparison.OrdinalIgnoreCase));
                if (spec is null)
                    continue;
                (paperW, paperH) = PaperSizeMm(spec);
                break;
            }

            tr2.Commit();
        }

        var pdfPath = DefaultPdfPath(db, layoutName);
        using (var dlg = new System.Windows.Forms.SaveFileDialog
        {
            Title = "Lưu file PDF",
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = Path.GetFileName(pdfPath),
            InitialDirectory = Path.GetDirectoryName(pdfPath),
            OverwritePrompt = true
        })
        {
            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                ed.WriteMessage("\n[iSurvey] Đã hủy in.");
                return;
            }

            pdfPath = dlg.FileName;
        }

        var ok = RoutePdfPlot.Plot(doc, layoutName, windows, pdfPath, paperW, paperH, out var issue);
        if (!ok)
        {
            ed.WriteMessage($"\n[iSurvey] In PDF thất bại: {issue}");
            System.Windows.MessageBox.Show("Chưa in được PDF: " + issue, "iSurvey", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        ed.WriteMessage($"\n[iSurvey] Đã in {windows.Count} trang vào {pdfPath}");
        ActivityLogger.Result("ISURVEY_ROUTE_PLOT", $"in lại {layoutName}, {windows.Count} trang, {Path.GetFileName(pdfPath)}");
        System.Windows.MessageBox.Show(
            $"Đã in {windows.Count} trang.\n{pdfPath}",
            "iSurvey",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }
}







