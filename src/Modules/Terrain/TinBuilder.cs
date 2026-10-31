namespace iSurvey.Modules.Terrain;

/// <summary>Một đỉnh lưới, tọa độ CAD.</summary>
public readonly struct TinVertex
{
    public TinVertex(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public double X { get; }

    public double Y { get; }

    public double Z { get; }
}

/// <summary>Một đoạn phải thành cạnh tam giác: đứt gãy hoặc đồng mức.</summary>
public readonly struct TinSegment
{
    public TinSegment(TinVertex a, TinVertex b)
    {
        A = a;
        B = b;
    }

    public TinVertex A { get; }

    public TinVertex B { get; }
}

/// <summary>Một tam giác của lưới.</summary>
public readonly struct TinTriangle
{
    public TinTriangle(TinVertex a, TinVertex b, TinVertex c)
    {
        A = a;
        B = b;
        C = c;
    }

    public TinVertex A { get; }

    public TinVertex B { get; }

    public TinVertex C { get; }
}

/// <summary>
/// Delaunay có ràng buộc. Đứt gãy, đồng mức và đường bao là cạnh bắt buộc.
/// Có đường bao thì chỉ giữ tam giác nằm trong bao. Không lọc cạnh dài.
/// </summary>
public static class TinBuilder
{
    public static List<TinTriangle> Build(
        IReadOnlyList<TinVertex> source,
        IReadOnlyList<TinSegment>? constraints,
        IReadOnlyList<TinVertex>? boundary,
        out int unforced)
    {
        unforced = 0;
        var real = Dedup(source);
        var edges = new List<(int A, int B)>();
        if (constraints is not null)
        {
            foreach (var segment in constraints)
                AddEdge(real, edges, segment.A, segment.B);
        }

        if (boundary is { Count: >= 3 })
        {
            var count = boundary.Count;
            if (Near(boundary[0], boundary[count - 1]))
                count--;
            if (count >= 3)
            {
                for (var i = 0; i < count; i++)
                    AddEdge(real, edges, boundary[i], boundary[(i + 1) % count]);
            }
        }

        if (real.Count < 3)
            return new List<TinTriangle>();

        Split(real, edges);

        var bounds = Bounds(real);
        var span = Math.Max(bounds.MaxX - bounds.MinX, bounds.MaxY - bounds.MinY);
        if (span < 1e-6)
            span = 1;
        var midX = (bounds.MinX + bounds.MaxX) / 2;
        var midY = (bounds.MinY + bounds.MaxY) / 2;
        var vertices = new List<TinVertex>(real.Count + 3)
        {
            new(midX - span * 20, midY - span * 20, 0),
            new(midX, midY + span * 20, 0),
            new(midX + span * 20, midY - span * 20, 0)
        };
        vertices.AddRange(real);

        var triangles = new List<Tri> { new(0, 1, 2) };
        for (var i = 3; i < vertices.Count; i++)
            Insert(vertices, triangles, i);

        foreach (var edge in edges)
        {
            if (!Force(triangles, vertices, edge.A + 3, edge.B + 3))
                unforced++;
        }

        var result = new List<TinTriangle>();
        foreach (var tri in triangles)
        {
            if (tri.A < 3 || tri.B < 3 || tri.C < 3)
                continue;
            var a = vertices[tri.A];
            var b = vertices[tri.B];
            var c = vertices[tri.C];
            if (Math.Abs(Orient(a, b, c)) < 1e-8)
                continue;
            if (boundary is { Count: >= 3 } && !Inside(boundary, (a.X + b.X + c.X) / 3, (a.Y + b.Y + c.Y) / 3))
                continue;
            result.Add(new TinTriangle(a, b, c));
        }

        return result;
    }

    public static bool TryElevation(IReadOnlyList<TinTriangle> triangles, double x, double y, out double z)
    {
        foreach (var tri in triangles)
        {
            if (!Barycentric(tri, x, y, out var wa, out var wb, out var wc))
                continue;
            z = wa * tri.A.Z + wb * tri.B.Z + wc * tri.C.Z;
            return true;
        }

        z = 0;
        return false;
    }

    /// <summary>Bốn điểm, một cạnh chéo bắt buộc, và một đường bao. Trả về null khi đúng.</summary>
    public static string? SelfCheck()
    {
        var plain = Build(
            new[]
            {
                new TinVertex(0, 0, 1),
                new TinVertex(10, 0, 2),
                new TinVertex(0, 10, 3),
                new TinVertex(10, 10, 4)
            },
            null,
            null,
            out _);
        if (plain.Count != 2)
            return "Số tam giác " + plain.Count;

        var forced = Build(
            new[]
            {
                new TinVertex(0, 0, 0),
                new TinVertex(10, 0, 0),
                new TinVertex(0, 10, 0),
                new TinVertex(10, 10, 10)
            },
            new[] { new TinSegment(new TinVertex(0, 0, 0), new TinVertex(10, 10, 10)) },
            null,
            out var unforced);
        if (unforced != 0)
            return "Không ép được cạnh";
        if (!TryElevation(forced, 5, 5, out var z) || Math.Abs(z - 5) > 0.05)
            return "Cao độ trên đứt gãy " + z;

        var clipped = Build(
            new[]
            {
                new TinVertex(0, 0, 0),
                new TinVertex(10, 0, 0),
                new TinVertex(0, 10, 0),
                new TinVertex(10, 10, 0)
            },
            null,
            new[]
            {
                new TinVertex(0, 0, 0),
                new TinVertex(4, 0, 0),
                new TinVertex(4, 4, 0),
                new TinVertex(0, 4, 0)
            },
            out _);
        if (!TryElevation(clipped, 2, 2, out _))
            return "Mất tam giác trong đường bao";
        if (TryElevation(clipped, 8, 8, out _))
            return "Còn tam giác ngoài đường bao";
        return null;
    }

    private static void AddEdge(List<TinVertex> vertices, List<(int A, int B)> edges, TinVertex a, TinVertex b)
    {
        var ia = FindOrAdd(vertices, a);
        var ib = FindOrAdd(vertices, b);
        if (ia != ib)
            edges.Add((ia, ib));
    }

    private static int FindOrAdd(List<TinVertex> vertices, TinVertex point)
    {
        for (var i = 0; i < vertices.Count; i++)
        {
            if (Near(vertices[i], point))
                return i;
        }

        vertices.Add(point);
        return vertices.Count - 1;
    }

    private static void Split(List<TinVertex> vertices, List<(int A, int B)> edges)
    {
        var guard = 0;
        while (guard++ < 4000)
        {
            if (SplitAtVertex(vertices, edges))
                continue;
            if (SplitAtCrossing(vertices, edges))
                continue;
            break;
        }
    }

    private static bool SplitAtVertex(List<TinVertex> vertices, List<(int A, int B)> edges)
    {
        for (var e = 0; e < edges.Count; e++)
        {
            var (a, b) = edges[e];
            for (var i = 0; i < vertices.Count; i++)
            {
                if (i == a || i == b)
                    continue;
                if (!OnSegment(vertices[i], vertices[a], vertices[b]))
                    continue;
                edges[e] = (a, i);
                edges.Add((i, b));
                return true;
            }
        }

        return false;
    }

    private static bool SplitAtCrossing(List<TinVertex> vertices, List<(int A, int B)> edges)
    {
        for (var i = 0; i < edges.Count; i++)
        {
            for (var j = i + 1; j < edges.Count; j++)
            {
                var (a, b) = edges[i];
                var (c, d) = edges[j];
                if (a == c || a == d || b == c || b == d)
                    continue;
                if (!ProperCross(vertices[a], vertices[b], vertices[c], vertices[d], out var t, out var u))
                    continue;
                var left = Lerp(vertices[a], vertices[b], t);
                var right = Lerp(vertices[c], vertices[d], u);
                var index = vertices.Count;
                vertices.Add(new TinVertex(left.X, left.Y, (left.Z + right.Z) / 2));
                edges[i] = (a, index);
                edges.Add((index, b));
                edges[j] = (c, index);
                edges.Add((index, d));
                return true;
            }
        }

        return false;
    }

    private static bool Force(List<Tri> triangles, List<TinVertex> vertices, int a, int b)
    {
        if (a == b)
            return true;
        var limit = triangles.Count * 4 + 8;
        for (var guard = 0; guard < limit; guard++)
        {
            if (HasEdge(triangles, a, b))
                return true;
            var flipped = false;
            for (var i = 0; i < triangles.Count && !flipped; i++)
            {
                if (!Crossing(triangles[i], vertices, a, b, out var u, out var w, out var p))
                    continue;
                var j = Neighbor(triangles, i, u, w, out var q);
                if (j < 0)
                    continue;
                if (!Opposite(vertices[u], vertices[w], vertices[p], vertices[q]))
                    continue;
                triangles[i] = Ccw(vertices, p, q, u);
                triangles[j] = Ccw(vertices, p, q, w);
                flipped = true;
            }

            if (!flipped)
                return false;
        }

        return HasEdge(triangles, a, b);
    }

    private static bool Crossing(Tri tri, List<TinVertex> vertices, int a, int b, out int u, out int w, out int p)
    {
        if (Cuts(vertices, a, b, tri.A, tri.B))
        {
            u = tri.A;
            w = tri.B;
            p = tri.C;
            return true;
        }

        if (Cuts(vertices, a, b, tri.B, tri.C))
        {
            u = tri.B;
            w = tri.C;
            p = tri.A;
            return true;
        }

        if (Cuts(vertices, a, b, tri.C, tri.A))
        {
            u = tri.C;
            w = tri.A;
            p = tri.B;
            return true;
        }

        u = 0;
        w = 0;
        p = 0;
        return false;
    }

    private static bool Cuts(List<TinVertex> vertices, int a, int b, int u, int w)
    {
        if (u == a || u == b || w == a || w == b)
            return false;
        return ProperCross(vertices[a], vertices[b], vertices[u], vertices[w], out _, out _);
    }

    private static int Neighbor(List<Tri> triangles, int skip, int u, int w, out int q)
    {
        for (var i = 0; i < triangles.Count; i++)
        {
            if (i == skip)
                continue;
            var tri = triangles[i];
            var hasU = tri.A == u || tri.B == u || tri.C == u;
            var hasW = tri.A == w || tri.B == w || tri.C == w;
            if (!hasU || !hasW)
                continue;
            q = tri.A != u && tri.A != w ? tri.A : tri.B != u && tri.B != w ? tri.B : tri.C;
            return i;
        }

        q = -1;
        return -1;
    }

    private static bool HasEdge(List<Tri> triangles, int a, int b)
    {
        foreach (var tri in triangles)
        {
            if ((tri.A == a && tri.B == b) || (tri.B == a && tri.C == b) || (tri.C == a && tri.A == b))
                return true;
            if ((tri.A == b && tri.B == a) || (tri.B == b && tri.C == a) || (tri.C == b && tri.A == a))
                return true;
        }

        return false;
    }

    private static bool Opposite(TinVertex u, TinVertex w, TinVertex p, TinVertex q)
    {
        var side = Orient(u, w, p) * Orient(u, w, q);
        var other = Orient(p, q, u) * Orient(p, q, w);
        return side < 0 && other < 0;
    }

    private static Tri Ccw(List<TinVertex> vertices, int a, int b, int c)
    {
        if (Orient(vertices[a], vertices[b], vertices[c]) < 0)
            return new Tri(a, c, b);
        return new Tri(a, b, c);
    }

    private static bool OnSegment(TinVertex point, TinVertex a, TinVertex b)
    {
        var abx = b.X - a.X;
        var aby = b.Y - a.Y;
        var len2 = abx * abx + aby * aby;
        if (len2 < 1e-12)
            return false;
        var apx = point.X - a.X;
        var apy = point.Y - a.Y;
        var dist = Math.Abs(abx * apy - aby * apx) / Math.Sqrt(len2);
        if (dist > 1e-4)
            return false;
        var t = (apx * abx + apy * aby) / len2;
        return t > 1e-6 && t < 1 - 1e-6;
    }

    private static bool ProperCross(
        TinVertex a,
        TinVertex b,
        TinVertex c,
        TinVertex d,
        out double t,
        out double u)
    {
        t = 0;
        u = 0;
        var rx = b.X - a.X;
        var ry = b.Y - a.Y;
        var sx = d.X - c.X;
        var sy = d.Y - c.Y;
        var lenA = Math.Sqrt(rx * rx + ry * ry);
        var lenB = Math.Sqrt(sx * sx + sy * sy);
        var den = rx * sy - ry * sx;
        if (lenA < 1e-9 || lenB < 1e-9 || Math.Abs(den) < 1e-12 * lenA * lenB)
            return false;
        var qpx = c.X - a.X;
        var qpy = c.Y - a.Y;
        t = (qpx * sy - qpy * sx) / den;
        u = (qpx * ry - qpy * rx) / den;
        const double eps = 1e-8;
        return t > eps && t < 1 - eps && u > eps && u < 1 - eps;
    }

    private static TinVertex Lerp(TinVertex a, TinVertex b, double t) =>
        new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t);

    private static bool Near(TinVertex a, TinVertex b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy <= 1e-8;
    }

    private static void Insert(List<TinVertex> vertices, List<Tri> triangles, int index)
    {
        var point = vertices[index];
        var bad = new List<int>();
        for (var i = 0; i < triangles.Count; i++)
        {
            var tri = triangles[i];
            if (InCircumcircle(vertices[tri.A], vertices[tri.B], vertices[tri.C], point))
                bad.Add(i);
        }

        var cavity = new List<(int U, int V)>();
        for (var i = 0; i < bad.Count; i++)
        {
            var tri = triangles[bad[i]];
            Consider(triangles, bad, cavity, tri.A, tri.B);
            Consider(triangles, bad, cavity, tri.B, tri.C);
            Consider(triangles, bad, cavity, tri.C, tri.A);
        }

        for (var i = bad.Count - 1; i >= 0; i--)
            triangles.RemoveAt(bad[i]);
        foreach (var edge in cavity)
            triangles.Add(new Tri(edge.U, edge.V, index));
    }

    private static void Consider(List<Tri> triangles, List<int> bad, List<(int U, int V)> cavity, int u, int v)
    {
        foreach (var index in bad)
        {
            if (HasDirected(triangles[index], v, u))
                return;
        }

        cavity.Add((u, v));
    }

    private static bool HasDirected(Tri tri, int u, int v) =>
        (tri.A == u && tri.B == v) || (tri.B == u && tri.C == v) || (tri.C == u && tri.A == v);

    private static bool InCircumcircle(TinVertex a, TinVertex b, TinVertex c, TinVertex p)
    {
        if (Orient(a, b, c) < 0)
            (b, c) = (c, b);
        var ax = a.X - p.X;
        var ay = a.Y - p.Y;
        var bx = b.X - p.X;
        var by = b.Y - p.Y;
        var cx = c.X - p.X;
        var cy = c.Y - p.Y;
        var az = ax * ax + ay * ay;
        var bz = bx * bx + by * by;
        var cz = cx * cx + cy * cy;
        var det = ax * (by * cz - cy * bz) - ay * (bx * cz - cx * bz) + az * (bx * cy - cx * by);
        return det > 1e-9;
    }

    private static double Orient(TinVertex a, TinVertex b, TinVertex c) =>
        (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

    private static bool Barycentric(TinTriangle tri, double x, double y, out double wa, out double wb, out double wc)
    {
        var area = Orient(tri.A, tri.B, tri.C);
        wa = 0;
        wb = 0;
        wc = 0;
        if (Math.Abs(area) < 1e-12)
            return false;
        var p = new TinVertex(x, y, 0);
        wb = Orient(tri.A, p, tri.C) / area;
        wc = Orient(tri.A, tri.B, p) / area;
        wa = 1 - wb - wc;
        const double slack = -1e-8;
        return wa >= slack && wb >= slack && wc >= slack;
    }

    private static bool Inside(IReadOnlyList<TinVertex> ring, double x, double y)
    {
        var inside = false;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            var yi = ring[i].Y;
            var yj = ring[j].Y;
            var xi = ring[i].X;
            var xj = ring[j].X;
            if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi + 0.0) + xi)
                inside = !inside;
        }

        return inside;
    }

    private static List<TinVertex> Dedup(IReadOnlyList<TinVertex> source)
    {
        var list = new List<TinVertex>();
        foreach (var point in source)
        {
            var same = false;
            foreach (var kept in list)
            {
                if (Near(kept, point))
                {
                    same = true;
                    break;
                }
            }

            if (!same)
                list.Add(point);
        }

        return list;
    }

    private static (double MinX, double MinY, double MaxX, double MaxY) Bounds(List<TinVertex> points)
    {
        var minX = points[0].X;
        var minY = points[0].Y;
        var maxX = minX;
        var maxY = minY;
        foreach (var point in points)
        {
            if (point.X < minX) minX = point.X;
            if (point.Y < minY) minY = point.Y;
            if (point.X > maxX) maxX = point.X;
            if (point.Y > maxY) maxY = point.Y;
        }

        return (minX, minY, maxX, maxY);
    }

    private readonly struct Tri
    {
        public Tri(int a, int b, int c)
        {
            A = a;
            B = b;
            C = c;
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }
    }
}
