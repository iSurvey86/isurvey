using System.Globalization;
using System.Reflection;
using System.Text.Json;
using iSurvey.Models;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace iSurvey.Modules.Map;

/// <summary>
/// Chuyển đổi tọa độ VN-2000 (TM-3 / TM-6) ↔ WGS84 bằng ProjNet + WKT có TOWGS84.
/// </summary>
public sealed class CoordinateService
{
    /// <summary>Kinh tuyến chuẩn múi 6° VN-2000 theo Thông tư 973/2001/TT-TCĐC.</summary>
    public static readonly double[] Tm6CentralMeridians = [105.0, 111.0, 117.0];

    private const string Towgs84 =
        "-191.90441429,-39.30318279,-111.45032835,0.00928836,-0.01975479,0.00427372,0.252906278";

    private readonly List<Vn2000CrsEntry> _entries;
    private readonly CoordinateSystemFactory _csFactory = new();
    private readonly CoordinateTransformationFactory _transformFactory = new();
    private readonly Dictionary<(double Cm, int Zone), CoordinateSystem> _csCache = new();

    public CoordinateService()
    {
        _entries = LoadEntries();
    }

    private static List<Vn2000CrsEntry> LoadEntries()
    {
        var asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                     ?? AppContext.BaseDirectory;
        var path = Path.Combine(asmDir, "Data", "isurvey_vn2000_tm3.json");
        if (!File.Exists(path))
            path = Path.Combine(AppContext.BaseDirectory, "Data", "isurvey_vn2000_tm3.json");

        if (!File.Exists(path))
            throw new FileNotFoundException("Không tìm thấy isurvey_vn2000_tm3.json", path);

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<Vn2000CrsEntry>>(json)
               ?? throw new InvalidOperationException("Không parse được isurvey_vn2000_tm3.json");
    }

    public Vn2000CrsEntry ResolveByCentralMeridian(double centralMeridian)
    {
        return _entries
            .OrderBy(e => Math.Abs(e.CentralMeridian - centralMeridian))
            .First();
    }

    /// <summary>Làm tròn kinh tuyến về 105 / 111 / 117 (múi 6°).</summary>
    public static double SnapToTm6CentralMeridian(double centralMeridian) =>
        Tm6CentralMeridians.OrderBy(m => Math.Abs(m - centralMeridian)).First();

    public static int NormalizeZoneWidth(int zoneWidthDegrees) =>
        zoneWidthDegrees == 6 ? 6 : 3;

    /// <summary>
    /// TM-6: chỉ 105 / 111 / 117. TM-3: kinh tuyến tỉnh trong khoảng 102°–117° (TT 973).
    /// </summary>
    public static bool IsValidCentralMeridian(double centralMeridian, int zoneWidthDegrees)
    {
        if (NormalizeZoneWidth(zoneWidthDegrees) == 6)
            return Tm6CentralMeridians.Contains(centralMeridian);
        return centralMeridian is >= 102 and <= 117;
    }

    /// <summary>Chuyển điểm VN2000 (E, N) sang WGS84.</summary>
    public GeoPoint ToWgs84(PlanePoint plane, double centralMeridian, int zoneWidthDegrees = 3)
    {
        var vn2000 = ResolveCoordinateSystem(centralMeridian, zoneWidthDegrees);
        var wgs84 = GeographicCoordinateSystem.WGS84;
        var toGeo = _transformFactory.CreateFromCoordinateSystems(vn2000, wgs84);
        var result = toGeo.MathTransform.Transform(new[] { plane.Easting, plane.Northing });
        return new GeoPoint(result[0], result[1]);
    }

    /// <summary>Chuyển điểm WGS84 sang VN2000 (E, N).</summary>
    public PlanePoint ToVn2000(GeoPoint geo, double centralMeridian, int zoneWidthDegrees = 3)
    {
        var vn2000 = ResolveCoordinateSystem(centralMeridian, zoneWidthDegrees);
        var wgs84 = GeographicCoordinateSystem.WGS84;
        var toProjected = _transformFactory.CreateFromCoordinateSystems(wgs84, vn2000);
        var result = toProjected.MathTransform.Transform(new[] { geo.Longitude, geo.Latitude });
        return new PlanePoint(result[0], result[1]);
    }

    public GeoBounds ViewportToWgs84Bounds(
        ViewportCorners corners, double centralMeridian, int zoneWidthDegrees = 3)
    {
        var pts = new[]
        {
            ToWgs84(corners.BottomLeft, centralMeridian, zoneWidthDegrees),
            ToWgs84(corners.BottomRight, centralMeridian, zoneWidthDegrees),
            ToWgs84(corners.TopRight, centralMeridian, zoneWidthDegrees),
            ToWgs84(corners.TopLeft, centralMeridian, zoneWidthDegrees)
        };

        return EnvelopePoints(pts);
    }

    public GeoBounds ViewportToWgs84BoundsThorough(
        ViewportCorners corners, double centralMeridian, int zoneWidthDegrees = 3)
    {
        var plane = new[]
        {
            corners.BottomLeft,
            corners.BottomRight,
            corners.TopRight,
            corners.TopLeft
        };

        var wgsPoints = new List<GeoPoint>(plane.Length * 2);
        for (var i = 0; i < plane.Length; i++)
        {
            var a = plane[i];
            var b = plane[(i + 1) % plane.Length];
            wgsPoints.Add(ToWgs84(a, centralMeridian, zoneWidthDegrees));
            wgsPoints.Add(ToWgs84(
                new PlanePoint((a.Easting + b.Easting) / 2.0, (a.Northing + b.Northing) / 2.0),
                centralMeridian,
                zoneWidthDegrees));
        }

        return EnvelopePoints(wgsPoints);
    }

    public GeoBounds PolygonToWgs84Bounds(
        IReadOnlyList<Autodesk.AutoCAD.Geometry.Point2d> polygon,
        double centralMeridian,
        int zoneWidthDegrees = 3)
    {
        if (polygon.Count < 3)
            throw new ArgumentException("Polygon cần ít nhất 3 đỉnh.", nameof(polygon));

        var wgsPoints = new List<GeoPoint>(polygon.Count * 2);
        for (var i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Count];
            wgsPoints.Add(ToWgs84(new PlanePoint(a.X, a.Y), centralMeridian, zoneWidthDegrees));
            wgsPoints.Add(ToWgs84(
                new PlanePoint((a.X + b.X) / 2.0, (a.Y + b.Y) / 2.0),
                centralMeridian,
                zoneWidthDegrees));
        }

        var core = EnvelopePoints(wgsPoints);
        return TileMath.ClampBounds(core);
    }

    private CoordinateSystem ResolveCoordinateSystem(double centralMeridian, int zoneWidthDegrees)
    {
        var zone = NormalizeZoneWidth(zoneWidthDegrees);
        double cm;
        string wkt;

        if (zone == 6)
        {
            cm = SnapToTm6CentralMeridian(centralMeridian);
            var key = (cm, 6);
            if (_csCache.TryGetValue(key, out var cached6))
                return cached6;

            wkt = BuildVn2000TmWkt(cm, scaleFactor: 0.9996, name: $"VN_2000_TM6_{cm.ToString(CultureInfo.InvariantCulture)}");
            var cs6 = _csFactory.CreateFromWkt(wkt) as CoordinateSystem
                      ?? throw new InvalidOperationException($"WKT TM-6 không hợp lệ (CM={cm}).");
            _csCache[key] = cs6;
            return cs6;
        }

        var entry = ResolveByCentralMeridian(centralMeridian);
        cm = entry.CentralMeridian;
        var key3 = (cm, 3);
        if (_csCache.TryGetValue(key3, out var cached3))
            return cached3;

        var cs3 = _csFactory.CreateFromWkt(entry.Wkt) as CoordinateSystem
                  ?? throw new InvalidOperationException($"WKT không hợp lệ: {entry.Name}");
        _csCache[key3] = cs3;
        return cs3;
    }

    /// <summary>WKT VN-2000 Transverse Mercator (cùng GEOGCS/TOWGS84 với bộ TM-3).</summary>
    public static string BuildVn2000TmWkt(double centralMeridian, double scaleFactor, string name)
    {
        var cm = centralMeridian.ToString("0.###", CultureInfo.InvariantCulture);
        var sf = scaleFactor.ToString("0.####", CultureInfo.InvariantCulture);
        return
            $"PROJCS[\"{name}\"," +
            "GEOGCS[\"GCS_VN_2000\"," +
            "DATUM[\"D_Vietnam_2000\"," +
            "SPHEROID[\"WGS_1984\",6378137.0,298.257223563]," +
            $"TOWGS84[{Towgs84}]]," +
            "PRIMEM[\"Greenwich\",0]," +
            "UNIT[\"Degree\",0.0174532925199433]]," +
            "PROJECTION[\"Transverse_Mercator\"]," +
            "PARAMETER[\"False_Easting\",500000]," +
            "PARAMETER[\"False_Northing\",0]," +
            $"PARAMETER[\"Central_Meridian\",{cm}]," +
            $"PARAMETER[\"Scale_Factor\",{sf}]," +
            "PARAMETER[\"Latitude_Of_Origin\",0]," +
            "UNIT[\"Meter\",1]]";
    }

    private static GeoBounds EnvelopePoints(IEnumerable<GeoPoint> points)
    {
        var list = points.ToList();
        return new GeoBounds(
            list.Min(p => p.Longitude),
            list.Min(p => p.Latitude),
            list.Max(p => p.Longitude),
            list.Max(p => p.Latitude));
    }
}
