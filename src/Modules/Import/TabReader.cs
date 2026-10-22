using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using iSurvey.Models;
using iSurvey.Modules.Map;
using OSGeo.OGR;
using OSGeo.OSR;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace iSurvey.Modules.Import;

/// <summary>Đọc MapInfo TAB qua GDAL/OGR: trường, style brush, hình, hệ tọa độ.</summary>
internal static class TabReader
{
    private static readonly Regex Foreground = new(@"fc:#([0-9A-Fa-f]{6})", RegexOptions.Compiled);
    private static readonly Regex EmptyBrush = new(@"mapinfo-brush-1(?!\d)", RegexOptions.Compiled);

    public static TabFileSummary Probe(string path, MapUserSettings drawing)
    {
        TabGdal.Ensure();
        using var ds = Ogr.Open(path, 0) ?? throw new InvalidOperationException("GDAL không mở được file TAB.");
        using var layer = ds.GetLayerByIndex(0) ?? throw new InvalidOperationException("File TAB không có layer.");
        var fields = ReadFields(layer);
        var srs = DescribeSrs(layer.GetSpatialRef(), drawing);
        var count = (int)layer.GetFeatureCount(1);
        return new TabFileSummary
        {
            Path = path,
            FeatureCount = count,
            Fields = fields,
            DefaultLabelField = PickDefault(fields),
            Geographic = srs.Geographic,
            HasProjection = srs.HasProjection,
            Lon0 = srs.Lon0,
            Scale = srs.Scale,
            FalseEasting = srs.FalseEasting,
            FalseNorthing = srs.FalseNorthing,
            SrsText = srs.Text,
            TargetText = srs.TargetText,
            NeedsTransform = srs.NeedsTransform,
            StatusText = $"{count:N0} đối tượng. Hệ: {srs.Text}. {srs.CompareText}"
        };
    }

    /// <summary>Duyệt feature. Người gọi vẽ ngay và không giữ Geometry sau callback.</summary>
    public static void Read(string path, MapUserSettings drawing, bool transform, Action<TabFeature> onFeature)
    {
        TabGdal.Ensure();
        using var ds = Ogr.Open(path, 0) ?? throw new InvalidOperationException("GDAL không mở được file TAB.");
        using var layer = ds.GetLayerByIndex(0) ?? throw new InvalidOperationException("File TAB không có layer.");
        var fields = ReadFields(layer);
        var mdsd = IndexOf(fields, "MDSD");
        var srs = DescribeSrs(layer.GetSpatialRef(), drawing);
        Func<double, double, (double X, double Y)>? map = null;
        if (transform && srs.NeedsTransform)
            map = BuildTransform(srs, drawing);

        layer.ResetReading();
        while (true)
        {
            using var feature = layer.GetNextFeature();
            if (feature is null)
                break;

            // GetGeometryRef trả hình thuộc feature. Không Dispose hình này.
            var geom = feature.GetGeometryRef();
            if (geom is null)
            {
                onFeature(new TabFeature(null, "", "", true, 0, 0, 0, true));
                continue;
            }

            var style = feature.GetStyleString() ?? "";
            var noFill = EmptyBrush.IsMatch(style);
            byte r = 200, g = 200, b = 200;
            var color = Foreground.Match(style);
            if (color.Success)
            {
                var hex = color.Groups[1].Value;
                r = byte.Parse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            var code = mdsd >= 0 ? FieldString(feature, mdsd) : "";
            onFeature(new TabFeature(geom, code, style, noFill, r, g, b, false)
            {
                Map = map,
                FieldList = fields,
                Source = feature
            });
        }
    }

    internal static string FieldString(Feature feature, int index)
    {
        if (index < 0 || !feature.IsFieldSet(index))
            return "";
        return (feature.GetFieldAsString(index) ?? "").Trim();
    }

    internal static int IndexOf(IReadOnlyList<string> fields, string name)
    {
        for (var i = 0; i < fields.Count; i++)
        {
            if (string.Equals(fields[i], name, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    internal static string PickDefault(IReadOnlyList<string> fields)
    {
        var mdsd = IndexOf(fields, "MDSD");
        if (mdsd >= 0)
            return fields[mdsd];
        var khoanh = IndexOf(fields, "KHOANH");
        return khoanh >= 0 ? fields[khoanh] : (fields.Count > 0 ? fields[0] : "");
    }

    private static List<string> ReadFields(Layer layer)
    {
        var defn = layer.GetLayerDefn();
        var fields = new List<string>(defn.GetFieldCount());
        for (var i = 0; i < defn.GetFieldCount(); i++)
            fields.Add(defn.GetFieldDefn(i).GetName());
        return fields;
    }

    private static SrsInfo DescribeSrs(SpatialReference? srs, MapUserSettings drawing)
    {
        var zone = CoordinateService.NormalizeZoneWidth(drawing.ZoneWidthDegrees);
        var targetK = zone == 6 ? 0.9996 : 0.9999;
        var target = $"bản vẽ TM-{zone} kinh tuyến {drawing.CentralMeridian.ToString("0.###", CultureInfo.InvariantCulture)}°";

        if (srs is null)
        {
            return new SrsInfo
            {
                Text = "không khai báo",
                TargetText = target,
                CompareText = $"Không chuyển hệ ({target})."
            };
        }

        var geographic = srs.IsGeographic() == 1;
        var projected = srs.IsProjected() == 1;
        var lon0 = FirstParm(srs, 0, "central_meridian", "longitude_of_origin", "lon_0");
        var scale = FirstParm(srs, geographic ? 1 : 0.9999, "scale_factor", "scale_factor_at_natural_origin");
        var fe = FirstParm(srs, 500000, "false_easting");
        var fn = FirstParm(srs, 0, "false_northing");

        if (geographic)
        {
            return new SrsInfo
            {
                Geographic = true,
                HasProjection = false,
                Text = "kinh/vĩ độ WGS84",
                TargetText = target,
                NeedsTransform = true,
                CompareText = $"Sẽ chiếu sang {target}."
            };
        }

        if (!projected)
        {
            return new SrsInfo
            {
                Text = "hệ lạ",
                TargetText = target,
                CompareText = $"Không chuyển hệ ({target})."
            };
        }

        var same = Math.Abs(lon0 - drawing.CentralMeridian) < 1e-4
                   && Math.Abs(scale - targetK) < 1e-6
                   && Math.Abs(fe - 500000) < 1
                   && Math.Abs(fn) < 1;
        var text = $"TM {lon0.ToString("0.###", CultureInfo.InvariantCulture)}° k={scale.ToString("0.####", CultureInfo.InvariantCulture)}";
        return new SrsInfo
        {
            HasProjection = true,
            Lon0 = lon0,
            Scale = scale,
            FalseEasting = fe,
            FalseNorthing = fn,
            Text = text,
            TargetText = target,
            NeedsTransform = !same,
            CompareText = same
                ? $"Khớp {target}, vẽ thẳng."
                : $"Khác {target}, sẽ chuyển hệ."
        };
    }

    private static double FirstParm(SpatialReference srs, double fallback, params string[] names)
    {
        foreach (var name in names)
        {
            try
            {
                var value = srs.GetProjParm(name, double.NaN);
                if (!double.IsNaN(value))
                    return value;
            }
            catch
            {
                // tham số không có trong SRS
            }
        }

        return fallback;
    }

    private static Func<double, double, (double X, double Y)> BuildTransform(SrsInfo srs, MapUserSettings drawing)
    {
        var coordinates = new CoordinateService();
        var zone = CoordinateService.NormalizeZoneWidth(drawing.ZoneWidthDegrees);
        if (srs.Geographic)
        {
            return (x, y) =>
            {
                var plane = coordinates.ToVn2000(new GeoPoint(x, y), drawing.CentralMeridian, zone);
                return (plane.Easting, plane.Northing);
            };
        }

        var targetK = zone == 6 ? 0.9996 : 0.9999;
        var factory = new CoordinateSystemFactory();
        var transforms = new CoordinateTransformationFactory();
        var source = (CoordinateSystem)factory.CreateFromWkt(TmWkt(srs.Lon0, srs.Scale, srs.FalseEasting, srs.FalseNorthing));
        var target = (CoordinateSystem)factory.CreateFromWkt(TmWkt(drawing.CentralMeridian, targetK, 500000, 0));
        var math = transforms.CreateFromCoordinateSystems(source, target).MathTransform;
        return (x, y) =>
        {
            var p = math.Transform(new[] { x, y });
            return (p[0], p[1]);
        };
    }

    /// <summary>TM trên ellipsoid WGS84, không TOWGS84, để chỉ đổi tham số chiếu.</summary>
    private static string TmWkt(double lon, double scale, double falseEasting, double falseNorthing)
    {
        var inv = CultureInfo.InvariantCulture;
        return new StringBuilder()
            .Append("PROJCS[\"TAB_TM\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563]],")
            .Append("PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.0174532925199433]],")
            .Append("PROJECTION[\"Transverse_Mercator\"],")
            .Append("PARAMETER[\"False_Easting\",").Append(falseEasting.ToString("0.###", inv)).Append("],")
            .Append("PARAMETER[\"False_Northing\",").Append(falseNorthing.ToString("0.###", inv)).Append("],")
            .Append("PARAMETER[\"Central_Meridian\",").Append(lon.ToString("0.######", inv)).Append("],")
            .Append("PARAMETER[\"Scale_Factor\",").Append(scale.ToString("0.######", inv)).Append("],")
            .Append("PARAMETER[\"Latitude_Of_Origin\",0],UNIT[\"Meter\",1]]")
            .ToString();
    }

    private sealed class SrsInfo
    {
        public bool Geographic { get; init; }
        public bool HasProjection { get; init; }
        public double Lon0 { get; init; }
        public double Scale { get; init; } = 1;
        public double FalseEasting { get; init; }
        public double FalseNorthing { get; init; }
        public string Text { get; init; } = "";
        public bool NeedsTransform { get; init; }
        public string TargetText { get; init; } = "";

        public string CompareText { get; init; } = "";
    }
}

/// <summary>Một feature đang được callback giữ. Geometry thuộc feature, đừng Dispose riêng.</summary>
internal sealed class TabFeature
{
    public TabFeature(Geometry? geometry, string mdsd, string style, bool noFill, byte r, byte g, byte b, bool missingGeometry)
    {
        Geometry = geometry;
        Mdsd = mdsd;
        Style = style;
        NoFill = noFill;
        R = r;
        G = g;
        B = b;
        MissingGeometry = missingGeometry;
    }

    public Geometry? Geometry { get; }
    public string Mdsd { get; }
    public string Style { get; }
    public bool NoFill { get; }
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public bool MissingGeometry { get; }

    /// <summary>Gán trong Read để lấy nhãn mà không expose Feature ra ngoài vòng Dispose.</summary>
    public Feature? Source { get; init; }

    public Func<double, double, (double X, double Y)>? Map { get; init; }

    public IReadOnlyList<string>? FieldList { get; init; }

    public string Label(string fieldName)
    {
        if (Source is null || FieldList is null)
            return "";
        return TabReader.FieldString(Source, TabReader.IndexOf(FieldList, fieldName));
    }
}
