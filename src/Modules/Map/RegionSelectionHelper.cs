using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using iSurvey.Models;
using AcadRegion = Autodesk.AutoCAD.DatabaseServices.Region;

namespace iSurvey.Modules.Map;

/// <summary>Chọn đường bao và trích xuất polygon clip.</summary>
internal static class RegionSelectionHelper
{
    /// <summary>Yêu cầu chọn đối tượng đóng; trả về null nếu hủy.</summary>
    public static BoundarySelection? TrySelectBoundary(Editor ed, Database db, string? prompt = null)
    {
        var options = new PromptEntityOptions(prompt ?? "\nChọn đường bao (Polyline/Region/Circle): ")
        {
            AllowNone = false
        };
        options.SetRejectMessage("\n[iSurvey] Chỉ chọn Polyline đóng, Region hoặc Circle.");
        options.AddAllowedClass(typeof(Polyline), true);
        options.AddAllowedClass(typeof(Polyline2d), true);
        options.AddAllowedClass(typeof(Polyline3d), true);
        options.AddAllowedClass(typeof(AcadRegion), true);
        options.AddAllowedClass(typeof(Circle), true);

        var pick = ed.GetEntity(options);
        if (pick.Status != PromptStatus.OK)
            return null;

        using var tr = db.TransactionManager.StartTransaction();
        var entity = tr.GetObject(pick.ObjectId, OpenMode.ForRead) as Entity
                       ?? throw new InvalidOperationException("Không đọc được đối tượng đã chọn.");

        ValidateBoundary(entity);
        var polygon = ExtractClipPolygon(entity, tr);
        if (polygon.Count < 3)
            throw new InvalidOperationException("Đường bao cần ít nhất 3 đỉnh.");

        var ext = entity.GeometricExtents;
        tr.Commit();

        return new BoundarySelection
        {
            Bounds = ExtentsToCorners(ext),
            ClipPolygonWcs = polygon
        };
    }

    private static void ValidateBoundary(Entity entity)
    {
        switch (entity)
        {
            case Polyline pl when !pl.Closed && pl.NumberOfVertices > 2:
            {
                var dist = pl.GetPoint3dAt(0).DistanceTo(pl.GetPoint3dAt(pl.NumberOfVertices - 1));
                if (dist > 1e-4)
                    throw new InvalidOperationException("Polyline chưa đóng. Dùng PEDIT → Close.");
                break;
            }
            case Polyline2d pl2d when !pl2d.Closed:
                throw new InvalidOperationException("Polyline2d chưa đóng.");
        }
    }

    /// <summary>Trích đỉnh đa giác clip từ entity đã chọn.</summary>
    private static List<Point2d> ExtractClipPolygon(Entity entity, Transaction tr)
    {
        switch (entity)
        {
            case Polyline pl:
                return ExtractPolylineVertices(pl);
            case Polyline2d pl2d:
                return ExtractPolyline2dVertices(pl2d, tr);
            case Polyline3d pl3d:
                return ExtractPolyline3dVertices(pl3d, tr);
            case Circle circle:
                return ApproximateCircle(circle, 64);
            case AcadRegion region:
                return ExtractRegionOutline(region);
            default:
                throw new InvalidOperationException($"Không hỗ trợ loại đối tượng: {entity.GetType().Name}");
        }
    }

    private static List<Point2d> ExtractPolylineVertices(Polyline pl)
    {
        var count = pl.NumberOfVertices;
        var pts = new List<Point2d>(count);
        for (var i = 0; i < count; i++)
        {
            var p = pl.GetPoint3dAt(i);
            pts.Add(new Point2d(p.X, p.Y));
        }

        // Bỏ đỉnh trùng cuối nếu trùng đầu
        if (pts.Count > 1 && pts[0].GetDistanceTo(pts[^1]) < 1e-4)
            pts.RemoveAt(pts.Count - 1);

        return pts;
    }

    private static List<Point2d> ExtractPolyline2dVertices(Polyline2d pl2d, Transaction tr)
    {
        var pts = new List<Point2d>();
        foreach (ObjectId id in pl2d)
        {
            if (tr.GetObject(id, OpenMode.ForRead) is Vertex2d vtx)
                pts.Add(new Point2d(vtx.Position.X, vtx.Position.Y));
        }

        if (pts.Count > 1 && pts[0].GetDistanceTo(pts[^1]) < 1e-4)
            pts.RemoveAt(pts.Count - 1);

        return pts;
    }

    private static List<Point2d> ExtractPolyline3dVertices(Polyline3d pl3d, Transaction tr)
    {
        var pts = new List<Point2d>();
        foreach (ObjectId id in pl3d)
        {
            if (tr.GetObject(id, OpenMode.ForRead) is PolylineVertex3d vtx)
            {
                var p = vtx.Position;
                pts.Add(new Point2d(p.X, p.Y));
            }
        }

        if (pts.Count > 1 && pts[0].GetDistanceTo(pts[^1]) < 1e-4)
            pts.RemoveAt(pts.Count - 1);

        return pts;
    }

    private static List<Point2d> ApproximateCircle(Circle circle, int segments)
    {
        var pts = new List<Point2d>(segments);
        var c = circle.Center;
        var r = circle.Radius;
        for (var i = 0; i < segments; i++)
        {
            var ang = 2.0 * Math.PI * i / segments;
            pts.Add(new Point2d(c.X + r * Math.Cos(ang), c.Y + r * Math.Sin(ang)));
        }

        return pts;
    }

    /// <summary>Region: explode lấy Polyline/Circle ngoài cùng.</summary>
    private static List<Point2d> ExtractRegionOutline(AcadRegion region)
    {
        var exploded = new DBObjectCollection();
        region.Explode(exploded);

        Entity? best = null;
        double bestArea = 0;
        foreach (DBObject obj in exploded)
        {
            if (obj is Entity ent)
            {
                try
                {
                    var ext = ent.GeometricExtents;
                    var area = (ext.MaxPoint.X - ext.MinPoint.X) * (ext.MaxPoint.Y - ext.MinPoint.Y);
                    if (area > bestArea)
                    {
                        bestArea = area;
                        best = ent;
                    }
                }
                catch
                {
                    // bỏ qua entity không có extents
                }
            }
        }

        if (best is null)
            throw new InvalidOperationException("Không trích được biên Region.");

        // Explode tạo entity tạm — không cần transaction cho read-only extract
        return best switch
        {
            Polyline pl => ExtractPolylineVertices(pl),
            Circle circle => ApproximateCircle(circle, 64),
            _ => throw new InvalidOperationException("Region explode không cho Polyline/Circle.")
        };
    }

    public static ViewportCorners ExtentsToCorners(Extents3d ext)
    {
        var min = ext.MinPoint;
        var max = ext.MaxPoint;
        return new ViewportCorners(
            new PlanePoint(min.X, min.Y),
            new PlanePoint(max.X, min.Y),
            new PlanePoint(max.X, max.Y),
            new PlanePoint(min.X, max.Y));
    }
}
