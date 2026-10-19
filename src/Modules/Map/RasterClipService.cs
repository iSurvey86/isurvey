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
