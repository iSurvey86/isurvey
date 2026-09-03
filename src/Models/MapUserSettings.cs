namespace iSurvey.Models;

/// <summary>Cài đặt người dùng — lưu tỉnh/kinh tuyến/loại basemap.</summary>
public sealed class MapUserSettings
{
    public string ProvinceName { get; set; } = string.Empty;

    public string AreaLabel { get; set; } = string.Empty;

    public double CentralMeridian { get; set; } = 105;

    /// <summary>3 = TM-3 (mặc định); 6 = TM-6.</summary>
    public int ZoneWidthDegrees { get; set; } = 3;

    public string BasemapId { get; set; } = "google-satellite-hybrid";

    public bool AutoRefresh { get; set; } = true;

    public bool UseBoundaryClip { get; set; }
}

/// <summary>Hành động từ hộp thoại chèn bản đồ.</summary>
public enum MapInsertAction
{
    Insert,
    DeleteGoogleEarth
}
