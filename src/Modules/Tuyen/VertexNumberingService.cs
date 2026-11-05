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

    /// <summary>Khoảng hở chữ–chấm trên giấy (mm), thêm vào offset — giữ chữ sát block góc.</summary>
    private const double TextGapMm = 0.35;

    /// <summary>Khoảng hở tối thiểu chữ–tim trên giấy (mm) — không đè line, không đẩy quá xa.</summary>
    private const double ClearOfTimMm = 1.0;

    /// <summary>Bước đẩy chữ ra xa đỉnh khi còn đè tim (theo chiều cao chữ).</summary>
    private const double PushStepFactor = 0.25;

    private const int PushMaxSteps = 32;

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
        var baseOffset = dotRadius + PaperMmToModel(TextGapMm, mapScale);
        var clearOfTim = PaperMmToModel(ClearOfTimMm, mapScale);
        var pushStep = Math.Max(textHeight * PushStepFactor, clearOfTim * 0.5);
        // Không để chữ cách đỉnh quá xa (kể cả khi đẩy né tim).
        var maxOffset = baseOffset + textHeight * 1.8;

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

        var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
        var plane = new Plane(Point3d.Origin, Vector3d.ZAxis);
        var pushed = 0;

        for (var i = 0; i < vertices.Count; i++)
        {
            var v_i = vertices[i];
            var vDir = ComputeOutsideBisector(vertices, i, closed);
            vDir = new Vector3d(vDir.X, vDir.Y, 0);
            if (vDir.Length < 1e-12)
                vDir = Vector3d.XAxis;
            else
                vDir = vDir.GetNormal();

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

            var label = $"{prefix}{startIndex + i}";
            GetAdjacent(vertices, i, closed, out var prev, out var next);
            var halfAngle = HalfAngleFromVertex(prev, v_i, next);

            // Text — gắn trước để đo vùng bao, rồi đẩy ra xa nếu còn đè tim.
            // Góc nhọn (< 90°): ưu tiên phía ngoài (nêm rộng). Góc tù: giữ phân giác hiện tại.
            Vector3d[] dirs;
            if (halfAngle * 2.0 < Math.PI * 0.5)
                dirs = [vDir * -1.0, vDir];
            else
                dirs = [vDir, vDir * -1.0];
            DBText? placed = null;
            for (var dirIdx = 0; dirIdx < dirs.Length; dirIdx++)
            {
                var dir = dirs[dirIdx];
                var justify = JustifyForDirection(dir, plane);
                var txt = placed ?? new DBText
                {
                    LayerId = layerId,
                    Color = AcadColor.FromColorIndex(ColorMethod.ByLayer, 256),
                    Height = textHeight,
                    TextString = label
                };
                if (placed is null)
                {
                    btr.AppendEntity(txt);
                    tr.AddNewlyCreatedDBObject(txt, true);
                    placed = txt;
                }

                txt.Justify = justify;
                // Luôn bắt đầu sát chấm/block góc; chỉ đẩy thêm khi còn đè tim, có trần maxOffset.
                var offset = baseOffset;

                PlaceTextAt(txt, v_i + dir * offset);
                SafeAdjustAlignment(txt, db);

                var moved = false;
                for (var step = 0; step < PushMaxSteps; step++)
                {
                    if (!TextOverlapsTim(txt, prev, v_i, next, clearOfTim, textHeight))
                        break;

                    if (offset + pushStep > maxOffset)
                        break;

                    offset += pushStep;
                    PlaceTextAt(txt, v_i + dir * offset);
                    SafeAdjustAlignment(txt, db);
                    moved = true;
                }

                if (!TextOverlapsTim(txt, prev, v_i, next, clearOfTim, textHeight))
                {
                    if (moved || dirIdx > 0)
                        pushed++;
                    break;
                }

                // Hết bước / chạm trần mà vẫn đè — thử phía phân giác còn lại.
            }
        }

        tr.Commit();
        ed.WriteMessage($"\n[iSurvey] Đã đánh số thành công {vertices.Count} góc tuyến (layer {LayerName}).");
        if (pushed > 0)
            ed.WriteMessage($"\n[iSurvey] Đã đẩy {pushed} chữ ra xa để không đè tim tuyến.");

        // Mô tả tuyến .txt UTF-8: từ Góc → Góc+1; MOTATUYEN placeholder điền tay.
        var txtPath = AngleTableExport.PromptSaveWriteAndOpen(ed, vertices, prefix, startIndex);
        if (!string.IsNullOrEmpty(txtPath))
            ed.WriteMessage($"\n[iSurvey] Đã lưu mô tả tuyến: {txtPath}");

        ActivityLogger.Result("ISURVEY_NUMBER_VERTICES",
            $"{vertices.Count} góc, tiền tố {prefix}, bắt đầu {startIndex}, tỷ lệ 1/{mapScale}, đẩy né tim {pushed}" +
            (string.IsNullOrEmpty(txtPath) ? "" : $", file {ActivityLogger.FileName(txtPath)}"));
    }

    private static AttachmentPoint JustifyForDirection(Vector3d vDir, Plane plane)
    {
        var deg = vDir.AngleOnPlane(plane) * 180.0 / Math.PI;
        if (deg < 0)
            deg += 360.0;

        if (deg is >= 0 and < 90)
            return AttachmentPoint.BottomLeft;
        if (deg is >= 90 and < 180)
            return AttachmentPoint.BottomRight;
        if (deg is >= 180 and < 270)
            return AttachmentPoint.TopRight;
        return AttachmentPoint.TopLeft;
    }

    private static void PlaceTextAt(DBText txt, Point3d insertPt)
    {
        txt.Position = insertPt;
        if (txt.Justify != AttachmentPoint.BaseLeft)
            txt.AlignmentPoint = insertPt;
    }

    private static void SafeAdjustAlignment(DBText txt, Database db)
    {
        try
        {
            txt.AdjustAlignment(db);
        }
        catch
        {
            // một số bản CAD không cần AdjustAlignment
        }
    }

    private static void GetAdjacent(
        IReadOnlyList<Point3d> vertices, int i, bool closed, out Point3d? prev, out Point3d? next)
    {
        var n = vertices.Count;
        prev = null;
        next = null;
        if (closed)
        {
            prev = vertices[(i - 1 + n) % n];
            next = vertices[(i + 1) % n];
            return;
        }

        if (i > 0)
            prev = vertices[i - 1];
        if (i < n - 1)
            next = vertices[i + 1];
    }

    /// <summary>Nửa góc giữa hai cạnh kề tại đỉnh (radian, 0..π/2).</summary>
    private static double HalfAngleFromVertex(Point3d? prev, Point3d vertex, Point3d? next)
    {
        if (prev is null || next is null)
            return Math.PI / 2.0;

        var a = new Vector3d(prev.Value.X - vertex.X, prev.Value.Y - vertex.Y, 0);
        var b = new Vector3d(next.Value.X - vertex.X, next.Value.Y - vertex.Y, 0);
        if (a.Length < 1e-12 || b.Length < 1e-12)
            return Math.PI / 2.0;

        a = a.GetNormal();
        b = b.GetNormal();
        var cos = Math.Max(-1.0, Math.Min(1.0, a.DotProduct(b)));
        var angle = Math.Acos(cos); // 0..π — góc giữa hai cánh tay
        return Math.Max(angle * 0.5, 0.05);
    }

    /// <summary>
    /// True nếu vùng bao chữ (cộng khoảng hở) còn cắt / sát đoạn tim kề đỉnh — phải đẩy chữ ra xa hơn.
    /// </summary>
    private static bool TextOverlapsTim(
        DBText txt, Point3d? prev, Point3d vertex, Point3d? next, double clearance, double textHeight)
    {
        if (!TryGetTextAabb(txt, textHeight, out var minX, out var minY, out var maxX, out var maxY))
            return true;

        // Phình AABB theo khoảng hở bắt buộc.
        minX -= clearance;
        minY -= clearance;
        maxX += clearance;
        maxY += clearance;

        if (prev is { } p && SegmentHitsAabb2d(p, vertex, minX, minY, maxX, maxY))
            return true;
        if (next is { } q && SegmentHitsAabb2d(vertex, q, minX, minY, maxX, maxY))
            return true;

        // Kiểm tra khoảng cách Euclid (tránh lọt góc hộp khi chỉ dựa giao cắt cạnh).
        if (prev is { } p2 && SegmentDistanceToAabb2d(p2, vertex, minX + clearance, minY + clearance, maxX - clearance, maxY - clearance) < clearance)
            return true;
        if (next is { } q2 && SegmentDistanceToAabb2d(vertex, q2, minX + clearance, minY + clearance, maxX - clearance, maxY - clearance) < clearance)
            return true;

        return false;
    }

    private static bool TryGetTextAabb(
        DBText txt, double textHeight, out double minX, out double minY, out double maxX, out double maxY)
    {
        minX = minY = maxX = maxY = 0;
        var insert = txt.Justify == AttachmentPoint.BaseLeft ? txt.Position : txt.AlignmentPoint;
        var estW = Math.Max((txt.TextString?.Length ?? 1), 1) * textHeight * 0.85;
        var estH = textHeight;

        try
        {
            var ext = txt.GeometricExtents;
            minX = ext.MinPoint.X;
            minY = ext.MinPoint.Y;
            maxX = ext.MaxPoint.X;
            maxY = ext.MaxPoint.Y;
            var w = maxX - minX;
            var h = maxY - minY;
            // Extents quá nhỏ so với ước lượng → dùng hộp theo justify.
            if (w >= estW * 0.45 && h >= estH * 0.45)
                return true;
        }
        catch
        {
            // fall through — ước lượng
        }

        // Ước lượng AABB theo điểm neo + justify.
        switch (txt.Justify)
        {
            case AttachmentPoint.BottomLeft:
            case AttachmentPoint.BaseLeft:
                minX = insert.X;
                minY = insert.Y;
                maxX = insert.X + estW;
                maxY = insert.Y + estH;
                break;
            case AttachmentPoint.BottomRight:
            case AttachmentPoint.BaseRight:
                minX = insert.X - estW;
                minY = insert.Y;
                maxX = insert.X;
                maxY = insert.Y + estH;
                break;
            case AttachmentPoint.TopRight:
                minX = insert.X - estW;
                minY = insert.Y - estH;
                maxX = insert.X;
                maxY = insert.Y;
                break;
            case AttachmentPoint.TopLeft:
                minX = insert.X;
                minY = insert.Y - estH;
                maxX = insert.X + estW;
                maxY = insert.Y;
                break;
            default:
                minX = insert.X - estW * 0.5;
                minY = insert.Y - estH * 0.5;
                maxX = insert.X + estW * 0.5;
                maxY = insert.Y + estH * 0.5;
                break;
        }

        return true;
    }

    /// <summary>Đoạn AB (XY) có cắt hoặc chạm hình chữ nhật [min..max] không.</summary>
    private static bool SegmentHitsAabb2d(
        Point3d a, Point3d b, double minX, double minY, double maxX, double maxY)
    {
        var ax = a.X;
        var ay = a.Y;
        var bx = b.X;
        var by = b.Y;

        // Một đầu nằm trong hộp.
        if (PointInAabb(ax, ay, minX, minY, maxX, maxY) || PointInAabb(bx, by, minX, minY, maxX, maxY))
            return true;

        // Cắt một trong bốn cạnh hộp.
        return SegmentsIntersect2d(ax, ay, bx, by, minX, minY, maxX, minY)
               || SegmentsIntersect2d(ax, ay, bx, by, maxX, minY, maxX, maxY)
               || SegmentsIntersect2d(ax, ay, bx, by, maxX, maxY, minX, maxY)
               || SegmentsIntersect2d(ax, ay, bx, by, minX, maxY, minX, minY);
    }

    /// <summary>Khoảng cách ngắn nhất (XY) từ đoạn AB tới AABB gốc (chưa phình).</summary>
    private static double SegmentDistanceToAabb2d(
        Point3d a, Point3d b, double minX, double minY, double maxX, double maxY)
    {
        if (maxX < minX || maxY < minY)
            return 0;

        if (SegmentHitsAabb2d(a, b, minX, minY, maxX, maxY))
            return 0;

        var best = double.MaxValue;
        // 4 góc hộp → đoạn
        best = Math.Min(best, PointToSegmentDistance2d(minX, minY, a.X, a.Y, b.X, b.Y));
        best = Math.Min(best, PointToSegmentDistance2d(maxX, minY, a.X, a.Y, b.X, b.Y));
        best = Math.Min(best, PointToSegmentDistance2d(maxX, maxY, a.X, a.Y, b.X, b.Y));
        best = Math.Min(best, PointToSegmentDistance2d(minX, maxY, a.X, a.Y, b.X, b.Y));
        // 2 đầu đoạn → hộp
        best = Math.Min(best, PointToAabbDistance2d(a.X, a.Y, minX, minY, maxX, maxY));
        best = Math.Min(best, PointToAabbDistance2d(b.X, b.Y, minX, minY, maxX, maxY));
        return best;
    }

    private static double PointToAabbDistance2d(
        double x, double y, double minX, double minY, double maxX, double maxY)
    {
        var dx = x < minX ? minX - x : x > maxX ? x - maxX : 0;
        var dy = y < minY ? minY - y : y > maxY ? y - maxY : 0;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static double PointToSegmentDistance2d(
        double px, double py, double ax, double ay, double bx, double by)
    {
        var abx = bx - ax;
        var aby = by - ay;
        var len2 = abx * abx + aby * aby;
        if (len2 < 1e-24)
        {
            var dx = px - ax;
            var dy = py - ay;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        var t = ((px - ax) * abx + (py - ay) * aby) / len2;
        if (t < 0) t = 0;
        else if (t > 1) t = 1;
        var qx = ax + t * abx;
        var qy = ay + t * aby;
        var ex = px - qx;
        var ey = py - qy;
        return Math.Sqrt(ex * ex + ey * ey);
    }

    private static bool PointInAabb(double x, double y, double minX, double minY, double maxX, double maxY) =>
        x >= minX && x <= maxX && y >= minY && y <= maxY;

    private static bool SegmentsIntersect2d(
        double ax, double ay, double bx, double by,
        double cx, double cy, double dx, double dy)
    {
        var d1 = Cross(bx - ax, by - ay, cx - ax, cy - ay);
        var d2 = Cross(bx - ax, by - ay, dx - ax, dy - ay);
        var d3 = Cross(dx - cx, dy - cy, ax - cx, ay - cy);
        var d4 = Cross(dx - cx, dy - cy, bx - cx, by - cy);

        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            return true;

        // Chạm đầu mút / trùng thẳng.
        const double eps = 1e-10;
        if (Math.Abs(d1) <= eps && PointOnSegment(ax, ay, bx, by, cx, cy))
            return true;
        if (Math.Abs(d2) <= eps && PointOnSegment(ax, ay, bx, by, dx, dy))
            return true;
        if (Math.Abs(d3) <= eps && PointOnSegment(cx, cy, dx, dy, ax, ay))
            return true;
        if (Math.Abs(d4) <= eps && PointOnSegment(cx, cy, dx, dy, bx, by))
            return true;
        return false;
    }

    private static double Cross(double ax, double ay, double bx, double by) => ax * by - ay * bx;

    private static bool PointOnSegment(double ax, double ay, double bx, double by, double px, double py)
    {
        var minX = Math.Min(ax, bx) - 1e-9;
        var maxX = Math.Max(ax, bx) + 1e-9;
        var minY = Math.Min(ay, by) - 1e-9;
        var maxY = Math.Max(ay, by) + 1e-9;
        return px >= minX && px <= maxX && py >= minY && py <= maxY;
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
