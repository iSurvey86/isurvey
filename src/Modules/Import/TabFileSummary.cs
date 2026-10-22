namespace iSurvey.Modules.Import;

/// <summary>Thông tin đọc nhanh từ file TAB, chưa vẽ.</summary>
internal sealed class TabFileSummary
{
    public required string Path { get; init; }

    public int FeatureCount { get; init; }

    public int NullGeometryCount { get; init; }

    public required IReadOnlyList<string> Fields { get; init; }

    public string? DefaultLabelField { get; init; }

    /// <summary>True khi file là kinh/vĩ độ, phải chiếu sang VN-2000 của bản vẽ.</summary>
    public bool Geographic { get; init; }

    public bool HasProjection { get; init; }

    public double Lon0 { get; init; }

    public double Scale { get; init; } = 1;

    public double FalseEasting { get; init; }

    public double FalseNorthing { get; init; }

    public string SrsText { get; init; } = "không có hệ tọa độ";

    /// <summary>Hệ của bản vẽ, ví dụ "bản vẽ TM-3 kinh tuyến 105°".</summary>
    public string TargetText { get; init; } = "bản vẽ";

    public bool NeedsTransform { get; init; }

    public string StatusText { get; init; } = "";
}
