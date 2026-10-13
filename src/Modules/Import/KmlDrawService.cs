using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using iSurvey.Core;
using iSurvey.Models;
using iSurvey.Modules.Map;
using AcadColor = Autodesk.AutoCAD.Colors.Color;

namespace iSurvey.Modules.Import;

internal sealed class KmlDrawReport
{
    public int Points { get; set; }
    public int Texts { get; set; }
    public int Lines { get; set; }
    public int Polygons { get; set; }
    public int InnerRings { get; set; }
    public int TracksSkipped { get; set; }
    public bool PointStyleAdjusted { get; set; }
    public bool Simplified { get; set; }
    public int OriginalVertices { get; set; }
    public int SimplifiedVertices { get; set; }
    public bool HasExtents { get; set; }
    public double MinX { get; set; }
    public double MinY { get; set; }
    public double MaxX { get; set; }
    public double MaxY { get; set; }
}

/// <summary>Vẽ shape đã đọc vào layer KML_IMPORT, một transaction.</summary>
internal static class KmlDrawService
{
    public const string LayerName = "KML_IMPORT";

    public static KmlDrawReport Draw(
        Database db,
        IReadOnlyList<KmlShape> shapes,
        CoordinateService coordinates,
        double centralMeridian,
        int zoneWidthDegrees,
        bool insertPoints,
        bool labelNames,
        double textHeight,
        bool simplifyVertices,
        double simplifyTolerance)
    {
        var report = new KmlDrawReport();
        using var tr = db.TransactionManager.StartTransaction();
        EnsureLayer(tr, db);

        var space = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
        var zone = CoordinateService.NormalizeZoneWidth(zoneWidthDegrees);

        foreach (var shape in shapes)
        {
            if (shape.IsPoint)
            {
                DrawPoint(tr, space, shape, coordinates, centralMeridian, zone, insertPoints, labelNames, textHeight, report);
                continue;
            }

            DrawCurve(tr, space, shape, coordinates, centralMeridian, zone, simplifyVertices, simplifyTolerance, report);
        }

        tr.Commit();
        return report;
    }

    private static void DrawPoint(
        Transaction tr,
        BlockTableRecord space,
        KmlShape shape,
        CoordinateService coordinates,
        double centralMeridian,
        int zone,
        bool insertPoints,
        bool labelNames,
        double textHeight,
        KmlDrawReport report)
    {
        var v = shape.Vertices[0];
        var plane = coordinates.ToVn2000(new GeoPoint(v.Longitude, v.Latitude), centralMeridian, zone);
        var z = v.HasElevation ? v.Elevation : 0;
        var position = new Point3d(plane.Easting, plane.Northing, z);
        if (insertPoints || (labelNames && !string.IsNullOrWhiteSpace(shape.Name)))
            IncludeExtents(report, position);

        if (insertPoints)
        {
            var point = new DBPoint(position);
            point.SetDatabaseDefaults();
            point.Layer = LayerName;
            point.Position = position;
            space.AppendEntity(point);
            tr.AddNewlyCreatedDBObject(point, true);
            report.Points++;
        }

        if (labelNames && !string.IsNullOrWhiteSpace(shape.Name))
        {
            var text = new DBText();
            text.SetDatabaseDefaults();
            text.Position = new Point3d(position.X + textHeight * 0.5, position.Y, position.Z);
            text.Height = textHeight;
            text.TextString = shape.Name;
            text.Layer = LayerName;
            space.AppendEntity(text);
            tr.AddNewlyCreatedDBObject(text, true);
            report.Texts++;
        }
    }

    private static void DrawCurve(
        Transaction tr,
        BlockTableRecord space,
        KmlShape shape,
        CoordinateService coordinates,
        double centralMeridian,
        int zone,
        bool simplifyVertices,
        double simplifyTolerance,
        KmlDrawReport report)
    {
        var projected = new List<Point3d>(shape.Vertices.Count);
        foreach (var v in shape.Vertices)
        {
            var plane = coordinates.ToVn2000(new GeoPoint(v.Longitude, v.Latitude), centralMeridian, zone);
            var z = v.HasElevation ? v.Elevation : 0;
            var point = new Point3d(plane.Easting, plane.Northing, z);
            projected.Add(point);
            IncludeExtents(report, point);
        }

        if (simplifyVertices)
        {
            report.Simplified = true;
            report.OriginalVertices += projected.Count;
            var minCount = shape.CloseRing ? 3 : 2;
            if (projected.Count >= 3 && simplifyTolerance > 0)
            {
                var cleaned = GeoMath.DouglasPeucker(projected, simplifyTolerance);
                if (cleaned.Count >= minCount)
                    projected = cleaned;
            }

            report.SimplifiedVertices += projected.Count;
        }

        var minZ = projected[0].Z;
        var maxZ = projected[0].Z;
        for (var i = 1; i < projected.Count; i++)
        {
            var z = projected[i].Z;
            if (z < minZ) minZ = z;
            if (z > maxZ) maxZ = z;
        }

        if (maxZ - minZ > 0.001)
            AppendPolyline3d(tr, space, projected, shape.CloseRing);
        else
            AppendPolyline(tr, space, projected, shape.CloseRing, projected[0].Z);

        if (shape.CloseRing)
            report.Polygons++;
        else
            report.Lines++;
    }

    private static void AppendPolyline(Transaction tr, BlockTableRecord space, List<Point3d> points, bool closed, double elevation)
    {
        var poly = new Polyline();
        for (var i = 0; i < points.Count; i++)
            poly.AddVertexAt(i, new Point2d(points[i].X, points[i].Y), 0, 0, 0);
        poly.SetDatabaseDefaults();
        poly.Elevation = elevation;
        poly.Closed = closed;
        poly.Layer = LayerName;
        space.AppendEntity(poly);
        tr.AddNewlyCreatedDBObject(poly, true);
    }

    private static void AppendPolyline3d(Transaction tr, BlockTableRecord space, List<Point3d> points, bool closed)
    {
        var poly = new Polyline3d(Poly3dType.SimplePoly, new Point3dCollection(), closed);
        poly.SetDatabaseDefaults();
        poly.Closed = closed;
        poly.Layer = LayerName;
        space.AppendEntity(poly);
        tr.AddNewlyCreatedDBObject(poly, true);
        foreach (var point in points)
        {
            var vertex = new PolylineVertex3d(point);
            poly.AppendVertex(vertex);
            tr.AddNewlyCreatedDBObject(vertex, true);
        }
    }

    private static void EnsureLayer(Transaction tr, Database db)
    {
        var layers = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (layers.Has(LayerName))
        {
            var existing = (LayerTableRecord)tr.GetObject(layers[LayerName], OpenMode.ForRead);
            if (existing.IsLocked)
            {
                existing.UpgradeOpen();
                existing.IsLocked = false;
            }

            return;
        }

        layers.UpgradeOpen();
        var created = new LayerTableRecord
        {
            Name = LayerName,
            Color = AcadColor.FromColorIndex(ColorMethod.ByAci, 4)
        };
        layers.Add(created);
        tr.AddNewlyCreatedDBObject(created, true);
    }

    private static void IncludeExtents(KmlDrawReport report, Point3d point)
    {
        if (!report.HasExtents)
        {
            report.MinX = report.MaxX = point.X;
            report.MinY = report.MaxY = point.Y;
            report.HasExtents = true;
            return;
        }

        if (point.X < report.MinX) report.MinX = point.X;
        if (point.Y < report.MinY) report.MinY = point.Y;
        if (point.X > report.MaxX) report.MaxX = point.X;
        if (point.Y > report.MaxY) report.MaxY = point.Y;
    }

    /// <summary>Phóng khung nhìn vừa khít hình vừa nhập, có chừa mép.</summary>
    public static void FitImported(Editor ed, KmlDrawReport report)
    {
        if (!report.HasExtents)
            return;

        try
        {
            var view = ed.GetCurrentView();
            var width = Math.Max(report.MaxX - report.MinX, 20);
            var height = Math.Max(report.MaxY - report.MinY, 20);
            width *= 1.08;
            height *= 1.08;
            var aspect = view.Height > 1e-9 ? view.Width / view.Height : 1.6;
            if (width / height > aspect)
                height = width / aspect;
            else
                width = height * aspect;

            view.CenterPoint = new Point2d((report.MinX + report.MaxX) / 2.0, (report.MinY + report.MaxY) / 2.0);
            view.Width = width;
            view.Height = height;
            ed.SetCurrentView(view);
        }
        catch
        {
            // Không chặn lệnh nếu không đổi được khung nhìn.
        }
    }
}
