namespace iSurvey.Models;

/// <summary>Tuỳ chọn xuất bản vẽ sang KML / KMZ.</summary>
public sealed class KmlExportSettings
{
    public double CentralMeridian { get; set; } = 105;

    /// <summary>3 = TM-3 (mặc định); 6 = TM-6.</summary>
    public int ZoneWidthDegrees { get; set; } = 3;

    /// <summary>True = chỉ đối tượng đang chọn / sẽ chọn; False = toàn bộ Model Space.</summary>
    public bool SelectionOnly { get; set; } = true;

    /// <summary>True = KMZ (mặc định); False = KML.</summary>
    public bool UseKmz { get; set; } = true;

    public bool GroupByLayer { get; set; } = true;

    public bool UseElevationZ { get; set; }

    public bool OpenAfterExport { get; set; } = true;

    public string OutputPath { get; set; } = string.Empty;
}

/// <summary>Một hình học đã chuyển sang WGS84, sẵn sàng ghi KML.</summary>
public sealed class KmlFeature
{
    public required string LayerName { get; init; }
    public required KmlGeometryKind Kind { get; init; }
    public required IReadOnlyList<GeoPoint3> Points { get; init; }

    /// <summary>Màu KML dạng aabbggrr (hex).</summary>
    public required string ColorKml { get; init; }

    public string? Name { get; init; }
    public string? Description { get; init; }
    public bool Closed { get; init; }

    /// <summary>Ẩn icon mặc định (pin vàng) — dùng cho text / điểm.</summary>
    public bool HideIcon { get; init; }

    /// <summary>Độ rộng nét (pixel GE), LineString.</summary>
    public double LineWidth { get; init; } = 2.0;
}

public enum KmlGeometryKind
{
    Point,
    LineString,
    /// <summary>Chỉ dùng khi thật sự cần vùng; mặc định xuất viền bằng LineString.</summary>
    Polygon
}

/// <summary>Điểm WGS84 kèm cao độ mét (có thể 0).</summary>
public readonly record struct GeoPoint3(double Longitude, double Latitude, double Elevation);
