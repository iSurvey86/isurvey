using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace iSurvey.Modules.Plan;

internal enum PathKind
{
    Line,
    LwPolyline,
    Polyline2d,
    Polyline3d
}

/// <summary>Đường dạng chuỗi phân đoạn đã chuẩn hóa từ LINE / LWPOLYLINE / POLYLINE 2D / 3D.</summary>
internal sealed record PathInfo(PathKind Kind, List<Seg> Segs, bool Closed, double Elevation);

internal static class PathExtractor
{
    /// <summary>Trả về null nếu không hỗ trợ (vd Normal khác trục Z) hoặc đường suy biến.</summary>
    public static PathInfo? TryGet(Entity ent, Transaction tr)
    {
        switch (ent)
        {
            case Line line:
                return new PathInfo(PathKind.Line,
                    [new Seg(line.StartPoint, line.EndPoint, 0, 0, 0)], false, line.StartPoint.Z);

            case Polyline pl:
                return FromLwPolyline(pl);

            case Polyline2d p2:
                return FromPolyline2d(p2, tr);

            case Polyline3d p3:
                return FromPolyline3d(p3, tr);

            default:
                return null;
        }
    }

    private static PathInfo? FromLwPolyline(Polyline pl)
    {
        if (!pl.Normal.IsEqualTo(Vector3d.ZAxis))
            return null;

        var n = pl.NumberOfVertices;
        if (n < 2)
            return null;

        var count = pl.Closed ? n : n - 1;
        var segs = new List<Seg>(count);
        for (var i = 0; i < count; i++)
        {
            segs.Add(new Seg(
                pl.GetPoint3dAt(i),
                pl.GetPoint3dAt((i + 1) % n),
                pl.GetBulgeAt(i),
                pl.GetStartWidthAt(i),
                pl.GetEndWidthAt(i)));
        }

        return new PathInfo(PathKind.LwPolyline, segs, pl.Closed, pl.Elevation);
    }

    private static PathInfo? FromPolyline2d(Polyline2d p2, Transaction tr)
    {
        if (!p2.Normal.IsEqualTo(Vector3d.ZAxis))
            return null;

        var verts = new List<(Point3d P, double Bulge, double Sw, double Ew)>();
        foreach (ObjectId id in p2)
        {
            if (tr.GetObject(id, OpenMode.ForRead) is not Vertex2d v)
                continue;
            if (v.VertexType == Vertex2dType.SplineControlVertex)
                continue;

            verts.Add((new Point3d(v.Position.X, v.Position.Y, p2.Elevation), v.Bulge, v.StartWidth, v.EndWidth));
        }

        if (verts.Count < 2)
            return null;

        var count = p2.Closed ? verts.Count : verts.Count - 1;
        var segs = new List<Seg>(count);
        for (var i = 0; i < count; i++)
        {
            var a = verts[i];
            var b = verts[(i + 1) % verts.Count];
            segs.Add(new Seg(a.P, b.P, a.Bulge, a.Sw, a.Ew));
        }

        return new PathInfo(PathKind.Polyline2d, segs, p2.Closed, p2.Elevation);
    }

    /// <summary>
    /// Polyline3d spline-fit: bỏ đỉnh điều khiển (ControlVertex, DXF 70 bit 16);
    /// các đỉnh còn lại nằm trực tiếp trên đường cong thật.
    /// </summary>
    private static PathInfo? FromPolyline3d(Polyline3d p3, Transaction tr)
    {
        var pts = new List<Point3d>();
        foreach (ObjectId id in p3)
        {
            if (tr.GetObject(id, OpenMode.ForRead) is not PolylineVertex3d v)
                continue;
            if (v.VertexType == Vertex3dType.ControlVertex)
                continue;

            pts.Add(v.Position);
        }

        if (pts.Count < 2)
            return null;

        var count = p3.Closed ? pts.Count : pts.Count - 1;
        var segs = new List<Seg>(count);
        for (var i = 0; i < count; i++)
            segs.Add(new Seg(pts[i], pts[(i + 1) % pts.Count], 0, 0, 0));

        return new PathInfo(PathKind.Polyline3d, segs, p3.Closed, 0);
    }
}

internal static class PathWriter
{
    /// <summary>Tạo các chuỗi mới (giữ Layer/Color/Linetype/Lineweight/XData), trả về số đối tượng đã tạo.</summary>
    public static int Write(
        PathInfo info,
        Entity original,
        IReadOnlyList<List<ChainPt>> chains,
        BlockTableRecord btr,
        Transaction tr)
    {
        var created = 0;
        foreach (var chain in chains)
        {
            Entity? ne = info.Kind switch
            {
                PathKind.Line => new Line(
                    new Point3d(chain[0].X, chain[0].Y, chain[0].Z),
                    new Point3d(chain[^1].X, chain[^1].Y, chain[^1].Z)),
                PathKind.Polyline3d => BuildPolyline3d(chain),
                _ => BuildLwPolyline(info, chain, original)
            };

            if (ne is null)
                continue;

            ne.SetPropertiesFrom(original);
            CopyXData(original, ne);

            btr.AppendEntity(ne);
            tr.AddNewlyCreatedDBObject(ne, true);
            created++;
        }

        return created;
    }

    private static Polyline3d BuildPolyline3d(List<ChainPt> chain)
    {
        var pts = new Point3dCollection();
        foreach (var c in chain)
            pts.Add(new Point3d(c.X, c.Y, c.Z));
        return new Polyline3d(Poly3dType.SimplePoly, pts, false);
    }

    private static Polyline BuildLwPolyline(PathInfo info, List<ChainPt> chain, Entity original)
    {
        var pl = new Polyline { Elevation = info.Elevation };
        for (var i = 0; i < chain.Count; i++)
        {
            var c = chain[i];
            var isLast = i == chain.Count - 1;
            pl.AddVertexAt(i, new Point2d(c.X, c.Y), isLast ? 0 : c.Bulge,
                isLast ? 0 : c.StartWidth, isLast ? 0 : c.EndWidth);
        }

        if (original is Polyline op)
            pl.Plinegen = op.Plinegen;

        return pl;
    }

    private static void CopyXData(Entity source, Entity target)
    {
        try
        {
            using var xd = source.XData;
            if (xd is not null)
                target.XData = xd;
        }
        catch
        {
            // XData không bắt buộc
        }
    }
}
