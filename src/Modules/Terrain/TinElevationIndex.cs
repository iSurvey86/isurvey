namespace iSurvey.Modules.Terrain;

/// <summary>
/// Chỉ mục lưới không gian để tra Z nhanh (cọc / mặt cắt).
/// Tránh quét O(T) mọi tam giác cho mỗi điểm hỏi.
/// </summary>
public sealed class TinElevationIndex
{
    private readonly double _originX;
    private readonly double _originY;
    private readonly double _cell;
    private readonly int _cols;
    private readonly int _rows;
    private readonly List<TinTriangle>[] _cells;
    private readonly IReadOnlyList<TinTriangle> _all;

    private TinElevationIndex(
        IReadOnlyList<TinTriangle> all,
        double originX,
        double originY,
        double cell,
        int cols,
        int rows,
        List<TinTriangle>[] cells)
    {
        _all = all;
        _originX = originX;
        _originY = originY;
        _cell = cell;
        _cols = cols;
        _rows = rows;
        _cells = cells;
    }

    public int TriangleCount => _all.Count;

    public static TinElevationIndex? Build(IReadOnlyList<TinTriangle> triangles)
    {
        if (triangles is null || triangles.Count == 0)
            return null;

        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;
        foreach (var tri in triangles)
        {
            Expand(tri.A, ref minX, ref minY, ref maxX, ref maxY);
            Expand(tri.B, ref minX, ref minY, ref maxX, ref maxY);
            Expand(tri.C, ref minX, ref minY, ref maxX, ref maxY);
        }

        if (double.IsNaN(minX) || double.IsInfinity(minX) || maxX < minX || maxY < minY)
            return null;

        var spanX = Math.Max(maxX - minX, 1e-3);
        var spanY = Math.Max(maxY - minY, 1e-3);
        // ~sqrt(N) ô mỗi chiều; tối thiểu 8, tối đa 256.
        var side = (int)Math.Ceiling(Math.Sqrt(triangles.Count));
        if (side < 8)
            side = 8;
        if (side > 256)
            side = 256;
        var cell = Math.Max(spanX, spanY) / side;
        if (cell < 1e-3)
            cell = 1e-3;
        var cols = Math.Max(1, (int)Math.Ceiling(spanX / cell) + 1);
        var rows = Math.Max(1, (int)Math.Ceiling(spanY / cell) + 1);
        var cells = new List<TinTriangle>[cols * rows];

        foreach (var tri in triangles)
        {
            var tMinX = Math.Min(tri.A.X, Math.Min(tri.B.X, tri.C.X));
            var tMinY = Math.Min(tri.A.Y, Math.Min(tri.B.Y, tri.C.Y));
            var tMaxX = Math.Max(tri.A.X, Math.Max(tri.B.X, tri.C.X));
            var tMaxY = Math.Max(tri.A.Y, Math.Max(tri.B.Y, tri.C.Y));
            var c0 = Clamp((int)Math.Floor((tMinX - minX) / cell), 0, cols - 1);
            var c1 = Clamp((int)Math.Floor((tMaxX - minX) / cell), 0, cols - 1);
            var r0 = Clamp((int)Math.Floor((tMinY - minY) / cell), 0, rows - 1);
            var r1 = Clamp((int)Math.Floor((tMaxY - minY) / cell), 0, rows - 1);
            for (var r = r0; r <= r1; r++)
            {
                for (var c = c0; c <= c1; c++)
                {
                    var i = r * cols + c;
                    cells[i] ??= new List<TinTriangle>();
                    cells[i].Add(tri);
                }
            }
        }

        return new TinElevationIndex(triangles, minX, minY, cell, cols, rows, cells);
    }

    public bool TryElevation(double x, double y, out double z)
    {
        var c = (int)Math.Floor((x - _originX) / _cell);
        var r = (int)Math.Floor((y - _originY) / _cell);
        if (c < 0 || r < 0 || c >= _cols || r >= _rows)
        {
            z = 0;
            return false;
        }

        var bucket = _cells[r * _cols + c];
        if (bucket is { Count: > 0 })
        {
            foreach (var tri in bucket)
            {
                if (!TinBuilder.Barycentric(tri, x, y, out var wa, out var wb, out var wc))
                    continue;
                z = wa * tri.A.Z + wb * tri.B.Z + wc * tri.C.Z;
                return true;
            }
        }

        // Ô kề (điểm sát biên ô / cạnh tam giác).
        for (var dr = -1; dr <= 1; dr++)
        {
            for (var dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0)
                    continue;
                var cc = c + dc;
                var rr = r + dr;
                if (cc < 0 || rr < 0 || cc >= _cols || rr >= _rows)
                    continue;
                var near = _cells[rr * _cols + cc];
                if (near is null)
                    continue;
                foreach (var tri in near)
                {
                    if (!TinBuilder.Barycentric(tri, x, y, out var wa, out var wb, out var wc))
                        continue;
                    z = wa * tri.A.Z + wb * tri.B.Z + wc * tri.C.Z;
                    return true;
                }
            }
        }

        z = 0;
        return false;
    }

    private static void Expand(TinVertex v, ref double minX, ref double minY, ref double maxX, ref double maxY)
    {
        if (v.X < minX) minX = v.X;
        if (v.Y < minY) minY = v.Y;
        if (v.X > maxX) maxX = v.X;
        if (v.Y > maxY) maxY = v.Y;
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min)
            return min;
        if (value > max)
            return max;
        return value;
    }
}
