using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using iSurvey.Models;

namespace iSurvey.Modules.Map;

/// <summary>Kiểm tra tile có giao đường bao trước khi tải/gắn.</summary>
internal static class TileBoundaryFilter
{
    public static bool IntersectsTile(
        int x,
        int y,
        int z,
        CoordinateService coordinates,
        double centralMeridian,
        IReadOnlyList<Point2d> clipPolygonWcs)
    {
        if (clipPolygonWcs.Count < 3)
            return true;

        var nw = TileMath.TileTopLeft(x, y, z);
        var ne = TileMath.TileTopLeft(x + 1, y, z);
        var sw = TileMath.TileTopLeft(x, y + 1, z);

        var pNw = coordinates.ToVn2000(nw, centralMeridian);
        var pNe = coordinates.ToVn2000(ne, centralMeridian);
        var pSw = coordinates.ToVn2000(sw, centralMeridian);

        var origin = new Point3d(pSw.Easting, pSw.Northing, 0);
        var xAxis = new Vector3d(
            pNe.Easting - pNw.Easting,
            pNe.Northing - pNw.Northing,
            0);
        var yAxis = new Vector3d(
            pNw.Easting - pSw.Easting,
            pNw.Northing - pSw.Northing,
            0);

        if (xAxis.Length < 1e-9 || yAxis.Length < 1e-9)
            return false;

        return RasterClipService.TileIntersectsBoundary(
            new CoordinateSystem3d(origin, xAxis, yAxis),
            clipPolygonWcs);
    }
}
