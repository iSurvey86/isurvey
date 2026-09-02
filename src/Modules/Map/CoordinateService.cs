using System.Reflection;
using System.Text.Json;
using iSurvey.Models;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace iSurvey.Modules.Map;

/// <summary>
/// Chuyển đổi tọa độ giữa VN2000 (theo kinh tuyến trục) và WGS84 bằng ProjNet + WKT có TOWGS84.
/// </summary>
public sealed class CoordinateService
{
    private readonly List<Vn2000CrsEntry> _entries;
    private readonly CoordinateSystemFactory _csFactory = new();
    private readonly CoordinateTransformationFactory _transformFactory = new();

    public CoordinateService()
    {
        _entries = LoadEntries();
    }

    /// <summary>Đọc danh sách CRS từ file JSON đi kèm assembly.</summary>
    private static List<Vn2000CrsEntry> LoadEntries()
    {
        var asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                     ?? AppContext.BaseDirectory;
        var path = Path.Combine(asmDir, "Data", "isurvey_vn2000_tm3.json");
        if (!File.Exists(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, "Data", "isurvey_vn2000_tm3.json");
        }

        if (!File.Exists(path))
            throw new FileNotFoundException("Không tìm thấy isurvey_vn2000_tm3.json", path);

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<Vn2000CrsEntry>>(json)
               ?? throw new InvalidOperationException("Không parse được isurvey_vn2000_tm3.json");
    }

    /// <summary>Chọn mục CRS có kinh tuyến trục gần nhất với giá trị người dùng nhập.</summary>
    public Vn2000CrsEntry ResolveByCentralMeridian(double centralMeridian)
    {
        return _entries
            .OrderBy(e => Math.Abs(e.CentralMeridian - centralMeridian))
            .First();
    }

    /// <summary>Chuyển điểm VN2000 (E, N) sang WGS84.</summary>
    public GeoPoint ToWgs84(PlanePoint plane, double centralMeridian)
    {
        var entry = ResolveByCentralMeridian(centralMeridian);
        var vn2000 = _csFactory.CreateFromWkt(entry.Wkt) as CoordinateSystem
                     ?? throw new InvalidOperationException($"WKT không hợp lệ: {entry.Name}");
        var wgs84 = GeographicCoordinateSystem.WGS84;

        var toGeo = _transformFactory.CreateFromCoordinateSystems(vn2000, wgs84);
        var result = toGeo.MathTransform.Transform(new[] { plane.Easting, plane.Northing });
        return new GeoPoint(result[0], result[1]);
    }

    /// <summary>Chuyển điểm WGS84 sang VN2000 (E, N).</summary>
    public PlanePoint ToVn2000(GeoPoint geo, double centralMeridian)
    {
        var entry = ResolveByCentralMeridian(centralMeridian);
        var vn2000 = _csFactory.CreateFromWkt(entry.Wkt) as CoordinateSystem
                     ?? throw new InvalidOperationException($"WKT không hợp lệ: {entry.Name}");
        var wgs84 = GeographicCoordinateSystem.WGS84;

        var toProjected = _transformFactory.CreateFromCoordinateSystems(wgs84, vn2000);
        var result = toProjected.MathTransform.Transform(new[] { geo.Longitude, geo.Latitude });
        return new PlanePoint(result[0], result[1]);
    }

    /// <summary>Biên độ WGS84 bao quanh các góc khung nhìn VN2000.</summary>
    public GeoBounds ViewportToWgs84Bounds(ViewportCorners corners, double centralMeridian)
    {
        var pts = new[]
        {
            ToWgs84(corners.BottomLeft, centralMeridian),
            ToWgs84(corners.BottomRight, centralMeridian),
            ToWgs84(corners.TopRight, centralMeridian),
            ToWgs84(corners.TopLeft, centralMeridian)
        };

        return EnvelopePoints(pts);
    }

    /// <summary>Biên WGS84 khung nhìn — góc + trung điểm cạnh (viewport xoay).</summary>
    public GeoBounds ViewportToWgs84BoundsThorough(ViewportCorners corners, double centralMeridian)
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
            wgsPoints.Add(ToWgs84(a, centralMeridian));
            wgsPoints.Add(ToWgs84(
                new PlanePoint((a.Easting + b.Easting) / 2.0, (a.Northing + b.Northing) / 2.0),
                centralMeridian));
        }

        return EnvelopePoints(wgsPoints);
    }

    /// <summary>
    /// Biên WGS84 từ mọi đỉnh + trung điểm cạnh polygon VN2000,
    /// mở rộng thêm 1 tile để không thiếu mép (đặc biệt cạnh xiên TM-3).
    /// </summary>
    public GeoBounds PolygonToWgs84Bounds(
        IReadOnlyList<Autodesk.AutoCAD.Geometry.Point2d> polygon,
        double centralMeridian)
    {
        if (polygon.Count < 3)
            throw new ArgumentException("Polygon cần ít nhất 3 đỉnh.", nameof(polygon));

        var wgsPoints = new List<GeoPoint>(polygon.Count * 2);
        for (var i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Count];
            wgsPoints.Add(ToWgs84(new PlanePoint(a.X, a.Y), centralMeridian));
            wgsPoints.Add(ToWgs84(new PlanePoint((a.X + b.X) / 2.0, (a.Y + b.Y) / 2.0), centralMeridian));
        }

        var core = EnvelopePoints(wgsPoints);
        return TileMath.ClampBounds(core);
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
