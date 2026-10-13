using Autodesk.AutoCAD.Geometry;

namespace iSurvey.Core;

/// <summary>Hình học mặt phẳng phục vụ nhập KML.</summary>
public static class GeoMath
{
    /// <summary>
    /// Ramer–Douglas–Peucker. Giữ hai đầu mút. Khoảng cách là vuông góc trên mặt phẳng XY (mét),
    /// cao độ Z của đỉnh được giữ thì giữ nguyên. Vòng lặp dùng ngăn xếp để danh sách vài chục nghìn đỉnh không tràn stack.
    /// </summary>
    public static List<Point3d> DouglasPeucker(List<Point3d> points, double tolerance)
    {
        var count = points.Count;
        if (count < 3 || tolerance <= 0)
            return points;

        var keep = new bool[count];
        keep[0] = true;
        keep[count - 1] = true;
        var tol2 = tolerance * tolerance;
        var stack = new Stack<(int Start, int End)>();
        stack.Push((0, count - 1));

        while (stack.Count > 0)
        {
            var (start, end) = stack.Pop();
            if (end <= start + 1)
                continue;

            var ax = points[start].X;
            var ay = points[start].Y;
            var dx = points[end].X - ax;
            var dy = points[end].Y - ay;
            var len2 = dx * dx + dy * dy;

            var maxDist2 = -1.0;
            var farthest = -1;
            for (var i = start + 1; i < end; i++)
            {
                double dist2;
                var px = points[i].X - ax;
                var py = points[i].Y - ay;
                if (len2 < 1e-12)
                {
                    dist2 = px * px + py * py;
                }
                else
                {
                    var cross = py * dx - px * dy;
                    dist2 = cross * cross / len2;
                }

                if (dist2 > maxDist2)
                {
                    maxDist2 = dist2;
                    farthest = i;
                }
            }

            if (farthest < 0 || maxDist2 <= tol2)
                continue;

            keep[farthest] = true;
            stack.Push((start, farthest));
            stack.Push((farthest, end));
        }

        var cleaned = new List<Point3d>(count);
        for (var i = 0; i < count; i++)
        {
            if (keep[i])
                cleaned.Add(points[i]);
        }

        return cleaned;
    }
}
