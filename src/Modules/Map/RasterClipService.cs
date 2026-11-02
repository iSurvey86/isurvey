using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace iSurvey.Modules.Map;

/// <summary>Clip RasterImage theo đường bao chèn — giữ nguyên tile gốc.</summary>
internal static class RasterClipService
{
    public static bool TileIntersectsBoundary(
        CoordinateSystem3d orientation,
        IReadOnlyList<Point2d> clipPolygonWcs)
    {
        if (clipPolygonWcs.Count < 3)
            return true;

        var tileQuad = BuildTileQuad(orientation);
        if (!PolygonClipHelper.BboxesOverlap(clipPolygonWcs, tileQuad))
            return false;

        return PolygonClipHelper.IntersectWithConvexClip(clipPolygonWcs, tileQuad).Count >= 3;
    }

    public static bool TryApplySessionClip(RasterImage raster)
    {
        if (!BasemapSession.HasBoundaryClip)
            return true;

        return TryApplyClip(raster, BasemapSession.ClipPolygonWcs!);
    }

    public static bool TryApplyClip(RasterImage raster, IReadOnlyList<Point2d> clipPolygonWcs)
    {
        if (clipPolygonWcs.Count < 3)
            return false;

        var tileQuad = BuildTileQuad(raster.Orientation);
        if (!PolygonClipHelper.BboxesOverlap(clipPolygonWcs, tileQuad))
            return false;

        var localClip = PolygonClipHelper.IntersectWithConvexClip(clipPolygonWcs, tileQuad);
        if (localClip.Count < 3)
            return false;

        return ApplyClipRing(raster, localClip);
    }

    /// <summary>
    /// Gắn clip đa giác đầy đủ (UAV / orthophoto) — không cắt theo hình chữ nhật tile GE.
    /// Tọa độ WCS ➔ hệ pixel qua PixelToModelTransform.Inverse().
    /// </summary>
    public static bool TryApplyClipPolygon(RasterImage raster, IReadOnlyList<Point2d> clipPolygonWcs)
    {
        if (clipPolygonWcs.Count < 3)
            return false;

        try
        {
            return ApplyClipRing(raster, clipPolygonWcs);
        }
        catch (Autodesk.AutoCAD.Runtime.Exception)
        {
            // Biên suy biến: báo thất bại cho ảnh này, không làm hỏng cả lô UAV.
            return false;
        }
    }

    /// <summary>Gắn clip trực tiếp bằng tọa độ pixel CAD (tránh round-trip WCS sai gốc Y).</summary>
    public static bool TryApplyClipPolygonPixels(RasterImage raster, IReadOnlyList<Point2d> pixelRing)
    {
        if (pixelRing.Count < 3)
            return false;

        var ring = new Point2dCollection();
        foreach (var p in pixelRing)
            ring.Add(p);

        var first = ring[0];
        var last = ring[ring.Count - 1];
        if (first.GetDistanceTo(last) > 1e-6)
            ring.Add(first);

        try
        {
            raster.SetClipBoundary(ClipBoundaryType.Poly, ring);
            raster.IsClipped = true;
            return true;
        }
        catch (Autodesk.AutoCAD.Runtime.Exception)
        {
            // Biên suy biến / không hợp lệ: báo thất bại cho ảnh này, không làm hỏng cả lô.
            return false;
        }
    }

    public static List<Point2d> BuildTileQuad(CoordinateSystem3d orientation)
    {
        var o = orientation.Origin;
        var x = orientation.Xaxis;
        var y = orientation.Yaxis;

        return
        [
            new Point2d(o.X, o.Y),
            new Point2d(o.X + x.X, o.Y + x.Y),
            new Point2d(o.X + x.X + y.X, o.Y + x.Y + y.Y),
            new Point2d(o.X + y.X, o.Y + y.Y)
        ];
    }

    private static bool ApplyClipRing(RasterImage raster, IReadOnlyList<Point2d> wcsRing)
    {
        var pixelRing = ToPixelRing(raster, wcsRing);
        if (pixelRing.Count < 3)
            return false;

        raster.SetClipBoundary(ClipBoundaryType.Poly, pixelRing);
        raster.IsClipped = true;
        return true;
    }

    /// <summary>Biên cắt đang gắn, hệ pixel ảnh. Bỏ điểm đóng trùng điểm đầu.</summary>
    public static List<Point2d> ReadClipPixels(RasterImage raster)
    {
        var ring = new List<Point2d>();
        if (!raster.IsClipped)
            return ring;

        try
        {
            foreach (Point2d p in raster.GetClipBoundary())
                ring.Add(p);
        }
        catch (Autodesk.AutoCAD.Runtime.Exception)
        {
            return [];
        }

        if (ring.Count >= 2 && ring[0].GetDistanceTo(ring[^1]) < 1e-4)
            ring.RemoveAt(ring.Count - 1);
        return ring;
    }

    /// <summary>
    /// Giữ phần biên cắt của ảnh đích nằm trong biên cắt pixel của ảnh gốc.
    /// Dùng cho sơ họa đã duỗi: cùng file ảnh, nhưng biên cắt là một ảnh khác.
    /// </summary>
    public static bool TryMaskClipWith(RasterImage target, IReadOnlyList<Point2d> sourcePixels)
    {
        if (sourcePixels.Count < 3 || !target.IsClipped)
            return false;

        var own = ReadClipPixels(target);
        if (own.Count < 3 || !IsConvex(own))
            return false;
        if (SignedArea(own) < 0)
            own.Reverse();

        var hit = PolygonClipHelper.IntersectWithConvexClip(sourcePixels, own);
        if (hit.Count < 3)
            return false;

        return TryApplyClipPolygonPixels(target, hit);
    }

    private static double SignedArea(List<Point2d> ring)
    {
        double sum = 0;
        for (var i = 0; i < ring.Count; i++)
        {
            var a = ring[i];
            var b = ring[(i + 1) % ring.Count];
            sum += a.X * b.Y - b.X * a.Y;
        }

        return sum;
    }

    private static bool IsConvex(List<Point2d> ring)
    {
        var sign = 0;
        for (var i = 0; i < ring.Count; i++)
        {
            var a = ring[i];
            var b = ring[(i + 1) % ring.Count];
            var c = ring[(i + 2) % ring.Count];
            var cross = (b.X - a.X) * (c.Y - b.Y) - (b.Y - a.Y) * (c.X - b.X);
            if (Math.Abs(cross) < 1e-8)
                continue;
            var s = cross > 0 ? 1 : -1;
            if (sign == 0)
                sign = s;
            else if (s != sign)
                return false;
        }

        return sign != 0;
    }

    private static Point2dCollection ToPixelRing(RasterImage raster, IReadOnlyList<Point2d> wcsRing)
    {
        var toPixel = raster.PixelToModelTransform.Inverse();
        var ring = new Point2dCollection();

        foreach (var wcs in wcsRing)
        {
            var px = new Point3d(wcs.X, wcs.Y, 0).TransformBy(toPixel);
            ring.Add(new Point2d(px.X, px.Y));
        }

        if (ring.Count > 0)
        {
            var first = ring[0];
            var last = ring[ring.Count - 1];
            if (first.GetDistanceTo(last) > 1e-6)
                ring.Add(first);
        }

        return ring;
    }
}
