using iSurvey.Models;

namespace iSurvey.Modules.Map;

/// <summary>Web Mercator tile math — ChooseZoom theo kích thước màn hình.</summary>
public static class TileMath
{
    public const int TileSize = 256;
    public const int MaxTilesPerView = 128;
    public const int MaxZoom = 19;
    private const double MaxLat = 85.05112878;

    public static int LonToTileX(double lon, int z)
    {
        var n = 1 << z;
        return (int)Math.Floor((lon + 180.0) / 360.0 * n);
    }

    public static int LatToTileY(double lat, int z)
    {
        var n = 1 << z;
        var latRad = ClampLat(lat) * Math.PI / 180.0;
        return (int)Math.Floor((1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * n);
    }

    public static double TileXToLon(int x, int z)
    {
        var n = 1 << z;
        return x / (double)n * 360.0 - 180.0;
    }

    public static double TileYToLat(int y, int z)
    {
        var n = 1 << z;
        var latRad = Math.Atan(Math.Sinh(Math.PI * (1.0 - 2.0 * y / (double)n)));
        return latRad * 180.0 / Math.PI;
    }

    /// <summary>Góc NW của tile (x,y) trong hệ Web Mercator.</summary>
    public static GeoPoint TileTopLeft(int x, int y, int z)
    {
        return new GeoPoint(TileXToLon(x, z), TileYToLat(y, z));
    }

    public static GeoBounds ClampBounds(GeoBounds bounds)
    {
        return new GeoBounds(
            Math.Max(-180.0, Math.Min(180.0, bounds.MinLongitude)),
            Math.Max(-MaxLat, Math.Min(MaxLat, bounds.MinLatitude)),
            Math.Max(-180.0, Math.Min(180.0, bounds.MaxLongitude)),
            Math.Max(-MaxLat, Math.Min(MaxLat, bounds.MaxLatitude)));
    }

    /// <summary>Chọn zoom theo kích thước màn hình + biên WGS (Web Mercator normalized).</summary>
    public static int ChooseZoom(GeoBounds bounds, double screenWidth, double screenHeight, int maxZoom = MaxZoom)
    {
        var b = ClampBounds(bounds);
        var lonSpan = Math.Max(Math.Abs(LonToNormalizedX(b.MaxLongitude) - LonToNormalizedX(b.MinLongitude)), 1e-12);
        var latSpan = Math.Max(Math.Abs(LatToNormalizedY(b.MinLatitude) - LatToNormalizedY(b.MaxLatitude)), 1e-12);
        var pw = Math.Max(320.0, screenWidth);
        var ph = Math.Max(240.0, screenHeight);
        var zx = Math.Log(pw / (TileSize * lonSpan), 2.0);
        var zy = Math.Log(ph / (TileSize * latSpan), 2.0);
        return Math.Max(0, Math.Min(maxZoom, (int)Math.Floor(Math.Min(zx, zy))));
    }

    /// <summary>Chọn zoom nét nhất rồi hạ dần đến khi ≤ MaxTilesPerView.</summary>
    public static int FitZoom(GeoBounds bounds, double screenWidth, double screenHeight, int maxTiles = MaxTilesPerView)
    {
        var zoom = ChooseZoom(bounds, screenWidth, screenHeight);
        while (CountTiles(bounds, zoom) > maxTiles && zoom > 0)
            zoom--;
        return zoom;
    }

    public static (int XMin, int XMax, int YMin, int YMax) TileRangeForBounds(GeoBounds bounds, int zoom)
    {
        var b = ClampBounds(bounds);
        var n = 1 << zoom;
        var xMin = Clamp(LonToTileX(b.MinLongitude, zoom), 0, n - 1);
        var xMax = Clamp(LonToTileX(b.MaxLongitude, zoom), 0, n - 1);
        var yMin = Clamp(LatToTileY(b.MaxLatitude, zoom), 0, n - 1);
        var yMax = Clamp(LatToTileY(b.MinLatitude, zoom), 0, n - 1);
        return (xMin, xMax, yMin, yMax);
    }

    public static int CountTiles(GeoBounds bounds, int zoom)
    {
        var (xMin, xMax, yMin, yMax) = TileRangeForBounds(bounds, zoom);
        return (xMax - xMin + 1) * (yMax - yMin + 1);
    }

    public static IEnumerable<(int X, int Y)> EnumerateTiles(GeoBounds bounds, int zoom)
    {
        var (xMin, xMax, yMin, yMax) = TileRangeForBounds(bounds, zoom);
        for (var y = yMin; y <= yMax; y++)
        {
            for (var x = xMin; x <= xMax; x++)
                yield return (x, y);
        }
    }

    public static double MetersPerPixelAtZoom(int zoom, double latitudeDegrees)
    {
        var cosLat = Math.Max(0.2, Math.Cos(latitudeDegrees * Math.PI / 180.0));
        return 156_543.03392 * cosLat / (1 << zoom);
    }

    private static double LonToNormalizedX(double lon)
        => (lon + 180.0) / 360.0;

    private static double LatToNormalizedY(double lat)
    {
        var latRad = ClampLat(lat) * Math.PI / 180.0;
        return (1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) * 0.5;
    }

    private static double ClampLat(double lat)
        => Math.Max(-MaxLat, Math.Min(MaxLat, lat));

    private static int Clamp(int value, int min, int max)
        => value < min ? min : value > max ? max : value;
}
