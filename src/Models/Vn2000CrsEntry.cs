using System.Text.Json.Serialization;

namespace iSurvey.Models;

/// <summary>Một mục hệ tọa độ VN2000 múi chiếu 3 độ từ dữ liệu JSON.</summary>
public sealed class Vn2000CrsEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("sourceProvinceKey")]
    public string SourceProvinceKey { get; set; } = string.Empty;

    [JsonPropertyName("centralMeridian")]
    public double CentralMeridian { get; set; }

    [JsonPropertyName("scaleFactor")]
    public double ScaleFactor { get; set; }

    [JsonPropertyName("falseEasting")]
    public double FalseEasting { get; set; }

    [JsonPropertyName("falseNorthing")]
    public double FalseNorthing { get; set; }

    [JsonPropertyName("wkt")]
    public string Wkt { get; set; } = string.Empty;
}

/// <summary>Nguồn ảnh nền bản đồ (Google Satellite, ...).</summary>
public sealed class MapSourceEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string UrlTemplate { get; set; } = string.Empty;
}
