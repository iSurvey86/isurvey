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
