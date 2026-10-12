using Autodesk.AutoCAD.Geometry;

namespace iSurvey.Modules.Plan;

/// <summary>Đa giác biên cắt 2D (XY). Điểm nằm trên cạnh coi là nằm trong.</summary>
internal sealed class ClipPolygon
{
    public const double Eps = 1e-7;
    private const double TEps = 1e-9;

    public double[] X { get; }
    public double[] Y { get; }
    public double MinX { get; }
    public double MaxX { get; }
    public double MinY { get; }
    public double MaxY { get; }
    public int Count => X.Length;

    public ClipPolygon(IReadOnlyList<Point2d> pts)
    {
        X = pts.Select(p => p.X).ToArray();
        Y = pts.Select(p => p.Y).ToArray();
        MinX = X.Min();
        MaxX = X.Max();
        MinY = Y.Min();
        MaxY = Y.Max();
    }

    public bool BboxOverlaps(double minX, double minY, double maxX, double maxY) =>
        !(maxX < MinX - Eps || minX > MaxX + Eps || maxY < MinY - Eps || minY > MaxY + Eps);

    /// <summary>Ray-casting 2D (bỏ qua Z), gồm cả điểm nằm trên biên.</summary>
    public bool Contains(double x, double y)
    {
        if (x < MinX - Eps || x > MaxX + Eps || y < MinY - Eps || y > MaxY + Eps)
            return false;

        var inside = false;
        var n = X.Length;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            double xi = X[i], yi = Y[i], xj = X[j], yj = Y[j];
            if (OnSegment(x, y, xj, yj, xi, yi))
                return true;

            if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi)
                inside = !inside;
        }

        return inside;
    }

    private static bool OnSegment(double px, double py, double ax, double ay, double bx, double by)
    {
        if (px < Math.Min(ax, bx) - Eps || px > Math.Max(ax, bx) + Eps ||
            py < Math.Min(ay, by) - Eps || py > Math.Max(ay, by) + Eps)
            return false;

        double dx = bx - ax, dy = by - ay;
        var len2 = dx * dx + dy * dy;
        if (len2 < 1e-24)
            return Math.Abs(px - ax) <= Eps && Math.Abs(py - ay) <= Eps;

        var t = Math.Clamp(((px - ax) * dx + (py - ay) * dy) / len2, 0, 1);
        var cx = ax + t * dx - px;
        var cy = ay + t * dy - py;
        return cx * cx + cy * cy <= Eps * Eps;
    }

    /// <summary>Tham số t∈[0,1] các giao điểm 2D của đoạn AB với cạnh đa giác.</summary>
    public void SegmentParams(double ax, double ay, double bx, double by, List<double> ts)
    {
        double dx = bx - ax, dy = by - ay;
        double sMinX = Math.Min(ax, bx), sMaxX = Math.Max(ax, bx);
        double sMinY = Math.Min(ay, by), sMaxY = Math.Max(ay, by);
        var dLen = Math.Abs(dx) + Math.Abs(dy);

        var n = X.Length;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            double cx = X[j], cy = Y[j];
            double ex = X[i] - cx, ey = Y[i] - cy;

            if (Math.Max(cx, cx + ex) < sMinX - Eps || Math.Min(cx, cx + ex) > sMaxX + Eps ||
                Math.Max(cy, cy + ey) < sMinY - Eps || Math.Min(cy, cy + ey) > sMaxY + Eps)
                continue;

            var denom = dx * ey - dy * ex;
            if (Math.Abs(denom) < 1e-12 * dLen * (Math.Abs(ex) + Math.Abs(ey)))
                continue; // song song / trùng phương: trung điểm sẽ quyết định

            double wx = cx - ax, wy = cy - ay;
            var t = (wx * ey - wy * ex) / denom;
            var u = (wx * dy - wy * dx) / denom;
            if (t >= -TEps && t <= 1 + TEps && u >= -TEps && u <= 1 + TEps)
                ts.Add(Math.Clamp(t, 0, 1));
        }
    }

    /// <summary>
    /// Tham số s∈[0,1] (theo góc quét) các giao điểm của cung tròn với cạnh đa giác.
    /// theta là góc quét có dấu (CCW dương).
    /// </summary>
    public void ArcParams(double cx0, double cy0, double r, double a0, double theta, List<double> ts)
    {
        var absTheta = Math.Abs(theta);
        var sign = theta >= 0 ? 1.0 : -1.0;
        var twoPi = 2 * Math.PI;

        var n = X.Length;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            double ex0 = X[j], ey0 = Y[j];
            double ex = X[i] - ex0, ey = Y[i] - ey0;
            var a = ex * ex + ey * ey;
            if (a < 1e-24)
                continue;

            double px = ex0 - cx0, py = ey0 - cy0;
            var b = 2 * (px * ex + py * ey);
            var c = px * px + py * py - r * r;
            var disc = b * b - 4 * a * c;
            if (disc < 0)
                continue;

            var sq = Math.Sqrt(disc);
            foreach (var u in new[] { (-b - sq) / (2 * a), (-b + sq) / (2 * a) })
            {
                if (u < -TEps || u > 1 + TEps)
                    continue;

                var qx = ex0 + u * ex;
                var qy = ey0 + u * ey;
                var ang = Math.Atan2(qy - cy0, qx - cx0);
                var delta = (ang - a0) * sign;
                delta %= twoPi;
                if (delta < 0)
                    delta += twoPi;
                if (delta > twoPi - 1e-9)
                    delta = 0;

                var s = delta / absTheta;
                if (s <= 1 + TEps)
                    ts.Add(Math.Clamp(s, 0, 1));
            }
        }
    }
}

/// <summary>Một phân đoạn 3D: thẳng (Bulge=0) hoặc cung (Bulge≠0, Z nội suy tuyến tính).</summary>
internal readonly record struct Seg(Point3d A, Point3d B, double Bulge, double StartWidth, double EndWidth);

/// <summary>Điểm của chuỗi kết quả; Bulge/StartWidth/EndWidth mô tả phân đoạn đi ra từ điểm này.</summary>
internal sealed class ChainPt
{
    public double X { get; init; }
    public double Y { get; init; }
    public double Z { get; init; }
    public double Bulge { get; set; }
    public double StartWidth { get; set; }
    public double EndWidth { get; set; }
}

internal sealed class CutResult
{
    /// <summary>Toàn bộ nằm trong biên — giữ nguyên đối tượng gốc.</summary>
    public bool Unchanged { get; init; }

    public List<List<ChainPt>> Chains { get; init; } = [];
}

internal static class SegmentCutter
{
    private const double TEps = 1e-9;
    private const double MergeEps = 1e-8;

    private static bool IsStraight(in Seg s)
    {
        if (Math.Abs(s.Bulge) < 1e-12)
            return true;
        var dx = s.B.X - s.A.X;
        var dy = s.B.Y - s.A.Y;
        return dx * dx + dy * dy < 1e-18;
    }

    private static (double Cx, double Cy, double R, double A0, double Theta) ArcOf(in Seg s)
    {
        double dx = s.B.X - s.A.X, dy = s.B.Y - s.A.Y;
        var chord = Math.Sqrt(dx * dx + dy * dy);
        var theta = 4 * Math.Atan(s.Bulge);
        var half = Math.Abs(theta) / 2;
        var r = chord / (2 * Math.Sin(half));
        var h = r * Math.Cos(half);
        double mx = (s.A.X + s.B.X) / 2, my = (s.A.Y + s.B.Y) / 2;
        double nx = -dy / chord, ny = dx / chord;
        var sign = s.Bulge > 0 ? 1.0 : -1.0;
        var cx = mx + nx * sign * h;
        var cy = my + ny * sign * h;
        return (cx, cy, r, Math.Atan2(s.A.Y - cy, s.A.X - cx), theta);
    }

    /// <summary>Điểm 3D tại tham số t (cung: theo góc quét; Z nội suy tuyến tính).</summary>
    public static (double X, double Y, double Z) PointAt(in Seg s, double t)
    {
        var z = s.A.Z + t * (s.B.Z - s.A.Z);
        if (IsStraight(s))
            return (s.A.X + t * (s.B.X - s.A.X), s.A.Y + t * (s.B.Y - s.A.Y), z);

        var arc = ArcOf(s);
        var ang = arc.A0 + arc.Theta * t;
        return (arc.Cx + arc.R * Math.Cos(ang), arc.Cy + arc.R * Math.Sin(ang), z);
    }

    /// <summary>Bulge của cung con [t0,t1].</summary>
    public static double SubBulge(in Seg s, double t0, double t1)
    {
        if (IsStraight(s))
            return 0;
        return Math.Tan(4 * Math.Atan(s.Bulge) * (t1 - t0) / 4);
    }

    /// <summary>Các khoảng tham số nằm trong biên (đã gộp khoảng liền kề).</summary>
    public static List<(double T0, double T1)> Intervals(ClipPolygon poly, in Seg s)
    {
        var result = new List<(double, double)>();
        var ts = new List<double> { 0, 1 };

        if (IsStraight(s))
        {
            double ax = s.A.X, ay = s.A.Y, bx = s.B.X, by = s.B.Y;
            if (!poly.BboxOverlaps(Math.Min(ax, bx), Math.Min(ay, by), Math.Max(ax, bx), Math.Max(ay, by)))
                return result;

            var dx = bx - ax;
            var dy = by - ay;
            if (dx * dx + dy * dy < 1e-18)
            {
                if (poly.Contains(ax, ay))
                    result.Add((0, 1));
                return result;
            }

            poly.SegmentParams(ax, ay, bx, by, ts);
        }
        else
        {
            var arc = ArcOf(s);
            if (!poly.BboxOverlaps(arc.Cx - arc.R, arc.Cy - arc.R, arc.Cx + arc.R, arc.Cy + arc.R))
                return result;

            poly.ArcParams(arc.Cx, arc.Cy, arc.R, arc.A0, arc.Theta, ts);
        }

        ts.Sort();
        for (var i = 0; i < ts.Count - 1; i++)
        {
            double t0 = ts[i], t1 = ts[i + 1];
            if (t1 - t0 < TEps)
                continue;

            var m = PointAt(s, (t0 + t1) / 2);
            if (!poly.Contains(m.X, m.Y))
                continue;

            if (result.Count > 0 && t0 - result[^1].Item2 <= MergeEps)
                result[^1] = (result[^1].Item1, t1);
            else
                result.Add((t0, t1));
        }

        return result;
    }

    /// <summary>
    /// Cắt một đường (chuỗi phân đoạn) theo biên: giữ phần trong, nội suy Z, nối lại các phân đoạn liền nhau.
    /// </summary>
    public static CutResult CutPath(ClipPolygon poly, IReadOnlyList<Seg> segs, bool closed)
    {
        var chains = new List<List<ChainPt>>();
        List<ChainPt>? cur = null;
        var allFull = segs.Count > 0;
        var firstStartsAtZero = false;

        for (var i = 0; i < segs.Count; i++)
        {
            var seg = segs[i];
            var ivs = Intervals(poly, seg);
            if (ivs.Count == 0)
            {
                allFull = false;
                cur = null;
                continue;
            }

            if (!(ivs.Count == 1 && ivs[0].T0 <= TEps && ivs[0].T1 >= 1 - TEps))
                allFull = false;

            foreach (var (t0, t1) in ivs)
            {
                var p0 = PointAt(seg, t0);
                var p1 = PointAt(seg, t1);
                var sw = seg.StartWidth + t0 * (seg.EndWidth - seg.StartWidth);
                var ew = seg.StartWidth + t1 * (seg.EndWidth - seg.StartWidth);

                var continues = cur is not null && t0 <= TEps;
                if (!continues)
                {
                    cur = [new ChainPt { X = p0.X, Y = p0.Y, Z = p0.Z }];
                    chains.Add(cur);
                    if (chains.Count == 1 && i == 0 && t0 <= TEps)
                        firstStartsAtZero = true;
                }

                var last = cur![^1];
                last.Bulge = SubBulge(seg, t0, t1);
                last.StartWidth = sw;
                last.EndWidth = ew;
                cur.Add(new ChainPt { X = p1.X, Y = p1.Y, Z = p1.Z });

                if (t1 < 1 - TEps)
                    cur = null;
            }
        }

        if (allFull)
            return new CutResult { Unchanged = true };

        // Đường kín: chuỗi cuối chạm đỉnh đóng + chuỗi đầu bắt đầu tại đỉnh đầu → nối lại thành một.
        if (closed && cur is not null && firstStartsAtZero && chains.Count > 1)
        {
            var first = chains[0];
            var lastChain = chains[^1];
            if (!ReferenceEquals(first, lastChain))
            {
                lastChain.RemoveAt(lastChain.Count - 1);
                lastChain.AddRange(first);
                chains.RemoveAt(0);
            }
        }

        chains.RemoveAll(c => c.Count < 2 || ChainLength(c) < 1e-6);
        return new CutResult { Chains = chains };
    }

    private static double ChainLength(List<ChainPt> c)
    {
        double len = 0;
        for (var i = 0; i < c.Count - 1; i++)
        {
            double dx = c[i + 1].X - c[i].X, dy = c[i + 1].Y - c[i].Y, dz = c[i + 1].Z - c[i].Z;
            len += Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        return len;
    }

    /// <summary>Lấy mẫu đường (thẳng/cung) thành danh sách điểm 2D — dùng khi dựng đa giác biên.</summary>
    public static List<Point2d> Tessellate(IReadOnlyList<Seg> segs, double maxAngleRad)
    {
        var pts = new List<Point2d>();
        foreach (var s in segs)
        {
            var steps = 1;
            if (!IsStraight(s))
                steps = Math.Max(2, (int)Math.Ceiling(Math.Abs(4 * Math.Atan(s.Bulge)) / maxAngleRad));

            for (var k = 0; k < steps; k++)
            {
                var p = PointAt(s, (double)k / steps);
                pts.Add(new Point2d(p.X, p.Y));
            }
        }

        return pts;
    }
}
