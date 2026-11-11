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
        Action<int, int, string>? progress = null)
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
        progress?.Invoke(0, 0, "Đang chuẩn bị điểm…");
        var shifted = new List<TinVertex>(source.Count);
        foreach (var point in source)
            shifted.Add(Shift(point));
        var real = Dedup(shifted);
        var edges = new List<(int A, int B)>();
        var index = new PointIndex(real);

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
                index = new PointIndex(real);
                foreach (var ring in shiftedBoundaries)
                    AddRing(index, edges, ring);
            }
        }

        if (constraints is not null)
        {
            var constraintTotal = constraints.Count;
            for (var c = 0; c < constraintTotal; c++)
            {
                var segment = constraints[c];
                var a = Shift(segment.A);
                var b = Shift(segment.B);
                if (shiftedBoundaries is { Count: > 0 }
                    && (!InsideOrOnAny(shiftedBoundaries, a.X, a.Y) || !InsideOrOnAny(shiftedBoundaries, b.X, b.Y)))
                    continue;
                AddEdge(index, edges, a, b);
                if ((c & 4095) == 0)
                    progress?.Invoke(
                        c,
                        constraintTotal,
                        "Đang gắn đường đứt gãy / đồng mức " + c.ToString("N0") + " / " + constraintTotal.ToString("N0"));
            }
        }

        if (real.Count < 3)
            return new List<TinTriangle>();

        progress?.Invoke(0, 0, "Đang xử lý đường ràng buộc (" + edges.Count.ToString("N0") + " đoạn)…");
        Split(real, edges, progress);

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
                progress?.Invoke(here, total, "Đang dựng lưới TIN " + here.ToString("N0") + " / " + total.ToString("N0") + " điểm");
        }

        progress?.Invoke(total, total, "Đang dựng lưới TIN " + total.ToString("N0") + " / " + total.ToString("N0") + " điểm");

        // Khớp đường ràng buộc trên MeshTri (đã có Nab/Nbc/Nca) — tránh Dictionary adj mỗi lần lật.
        // Cùng CDT; chỉ đổi cách lưu liên kết.
        var live = CompactMesh(mesh);
        var edgeKeys = BuildEdgeKeysMesh(live);
        var tip = BuildTipMesh(live, vertices.Count);
        var edgeTotal = edges.Count;
        for (var e = 0; e < edgeTotal; e++)
        {
            if ((e & 255) == 0)
                progress?.Invoke(
                    e,
                    edgeTotal,
                    "Đang khớp đường đứt gãy / đồng mức vào lưới " + e.ToString("N0") + " / " + edgeTotal.ToString("N0"));
            var edge = edges[e];
            var a = edge.A + 3;
            var b = edge.B + 3;
            if (a == b || edgeKeys.Contains(EdgeKey(a, b)))
                continue;
            if (!ForceMesh(live, vertices, edgeKeys, tip, a, b))
                unforced++;
        }

        if (edgeTotal > 0)
            progress?.Invoke(
                edgeTotal,
                edgeTotal,
                "Đang khớp đường đứt gãy / đồng mức vào lưới " + edgeTotal.ToString("N0") + " / " + edgeTotal.ToString("N0"));

        var result = new List<TinTriangle>(live.Count);
        foreach (var tri in live)
        {
            if (tri.A < 3 || tri.B < 3 || tri.C < 3)
                continue;
            var a = vertices[tri.A];
            var b = vertices[tri.B];
            var c = vertices[tri.C];
            if (Math.Abs(Orient(a, b, c)) < 1e-8)
                continue;
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

    private static List<MeshTri> CompactMesh(List<MeshTri> mesh)
    {
        var map = new int[mesh.Count];
        for (var i = 0; i < map.Length; i++)
            map[i] = -1;
        var live = new List<MeshTri>(mesh.Count);
        for (var i = 0; i < mesh.Count; i++)
        {
            if (mesh[i].Dead)
                continue;
            map[i] = live.Count;
            live.Add(mesh[i]);
        }

        foreach (var tri in live)
        {
            if (tri.Nab >= 0)
                tri.Nab = map[tri.Nab];
            if (tri.Nbc >= 0)
                tri.Nbc = map[tri.Nbc];
            if (tri.Nca >= 0)
                tri.Nca = map[tri.Nca];
        }

        return live;
    }

    private static HashSet<long> BuildEdgeKeysMesh(List<MeshTri> mesh)
    {
        var keys = new HashSet<long>(mesh.Count * 3);
        foreach (var tri in mesh)
        {
            keys.Add(EdgeKey(tri.A, tri.B));
            keys.Add(EdgeKey(tri.B, tri.C));
            keys.Add(EdgeKey(tri.C, tri.A));
        }

        return keys;
    }

    private static int[] BuildTipMesh(List<MeshTri> mesh, int vertexCount)
    {
        var tip = new int[vertexCount];
        for (var i = 0; i < tip.Length; i++)
            tip[i] = -1;
        for (var i = 0; i < mesh.Count; i++)
        {
            var tri = mesh[i];
            tip[tri.A] = i;
            tip[tri.B] = i;
            tip[tri.C] = i;
        }

        return tip;
    }

    private static bool ForceMesh(
        List<MeshTri> mesh,
        List<TinVertex> vertices,
        HashSet<long> edgeKeys,
        int[] tip,
        int a,
        int b)
    {
        var ab = EdgeKey(a, b);
        if (edgeKeys.Contains(ab))
            return true;
        var limit = mesh.Count * 4 + 8;
        var resume = -1;
        for (var guard = 0; guard < limit; guard++)
        {
            if (edgeKeys.Contains(ab))
                return true;
            if (!FindCrossingWalkMesh(mesh, vertices, tip, a, b, resume, out var i, out var u, out var w, out var p))
                return false;
            var j = NeighborAcross(mesh, i, u, w);
            if (j < 0)
                return false;
            var q = OtherVertexMesh(mesh[j], u, w);
            if (!Opposite(vertices[u], vertices[w], vertices[p], vertices[q]))
                return false;

            FlipMesh(mesh, vertices, edgeKeys, tip, i, j, u, w, p, q);
            if (edgeKeys.Contains(ab))
                return true;

            if (CutsMesh(vertices, a, b, mesh[i]))
                resume = i;
            else if (CutsMesh(vertices, a, b, mesh[j]))
                resume = j;
            else
                resume = -1;
        }

        return edgeKeys.Contains(ab);
    }

    private static bool CutsMesh(List<TinVertex> vertices, int a, int b, MeshTri tri) =>
        Cuts(vertices, a, b, tri.A, tri.B)
        || Cuts(vertices, a, b, tri.B, tri.C)
        || Cuts(vertices, a, b, tri.C, tri.A);

    private static void FlipMesh(
        List<MeshTri> mesh,
        List<TinVertex> vertices,
        HashSet<long> edgeKeys,
        int[] tip,
        int i,
        int j,
        int u,
        int w,
        int p,
        int q)
    {
        var ti = mesh[i];
        var tj = mesh[j];
        var nPu = NeighborAcross(mesh, i, p, u);
        var nWp = NeighborAcross(mesh, i, w, p);
        var nQu = NeighborAcross(mesh, j, q, u);
        var nWq = NeighborAcross(mesh, j, w, q);

        edgeKeys.Remove(EdgeKey(u, w));

        WriteCcw(ti, vertices, p, q, u);
        WriteCcw(tj, vertices, p, q, w);
        WireTriangle(ti, p, q, u, j, nQu, nPu);
        WireTriangle(tj, p, q, w, i, nWq, nWp);

        if (nPu >= 0)
            PointNeighbor(mesh[nPu], u, p, i);
        if (nQu >= 0)
            PointNeighbor(mesh[nQu], q, u, i);
        if (nWp >= 0)
            PointNeighbor(mesh[nWp], p, w, j);
        if (nWq >= 0)
            PointNeighbor(mesh[nWq], w, q, j);

        edgeKeys.Add(EdgeKey(p, q));
        tip[p] = i;
        tip[q] = i;
        tip[u] = i;
        tip[w] = j;
    }

    private static void WriteCcw(MeshTri tri, List<TinVertex> vertices, int a, int b, int c)
    {
        if (Orient(vertices[a], vertices[b], vertices[c]) < 0)
            (b, c) = (c, b);
        tri.A = a;
        tri.B = b;
        tri.C = c;
    }

    /// <summary>Gán neighbor cho tam giác CCW (a,b,c): cạnh ab→nAb, bc→nBc, ca→nCa.</summary>
    private static void WireTriangle(MeshTri tri, int a, int b, int c, int nAb, int nBc, int nCa)
    {
        // tri đã là CCW của {a,b,c} nhưng thứ tự đỉnh có thể xoay.
        tri.Nab = -1;
        tri.Nbc = -1;
        tri.Nca = -1;
        AssignOrientedNeighbor(tri, a, b, nAb);
        AssignOrientedNeighbor(tri, b, c, nBc);
        AssignOrientedNeighbor(tri, c, a, nCa);
    }

    private static void AssignOrientedNeighbor(MeshTri tri, int u, int v, int neighbor)
    {
        if (tri.A == u && tri.B == v)
            tri.Nab = neighbor;
        else if (tri.B == u && tri.C == v)
            tri.Nbc = neighbor;
        else if (tri.C == u && tri.A == v)
            tri.Nca = neighbor;
        else if (tri.A == v && tri.B == u)
            tri.Nab = neighbor;
        else if (tri.B == v && tri.C == u)
            tri.Nbc = neighbor;
        else if (tri.C == v && tri.A == u)
            tri.Nca = neighbor;
    }

    private static bool IsSameEdge(int a, int b, int c, int d) =>
        (a == c && b == d) || (a == d && b == c);

    private static int NeighborAcross(List<MeshTri> mesh, int triIndex, int u, int w)
    {
        var tri = mesh[triIndex];
        if (IsSameEdge(tri.A, tri.B, u, w))
            return tri.Nab;
        if (IsSameEdge(tri.B, tri.C, u, w))
            return tri.Nbc;
        if (IsSameEdge(tri.C, tri.A, u, w))
            return tri.Nca;
        return -1;
    }

    private static int OtherVertexMesh(MeshTri tri, int u, int w)
    {
        if (tri.A != u && tri.A != w)
            return tri.A;
        if (tri.B != u && tri.B != w)
            return tri.B;
        return tri.C;
    }

    private static bool FindCrossingWalkMesh(
        List<MeshTri> mesh,
        List<TinVertex> vertices,
        int[] tip,
        int a,
        int b,
        int resume,
        out int triIndex,
        out int u,
        out int w,
        out int p)
    {
        triIndex = -1;
        u = 0;
        w = 0;
        p = 0;

        if (resume >= 0 && resume < mesh.Count)
        {
            if (TryEdgeCutMesh(mesh[resume], vertices, a, b, -1, -1, out u, out w, out p))
            {
                triIndex = resume;
                return true;
            }

            if (ContinueCrossingWalkMesh(mesh, vertices, a, b, resume, -1, -1, out triIndex, out u, out w, out p))
                return true;
        }

        var start = tip[a];
        if (start < 0 || start >= mesh.Count)
            return false;

        const double eps = 1e-8;
        var current = start;
        var guard = 64;
        for (var step = 0; step < guard; step++)
        {
            var tri = mesh[current];
            if (!OtherTwoMesh(tri, a, out var v1, out var v2))
                return false;
            if (v1 == b || v2 == b)
                return false;
            if (Orient(vertices[a], vertices[v1], vertices[v2]) < 0)
                (v1, v2) = (v2, v1);

            if (Orient(vertices[a], vertices[v1], vertices[b]) >= -eps
                && Orient(vertices[a], vertices[b], vertices[v2]) >= -eps)
            {
                if (Cuts(vertices, a, b, v1, v2))
                {
                    triIndex = current;
                    u = v1;
                    w = v2;
                    p = a;
                    return true;
                }

                if (Orient(vertices[v1], vertices[v2], vertices[b]) >= -eps)
                    return false;

                var next = NeighborAcross(mesh, current, v1, v2);
                if (next < 0)
                    return false;
                return ContinueCrossingWalkMesh(
                    mesh, vertices, a, b, next, v1, v2, out triIndex, out u, out w, out p);
            }

            // Xoay quanh a sang tam giác kế.
            var across = NeighborAcross(mesh, current, a, v2);
            if (across < 0)
                across = NeighborAcross(mesh, current, a, v1);
            if (across < 0 || across == start && step > 0)
                return false;
            current = across;
            if (current == start)
                return false;
        }

        return false;
    }

    private static bool ContinueCrossingWalkMesh(
        List<MeshTri> mesh,
        List<TinVertex> vertices,
        int a,
        int b,
        int start,
        int avoidU,
        int avoidW,
        out int triIndex,
        out int u,
        out int w,
        out int p)
    {
        triIndex = -1;
        u = 0;
        w = 0;
        p = 0;
        var current = start;
        var guard = mesh.Count + 8;
        for (var step = 0; step < guard; step++)
        {
            var tri = mesh[current];
            if (tri.A == b || tri.B == b || tri.C == b)
                return false;
            if (TryEdgeCutMesh(tri, vertices, a, b, avoidU, avoidW, out u, out w, out p))
            {
                triIndex = current;
                return true;
            }

            if (!PickExitTowardMesh(tri, vertices, b, avoidU, avoidW, out var nextU, out var nextW))
                return false;
            var nxt = NeighborAcross(mesh, current, nextU, nextW);
            if (nxt < 0)
                return false;
            avoidU = nextU;
            avoidW = nextW;
            current = nxt;
        }

        return false;
    }

    private static bool TryEdgeCutMesh(
        MeshTri tri,
        List<TinVertex> vertices,
        int a,
        int b,
        int avoidU,
        int avoidW,
        out int u,
        out int w,
        out int p)
    {
        if (!IsAvoid(tri.A, tri.B, avoidU, avoidW) && Cuts(vertices, a, b, tri.A, tri.B))
        {
            u = tri.A;
            w = tri.B;
            p = tri.C;
            return true;
        }

        if (!IsAvoid(tri.B, tri.C, avoidU, avoidW) && Cuts(vertices, a, b, tri.B, tri.C))
        {
            u = tri.B;
            w = tri.C;
            p = tri.A;
            return true;
        }

        if (!IsAvoid(tri.C, tri.A, avoidU, avoidW) && Cuts(vertices, a, b, tri.C, tri.A))
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

    private static bool PickExitTowardMesh(
        MeshTri tri,
        List<TinVertex> vertices,
        int b,
        int avoidU,
        int avoidW,
        out int exitU,
        out int exitW)
    {
        exitU = 0;
        exitW = 0;
        var best = -1;
        var bestScore = double.PositiveInfinity;
        var target = vertices[b];
        Span<(int U, int V)> edges = stackalloc (int U, int V)[3];
        edges[0] = (tri.A, tri.B);
        edges[1] = (tri.B, tri.C);
        edges[2] = (tri.C, tri.A);
        for (var i = 0; i < 3; i++)
        {
            var (eu, ev) = edges[i];
            if (IsAvoid(eu, ev, avoidU, avoidW))
                continue;
            var midX = (vertices[eu].X + vertices[ev].X) * 0.5;
            var midY = (vertices[eu].Y + vertices[ev].Y) * 0.5;
            var dx = midX - target.X;
            var dy = midY - target.Y;
            var score = dx * dx + dy * dy;
            if (score >= bestScore)
                continue;
            bestScore = score;
            best = i;
        }

        if (best < 0)
            return false;
        exitU = edges[best].U;
        exitW = edges[best].V;
        return true;
    }

    private static bool OtherTwoMesh(MeshTri tri, int vertex, out int v1, out int v2)
    {
        if (tri.A == vertex)
        {
            v1 = tri.B;
            v2 = tri.C;
            return true;
        }

        if (tri.B == vertex)
        {
            v1 = tri.C;
            v2 = tri.A;
            return true;
        }

        if (tri.C == vertex)
        {
            v1 = tri.A;
            v2 = tri.B;
            return true;
        }

        v1 = 0;
        v2 = 0;
        return false;
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

    private static void AddRing(PointIndex index, List<(int A, int B)> edges, IReadOnlyList<TinVertex> ring)
    {
        var count = NormalizeRing(ring);
        if (count < 3)
            return;
        for (var i = 0; i < count; i++)
            AddEdge(index, edges, ring[i], ring[(i + 1) % count]);
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

    private static void AddEdge(PointIndex index, List<(int A, int B)> edges, TinVertex a, TinVertex b)
    {
        var ia = index.FindOrAdd(a);
        var ib = index.FindOrAdd(b);
        if (ia != ib)
            edges.Add((ia, ib));
    }

    /// <summary>Lưới không gian cho FindOrAdd — tránh quét tuyến tính khi gắn hàng chục nghìn cạnh đồng mức.</summary>
    private sealed class PointIndex
    {
        private const double Cell = 1e-4;
        private readonly List<TinVertex> _vertices;
        private readonly Dictionary<(int X, int Y), List<int>> _grid = new();

        public PointIndex(List<TinVertex> vertices)
        {
            _vertices = vertices;
            for (var i = 0; i < vertices.Count; i++)
                AddToGrid(i);
        }

        public int FindOrAdd(TinVertex point)
        {
            var ix = (int)Math.Floor(point.X / Cell);
            var iy = (int)Math.Floor(point.Y / Cell);
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (!_grid.TryGetValue((ix + dx, iy + dy), out var bucket))
                        continue;
                    foreach (var i in bucket)
                    {
                        if (Near(_vertices[i], point))
                            return i;
                    }
                }
            }

            var index = _vertices.Count;
            _vertices.Add(point);
            AddToGrid(index);
            return index;
        }

        private void AddToGrid(int index)
        {
            var point = _vertices[index];
            var key = ((int)Math.Floor(point.X / Cell), (int)Math.Floor(point.Y / Cell));
            if (!_grid.TryGetValue(key, out var bucket))
            {
                bucket = new List<int>(4);
                _grid[key] = bucket;
            }

            bucket.Add(index);
        }
    }

    private static void Split(
        List<TinVertex> vertices,
        List<(int A, int B)> edges,
        Action<int, int, string>? progress)
    {
        var guard = 0;
        while (guard++ < 4000)
        {
            if ((guard & 15) == 1)
                progress?.Invoke(
                    guard,
                    0,
                    "Đang xử lý đường ràng buộc (lượt " + guard + ", " + edges.Count.ToString("N0") + " đoạn)…");
            if (SplitAtVertex(vertices, edges))
                continue;
            if (SplitAtCrossing(vertices, edges))
                continue;
            break;
        }
    }

    private static double GridCellSize(List<TinVertex> vertices, List<(int A, int B)> edges)
    {
        if (edges.Count == 0)
            return 1;
        var sum = 0.0;
        var n = 0;
        var step = Math.Max(1, edges.Count / 256);
        for (var i = 0; i < edges.Count; i += step)
        {
            var (a, b) = edges[i];
            var dx = vertices[a].X - vertices[b].X;
            var dy = vertices[a].Y - vertices[b].Y;
            sum += Math.Sqrt(dx * dx + dy * dy);
            n++;
        }

        var avg = sum / Math.Max(1, n);
        if (avg < 1e-3)
            avg = 1e-3;
        return avg * 2;
    }

    private static void AddEdgeToGrid(
        Dictionary<(int X, int Y), List<int>> grid,
        double cell,
        List<TinVertex> vertices,
        int edgeIndex,
        int a,
        int b)
    {
        var va = vertices[a];
        var vb = vertices[b];
        var minX = Math.Min(va.X, vb.X);
        var maxX = Math.Max(va.X, vb.X);
        var minY = Math.Min(va.Y, vb.Y);
        var maxY = Math.Max(va.Y, vb.Y);
        var x0 = (int)Math.Floor(minX / cell);
        var x1 = (int)Math.Floor(maxX / cell);
        var y0 = (int)Math.Floor(minY / cell);
        var y1 = (int)Math.Floor(maxY / cell);
        for (var x = x0; x <= x1; x++)
        {
            for (var y = y0; y <= y1; y++)
            {
                var key = (x, y);
                if (!grid.TryGetValue(key, out var bucket))
                {
                    bucket = new List<int>(4);
                    grid[key] = bucket;
                }

                bucket.Add(edgeIndex);
            }
        }
    }

    private static bool SplitAtVertex(List<TinVertex> vertices, List<(int A, int B)> edges)
    {
        if (edges.Count == 0 || vertices.Count == 0)
            return false;
        var cell = GridCellSize(vertices, edges);
        var grid = new Dictionary<(int X, int Y), List<int>>();
        for (var i = 0; i < vertices.Count; i++)
        {
            var p = vertices[i];
            var key = ((int)Math.Floor(p.X / cell), (int)Math.Floor(p.Y / cell));
            if (!grid.TryGetValue(key, out var bucket))
            {
                bucket = new List<int>(4);
                grid[key] = bucket;
            }

            bucket.Add(i);
        }

        for (var e = 0; e < edges.Count; e++)
        {
            var (a, b) = edges[e];
            var va = vertices[a];
            var vb = vertices[b];
            var minX = Math.Min(va.X, vb.X);
            var maxX = Math.Max(va.X, vb.X);
            var minY = Math.Min(va.Y, vb.Y);
            var maxY = Math.Max(va.Y, vb.Y);
            var x0 = (int)Math.Floor(minX / cell) - 1;
            var x1 = (int)Math.Floor(maxX / cell) + 1;
            var y0 = (int)Math.Floor(minY / cell) - 1;
            var y1 = (int)Math.Floor(maxY / cell) + 1;
            for (var x = x0; x <= x1; x++)
            {
                for (var y = y0; y <= y1; y++)
                {
                    if (!grid.TryGetValue((x, y), out var bucket))
                        continue;
                    foreach (var i in bucket)
                    {
                        if (i == a || i == b)
                            continue;
                        if (!OnSegment(vertices[i], va, vb))
                            continue;
                        edges[e] = (a, i);
                        edges.Add((i, b));
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool SplitAtCrossing(List<TinVertex> vertices, List<(int A, int B)> edges)
    {
        if (edges.Count < 2)
            return false;
        var cell = GridCellSize(vertices, edges);
        var grid = new Dictionary<(int X, int Y), List<int>>();
        for (var i = 0; i < edges.Count; i++)
        {
            var (a, b) = edges[i];
            AddEdgeToGrid(grid, cell, vertices, i, a, b);
        }

        foreach (var bucket in grid.Values)
        {
            for (var i = 0; i < bucket.Count; i++)
            {
                for (var j = i + 1; j < bucket.Count; j++)
                {
                    var ei = bucket[i];
                    var ej = bucket[j];
                    if (ei == ej)
                        continue;
                    if (ei > ej)
                        (ei, ej) = (ej, ei);
                    var (a, b) = edges[ei];
                    var (c, d) = edges[ej];
                    if (a == c || a == d || b == c || b == d)
                        continue;
                    if (!ProperCross(vertices[a], vertices[b], vertices[c], vertices[d], out var t, out var u))
                        continue;
                    var left = Lerp(vertices[a], vertices[b], t);
                    var right = Lerp(vertices[c], vertices[d], u);
                    var index = vertices.Count;
                    vertices.Add(new TinVertex(left.X, left.Y, (left.Z + right.Z) / 2));
                    edges[ei] = (a, index);
                    edges.Add((index, b));
                    edges[ej] = (c, index);
                    edges.Add((index, d));
                    return true;
                }
            }
        }

        return false;
    }

    private static HashSet<long> BuildEdgeKeys(List<Tri> triangles)
    {
        var keys = new HashSet<long>(triangles.Count * 3);
        foreach (var tri in triangles)
        {
            keys.Add(EdgeKey(tri.A, tri.B));
            keys.Add(EdgeKey(tri.B, tri.C));
            keys.Add(EdgeKey(tri.C, tri.A));
        }

        return keys;
    }

    private static Dictionary<long, (int T0, int T1)> BuildAdjacency(List<Tri> triangles)
    {
        var adj = new Dictionary<long, (int T0, int T1)>(triangles.Count * 3);
        for (var i = 0; i < triangles.Count; i++)
        {
            LinkEdge(adj, triangles[i].A, triangles[i].B, i);
            LinkEdge(adj, triangles[i].B, triangles[i].C, i);
            LinkEdge(adj, triangles[i].C, triangles[i].A, i);
        }

        return adj;
    }

    private static void LinkEdge(Dictionary<long, (int T0, int T1)> adj, int u, int v, int tri)
    {
        var key = EdgeKey(u, v);
        if (!adj.TryGetValue(key, out var pair))
            adj[key] = (tri, -1);
        else if (pair.T0 == tri || pair.T1 == tri)
            return;
        else if (pair.T1 < 0)
            adj[key] = (pair.T0, tri);
    }

    private static void UnlinkEdge(Dictionary<long, (int T0, int T1)> adj, int u, int v, int tri)
    {
        var key = EdgeKey(u, v);
        if (!adj.TryGetValue(key, out var pair))
            return;
        if (pair.T0 == tri)
        {
            if (pair.T1 < 0)
                adj.Remove(key);
            else
                adj[key] = (pair.T1, -1);
        }
        else if (pair.T1 == tri)
            adj[key] = (pair.T0, -1);
    }

    private static List<int>[] BuildIncident(List<Tri> triangles, int vertexCount)
    {
        var incident = new List<int>[vertexCount];
        for (var i = 0; i < triangles.Count; i++)
        {
            var tri = triangles[i];
            AddIncident(incident, tri.A, i);
            AddIncident(incident, tri.B, i);
            AddIncident(incident, tri.C, i);
        }

        return incident;
    }

    private static void AddIncident(List<int>[] incident, int vertex, int tri)
    {
        var list = incident[vertex];
        if (list is null)
        {
            incident[vertex] = new List<int>(8) { tri };
            return;
        }

        list.Add(tri);
    }

    private static void RemoveIncident(List<int>[] incident, int vertex, int tri)
    {
        incident[vertex]?.Remove(tri);
    }

    private static long EdgeKey(int a, int b)
    {
        if (a > b)
            (a, b) = (b, a);
        return ((long)a << 32) | (uint)b;
    }

    private static bool Force(
        List<Tri> triangles,
        List<TinVertex> vertices,
        HashSet<long> edgeKeys,
        Dictionary<long, (int T0, int T1)> adj,
        List<int>[] incident,
        int a,
        int b)
    {
        if (a == b)
            return true;
        var ab = EdgeKey(a, b);
        if (edgeKeys.Contains(ab))
            return true;
        var limit = triangles.Count * 4 + 8;
        var resume = -1;
        var avoidU = -1;
        var avoidW = -1;
        for (var guard = 0; guard < limit; guard++)
        {
            if (edgeKeys.Contains(ab))
                return true;

            int i, u, w, p;
            if (resume >= 0
                && resume < triangles.Count
                && TryEdgeCut(triangles[resume], vertices, a, b, avoidU, avoidW, out u, out w, out p))
            {
                i = resume;
            }
            else if (resume >= 0
                     && ContinueCrossingWalk(
                         triangles, vertices, adj, a, b, resume, avoidU, avoidW, out i, out u, out w, out p))
            {
                // tiếp từ chỗ vừa lật — không quay lại quét quạt đỉnh a
            }
            else if (!FindCrossingWalk(triangles, vertices, adj, incident, a, b, out i, out u, out w, out p))
                return false;

            var j = NeighborAdj(adj, triangles, i, u, w, out var q);
            if (j < 0)
                return false;
            if (!Opposite(vertices[u], vertices[w], vertices[p], vertices[q]))
                return false;

            edgeKeys.Remove(EdgeKey(u, w));
            UnlinkEdge(adj, u, w, i);
            UnlinkEdge(adj, u, w, j);
            RemoveIncident(incident, u, i);
            RemoveIncident(incident, w, i);
            RemoveIncident(incident, p, i);
            RemoveIncident(incident, u, j);
            RemoveIncident(incident, w, j);
            RemoveIncident(incident, q, j);

            UnlinkEdge(adj, p, u, i);
            UnlinkEdge(adj, p, w, i);
            UnlinkEdge(adj, q, u, j);
            UnlinkEdge(adj, q, w, j);

            triangles[i] = Ccw(vertices, p, q, u);
            triangles[j] = Ccw(vertices, p, q, w);

            edgeKeys.Add(EdgeKey(p, q));
            edgeKeys.Add(EdgeKey(p, u));
            edgeKeys.Add(EdgeKey(q, u));
            edgeKeys.Add(EdgeKey(p, w));
            edgeKeys.Add(EdgeKey(q, w));

            LinkEdge(adj, p, q, i);
            LinkEdge(adj, p, q, j);
            LinkEdge(adj, p, u, i);
            LinkEdge(adj, q, u, i);
            LinkEdge(adj, p, w, j);
            LinkEdge(adj, q, w, j);

            AddIncident(incident, p, i);
            AddIncident(incident, q, i);
            AddIncident(incident, u, i);
            AddIncident(incident, p, j);
            AddIncident(incident, q, j);
            AddIncident(incident, w, j);

            // Tiếp tục về phía b từ một trong hai tam giác mới.
            avoidU = -1;
            avoidW = -1;
            if (EdgeKey(p, q) == ab)
            {
                resume = -1;
                return true;
            }

            if (TryEdgeCut(triangles[i], vertices, a, b, -1, -1, out _, out _, out _))
                resume = i;
            else if (TryEdgeCut(triangles[j], vertices, a, b, -1, -1, out _, out _, out _))
                resume = j;
            else
                resume = -1;
        }

        return edgeKeys.Contains(ab);
    }

    /// <summary>
    /// Đi từ đỉnh a về phía b trên lưới, tìm một cạnh đang cắt đoạn ab — O(đường đi), không O(T).
    /// </summary>
    private static bool FindCrossingWalk(
        List<Tri> triangles,
        List<TinVertex> vertices,
        Dictionary<long, (int T0, int T1)> adj,
        List<int>[] incident,
        int a,
        int b,
        out int triIndex,
        out int u,
        out int w,
        out int p)
    {
        triIndex = -1;
        u = 0;
        w = 0;
        p = 0;
        var around = incident[a];
        if (around is null || around.Count == 0)
            return false;

        const double eps = 1e-8;
        var current = -1;
        var avoidU = -1;
        var avoidW = -1;
        foreach (var ti in around)
        {
            if (ti < 0 || ti >= triangles.Count)
                continue;
            var tri = triangles[ti];
            if (!OtherTwo(tri, a, out var v1, out var v2))
                continue;
            if (v1 == b || v2 == b)
                return false;
            if (Orient(vertices[a], vertices[v1], vertices[v2]) < 0)
                (v1, v2) = (v2, v1);
            if (Orient(vertices[a], vertices[v1], vertices[b]) < -eps)
                continue;
            if (Orient(vertices[a], vertices[b], vertices[v2]) < -eps)
                continue;

            if (Cuts(vertices, a, b, v1, v2))
            {
                triIndex = ti;
                u = v1;
                w = v2;
                p = a;
                return true;
            }

            // b nằm trong tam giác → không ép được ab một cạnh.
            if (Orient(vertices[v1], vertices[v2], vertices[b]) >= -eps)
                return false;

            var next = NeighborAdj(adj, triangles, ti, v1, v2, out _);
            if (next < 0)
                return false;
            current = next;
            avoidU = v1;
            avoidW = v2;
            break;
        }

        if (current < 0)
            return false;

        var guard = triangles.Count + 8;
        for (var step = 0; step < guard; step++)
        {
            var tri = triangles[current];
            if (tri.A == b || tri.B == b || tri.C == b)
                return false;

            if (TryEdgeCut(tri, vertices, a, b, avoidU, avoidW, out u, out w, out p))
            {
                triIndex = current;
                return true;
            }

            if (!PickExitToward(tri, vertices, b, avoidU, avoidW, out var nextU, out var nextW))
                return false;
            var nxt = NeighborAdj(adj, triangles, current, nextU, nextW, out _);
            if (nxt < 0)
                return false;
            avoidU = nextU;
            avoidW = nextW;
            current = nxt;
        }

        return false;
    }

    /// <summary>Tiếp walk từ một tam giác đã biết (sau lần lật), cùng kết quả hình học.</summary>
    private static bool ContinueCrossingWalk(
        List<Tri> triangles,
        List<TinVertex> vertices,
        Dictionary<long, (int T0, int T1)> adj,
        int a,
        int b,
        int start,
        int avoidU,
        int avoidW,
        out int triIndex,
        out int u,
        out int w,
        out int p)
    {
        triIndex = -1;
        u = 0;
        w = 0;
        p = 0;
        if (start < 0 || start >= triangles.Count)
            return false;
        var current = start;
        var guard = triangles.Count + 8;
        for (var step = 0; step < guard; step++)
        {
            var tri = triangles[current];
            if (tri.A == b || tri.B == b || tri.C == b)
                return false;
            if (TryEdgeCut(tri, vertices, a, b, avoidU, avoidW, out u, out w, out p))
            {
                triIndex = current;
                return true;
            }

            if (!PickExitToward(tri, vertices, b, avoidU, avoidW, out var nextU, out var nextW))
                return false;
            var nxt = NeighborAdj(adj, triangles, current, nextU, nextW, out _);
            if (nxt < 0)
                return false;
            avoidU = nextU;
            avoidW = nextW;
            current = nxt;
        }

        return false;
    }

    private static bool TryEdgeCut(
        Tri tri,
        List<TinVertex> vertices,
        int a,
        int b,
        int avoidU,
        int avoidW,
        out int u,
        out int w,
        out int p)
    {
        if (IsAvoid(tri.A, tri.B, avoidU, avoidW))
        {
            // skip
        }
        else if (Cuts(vertices, a, b, tri.A, tri.B))
        {
            u = tri.A;
            w = tri.B;
            p = tri.C;
            return true;
        }

        if (!IsAvoid(tri.B, tri.C, avoidU, avoidW) && Cuts(vertices, a, b, tri.B, tri.C))
        {
            u = tri.B;
            w = tri.C;
            p = tri.A;
            return true;
        }

        if (!IsAvoid(tri.C, tri.A, avoidU, avoidW) && Cuts(vertices, a, b, tri.C, tri.A))
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

    private static bool IsAvoid(int u, int w, int avoidU, int avoidW) =>
        avoidU >= 0 && ((u == avoidU && w == avoidW) || (u == avoidW && w == avoidU));

    private static bool PickExitToward(
        Tri tri,
        List<TinVertex> vertices,
        int b,
        int avoidU,
        int avoidW,
        out int exitU,
        out int exitW)
    {
        exitU = 0;
        exitW = 0;
        var best = -1;
        var bestScore = double.PositiveInfinity;
        var target = vertices[b];
        Span<(int U, int V)> edges = stackalloc (int U, int V)[3];
        edges[0] = (tri.A, tri.B);
        edges[1] = (tri.B, tri.C);
        edges[2] = (tri.C, tri.A);
        for (var i = 0; i < 3; i++)
        {
            var (eu, ev) = edges[i];
            if (IsAvoid(eu, ev, avoidU, avoidW))
                continue;
            var midX = (vertices[eu].X + vertices[ev].X) * 0.5;
            var midY = (vertices[eu].Y + vertices[ev].Y) * 0.5;
            var dx = midX - target.X;
            var dy = midY - target.Y;
            var score = dx * dx + dy * dy;
            if (score >= bestScore)
                continue;
            bestScore = score;
            best = i;
        }

        if (best < 0)
            return false;
        exitU = edges[best].U;
        exitW = edges[best].V;
        return true;
    }

    private static int OtherVertex(Tri tri, int u, int w)
    {
        if (tri.A != u && tri.A != w)
            return tri.A;
        if (tri.B != u && tri.B != w)
            return tri.B;
        return tri.C;
    }

    private static bool OtherTwo(Tri tri, int vertex, out int v1, out int v2)
    {
        if (tri.A == vertex)
        {
            v1 = tri.B;
            v2 = tri.C;
            return true;
        }

        if (tri.B == vertex)
        {
            v1 = tri.C;
            v2 = tri.A;
            return true;
        }

        if (tri.C == vertex)
        {
            v1 = tri.A;
            v2 = tri.B;
            return true;
        }

        v1 = 0;
        v2 = 0;
        return false;
    }

    private static bool Cuts(List<TinVertex> vertices, int a, int b, int u, int w)
    {
        if (u == a || u == b || w == a || w == b)
            return false;
        return ProperCross(vertices[a], vertices[b], vertices[u], vertices[w], out _, out _);
    }

    private static int NeighborAdj(
        Dictionary<long, (int T0, int T1)> adj,
        List<Tri> triangles,
        int skip,
        int u,
        int w,
        out int q)
    {
        q = -1;
        if (!adj.TryGetValue(EdgeKey(u, w), out var pair))
            return -1;
        int other;
        if (pair.T0 == skip)
            other = pair.T1;
        else if (pair.T1 == skip)
            other = pair.T0;
        else
            other = pair.T0 == skip ? pair.T1 : pair.T0;
        if (other < 0 || other >= triangles.Count)
            return -1;
        q = OtherVertex(triangles[other], u, w);
        return other;
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
        // |cross| / sqrt(len2) > 1e-4  ⇔  cross² > (1e-4)² * len2
        var cross = abx * apy - aby * apx;
        if (cross * cross > 1e-8 * len2)
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
        var lenA2 = rx * rx + ry * ry;
        var lenB2 = sx * sx + sy * sy;
        if (lenA2 < 1e-18 || lenB2 < 1e-18)
            return false;
        var den = rx * sy - ry * sx;
        // |den| < 1e-12 * |A| * |B|  ⇔  den² < 1e-24 * lenA2 * lenB2
        if (den * den < 1e-24 * lenA2 * lenB2)
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

    [ThreadStatic]
    private static InsertScratch? _insertScratch;

    private sealed class InsertScratch
    {
        public readonly List<int> Bad = new(64);
        public readonly HashSet<int> Mark = new();
        public readonly Queue<int> Queue = new();
        public readonly List<(int U, int V, int Outside)> Boundary = new(64);
        public readonly Dictionary<int, int> ByStart = new(64);
        public readonly Dictionary<int, int> ByEnd = new(64);
    }

    private static void Insert(List<TinVertex> vertices, List<MeshTri> mesh, int index, ref int hint)
    {
        var point = vertices[index];
        var seed = Locate(vertices, mesh, hint, point);
        if (seed < 0)
            return;

        var scratch = _insertScratch ??= new InsertScratch();
        var bad = scratch.Bad;
        var mark = scratch.Mark;
        var queue = scratch.Queue;
        var boundary = scratch.Boundary;
        var byStart = scratch.ByStart;
        var byEnd = scratch.ByEnd;
        bad.Clear();
        mark.Clear();
        queue.Clear();
        boundary.Clear();
        byStart.Clear();
        byEnd.Clear();

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

        foreach (var id in bad)
        {
            var tri = mesh[id];
            Edge(mark, boundary, tri.A, tri.B, tri.Nab);
            Edge(mark, boundary, tri.B, tri.C, tri.Nbc);
            Edge(mark, boundary, tri.C, tri.A, tri.Nca);
            tri.Dead = true;
        }

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

    internal static bool Barycentric(TinTriangle tri, double x, double y, out double wa, out double wb, out double wc)
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
