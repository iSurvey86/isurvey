using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using iSurvey.Models;
using iSurvey.Modules.Map;
using AcadColor = Autodesk.AutoCAD.Colors.Color;

namespace iSurvey.Modules.Export;

/// <summary>
/// Thu thập entity CAD → KML. Giữ màu; biên không tô; làm sạch MText; ẩn pin.
/// Bỏ Dim, layer ẩn/đóng băng; không nổ block.
/// </summary>
internal sealed class KmlGeometryCollector
{
    private static readonly Regex MtextGroup = new(
        @"\{\\[^;]*;([^{}]*)\}",
        RegexOptions.Compiled);

    private static readonly Regex MtextEscape = new(
        @"\\[PpCcHhTtQqWwAaFfVvOoLlKk][^;\\]*;?",
        RegexOptions.Compiled);

    private readonly CoordinateService _coordinates;
    private readonly double _centralMeridian;
    private readonly int _zoneWidthDegrees;
    private readonly bool _useZ;

    public int ExportedCount { get; private set; }
    public int SkippedHiddenLayer { get; private set; }
    public int SkippedDimension { get; private set; }
    public int SkippedRaster { get; private set; }
    public int SkippedUnsupported { get; private set; }

    public KmlGeometryCollector(
        CoordinateService coordinates, double centralMeridian, bool useZ, int zoneWidthDegrees = 3)
    {
        _coordinates = coordinates;
        _centralMeridian = centralMeridian;
        _useZ = useZ;
        _zoneWidthDegrees = CoordinateService.NormalizeZoneWidth(zoneWidthDegrees);
    }

    public List<KmlFeature> Collect(Transaction tr, Database db, IEnumerable<ObjectId> ids)
    {
        var features = new List<KmlFeature>();
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

        foreach (var id in ids)
        {
            if (id.IsNull || id.IsErased)
                continue;

            Entity? ent;
            try
            {
                ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            }
            catch
            {
                SkippedUnsupported++;
                continue;
            }

            if (ent is null || !ent.Visible)
            {
                SkippedUnsupported++;
                continue;
            }

            if (!lt.Has(ent.Layer))
            {
                SkippedUnsupported++;
                continue;
            }

            var layer = (LayerTableRecord)tr.GetObject(lt[ent.Layer], OpenMode.ForRead);
            if (layer.IsOff || layer.IsFrozen)
            {
                SkippedHiddenLayer++;
                continue;
            }

            if (ent is Dimension or Leader or MLeader)
            {
                SkippedDimension++;
                continue;
            }

            if (ent is RasterImage)
            {
                SkippedRaster++;
                continue;
            }

            if (TryConvert(tr, ent, layer, out var feature) && feature is not null)
            {
                features.Add(feature);
                ExportedCount++;
            }
            else
            {
                SkippedUnsupported++;
            }
        }

        return features;
    }

    private bool TryConvert(Transaction tr, Entity ent, LayerTableRecord layerRec, out KmlFeature? feature)
    {
        feature = null;
        var layer = ent.Layer;
        var color = ResolveKmlColor(ent, layerRec);
        var width = ResolveLineWidth(ent, layerRec);

        switch (ent)
        {
            case Line line:
                feature = LineString(layer, color, width, [line.StartPoint, line.EndPoint], closed: false, name: null);
                return feature is not null;

            case Polyline pl:
                return FromPolylineVertices(layer, color, width, EnumerateLw(pl), pl.Closed, null, out feature);

            case Polyline2d pl2:
                return FromPolylineVertices(layer, color, width, EnumeratePoly2d(tr, pl2), pl2.Closed, null, out feature);

            case Polyline3d pl3:
                return FromPolylineVertices(layer, color, width, EnumeratePoly3d(tr, pl3), pl3.Closed, null, out feature);

            case Circle circle:
                // Viền tròn — LineString đóng, không Polygon tô.
                feature = LineString(layer, color, width, SampleCircle(circle), closed: true, name: null);
                return feature is not null;

            case Arc arc:
                feature = LineString(layer, color, width, SampleArc(arc), closed: false, name: null);
                return feature is not null;

            case Ellipse ellipse:
                feature = LineString(layer, color, width, SampleEllipse(ellipse), closed: ellipse.Closed, name: null);
                return feature is not null;

            case Spline spline:
                feature = LineString(layer, color, width, SampleCurve(spline, 64), closed: spline.Closed, name: null);
                return feature is not null;

            case DBPoint pt:
                feature = PointFeature(layer, color, pt.Position, null, hideIcon: true);
                return true;

            case BlockReference br:
                feature = PointFeature(layer, color, br.Position, CleanCadText(br.Name), hideIcon: true);
                return true;

            case DBText text:
                feature = PointFeature(layer, color, text.Position, CleanCadText(text.TextString), hideIcon: true);
                return true;

            case MText mtext:
                {
                    var plain = SafeMtextPlain(mtext);
                    feature = PointFeature(layer, color, mtext.Location, plain, hideIcon: true);
                    return true;
                }

            case Hatch hatch:
                return FromHatch(layer, color, width, hatch, out feature);

            case Solid solid:
                return FromSolid(layer, color, width, solid, out feature);

            case Face face:
                return FromFace(layer, color, width, face, out feature);

            case Ray or Xline:
                return false;

            default:
                if (ent is Curve curve)
                {
                    feature = LineString(layer, color, width, SampleCurve(curve, 48), closed: false, name: null);
                    return feature is not null;
                }

                return false;
        }
    }

    private bool FromHatch(string layer, string color, double width, Hatch hatch, out KmlFeature? feature)
    {
        feature = null;
        try
        {
            if (hatch.NumberOfLoops < 1)
                return false;

            var loop = hatch.GetLoopAt(0);
            var pts = new List<Point3d>();

            if (loop.IsPolyline)
            {
                foreach (BulgeVertex bv in loop.Polyline)
                    pts.Add(new Point3d(bv.Vertex.X, bv.Vertex.Y, 0));
            }
            else
            {
                foreach (Curve2d edge in loop.Curves)
                {
                    if (edge is LineSegment2d seg)
                    {
                        pts.Add(new Point3d(seg.StartPoint.X, seg.StartPoint.Y, 0));
                        pts.Add(new Point3d(seg.EndPoint.X, seg.EndPoint.Y, 0));
                    }
                    else if (edge is CircularArc2d carc)
                    {
                        pts.AddRange(SampleCircularArc2d(carc));
                    }
                }
            }

            pts = Deduplicate(pts);
            if (pts.Count < 3)
                return false;

            // Chỉ viền — không tô mảng.
            feature = LineString(layer, color, width, pts, closed: true, name: null);
            return feature is not null;
        }
        catch
        {
            return false;
        }
    }

    private bool FromSolid(string layer, string color, double width, Solid solid, out KmlFeature? feature)
    {
        feature = null;
        try
        {
            var pts = new List<Point3d>();
            for (short i = 0; i < 4; i++)
            {
                var p = solid.GetPointAt(i);
                if (pts.Count == 0 || !pts[^1].IsEqualTo(p))
                    pts.Add(p);
            }

            if (pts.Count >= 3 && pts[0].IsEqualTo(pts[^1]))
                pts.RemoveAt(pts.Count - 1);

            pts = Deduplicate(pts);
            if (pts.Count < 3)
                return false;

            feature = LineString(layer, color, width, pts, closed: true, name: null);
            return feature is not null;
        }
        catch
        {
            return false;
        }
    }

    private bool FromFace(string layer, string color, double width, Face face, out KmlFeature? feature)
    {
        feature = null;
        try
        {
            var pts = new List<Point3d>();
            for (short i = 0; i < 4; i++)
            {
                var p = face.GetVertexAt(i);
                if (pts.Count == 0 || !pts[^1].IsEqualTo(p))
                    pts.Add(p);
            }

            pts = Deduplicate(pts);
            if (pts.Count < 3)
                return false;

            feature = LineString(layer, color, width, pts, closed: true, name: null);
            return feature is not null;
        }
        catch
        {
            return false;
        }
    }

    private bool FromPolylineVertices(
        string layer,
        string color,
        double width,
        IEnumerable<Point3d> vertices,
        bool closed,
        string? name,
        out KmlFeature? feature)
    {
        var pts = Deduplicate(vertices.ToList());
        if (pts.Count < 2)
        {
            feature = null;
            return false;
        }

        // Pline đóng cũng xuất LineString (viền), không Polygon tô trắng.
        feature = LineString(layer, color, width, pts, closed, name);
        return feature is not null;
    }

    private KmlFeature PointFeature(string layer, string color, Point3d p, string? name, bool hideIcon)
    {
        return new KmlFeature
        {
            LayerName = layer,
            Kind = KmlGeometryKind.Point,
            Points = [ToGeo(p)],
            ColorKml = color,
            Name = Truncate(name),
            HideIcon = hideIcon
        };
    }

    private KmlFeature? LineString(
        string layer, string color, double width, IReadOnlyList<Point3d> pts, bool closed, string? name)
    {
        if (pts.Count < 2)
            return null;

        return new KmlFeature
        {
            LayerName = layer,
            Kind = KmlGeometryKind.LineString,
            Points = pts.Select(ToGeo).ToList(),
            ColorKml = color,
            Name = Truncate(name),
            Closed = closed,
            LineWidth = width
        };
    }

    private GeoPoint3 ToGeo(Point3d p)
    {
        var g = _coordinates.ToWgs84(new PlanePoint(p.X, p.Y), _centralMeridian, _zoneWidthDegrees);
        return new GeoPoint3(g.Longitude, g.Latitude, _useZ ? p.Z : 0);
    }

    /// <summary>Màu entity/layer → KML aabbggrr. ACI qua FromColorIndex; đen CAD → trắng trên GE.</summary>
    internal static string ResolveKmlColor(Entity ent, LayerTableRecord layer)
    {
        try
        {
            var c = ent.Color;
            if (c.IsByLayer || c.ColorMethod == ColorMethod.ByLayer)
                c = layer.Color;
            else if (c.IsByBlock || c.ColorMethod == ColorMethod.ByBlock)
                c = layer.Color;

            if (!TryGetRgb(c, out var r, out var g, out var b))
                return "ffffffff";

            // Giữ nguyên RGB CAD — không remap đen/trắng.
            return ToKmlAabbggrr(r, g, b);
        }
        catch
        {
            return "ffffffff";
        }
    }

    internal static double ResolveLineWidth(Entity ent, LayerTableRecord layer)
    {
        try
        {
            var lw = ent.LineWeight;
            if (lw == LineWeight.ByLayer)
                lw = layer.LineWeight;
            if (lw is LineWeight.ByBlock or LineWeight.ByLineWeightDefault or LineWeight.ByLayer)
                return 2.0;

            // Enum value ≈ phần trăm mm (vd LineWeight025 = 25 → 0.25mm)
            var hundredths = (int)lw;
            if (hundredths <= 0)
                return 2.0;

            var px = hundredths / 100.0 * 3.0;
            if (px < 1.5) px = 1.5;
            if (px > 6.0) px = 6.0;
            return px;
        }
        catch
        {
            return 2.0;
        }
    }

    private static bool TryGetRgb(AcadColor c, out byte r, out byte g, out byte b)
    {
        r = g = b = 255;
        try
        {
            // TrueColor
            if (c.ColorMethod == ColorMethod.ByColor)
            {
                r = c.Red;
                g = c.Green;
                b = c.Blue;
                return true;
            }

            // ACI — bắt buộc FromColorIndex; không tin Red trên object ByAci gốc.
            short idx = 7;
            try { idx = (short)c.ColorIndex; }
            catch { idx = 7; }

            if (idx < 0) idx = 7;
            if (idx > 255) idx = 7;

            var resolved = AcadColor.FromColorIndex(ColorMethod.ByAci, idx);
            r = resolved.Red;
            g = resolved.Green;
            b = resolved.Blue;

            // LookUpRgb dự phòng nếu RGB vẫn 0 với index màu thật
            if (r == 0 && g == 0 && b == 0 && idx is not (0 or 250 or 256))
            {
                var rgb = EntityColor.LookUpRgb((byte)idx);
                // AutoCAD LookUpRgb: 0x00RRGGBB
                r = (byte)((rgb >> 16) & 0xFF);
                g = (byte)((rgb >> 8) & 0xFF);
                b = (byte)(rgb & 0xFF);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ToKmlAabbggrr(byte r, byte g, byte b) =>
        string.Create(CultureInfo.InvariantCulture, $"ff{b:x2}{g:x2}{r:x2}");


    private static string? SafeMtextPlain(MText mtext)
    {
        try
        {
            var plain = mtext.Text;
            if (!string.IsNullOrWhiteSpace(plain) && !plain.Contains("{\\", StringComparison.Ordinal))
                return Truncate(CleanCadText(plain));
        }
        catch
        {
            // fallback Contents
        }

        return Truncate(CleanCadText(mtext.Contents));
    }

    /// <summary>Gỡ mã format MText/CAD, giữ nội dung chữ.</summary>
    internal static string? CleanCadText(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var s = raw.Replace("\\P", " ", StringComparison.OrdinalIgnoreCase)
            .Replace("\\p", " ", StringComparison.Ordinal)
            .Replace("\\~", " ", StringComparison.Ordinal);

        for (var i = 0; i < 12; i++)
        {
            var next = MtextGroup.Replace(s, "$1");
            if (next == s)
                break;
            s = next;
        }

        s = MtextEscape.Replace(s, string.Empty);
        s = s.Replace("{", string.Empty, StringComparison.Ordinal)
            .Replace("}", string.Empty, StringComparison.Ordinal);
        s = Regex.Replace(s, @"\s+", " ").Trim();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    private static string? Truncate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return null;
        s = s.Trim();
        return s.Length > 200 ? s[..200] : s;
    }

    private static List<Point3d> Deduplicate(List<Point3d> pts)
    {
        var result = new List<Point3d>();
        foreach (var p in pts)
        {
            if (result.Count == 0 || !result[^1].IsEqualTo(p, new Tolerance(1e-8, 1e-8)))
                result.Add(p);
        }

        return result;
    }

    private static IEnumerable<Point3d> EnumerateLw(Polyline pl)
    {
        for (int i = 0; i < pl.NumberOfVertices; i++)
            yield return pl.GetPoint3dAt(i);
    }

    private static IEnumerable<Point3d> EnumeratePoly2d(Transaction tr, Polyline2d pl)
    {
        foreach (ObjectId id in pl)
        {
            if (tr.GetObject(id, OpenMode.ForRead) is Vertex2d v)
                yield return v.Position;
        }
    }

    private static IEnumerable<Point3d> EnumeratePoly3d(Transaction tr, Polyline3d pl)
    {
        foreach (ObjectId id in pl)
        {
            if (tr.GetObject(id, OpenMode.ForRead) is PolylineVertex3d v)
                yield return v.Position;
        }
    }

    private static List<Point3d> SampleCircle(Circle c, int segments = 48)
    {
        var pts = new List<Point3d>(segments + 1);
        for (int i = 0; i <= segments; i++)
        {
            var a = 2 * Math.PI * i / segments;
            pts.Add(new Point3d(
                c.Center.X + c.Radius * Math.Cos(a),
                c.Center.Y + c.Radius * Math.Sin(a),
                c.Center.Z));
        }

        return pts;
    }

    private static List<Point3d> SampleArc(Arc arc, int segments = 32)
    {
        var pts = new List<Point3d>();
        var start = arc.StartAngle;
        var end = arc.EndAngle;
        var sweep = end - start;
        if (sweep <= 0)
            sweep += 2 * Math.PI;

        for (int i = 0; i <= segments; i++)
        {
            var a = start + sweep * i / segments;
            pts.Add(new Point3d(
                arc.Center.X + arc.Radius * Math.Cos(a),
                arc.Center.Y + arc.Radius * Math.Sin(a),
                arc.Center.Z));
        }

        return pts;
    }

    private static List<Point3d> SampleEllipse(Ellipse ellipse, int segments = 48)
    {
        var pts = new List<Point3d>();
        var start = ellipse.StartParam;
        var end = ellipse.EndParam;
        if (Math.Abs(end - start) < 1e-12)
        {
            start = 0;
            end = 2 * Math.PI;
        }

        for (int i = 0; i <= segments; i++)
        {
            var t = start + (end - start) * i / segments;
            pts.Add(ellipse.GetPointAtParameter(t));
        }

        return pts;
    }

    private static List<Point3d> SampleCurve(Curve curve, int segments)
    {
        var pts = new List<Point3d>();
        try
        {
            var start = curve.StartParam;
            var end = curve.EndParam;
            if (double.IsNaN(start) || double.IsNaN(end) || Math.Abs(end - start) < 1e-12)
                return pts;

            for (int i = 0; i <= segments; i++)
            {
                var t = start + (end - start) * i / segments;
                pts.Add(curve.GetPointAtParameter(t));
            }
        }
        catch
        {
            // ignore
        }

        return Deduplicate(pts);
    }

    private static List<Point3d> SampleCircularArc2d(CircularArc2d carc, int segments = 24)
    {
        var pts = new List<Point3d>();
        var start = carc.StartAngle;
        var end = carc.EndAngle;
        var sweep = end - start;
        if (Math.Abs(sweep) < 1e-12)
            return pts;

        for (int i = 0; i <= segments; i++)
        {
            var a = start + sweep * i / segments;
            var p = carc.Center + carc.Radius * new Vector2d(Math.Cos(a), Math.Sin(a));
            pts.Add(new Point3d(p.X, p.Y, 0));
        }

        return pts;
    }

    public string FormatReport()
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture,
            $"xuất {ExportedCount}; bỏ layer ẩn/đóng băng {SkippedHiddenLayer}; Dim/Leader {SkippedDimension}; raster {SkippedRaster}; không hỗ trợ {SkippedUnsupported}");
        return sb.ToString();
    }
}
