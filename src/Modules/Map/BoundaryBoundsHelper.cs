using Autodesk.AutoCAD.Geometry;
using iSurvey.Models;

namespace iSurvey.Modules.Map;

/// <summary>Chuyển đường bao WCS ↔ WGS và giao biên.</summary>
internal static class BoundaryBoundsHelper
{
    public static GeoBounds ToWgs84Bounds(
        IReadOnlyList<Point2d> polygonWcs,
        CoordinateService coordinates,
        double centralMeridian,
        int zoneWidthDegrees = 3)
    {
        var wgs = polygonWcs
            .Select(p => coordinates.ToWgs84(new PlanePoint(p.X, p.Y), centralMeridian, zoneWidthDegrees))
            .ToList();

        return new GeoBounds(
            wgs.Min(p => p.Longitude),
            wgs.Min(p => p.Latitude),
            wgs.Max(p => p.Longitude),
            wgs.Max(p => p.Latitude));
    }

    public static GeoBounds Intersect(GeoBounds a, GeoBounds b)
    {
        return new GeoBounds(
            Math.Max(a.MinLongitude, b.MinLongitude),
            Math.Max(a.MinLatitude, b.MinLatitude),
            Math.Min(a.MaxLongitude, b.MaxLongitude),
            Math.Min(a.MaxLatitude, b.MaxLatitude));
    }

    public static bool IsValid(GeoBounds bounds)
        => bounds.MaxLongitude > bounds.MinLongitude + 1e-12
           && bounds.MaxLatitude > bounds.MinLatitude + 1e-12;
}
