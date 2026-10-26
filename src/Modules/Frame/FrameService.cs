using System.Globalization;
using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using iSurvey.Auth;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Modules.Frame;

/// <summary>
/// ISURVEY_KHUNG (KHUNG): chèn khung bản đồ chuẩn TCVN 7285:2003 (ISO 5457) khổ A0–A4, tỷ lệ 1/200…1/5000.
/// Khung (block KB_A0…KB_A4, thuộc tính khung tên) lấy từ file mẫu <c>KhungBanDo_Chuan.dwg</c>;
/// phần phụ thuộc vị trí (lưới tọa độ, mũi tên Bắc, thước tỷ lệ) được vẽ theo tọa độ thật của bản vẽ.
/// Kích thước khung ngoài bản vẽ = kích thước giấy (mm) × mẫu số / 1000 (đơn vị bản vẽ = mét).
/// </summary>
public sealed class FrameService
{
    private const string TemplateName = "KhungBanDo_Chuan.dwg";
    private const string UserTemplateName = "KhungBanDo.dwg";
    private const string LayerGrid = "KB_TOADO";
    private const string LayerAux = "KB_PHUTRO";
    private const string FrameStyle = "KB_ARIAL";
    private const string DefaultProjectName = "Đường dây và TBA 110KV KCN Minh Châu";

    private static readonly int[] Scales = [200, 500, 1000, 2000, 5000];

    private static string _lastSize = "A3";
    private static int _lastScale = 1000;
    private static string _lastRotation = "Khong";
    private static double _lastAngle;
    private static string _lastExtras = "Co";

    /// <summary>Khổ giấy: kích thước vùng vẽ (mm, gốc = góc dưới-trái khung) và vùng bản đồ (trừ khung tên).</summary>
    private sealed record Spec(string Name, double Fw, double Fh, double Mx0, double My0, double Mx1, double My1)
    {
        public string Block => "KB_" + Name;
        public double Cx => (Mx0 + Mx1) / 2.0;
        public double Cy => (My0 + My1) / 2.0;
    }

    private static readonly Spec[] Specs =
    [
        new("A0", 1159, 821, 0, 0, 1109, 821),
        new("A1", 811, 574, 0, 0, 761, 574),
        new("A2", 564, 400, 0, 0, 514, 400),
        new("A3", 390, 277, 0, 0, 340, 277),
        new("A4", 180, 277, 0, 70, 180, 277)
    ];

    /// <summary>Khung bản đồ (khảo sát): khung + khung tên + lưới tọa độ, mũi tên Bắc, thước tỷ lệ.</summary>
    [CommandMethod("ISURVEY_KHUNG")]
    public void FrameCommand() => Execute(mapMode: true, "ISURVEY_KHUNG");

    /// <summary>Khung bản vẽ kỹ thuật: khung + khung tên, không có lưới tọa độ.</summary>
    [CommandMethod("ISURVEY_KHUNG_BV")]
    public void TechFrameCommand() => Execute(mapMode: false, "ISURVEY_KHUNG_BV");

    /// <summary>Hỏi ẩn logo chủ đầu tư hay logo đơn vị thiết kế. Gõ ANLOGO1 / ANLOGO2 để chọn thẳng.</summary>
    [CommandMethod("ISURVEY_LOGO")]
    public void ToggleLogoCommand()
    {
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;
        var which = PromptLogo(doc.Editor);
        if (which is null)
            return;
        ApplyLogo(doc, which.Value);
    }

    [CommandMethod("ISURVEY_LOGO1")]
    public void ToggleLogoOwnerCommand() => ToggleLogoDirect(owner: true);

    [CommandMethod("ISURVEY_LOGO2")]
    public void ToggleLogoDesignCommand() => ToggleLogoDirect(owner: false);

    private static void ToggleLogoDirect(bool owner)
    {
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;
        ApplyLogo(doc, owner);
    }

    private static bool? PromptLogo(Editor ed)
    {
        var opts = new PromptKeywordOptions("\niSurvey: 1 logo chủ đầu tư, 2 logo đơn vị thiết kế")
        {
            AllowNone = true,
            AppendKeywordsToMessage = true
        };
        opts.Keywords.Add("1", "1", "1");
        opts.Keywords.Add("2", "2", "2");
        var res = ed.GetKeywords(opts);
        if (res.Status != PromptStatus.OK)
            return null;
        return res.StringResult == "1";
    }

    private static void ApplyLogo(Document doc, bool owner)
    {
        var ed = doc.Editor;
        using var _ = doc.LockDocument();
        using var tr = doc.Database.TransactionManager.StartTransaction();
        var split = TitleBlockFit.SeparateLogos(tr, doc.Database);
        if (split.Count == 0)
        {
            ed.WriteMessage("\n[iSurvey] Bản vẽ này không có logo EVN trong khung tên.");
            return;
        }

        var note = TitleBlockFit.ToggleLogoLayer(tr, doc.Database, owner, split.ReleasedSharedLayer);
        tr.Commit();
        ed.Regen();
        ed.WriteMessage($"\n[iSurvey] {note}. Gõ lại ANLOGO và chọn cùng mục để đổi lại.");
        ActivityLogger.Result("ISURVEY_LOGO", note);
    }

    /// <summary>Co ô chủ đầu tư về một dòng cho khung đã chèn trong bản vẽ này.</summary>
    [CommandMethod("ISURVEY_FIT_TITLE")]
    public void FitTitleCommand()
    {
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;
        var n = TitleBlockFit.FitDocument(doc);
        doc.Editor.WriteMessage(n > 0
            ? $"\n[iSurvey] Đã co {n} ô chủ đầu tư về một dòng."
            : "\n[iSurvey] Không có ô chủ đầu tư nào cần co.");
    }

    private static void Execute(bool mapMode, string commandName)
    {
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        var ed = doc.Editor;
        try
        {
            Run(doc, ed, mapMode);
        }
        catch (System.Exception ex)
        {
            ActivityLogger.TrackError(ex, commandName);
            ed.WriteMessage($"\n[iSurvey] Chèn khung lỗi: {ex.Message}");
        }
    }

    private static void Run(Document doc, Editor ed, bool mapMode)
    {
        var db = doc.Database;
        var template = ResolveTemplate();
        if (template is null)
        {
            ed.WriteMessage($"\n[iSurvey] Không thấy file khung mẫu {TemplateName} (Contents\\Templates của bundle).");
            return;
        }

        var unitM = UnitMeters(db.Insunits, out var unitNote);
        if (unitNote is not null)
            ed.WriteMessage("\n[iSurvey] " + unitNote);

        // 1) Khổ giấy
        var pkSize = new PromptKeywordOptions("\niSurvey: Khổ giấy") { AllowNone = true, AppendKeywordsToMessage = true };
        foreach (var s in Specs)
            pkSize.Keywords.Add(s.Name);
        pkSize.Keywords.Default = _lastSize;
        var rSize = ed.GetKeywords(pkSize);
        if (rSize.Status is not (PromptStatus.OK or PromptStatus.None))
            return;
        if (rSize.Status == PromptStatus.OK)
            _lastSize = rSize.StringResult.ToUpperInvariant();
        var spec = Specs.First(s => s.Name.Equals(_lastSize, StringComparison.OrdinalIgnoreCase));

        // 2) Tỷ lệ
        var pkScale = new PromptKeywordOptions("\niSurvey: Mẫu số tỷ lệ 1/N") { AllowNone = true, AppendKeywordsToMessage = true };
        foreach (var n in Scales)
            pkScale.Keywords.Add(n.ToString(CultureInfo.InvariantCulture));
        pkScale.Keywords.Default = _lastScale.ToString(CultureInfo.InvariantCulture);
        var rScale = ed.GetKeywords(pkScale);
        if (rScale.Status is not (PromptStatus.OK or PromptStatus.None))
            return;
        if (rScale.Status == PromptStatus.OK && int.TryParse(rScale.StringResult, out var picked))
            _lastScale = picked;
        var scaleN = _lastScale;

        // 3) Xoay khung (khung nằm theo hướng tuyến)
        var pkRot = new PromptKeywordOptions("\niSurvey: Xoay khung") { AllowNone = true, AppendKeywordsToMessage = true };
        pkRot.Keywords.Add("Khong", "Khong", "Không xoay");
        pkRot.Keywords.Add("Goc", "Goc", "Nhập góc");
        pkRot.Keywords.Add("Tuyen", "Tuyen", "Theo tuyến");
        pkRot.Keywords.Default = _lastRotation;
        var rRot = ed.GetKeywords(pkRot);
        if (rRot.Status is not (PromptStatus.OK or PromptStatus.None))
            return;
        if (rRot.Status == PromptStatus.OK)
            _lastRotation = Canon(rRot.StringResult, "Khong", "Goc", "Tuyen");

        var ucsAngle = UcsAngle(ed);
        double theta;
        switch (_lastRotation)
        {
            case "Goc":
                var pa = new PromptAngleOptions("\niSurvey: Góc xoay khung (độ, ngược chiều kim đồng hồ)")
                {
                    AllowNone = true,
                    DefaultValue = _lastAngle,
                    UseDefaultValue = true
                };
                var ra = ed.GetAngle(pa);
                if (ra.Status is not (PromptStatus.OK or PromptStatus.None))
                    return;
                if (ra.Status == PromptStatus.OK)
                    _lastAngle = ra.Value;
                theta = _lastAngle + ucsAngle;
                break;
            case "Tuyen":
                var p1 = ed.GetPoint(new PromptPointOptions("\niSurvey: Điểm đầu đoạn tuyến: ") { AllowNone = false });
                if (p1.Status != PromptStatus.OK)
                    return;
                var p2 = ed.GetPoint(new PromptPointOptions("\niSurvey: Điểm cuối đoạn tuyến: ")
                {
                    AllowNone = false,
                    UseBasePoint = true,
                    BasePoint = p1.Value
                });
                if (p2.Status != PromptStatus.OK)
                    return;
                var w1 = p1.Value.TransformBy(ed.CurrentUserCoordinateSystem);
                var w2 = p2.Value.TransformBy(ed.CurrentUserCoordinateSystem);
                if (Math.Abs(w2.X - w1.X) + Math.Abs(w2.Y - w1.Y) < 1e-9)
                {
                    ed.WriteMessage("\n[iSurvey] Hai điểm trùng nhau — dùng góc 0.");
                    theta = 0;
                }
                else
                {
                    theta = Math.Atan2(w2.Y - w1.Y, w2.X - w1.X);
                }

                break;
            default:
                theta = 0.0;
                break;
        }

        // 4) Phụ trợ (chỉ khung bản đồ): lưới tọa độ + mũi tên Bắc + thước tỷ lệ
        var extras = false;
        if (mapMode)
        {
            var pkExtra = new PromptKeywordOptions("\niSurvey: Vẽ lưới tọa độ + mũi tên Bắc + thước tỷ lệ")
            {
                AllowNone = true,
                AppendKeywordsToMessage = true
            };
            pkExtra.Keywords.Add("Co", "Co", "Có");
            pkExtra.Keywords.Add("Khong", "Khong", "Không");
            pkExtra.Keywords.Default = _lastExtras;
            var rExtra = ed.GetKeywords(pkExtra);
            if (rExtra.Status is not (PromptStatus.OK or PromptStatus.None))
                return;
            if (rExtra.Status == PromptStatus.OK)
                _lastExtras = Canon(rExtra.StringResult, "Co", "Khong");
            extras = _lastExtras == "Co";
        }

        var s0 = scaleN / (1000.0 * unitM);   // hệ số chèn: 1 mm giấy = s0 đơn vị bản vẽ
        ed.WriteMessage(
            $"\n[iSurvey] Khung {(mapMode ? "bản đồ" : "bản vẽ kỹ thuật")} {spec.Name} 1/{scaleN}: vùng vẽ {spec.Fw * scaleN / 1000.0:0.##} × {spec.Fh * scaleN / 1000.0:0.##} m; " +
            $"vùng {(mapMode ? "bản đồ" : "hình vẽ")} {(spec.Mx1 - spec.Mx0) * scaleN / 1000.0:0.##} × {(spec.My1 - spec.My0) * scaleN / 1000.0:0.##} m. " +
            $"Chọn tâm vùng {(mapMode ? "bản đồ" : "hình vẽ")} (Enter / Esc để kết thúc).");

        var placed = 0;
        while (true)
        {
            var jig = new FrameJig(c => Outline(spec, c, s0, theta));
            var drag = ed.Drag(jig);
            if (drag.Status != PromptStatus.OK)
                break;

            PlaceFrame(doc, spec, template, scaleN, s0, theta, jig.Position, extras, unitM);
            placed++;
        }

        if (placed > 0)
        {
            ActivityLogger.Enqueue("khung", mapMode ? "ISURVEY_KHUNG" : "ISURVEY_KHUNG_BV", new
            {
                size = spec.Name,
                scale = scaleN,
                count = placed,
                extras,
                drawing = ActivityLogger.FileName(doc.Database.Filename)
            });
            ed.WriteMessage($"\n[iSurvey] Đã chèn {placed} khung. Nhấp đúp vào khung để điền khung tên; một lần UNDO hoàn tác cả lượt chèn vừa rồi.");
        }
    }

    // -------------------------------------------------------------------------------------------------
    //  Chèn một khung
    // -------------------------------------------------------------------------------------------------
    private static void PlaceFrame(
        Document doc, Spec spec, string template, int scaleN, double s0, double theta, Point3d center, bool extras, double unitM)
    {
        var db = doc.Database;
        var ed = doc.Editor;
        var u = new Vector3d(Math.Cos(theta), Math.Sin(theta), 0);
        var v = new Vector3d(-Math.Sin(theta), Math.Cos(theta), 0);
        // gốc khối = góc dưới-trái vùng vẽ; tâm vùng bản đồ = center
        var origin = center - (u * spec.Cx + v * spec.Cy) * s0;

        Point3d ToWcs(double x, double y) => origin + (u * x + v * y) * s0;

        using var _ = doc.LockDocument();
        using var tr = db.TransactionManager.StartTransaction();

        var blockId = EnsureBlock(db, tr, spec.Block, template);
        var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
        var layers = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        var frameLayer = layers.Has("KB_KHUNG") ? "KB_KHUNG" : "0";

        var br = new BlockReference(origin, blockId)
        {
            ScaleFactors = new Scale3d(s0),
            Rotation = theta,
            Layer = frameLayer
        };
        space.AppendEntity(br);
        tr.AddNewlyCreatedDBObject(br, true);

        var def = (BlockTableRecord)tr.GetObject(blockId, OpenMode.ForRead);
        foreach (ObjectId id in def)
        {
            if (tr.GetObject(id, OpenMode.ForRead) is not AttributeDefinition ad || ad.Constant)
                continue;
            var ar = new AttributeReference();
            ar.SetAttributeFromBlock(ad, br.BlockTransform);
            ar.TextString = AttributeText(ad.Tag, ad.TextString, scaleN);
            br.AttributeCollection.AppendAttribute(ar);
            tr.AddNewlyCreatedDBObject(ar, true);
            TitleBlockFit.Fit(tr, ar, br);
        }

        var made = new ObjectIdCollection { br.ObjectId };
        if (extras)
            DrawExtras(db, tr, space, spec, scaleN, s0, theta, origin, unitM, made);

        // Gộp thành 1 nhóm vô danh: chọn khung = chọn cả lưới/Bắc/thước; MOVE/ROTATE đi cùng.
        var groups = (DBDictionary)tr.GetObject(db.GroupDictionaryId, OpenMode.ForWrite);
        var grp = new Group($"iSurvey khung {spec.Name} 1/{scaleN}", true);
        groups.SetAt("*", grp);
        tr.AddNewlyCreatedDBObject(grp, true);
        foreach (ObjectId id in made)
            grp.Append(id);

        tr.Commit();

        var c0 = ToWcs(spec.Mx0, spec.My0);
        var c1 = ToWcs(spec.Mx1, spec.My1);
        ed.WriteMessage($"\n[iSurvey] Khung {spec.Name} 1/{scaleN}: vùng bản đồ góc ({c0.X:0.##}, {c0.Y:0.##}) → ({c1.X:0.##}, {c1.Y:0.##}).");
    }

    // -------------------------------------------------------------------------------------------------
    //  Lưới tọa độ, mũi tên Bắc, thước tỷ lệ
    // -------------------------------------------------------------------------------------------------
    private static void DrawExtras(
        Database db, Transaction tr, BlockTableRecord space, Spec sp, int scaleN, double s0, double theta,
        Point3d origin, double unitM, ObjectIdCollection made)
    {
        EnsureLayer(tr, db, LayerGrid);
        EnsureLayer(tr, db, LayerAux);
        var style = TextStyleId(tr, db);
        var ux = Math.Cos(theta);
        var uy = Math.Sin(theta);
        var vx = -uy;
        var vy = ux;

        Point3d W(double x, double y) =>
            new(origin.X + (ux * x + vx * y) * s0, origin.Y + (uy * x + vy * y) * s0, origin.Z);

        (double x, double y) Local(Point3d p)
        {
            var dx = p.X - origin.X;
            var dy = p.Y - origin.Y;
            return ((dx * ux + dy * uy) / s0, (dx * vx + dy * vy) / s0);
        }

        // --- Lưới ô vuông: cách nhau 100 mm trên giấy (= 0.1·N mét)
        var g = 0.1 * scaleN / unitM;
        var bl = W(sp.Mx0, sp.My0);
        var br = W(sp.Mx1, sp.My0);
        var tr2 = W(sp.Mx1, sp.My1);
        var tl = W(sp.Mx0, sp.My1);
        var corners = new[] { bl, br, tr2, tl };
        var minX = corners.Min(p => p.X);
        var maxX = corners.Max(p => p.X);
        var minY = corners.Min(p => p.Y);
        var maxY = corners.Max(p => p.Y);

        // lề trống để ghi nhãn: trái 22 mm, trên 10 mm; phải/dưới 5 mm (gần khung tên thì không ghi nhãn)
        const double insetL = 22, insetT = 10, insetR = 5, insetB = 5;
        var arm = 2.5 * s0;
        var iLo = (long)Math.Ceiling(minX / g - 1e-9);
        var iHi = (long)Math.Floor(maxX / g + 1e-9);
        var jLo = (long)Math.Ceiling(minY / g - 1e-9);
        var jHi = (long)Math.Floor(maxY / g + 1e-9);
        for (var i = iLo; i <= iHi; i++)
        {
            for (var j = jLo; j <= jHi; j++)
            {
                var p = new Point3d(i * g, j * g, origin.Z);
                var (lx, ly) = Local(p);
                if (lx < sp.Mx0 + insetL || lx > sp.Mx1 - insetR || ly < sp.My0 + insetB || ly > sp.My1 - insetT)
                    continue;
                made.Add(AddLine(tr, space, LayerGrid, new Point3d(p.X - arm, p.Y, p.Z), new Point3d(p.X + arm, p.Y, p.Z)));
                made.Add(AddLine(tr, space, LayerGrid, new Point3d(p.X, p.Y - arm, p.Z), new Point3d(p.X, p.Y + arm, p.Z)));
            }
        }

        // --- Nhãn tọa độ + vạch chia: cạnh trái và cạnh trên của vùng bản đồ (nhãn nằm trong vùng bản đồ)
        var th = 2.5 * s0;
        var tick = 2.5 * s0;
        var corner = 12.0;   // mm giấy tính từ góc
        var uVec = new Vector3d(ux, uy, 0);
        var vVec = new Vector3d(vx, vy, 0);

        void Edge(Point3d a, Point3d b, Vector3d inward, bool leftEdge)
        {
            var len = a.DistanceTo(b) / s0;
            // đường lưới X = c (đứng) và Y = c (ngang)
            for (var axis = 0; axis < 2; axis++)
            {
                var lo = axis == 0 ? Math.Min(a.X, b.X) : Math.Min(a.Y, b.Y);
                var hi = axis == 0 ? Math.Max(a.X, b.X) : Math.Max(a.Y, b.Y);
                var da = axis == 0 ? b.X - a.X : b.Y - a.Y;
                if (Math.Abs(da) < 1e-12)
                    continue;
                var kLo = (long)Math.Ceiling(lo / g - 1e-9);
                var kHi = (long)Math.Floor(hi / g + 1e-9);
                for (var k = kLo; k <= kHi; k++)
                {
                    var c = k * g;
                    var t = ((axis == 0 ? c - a.X : c - a.Y)) / da;
                    if (t < 0 || t > 1)
                        continue;
                    var along = t * len;
                    if (along < corner || along > len - corner)
                        continue;
                    var hit = new Point3d(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, origin.Z);
                    made.Add(AddLine(tr, space, LayerGrid, hit, hit + inward * tick));
                    var label = (axis == 0 ? "X=" : "Y=") + Math.Round(c, 0).ToString("0", CultureInfo.InvariantCulture);
                    var anchor = hit + inward * (3.2 * s0);
                    made.Add(leftEdge
                        ? AddText(tr, db, space, style, LayerGrid, label, anchor, th, theta, TextHorizontalMode.TextLeft, TextVerticalMode.TextVerticalMid)
                        : AddText(tr, db, space, style, LayerGrid, label, anchor, th, theta, TextHorizontalMode.TextCenter, TextVerticalMode.TextTop));
                }
            }
        }

        Edge(bl, tl, uVec, true);     // cạnh trái, hướng vào trong = +u
        Edge(tl, tr2, -vVec, false);  // cạnh trên, hướng vào trong = -v

        // --- Mũi tên Bắc (luôn hướng +Y của bản vẽ), góc trên-phải vùng bản đồ
        var nb = W(sp.Mx1 - 14, sp.My1 - 38);
        var tip = new Point3d(nb.X, nb.Y + 20 * s0, nb.Z);
        var neck = new Point3d(nb.X, nb.Y + 12 * s0, nb.Z);
        made.Add(AddLine(tr, space, LayerAux, nb, neck));
        made.Add(AddSolid(tr, space, LayerAux, tip,
            new Point3d(nb.X - 2.5 * s0, neck.Y, nb.Z), new Point3d(nb.X + 2.5 * s0, neck.Y, nb.Z), tip));
        made.Add(AddText(tr, db, space, style, LayerAux, "B",
            new Point3d(nb.X, tip.Y + 1.5 * s0, nb.Z), 3.5 * s0, 0, TextHorizontalMode.TextCenter, TextVerticalMode.TextBottom));

        // --- Thước tỷ lệ: 4 đoạn × 10 mm giấy (= 0.01·N mét/đoạn), góc dưới-trái vùng bản đồ
        var seg = 0.01 * scaleN;   // mét
        var bx = sp.Mx0 + 26;
        var by = sp.My0 + 10;
        for (var i = 0; i < 4; i++)
        {
            var a0 = W(bx + 10 * i, by);
            var a1 = W(bx + 10 * (i + 1), by);
            var b0 = W(bx + 10 * i, by + 2);
            var b1 = W(bx + 10 * (i + 1), by + 2);
            made.Add(AddPoly(tr, space, LayerAux, [a0, a1, b1, b0]));
            if (i % 2 == 0)
                made.Add(AddSolid(tr, space, LayerAux, a0, a1, b0, b1));
        }

        for (var i = 0; i <= 4; i++)
        {
            var lab = (seg * i).ToString("0.##", CultureInfo.InvariantCulture) + (i == 4 ? " m" : "");
            made.Add(AddText(tr, db, space, style, LayerAux, lab, W(bx + 10 * i, by - 1), 2.0 * s0, theta,
                TextHorizontalMode.TextCenter, TextVerticalMode.TextTop));
        }

        made.Add(AddText(tr, db, space, style, LayerAux, $"TỶ LỆ 1/{scaleN}", W(bx, by + 3.5), 2.5 * s0, theta,
            TextHorizontalMode.TextLeft, TextVerticalMode.TextBottom));
    }

    // -------------------------------------------------------------------------------------------------
    //  Block + entity helpers
    // -------------------------------------------------------------------------------------------------
    private static ObjectId EnsureBlock(Database db, Transaction tr, string name, string templatePath)
    {
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        if (bt.Has(name))
            return bt[name];

        using var src = new Database(false, true);
        src.ReadDwgFile(templatePath, FileOpenMode.OpenForReadAndAllShare, true, null);
        ObjectId srcId;
        using (var srcTr = src.TransactionManager.StartTransaction())
        {
            var srcBt = (BlockTable)srcTr.GetObject(src.BlockTableId, OpenMode.ForRead);
            if (!srcBt.Has(name))
                throw new InvalidOperationException($"File mẫu không có block {name}.");
            srcId = srcBt[name];
            srcTr.Commit();
        }

        var map = new IdMapping();
        src.WblockCloneObjects(new ObjectIdCollection { srcId }, db.BlockTableId, map, DuplicateRecordCloning.Ignore, false);
        return map[srcId].Value;
    }

    private static void EnsureLayer(Transaction tr, Database db, string name)
    {
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (lt.Has(name))
            return;
        lt.UpgradeOpen();
        var rec = new LayerTableRecord
        {
            Name = name,
            Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 7),
            LineWeight = LineWeight.LineWeight018
        };
        lt.Add(rec);
        tr.AddNewlyCreatedDBObject(rec, true);
    }

    private static ObjectId TextStyleId(Transaction tr, Database db)
    {
        var tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
        return tst.Has(FrameStyle) ? tst[FrameStyle] : db.Textstyle;
    }

    private static ObjectId AddLine(Transaction tr, BlockTableRecord space, string layer, Point3d a, Point3d b)
    {
        var ln = new Line(a, b) { Layer = layer };
        space.AppendEntity(ln);
        tr.AddNewlyCreatedDBObject(ln, true);
        return ln.ObjectId;
    }

    private static ObjectId AddSolid(
        Transaction tr, BlockTableRecord space, string layer, Point3d p1, Point3d p2, Point3d p3, Point3d p4)
    {
        var s = new Solid(p1, p2, p3, p4) { Layer = layer };
        space.AppendEntity(s);
        tr.AddNewlyCreatedDBObject(s, true);
        return s.ObjectId;
    }

    private static ObjectId AddPoly(Transaction tr, BlockTableRecord space, string layer, IReadOnlyList<Point3d> pts)
    {
        var pl = new Autodesk.AutoCAD.DatabaseServices.Polyline { Layer = layer, Closed = true };
        for (var i = 0; i < pts.Count; i++)
            pl.AddVertexAt(i, new Point2d(pts[i].X, pts[i].Y), 0, 0, 0);
        pl.Elevation = pts[0].Z;
        space.AppendEntity(pl);
        tr.AddNewlyCreatedDBObject(pl, true);
        return pl.ObjectId;
    }

    private static ObjectId AddText(
        Transaction tr, Database db, BlockTableRecord space, ObjectId style, string layer, string text,
        Point3d anchor, double height, double rotation, TextHorizontalMode hm, TextVerticalMode vm)
    {
        var t = new DBText
        {
            Layer = layer,
            TextStyleId = style,
            TextString = text,
            Height = height,
            Rotation = rotation,
            Position = anchor
        };
        if (hm != TextHorizontalMode.TextLeft || vm != TextVerticalMode.TextBase)
        {
            t.HorizontalMode = hm;
            t.VerticalMode = vm;
            t.AlignmentPoint = anchor;
        }

        space.AppendEntity(t);
        tr.AddNewlyCreatedDBObject(t, true);
        t.AdjustAlignment(db);
        return t.ObjectId;
    }

    // -------------------------------------------------------------------------------------------------
    //  Tiện ích
    // -------------------------------------------------------------------------------------------------
    /// <summary>Tỷ lệ ghi theo khổ đang chèn. Ô dự án còn placeholder thì điền tên mặc định.</summary>
    private static string AttributeText(string tag, string current, int scaleN)
    {
        if (tag.Equals("TY_LE", StringComparison.OrdinalIgnoreCase))
            return $"1/{scaleN}";
        if (tag.Equals("DU_AN", StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrWhiteSpace(current) || current.Contains("TÊN DỰ ÁN", StringComparison.OrdinalIgnoreCase)))
            return DefaultProjectName;
        return current;
    }

    private static string? ResolveTemplate()
    {
        var candidates = new List<string>();
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            candidates.Add(System.IO.Path.Combine(appData, "iSurvey", UserTemplateName));   // mẫu riêng của người dùng (logo…)
        }
        catch
        {
            // bỏ qua
        }

        try
        {
            var asmDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(asmDir))
            {
                // …\iSurvey.bundle\Contents\Win64\<năm>\iSurvey.dll → …\Contents\Templates
                candidates.Add(System.IO.Path.Combine(System.IO.Path.GetFullPath(System.IO.Path.Combine(asmDir, "..", "..")), "Templates", TemplateName));
                candidates.Add(System.IO.Path.Combine(asmDir, "Templates", TemplateName));
            }
        }
        catch
        {
            // bỏ qua
        }

        return candidates.FirstOrDefault(File.Exists);
    }

    private static double UnitMeters(UnitsValue u, out string? note)
    {
        note = null;
        switch (u)
        {
            case UnitsValue.Meters: return 1.0;
            case UnitsValue.Millimeters: return 0.001;
            case UnitsValue.Centimeters: return 0.01;
            case UnitsValue.Decimeters: return 0.1;
            case UnitsValue.Kilometers: return 1000.0;
            case UnitsValue.Feet: return 0.3048;
            case UnitsValue.Inches: return 0.0254;
            default:
                note = "Bản vẽ chưa đặt đơn vị (INSUNITS) — coi 1 đơn vị = 1 m.";
                return 1.0;
        }
    }

    private static string Canon(string typed, params string[] options) =>
        options.FirstOrDefault(o => o.Equals(typed, StringComparison.OrdinalIgnoreCase)) ?? options[0];

    private static double UcsAngle(Editor ed)
    {
        var x = ed.CurrentUserCoordinateSystem.CoordinateSystem3d.Xaxis;
        return Math.Atan2(x.Y, x.X);
    }

    /// <summary>Đường bao khung + vùng bản đồ (hệ WCS) để xem trước khi rê chuột.</summary>
    private static List<Point3d[]> Outline(Spec sp, Point3d center, double s0, double theta)
    {
        var u = new Vector3d(Math.Cos(theta), Math.Sin(theta), 0);
        var v = new Vector3d(-Math.Sin(theta), Math.Cos(theta), 0);
        var origin = center - (u * sp.Cx + v * sp.Cy) * s0;

        Point3d W(double x, double y) => origin + (u * x + v * y) * s0;

        return
        [
            [W(0, 0), W(sp.Fw, 0), W(sp.Fw, sp.Fh), W(0, sp.Fh)],
            [W(sp.Mx0, sp.My0), W(sp.Mx1, sp.My0), W(sp.Mx1, sp.My1), W(sp.Mx0, sp.My1)]
        ];
    }

    /// <summary>Jig xem trước: rê chuột thì khung chạy theo, tâm vùng bản đồ = con trỏ.</summary>
    private sealed class FrameJig : DrawJig
    {
        private readonly Func<Point3d, List<Point3d[]>> _shapes;
        private Point3d _pos = Point3d.Origin;

        public FrameJig(Func<Point3d, List<Point3d[]>> shapes) => _shapes = shapes;

        public Point3d Position => _pos;

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var o = new JigPromptPointOptions("\niSurvey: Tâm vùng bản đồ: ")
            {
                UserInputControls = UserInputControls.Accept3dCoordinates | UserInputControls.NullResponseAccepted
            };
            var r = prompts.AcquirePoint(o);
            if (r.Status != PromptStatus.OK)
                return SamplerStatus.Cancel;
            if (r.Value.DistanceTo(_pos) < 1e-9)
                return SamplerStatus.NoChange;
            _pos = r.Value;
            return SamplerStatus.OK;
        }

        protected override bool WorldDraw(Autodesk.AutoCAD.GraphicsInterface.WorldDraw draw)
        {
            draw.SubEntityTraits.Color = 3;
            foreach (var pts in _shapes(_pos))
            {
                for (var i = 0; i < pts.Length; i++)
                    draw.Geometry.WorldLine(pts[i], pts[(i + 1) % pts.Length]);
            }

            return true;
        }
    }
}
