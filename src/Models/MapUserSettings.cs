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

    public bool ImportInsertPoints { get; set; } = true;

    public bool ImportLabelNames { get; set; } = true;

    /// <summary>Cỡ chữ ghi chú điểm, đơn vị bản vẽ.</summary>
    public double ImportTextHeight { get; set; } = 2.0;

    /// <summary>Tắt mặc định: giữ 100% đỉnh gốc của ranh giới.</summary>
    public bool ImportSimplifyVertices { get; set; }

    /// <summary>Sai số Douglas-Peucker, mét trên mặt phẳng VN-2000.</summary>
    public double ImportSimplifyTolerance { get; set; } = 0.2;

    public bool ExportSelectionOnly { get; set; } = true;

    public bool ExportUseKmz { get; set; } = true;

    public bool ExportGroupByLayer { get; set; } = true;

    public bool ExportUseElevationZ { get; set; }

    public bool ExportOpenAfter { get; set; } = true;
}

/// <summary>Hành động từ hộp thoại chèn bản đồ.</summary>
public enum MapInsertAction
{
    Insert,
    DeleteGoogleEarth
}
