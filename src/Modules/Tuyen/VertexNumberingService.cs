using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using iSurvey.Auth;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcadColor = Autodesk.AutoCAD.Colors.Color;
using ColorMethod = Autodesk.AutoCAD.Colors.ColorMethod;

namespace iSurvey.Modules.Tuyen;

/// <summary>
/// Đánh số góc tuyến — chấm đỏ + text né theo phân giác ngoài.
/// Cỡ chữ/ký hiệu theo tỷ lệ bản đồ địa hình (mặc định 1:1000, đơn vị model = mét).
/// </summary>
public sealed class VertexNumberingService
{
    private const string LayerName = "ISURVEY_GOC_TUYEN";

    /// <summary>Cỡ chữ trên giấy (mm) — số hiệu góc / chú thích điểm trên BĐĐH.</summary>
    private const double TextHeightMm = 2.5;

    /// <summary>Đường kính chấm đỉnh trên giấy (mm).</summary>
    private const double DotDiameterMm = 1.2;

    /// <summary>Khoảng hở chữ–chấm trên giấy (mm), thêm vào offset.</summary>
    private const double TextGapMm = 0.8;

    [CommandMethod("ISURVEY_NUMBER_VERTICES")]
    public void NumberVerticesCommand()
    {
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        var db = doc.Database;
        var ed = doc.Editor;

        var peo = new PromptEntityOptions("\niSurvey: Chọn Polyline tim tuyến: ")
        {
            AllowNone = false
        };
        peo.SetRejectMessage("\niSurvey: Chỉ chấp nhận Polyline.");
        peo.AddAllowedClass(typeof(Polyline), true);
        peo.AddAllowedClass(typeof(Polyline2d), true);
        peo.AddAllowedClass(typeof(Polyline3d), true);

        var per = ed.GetEntity(peo);
        if (per.Status != PromptStatus.OK)
            return;

        var pso = new PromptStringOptions("\niSurvey: Nhập tiền tố (Prefix) <G>: ")
        {
            AllowSpaces = false,
            UseDefaultValue = true,
            DefaultValue = "G"
        };
        var prStr = ed.GetString(pso);
        if (prStr.Status != PromptStatus.OK)
            return;
        var prefix = string.IsNullOrEmpty(prStr.StringResult) ? "G" : prStr.StringResult;

        var pio = new PromptIntegerOptions("\niSurvey: Nhập chỉ số bắt đầu <0>: ")
        {
            AllowNone = true,
            DefaultValue = 0,
            UseDefaultValue = true
        };
        var pir = ed.GetInteger(pio);
        if (pir.Status == PromptStatus.Cancel)
            return;
        var startIndex = pir.Status == PromptStatus.OK ? pir.Value : 0;

        // Tỷ lệ in bản đồ địa hình — mặc định 1:1000.
        var pScale = new PromptIntegerOptions("\niSurvey: Tỷ lệ bản đồ 1:<1000>: ")
        {
            AllowNone = true,
            AllowNegative = false,
            AllowZero = false,
            LowerLimit = 100,
            UpperLimit = 100000,
            DefaultValue = 1000,
            UseDefaultValue = true
        };
        var prScale = ed.GetInteger(pScale);
        if (prScale.Status == PromptStatus.Cancel)
            return;
        var mapScale = prScale.Status == PromptStatus.OK ? prScale.Value : 1000;

        // mm giấy → mét model: H_m = H_mm * M / 1000
        var textHeight = PaperMmToModel(TextHeightMm, mapScale);
        var dotRadius = PaperMmToModel(DotDiameterMm * 0.5, mapScale);
        var offsetDist = dotRadius + PaperMmToModel(TextGapMm, mapScale) + textHeight * 0.15;

        ed.WriteMessage(
            $"\n[iSurvey] Tỷ lệ 1:{mapScale} — chữ {TextHeightMm:0.#} mm ({textHeight:0.###} m), " +
            $"chấm Ø {DotDiameterMm:0.#} mm ({dotRadius * 2:0.###} m).");

        using var tr = db.TransactionManager.StartTransaction();
        var layerId = EnsureRedLayer(tr, db);

        var ent = (Entity)tr.GetObject(per.ObjectId, OpenMode.ForRead);
        var vertices = GetVertices(ent, tr, out var closed);
        NormalizeVertices(vertices, ref closed);
        if (vertices.Count < (closed ? 3 : 2))
        {
            ed.WriteMessage("\n[iSurvey] Polyline cần ít nhất 2 đỉnh khác nhau (3 nếu khép kín).");
            return;
        }

        // (sizes đã tính theo tỷ lệ — không dùng TEXTSIZE)

        var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
        var plane = new Plane(Point3d.Origin, Vector3d.ZAxis);

        for (var i = 0; i < vertices.Count; i++)
        {
            var v_i = vertices[i];
            var vDir = ComputeOutsideBisector(vertices, i, closed);
            vDir = new Vector3d(vDir.X, vDir.Y, 0);
            if (vDir.Length < 1e-12)
                vDir = Vector3d.XAxis;
            else
                vDir = vDir.GetNormal();

            var insertPt = v_i + vDir * offsetDist;

            // Chấm tròn đặc
            var dot = new Circle
            {
                Center = v_i,
                Radius = dotRadius,
                LayerId = layerId,
                Color = AcadColor.FromColorIndex(ColorMethod.ByLayer, 256)
            };
            var dotId = btr.AppendEntity(dot);
            tr.AddNewlyCreatedDBObject(dot, true);

            var hatch = new Hatch();
            btr.AppendEntity(hatch);
            tr.AddNewlyCreatedDBObject(hatch, true);
            hatch.Elevation = v_i.Z; // cùng cao độ với chấm tròn (đỉnh có Z ≠ 0 vẫn tô được)
            hatch.LayerId = layerId;
            hatch.Color = AcadColor.FromColorIndex(ColorMethod.ByLayer, 256);
            hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
            hatch.Associative = false;
            hatch.AppendLoop(HatchLoopTypes.Default, new ObjectIdCollection { dotId });
            hatch.EvaluateHatch(true);

            // Text
            var txt = new DBText
            {
                LayerId = layerId,
                Color = AcadColor.FromColorIndex(ColorMethod.ByLayer, 256),
                Height = textHeight,
                TextString = $"{prefix}{startIndex + i}"
            };

            var deg = vDir.AngleOnPlane(plane) * 180.0 / Math.PI;
            if (deg < 0)
                deg += 360.0;

            if (deg is >= 0 and < 90)
                txt.Justify = AttachmentPoint.BottomLeft;
            else if (deg is >= 90 and < 180)
                txt.Justify = AttachmentPoint.BottomRight;
            else if (deg is >= 180 and < 270)
                txt.Justify = AttachmentPoint.TopRight;
            else
                txt.Justify = AttachmentPoint.TopLeft;

            txt.Position = insertPt;
            if (txt.Justify != AttachmentPoint.BaseLeft)
                txt.AlignmentPoint = insertPt;

            btr.AppendEntity(txt);
            tr.AddNewlyCreatedDBObject(txt, true);
        }

        tr.Commit();
        ed.WriteMessage($"\n[iSurvey] Đã đánh số thành công {vertices.Count} góc tuyến (layer {LayerName}).");
        ActivityLogger.Result("ISURVEY_NUMBER_VERTICES",
            $"{vertices.Count} góc, tiền tố {prefix}, bắt đầu {startIndex}, tỷ lệ 1/{mapScale}");
    }

    /// <summary>Đổi kích thước trên giấy (mm) sang đơn vị model (mét) theo tỷ lệ 1:M.</summary>
    private static double PaperMmToModel(double sizeMm, int mapScaleDenominator) =>
        sizeMm * mapScaleDenominator / 1000.0;

    private static ObjectId EnsureRedLayer(Transaction tr, Database db)
    {
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (lt.Has(LayerName))
            return lt[LayerName];

        lt.UpgradeOpen();
        var ltr = new LayerTableRecord
        {
            Name = LayerName,
            Color = AcadColor.FromColorIndex(ColorMethod.ByAci, 1)
        };
        var id = lt.Add(ltr);
        tr.AddNewlyCreatedDBObject(ltr, true);
        return id;
    }

    /// <summary>Vector phân giác (né đường) theo công thức đã chốt — Z bỏ qua.</summary>
    private static Vector3d ComputeOutsideBisector(IReadOnlyList<Point3d> vertices, int i, bool closed)
    {
        var v_i = vertices[i];
        var n = vertices.Count;
        if (!closed && i == 0)
            return (v_i - vertices[1]).GetNormal();

        if (!closed && i == n - 1)
            return (v_i - vertices[i - 1]).GetNormal();

        // Khép kín: đỉnh đầu/cuối cũng có đỉnh kề hai phía (vòng).
        var vIn = (vertices[(i - 1 + n) % n] - v_i).GetNormal();
        var vOut = (vertices[(i + 1) % n] - v_i).GetNormal();
        // Ép 2D trước khi cộng.
        vIn = new Vector3d(vIn.X, vIn.Y, 0);
        vOut = new Vector3d(vOut.X, vOut.Y, 0);
        if (vIn.Length > 1e-12)
            vIn = vIn.GetNormal();
        if (vOut.Length > 1e-12)
            vOut = vOut.GetNormal();

        var vSum = vIn + vOut;
        if (vSum.Length < 1e-6)
            return vIn.RotateBy(Math.PI / 2.0, Vector3d.ZAxis);

        return vSum.GetNormal();
    }

    /// <summary>
    /// Bỏ đỉnh trùng liên tiếp (GetNormal ném lỗi với vector 0); đỉnh cuối trùng đỉnh đầu = tuyến khép kín.
    /// </summary>
    private static void NormalizeVertices(List<Point3d> pts, ref bool closed)
    {
        const double tol = 1e-9;
        for (var i = pts.Count - 1; i > 0; i--)
        {
            if (Dist2d(pts[i], pts[i - 1]) < tol)
                pts.RemoveAt(i);
        }

        if (pts.Count > 2 && Dist2d(pts[0], pts[^1]) < tol)
        {
            pts.RemoveAt(pts.Count - 1);
            closed = true;
        }
    }

    private static double Dist2d(Point3d a, Point3d b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static List<Point3d> GetVertices(Entity ent, Transaction tr, out bool closed)
    {
        var pts = new List<Point3d>();
        closed = ent switch
        {
            Polyline p => p.Closed,
            Polyline2d p2 => p2.Closed,
            Polyline3d p3 => p3.Closed,
            _ => false
        };

        switch (ent)
        {
            case Polyline pl:
                for (var i = 0; i < pl.NumberOfVertices; i++)
                    pts.Add(pl.GetPoint3dAt(i));
                break;

            case Polyline2d pl2d:
                foreach (ObjectId vxId in pl2d)
                {
                    if (tr.GetObject(vxId, OpenMode.ForRead) is Vertex2d vx)
                        pts.Add(vx.Position);
                }

                break;

            case Polyline3d pl3d:
                foreach (ObjectId vxId in pl3d)
                {
                    if (tr.GetObject(vxId, OpenMode.ForRead) is PolylineVertex3d vx)
                        pts.Add(vx.Position);
                }

                break;
        }

        return pts;
    }
}
