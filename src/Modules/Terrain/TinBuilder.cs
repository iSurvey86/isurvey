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
        IReadOnlyList<IReadOnlyList<TinVertex>>? boundaries,
        out int unforced,
        Action<int, int>? progress = null)
    {
        unforced = 0;
        if (source.Count == 0)
            return new List<TinTriangle>();
        var originX = source[0].X;
        var originY = source[0].Y;
        foreach (var point in source)
        {
            if (point.X < originX)
                originX = point.X;
            if (point.Y < originY)
                originY = point.Y;
        }

        TinVertex Shift(TinVertex point) => new(point.X - originX, point.Y - originY, point.Z);
        var shifted = new List<TinVertex>(source.Count);
        foreach (var point in source)
            shifted.Add(Shift(point));
        var real = Dedup(shifted);
        var edges = new List<(int A, int B)>();

        List<List<TinVertex>>? shiftedBoundaries = null;
        if (boundaries is { Count: > 0 })
        {
            shiftedBoundaries = new List<List<TinVertex>>();
            foreach (var ring in boundaries)
            {
                if (ring is null || ring.Count < 3)
                    continue;
                var shiftedRing = new List<TinVertex>(ring.Count);
                foreach (var point in ring)
                    shiftedRing.Add(Shift(point));
                if (NormalizeRing(shiftedRing) < 3)
                    continue;
                shiftedBoundaries.Add(shiftedRing);
            }

            if (shiftedBoundaries.Count == 0)
                shiftedBoundaries = null;
            else
            {
                var inside = new List<TinVertex>(real.Count);
                foreach (var point in real)
                {
                    if (InsideOrOnAny(shiftedBoundaries, point.X, point.Y))
                        inside.Add(point);
                }

                real = inside;
                foreach (var ring in shiftedBoundaries)
                    AddRing(real, edges, ring);
            }
        }

        if (constraints is not null)
        {
            foreach (var segment in constraints)
            {
                var a = Shift(segment.A);
                var b = Shift(segment.B);
                if (shiftedBoundaries is { Count: > 0 }
                    && (!InsideOrOnAny(shiftedBoundaries, a.X, a.Y) || !InsideOrOnAny(shiftedBoundaries, b.X, b.Y)))
                    continue;
                AddEdge(real, edges, a, b);
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
            new(midX + span * 20, midY - span * 20, 0),
            new(midX, midY + span * 20, 0)
        };
        vertices.AddRange(real);

        var mesh = new List<MeshTri> { new(0, 1, 2) };
        var total = vertices.Count - 3;
        var here = 0;
        var hint = 0;
        for (var i = 3; i < vertices.Count; i++)
        {
            Insert(vertices, mesh, i, ref hint);
            here++;
            if ((here & 63) == 0)
                progress?.Invoke(here, total);
        }

        progress?.Invoke(total, total);
        var triangles = new List<Tri>(mesh.Count);
        foreach (var tri in mesh)
        {
            if (!tri.Dead)
                triangles.Add(new Tri(tri.A, tri.B, tri.C));
        }

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
            // Chỉ giữ tam giác nằm trong bao: ba đỉnh + tâm + giữa ba cạnh.
            // Chỉ kiểm tra đỉnh vẫn để lọt tam giác “bắc cầu” chỗ lõm (chữ L).
            if (shiftedBoundaries is { Count: > 0 }
                && !TriangleInsideOrOn(shiftedBoundaries, a, b, c))
                continue;
            result.Add(new TinTriangle(
                new TinVertex(a.X + originX, a.Y + originY, a.Z),
                new TinVertex(b.X + originX, b.Y + originY, b.Z),
                new TinVertex(c.X + originX, c.Y + originY, c.Z)));
        }

        return result;
    }

    private static bool TriangleInsideOrOn(
        IReadOnlyList<IReadOnlyList<TinVertex>> rings,
        TinVertex a,
        TinVertex b,
        TinVertex c)
    {
        if (!InsideOrOnAny(rings, a.X, a.Y)
            || !InsideOrOnAny(rings, b.X, b.Y)
            || !InsideOrOnAny(rings, c.X, c.Y))
            return false;
        if (!InsideOrOnAny(rings, (a.X + b.X + c.X) / 3.0, (a.Y + b.Y + c.Y) / 3.0))
            return false;
        if (!InsideOrOnAny(rings, (a.X + b.X) / 2.0, (a.Y + b.Y) / 2.0))
            return false;
        if (!InsideOrOnAny(rings, (b.X + c.X) / 2.0, (b.Y + c.Y) / 2.0))
            return false;
        if (!InsideOrOnAny(rings, (c.X + a.X) / 2.0, (c.Y + a.Y) / 2.0))
            return false;
        return true;
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
                new TinVertex(10, 10, 0),
                new TinVertex(20, 20, 0),
                new TinVertex(2, 2, 1),
                new TinVertex(1, 3, 2)
            },
            null,
            new IReadOnlyList<TinVertex>[]
            {
                new[]
                {
                    new TinVertex(0, 0, 0),
                    new TinVertex(4, 0, 0),
                    new TinVertex(4, 4, 0),
                    new TinVertex(0, 4, 0)
                }
            },
            out _);
        if (!TryElevation(clipped, 2, 2, out _))
            return "Mất tam giác trong đường bao";
        if (TryElevation(clipped, 8, 8, out _))
            return "Còn tam giác ngoài đường bao";
        if (TryElevation(clipped, 15, 15, out _))
            return "Còn tam giác từ điểm ngoài bao";

        // Chữ L: ba đỉnh trong bao nhưng tâm tam giác bắc cầu nằm ngoài chỗ lõm.
        var elbow = Build(
            new[]
            {
                new TinVertex(0, 0, 0),
                new TinVertex(10, 0, 0),
                new TinVertex(10, 4, 0),
                new TinVertex(4, 4, 0),
                new TinVertex(4, 10, 0),
                new TinVertex(0, 10, 0),
                new TinVertex(1, 1, 1),
                new TinVertex(9, 1, 1),
                new TinVertex(9, 3, 1),
                new TinVertex(1, 9, 1),
                new TinVertex(3, 9, 1)
            },
            null,
            new IReadOnlyList<TinVertex>[]
            {
                new[]
                {
                    new TinVertex(0, 0, 0),
                    new TinVertex(10, 0, 0),
                    new TinVertex(10, 4, 0),
                    new TinVertex(4, 4, 0),
                    new TinVertex(4, 10, 0),
                    new TinVertex(0, 10, 0)
                }
            },
            out _);
        if (TryElevation(elbow, 7, 7, out _))
            return "Còn tam giác bắc cầu chỗ lõm đường bao";
        if (!TryElevation(elbow, 2, 2, out _) || !TryElevation(elbow, 8, 2, out _))
            return "Mất tam giác hợp lệ trong đường bao chữ L";
        return null;
    }

    private static void AddRing(List<TinVertex> vertices, List<(int A, int B)> edges, IReadOnlyList<TinVertex> ring)
    {
        var count = NormalizeRing(ring);
        if (count < 3)
            return;
        for (var i = 0; i < count; i++)
            AddEdge(vertices, edges, ring[i], ring[(i + 1) % count]);
    }

    private static int NormalizeRing(IReadOnlyList<TinVertex> ring)
    {
        if (ring.Count < 3)
            return 0;
        var count = ring.Count;
        if (Near(ring[0], ring[count - 1]))
            count--;
        return count < 3 ? 0 : count;
    }

    private static bool InsideOrOnAny(IReadOnlyList<IReadOnlyList<TinVertex>> rings, double x, double y)
    {
        foreach (var ring in rings)
        {
            if (ring.Count >= 3 && InsideOrOn(ring, x, y))
                return true;
        }

        return false;
    }

    private static bool InsideOrOn(IReadOnlyList<TinVertex> ring, double x, double y)
    {
        if (Inside(ring, x, y))
            return true;
        var count = NormalizeRing(ring);
        if (count < 3)
            return false;
        var point = new TinVertex(x, y, 0);
        for (var i = 0; i < count; i++)
        {
            var a = ring[i];
            var b = ring[(i + 1) % count];
            if (Near(point, a) || Near(point, b) || OnSegment(point, a, b))
                return true;
        }

        return false;
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

    private static void Insert(List<TinVertex> vertices, List<MeshTri> mesh, int index, ref int hint)
    {
        var point = vertices[index];
        var seed = Locate(vertices, mesh, hint, point);
        if (seed < 0)
            return;

        var bad = new List<int>();
        var mark = new HashSet<int>();
        var queue = new Queue<int>();
        mark.Add(seed);
        queue.Enqueue(seed);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            bad.Add(current);
            var tri = mesh[current];
            Consider(vertices, mesh, mark, queue, tri.Nab, point);
            Consider(vertices, mesh, mark, queue, tri.Nbc, point);
            Consider(vertices, mesh, mark, queue, tri.Nca, point);
        }

        var boundary = new List<(int U, int V, int Outside)>(bad.Count * 2);
        foreach (var id in bad)
        {
            var tri = mesh[id];
            Edge(mark, boundary, tri.A, tri.B, tri.Nab);
            Edge(mark, boundary, tri.B, tri.C, tri.Nbc);
            Edge(mark, boundary, tri.C, tri.A, tri.Nca);
            tri.Dead = true;
        }

        var byStart = new Dictionary<int, int>(boundary.Count);
        var byEnd = new Dictionary<int, int>(boundary.Count);
        foreach (var edge in boundary)
        {
            var tri = new MeshTri(edge.U, edge.V, index);
            var id = mesh.Count;
            mesh.Add(tri);
            byStart[tri.A] = id;
            byEnd[tri.B] = id;
            tri.Nab = edge.Outside;
            if (edge.Outside >= 0)
                PointNeighbor(mesh[edge.Outside], tri.B, tri.A, id);
            hint = id;
        }

        for (var id = mesh.Count - boundary.Count; id < mesh.Count; id++)
        {
            var tri = mesh[id];
            if (byStart.TryGetValue(tri.B, out var acrossBc))
                tri.Nbc = acrossBc;
            if (byEnd.TryGetValue(tri.A, out var acrossCa))
                tri.Nca = acrossCa;
        }
    }

    private static void Consider(
        List<TinVertex> vertices,
        List<MeshTri> mesh,
        HashSet<int> mark,
        Queue<int> queue,
        int neighbor,
        TinVertex point)
    {
        if (neighbor < 0 || mesh[neighbor].Dead || !mark.Add(neighbor))
            return;
        var tri = mesh[neighbor];
        if (InCircumcircle(vertices[tri.A], vertices[tri.B], vertices[tri.C], point))
            queue.Enqueue(neighbor);
        else
            mark.Remove(neighbor);
    }

    private static void Edge(HashSet<int> mark, List<(int U, int V, int Outside)> boundary, int u, int v, int neighbor)
    {
        if (neighbor >= 0 && mark.Contains(neighbor))
            return;
        boundary.Add((u, v, neighbor));
    }

    private static void PointNeighbor(MeshTri tri, int u, int v, int neighbor)
    {
        if (tri.A == u && tri.B == v)
            tri.Nab = neighbor;
        else if (tri.B == u && tri.C == v)
            tri.Nbc = neighbor;
        else if (tri.C == u && tri.A == v)
            tri.Nca = neighbor;
    }

    private static int Locate(List<TinVertex> vertices, List<MeshTri> mesh, int start, TinVertex point)
    {
        var current = start >= 0 && start < mesh.Count && !mesh[start].Dead ? start : mesh.Count - 1;
        var guard = mesh.Count + 8;
        for (var step = 0; step < guard; step++)
        {
            var tri = mesh[current];
            if (tri.Dead)
            {
                current = LastAlive(mesh);
                if (current < 0)
                    return -1;
                continue;
            }

            var ab = Orient(vertices[tri.A], vertices[tri.B], point);
            var bc = Orient(vertices[tri.B], vertices[tri.C], point);
            var ca = Orient(vertices[tri.C], vertices[tri.A], point);
            const double eps = 1e-8;
            if (ab >= -eps && bc >= -eps && ca >= -eps)
                return current;

            var next = ab <= bc && ab <= ca ? tri.Nab : bc <= ca ? tri.Nbc : tri.Nca;
            if (next < 0 || mesh[next].Dead)
                return Scan(vertices, mesh, point);
            current = next;
        }

        return Scan(vertices, mesh, point);
    }

    private static int Scan(List<TinVertex> vertices, List<MeshTri> mesh, TinVertex point)
    {
        const double eps = 1e-8;
        for (var i = mesh.Count - 1; i >= 0; i--)
        {
            var tri = mesh[i];
            if (tri.Dead)
                continue;
            if (Orient(vertices[tri.A], vertices[tri.B], point) >= -eps
                && Orient(vertices[tri.B], vertices[tri.C], point) >= -eps
                && Orient(vertices[tri.C], vertices[tri.A], point) >= -eps)
                return i;
        }

        return -1;
    }

    private static int LastAlive(List<MeshTri> mesh)
    {
        for (var i = mesh.Count - 1; i >= 0; i--)
        {
            if (!mesh[i].Dead)
                return i;
        }

        return -1;
    }

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
        var count = NormalizeRing(ring);
        if (count < 3)
            return false;
        var inside = false;
        for (int i = 0, j = count - 1; i < count; j = i++)
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
        const double cell = 1e-4;
        var grid = new Dictionary<(int X, int Y), List<int>>();
        var list = new List<TinVertex>(source.Count);
        foreach (var point in source)
        {
            var ix = (int)Math.Floor(point.X / cell);
            var iy = (int)Math.Floor(point.Y / cell);
            var same = false;
            for (var dx = -1; dx <= 1 && !same; dx++)
            {
                for (var dy = -1; dy <= 1 && !same; dy++)
                {
                    if (!grid.TryGetValue((ix + dx, iy + dy), out var bucket))
                        continue;
                    foreach (var kept in bucket)
                    {
                        if (!Near(list[kept], point))
                            continue;
                        same = true;
                        break;
                    }
                }
            }

            if (same)
                continue;
            if (!grid.TryGetValue((ix, iy), out var home))
            {
                home = new List<int>();
                grid[(ix, iy)] = home;
            }

            home.Add(list.Count);
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

    private sealed class MeshTri
    {
        public MeshTri(int a, int b, int c)
        {
            A = a;
            B = b;
            C = c;
        }

        public int A { get; set; }

        public int B { get; set; }

        public int C { get; set; }

        public int Nab { get; set; } = -1;

        public int Nbc { get; set; } = -1;

        public int Nca { get; set; } = -1;

        public bool Dead { get; set; }
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
