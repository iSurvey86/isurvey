using System.Text.Json.Serialization;

namespace iSurvey.Models;

/// <summary>Nhóm tỉnh/thành và các khu vực CRS VN-2000 liên quan.</summary>
public sealed class ProvinceCrsGroup
{
    [JsonPropertyName("provinceName")]
    public string ProvinceName { get; set; } = string.Empty;

    [JsonPropertyName("legacyAreas")]
    public List<LegacyAreaEntry> LegacyAreas { get; set; } = [];
}

/// <summary>Một khu vực hành chính cũ gắn với kinh tuyến trục TM-3.</summary>
public sealed class LegacyAreaEntry
{
    [JsonPropertyName("sourceProvinceKey")]
    public string SourceProvinceKey { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;
}

/// <summary>Thông tin người dùng chọn trên hộp thoại chèn bản đồ.</summary>
public sealed class MapInsertSettings
{
    public required string ProvinceName { get; init; }
    public required string AreaLabel { get; init; }
    public required double CentralMeridian { get; init; }
    /// <summary>3 = TM-3; 6 = TM-6.</summary>
    public int ZoneWidthDegrees { get; init; } = 3;
    public required string BasemapId { get; init; }
    public bool AutoRefresh { get; init; } = true;
    public bool UseBoundaryClip { get; init; }
    public MapInsertAction Action { get; init; } = MapInsertAction.Insert;
}
