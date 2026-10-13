namespace iSurvey.Models;

/// <summary>Hộp thoại tỉnh / múi chiếu dùng cho nhập KML, xuất KML và chèn ảnh.</summary>
public enum Vn2000SetupMode
{
    Import,
    Export,
    InsertImage
}

/// <summary>Thông số người dùng vừa chốt. Hủy hộp thoại thì không có object này.</summary>
public sealed class Vn2000SetupResult
{
    public required string ProvinceName { get; init; }
    public required string AreaLabel { get; init; }

    /// <summary>Kinh tuyến người dùng nhập (độ + phút), trước khi snap múi 6°.</summary>
    public double EnteredMeridian { get; init; }

    /// <summary>Kinh tuyến dùng để chiếu. Múi 6° đã snap 105 / 111 / 117.</summary>
    public double CentralMeridian { get; init; }

    public int ZoneWidthDegrees { get; init; } = 3;

    public bool InsertPoints { get; init; } = true;
    public bool LabelNames { get; init; } = true;
    public double TextHeight { get; init; } = 2.0;
    public bool SimplifyVertices { get; init; }
    public double SimplifyTolerance { get; init; } = 0.2;

    public bool SelectionOnly { get; init; } = true;
    public bool UseKmz { get; init; } = true;
    public bool GroupByLayer { get; init; } = true;
    public bool UseElevationZ { get; init; }
    public bool OpenAfterExport { get; init; } = true;
    public string OutputPath { get; init; } = "";
}
