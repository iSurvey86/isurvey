using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using iSurvey.Modules.Terrain;
using iSurvey.Modules.Tuyen;

namespace iSurvey.Modules.Profile;

/// <summary>
/// Lấy mẫu Z dọc tim tuyến: cọc + bước khoảng cách + giao cạnh TIN (nếu có mô hình).
/// </summary>
internal static class ProfileSampleService
{
    public sealed class SamplePoint
    {
        public double Chainage { get; init; }

        public double X { get; init; }

        public double Y { get; init; }

        public double Z { get; init; }

        public bool HasZ { get; init; }

        public bool IsStation { get; init; }

        public string Name { get; init; } = "";
    }

    public static List<SamplePoint> Collect(
        Document doc,
        RouteRecord route,
        double sampleStepM,
        TerrainRecord? terrain)
    {
        var map = new SortedDictionary<double, SamplePoint>();

        void Upsert(double chainage, double x, double y, double z, bool hasZ, bool isStation, string name)
        {
            var key = Math.Round(chainage, 4);
            if (map.TryGetValue(key, out var existing))
            {
                if (isStation && !existing.IsStation)
                {
                    map[key] = new SamplePoint
                    {
                        Chainage = chainage,
                        X = x,
                        Y = y,
                        Z = hasZ ? z : existing.Z,
                        HasZ = hasZ || existing.HasZ,
                        IsStation = true,
                        Name = string.IsNullOrWhiteSpace(name) ? existing.Name : name
                    };
                }
                else if (hasZ && !existing.HasZ)
                {
                    map[key] = new SamplePoint
                    {
                        Chainage = existing.Chainage,
                        X = existing.X,
                        Y = existing.Y,
                        Z = z,
                        HasZ = true,
                        IsStation = existing.IsStation,
                        Name = existing.Name
                    };
                }

                return;
            }

            map[key] = new SamplePoint
            {
                Chainage = chainage,
                X = x,
                Y = y,
                Z = z,
                HasZ = hasZ,
                IsStation = isStation,
                Name = name ?? ""
            };
        }

        if (route.Stations is not null)
        {
            foreach (var st in route.Stations)
            {
                Upsert(st.Chainage, st.X, st.Y, st.Z, st.HasZ, true, st.Name);
            }
        }

        Curve? curve = null;
        using (var tr = doc.TransactionManager.StartTransaction())
        {
            if (TryOpenCurve(tr, doc.Database, route.PolylineHandle, out curve) && curve is not null)
            {
                var length = curve.GetDistanceAtParameter(curve.EndParam) - curve.GetDistanceAtParameter(curve.StartParam);
                if (sampleStepM > 1e-3 && length > sampleStepM)
                {
                    for (var d = sampleStepM; d < length - 1e-6; d += sampleStepM)
                    {
                        try
                        {
                            var pt = curve.GetPointAtDist(d);
                            var chain = route.Reversed
                                ? route.StartChainage + (length - d)
                                : route.StartChainage + d;
                            Upsert(chain, pt.X, pt.Y, 0, false, false, "");
                        }
                        catch
                        {
                            // bỏ điểm lỗi
                        }
                    }
                }

                if (terrain is not null)
                    AddTinEdgeCrossings(tr, curve, route, terrain, Upsert);
            }

            tr.Commit();
        }

        // Gán Z từ TIN / Face cho điểm chưa có Z.
        if (terrain is not null)
        {
            foreach (var key in map.Keys.ToList())
            {
                var p = map[key];
                if (p.HasZ)
                    continue;
                if (TerrainService.TryQuery(doc, terrain, p.X, p.Y, out var z))
                {
                    map[key] = new SamplePoint
                    {
                        Chainage = p.Chainage,
                        X = p.X,
                        Y = p.Y,
                        Z = z,
                        HasZ = true,
                        IsStation = p.IsStation,
                        Name = p.Name
                    };
                }
            }
        }

        var list = new List<SamplePoint>();
        foreach (var p in map.Values)
        {
            if (p.HasZ)
                list.Add(p);
        }

        return list;
    }

    private static void AddTinEdgeCrossings(
        Transaction tr,
        Curve curve,
        RouteRecord route,
        TerrainRecord terrain,
        Action<double, double, double, double, bool, bool, string> upsert)
    {
        var triangles = terrain.Triangles();
        if (triangles.Count == 0)
        {
            // Thử Face trên bản vẽ.
            var space = (BlockTableRecord)tr.GetObject(curve.Database.CurrentSpaceId, OpenMode.ForRead);
            foreach (ObjectId id in space)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is not Face face || face.IsErased)
                    continue;
                var a = face.GetVertexAt(0);
                var b = face.GetVertexAt(1);
                var c = face.GetVertexAt(2);
                ConsiderEdge(curve, route, a, b, upsert);
                ConsiderEdge(curve, route, b, c, upsert);
                ConsiderEdge(curve, route, c, a, upsert);
            }

            return;
        }

        foreach (var tri in triangles)
        {
            ConsiderEdge(curve, route,
                new Point3d(tri.A.X, tri.A.Y, tri.A.Z),
                new Point3d(tri.B.X, tri.B.Y, tri.B.Z), upsert);
            ConsiderEdge(curve, route,
                new Point3d(tri.B.X, tri.B.Y, tri.B.Z),
                new Point3d(tri.C.X, tri.C.Y, tri.C.Z), upsert);
            ConsiderEdge(curve, route,
                new Point3d(tri.C.X, tri.C.Y, tri.C.Z),
                new Point3d(tri.A.X, tri.A.Y, tri.A.Z), upsert);
        }
    }

    private static void ConsiderEdge(
        Curve curve,
        RouteRecord route,
        Point3d a,
        Point3d b,
        Action<double, double, double, double, bool, bool, string> upsert)
    {
        if (!TryIntersect2d(curve, a, b, out var hit, out var along))
            return;
        var length = curve.GetDistanceAtParameter(curve.EndParam) - curve.GetDistanceAtParameter(curve.StartParam);
        var chain = route.Reversed
            ? route.StartChainage + (length - along)
            : route.StartChainage + along;
        var t = SegmentT(a, b, hit);
        var z = a.Z + (b.Z - a.Z) * t;
        upsert(chain, hit.X, hit.Y, z, true, false, "");
    }

    private static bool TryIntersect2d(Curve curve, Point3d a, Point3d b, out Point3d hit, out double along)
    {
        hit = Point3d.Origin;
        along = 0;
        try
        {
            using var edge = new Line(new Point3d(a.X, a.Y, 0), new Point3d(b.X, b.Y, 0));
            var pts = new Point3dCollection();
            curve.IntersectWith(edge, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
            if (pts.Count == 0)
                return false;
            hit = pts[0];
            var on = curve.GetClosestPointTo(hit, false);
            along = curve.GetDistAtPoint(on);
            hit = new Point3d(on.X, on.Y, 0);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static double SegmentT(Point3d a, Point3d b, Point3d p)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var len2 = dx * dx + dy * dy;
        if (len2 < 1e-12)
            return 0;
        var t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / len2;
        if (t < 0)
            return 0;
        if (t > 1)
            return 1;
        return t;
    }

    private static bool TryOpenCurve(Transaction tr, Database db, string handleText, out Curve? curve)
    {
        curve = null;
        if (string.IsNullOrWhiteSpace(handleText))
            return false;
        try
        {
            var handle = new Handle(Convert.ToInt64(handleText, 16));
            if (!db.TryGetObjectId(handle, out var id) || id.IsNull)
                return false;
            if (tr.GetObject(id, OpenMode.ForRead) is not Curve c || c.IsErased)
                return false;
            curve = c;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
