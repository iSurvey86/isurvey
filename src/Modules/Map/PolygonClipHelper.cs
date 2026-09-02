using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace iSurvey.Modules.Map;

/// <summary>Giao 2D polygon — dùng clip tile theo đường bao.</summary>
internal static class PolygonClipHelper
{
    /// <summary>Giao subject (có thể lõm) với vùng clip lồi (tile quad).</summary>
    public static List<Point2d> IntersectWithConvexClip(
        IReadOnlyList<Point2d> subject,
        IReadOnlyList<Point2d> convexClip)
    {
        var output = subject.ToList();
        for (var i = 0; i < convexClip.Count; i++)
        {
            var edgeStart = convexClip[i];
            var edgeEnd = convexClip[(i + 1) % convexClip.Count];
            output = ClipAgainstEdge(output, edgeStart, edgeEnd);
            if (output.Count == 0)
                break;
        }

        return output;
    }

    public static bool BboxesOverlap(
        IReadOnlyList<Point2d> a,
        IReadOnlyList<Point2d> b)
    {
        var aMinX = a.Min(p => p.X);
        var aMaxX = a.Max(p => p.X);
        var aMinY = a.Min(p => p.Y);
        var aMaxY = a.Max(p => p.Y);
        var bMinX = b.Min(p => p.X);
        var bMaxX = b.Max(p => p.X);
        var bMinY = b.Min(p => p.Y);
        var bMaxY = b.Max(p => p.Y);

        return !(aMaxX < bMinX || aMinX > bMaxX || aMaxY < bMinY || aMinY > bMaxY);
    }

    private static List<Point2d> ClipAgainstEdge(
        List<Point2d> input,
        Point2d edgeStart,
        Point2d edgeEnd)
    {
        var output = new List<Point2d>();
        if (input.Count == 0)
            return output;

        for (var i = 0; i < input.Count; i++)
        {
            var current = input[i];
            var previous = input[(i + input.Count - 1) % input.Count];
            var currInside = IsInside(current, edgeStart, edgeEnd);
            var prevInside = IsInside(previous, edgeStart, edgeEnd);

            if (currInside)
            {
                if (!prevInside)
                    output.Add(LineIntersection(previous, current, edgeStart, edgeEnd));
                output.Add(current);
            }
            else if (prevInside)
            {
                output.Add(LineIntersection(previous, current, edgeStart, edgeEnd));
            }
        }

        return output;
    }

    private static bool IsInside(Point2d point, Point2d edgeStart, Point2d edgeEnd)
    {
        return (edgeEnd.X - edgeStart.X) * (point.Y - edgeStart.Y)
               - (edgeEnd.Y - edgeStart.Y) * (point.X - edgeStart.X) >= -1e-9;
    }

    private static Point2d LineIntersection(
        Point2d p1,
        Point2d p2,
        Point2d edgeStart,
        Point2d edgeEnd)
    {
        var x1 = p1.X;
        var y1 = p1.Y;
        var x2 = p2.X;
        var y2 = p2.Y;
        var x3 = edgeStart.X;
        var y3 = edgeStart.Y;
        var x4 = edgeEnd.X;
        var y4 = edgeEnd.Y;

        var denom = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
        if (Math.Abs(denom) < 1e-12)
            return p2;

        var t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / denom;
        return new Point2d(x1 + t * (x2 - x1), y1 + t * (y2 - y1));
    }
}
