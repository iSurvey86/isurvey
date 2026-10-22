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

    // ---------- DGN ➔ CAD (Giai đoạn 1) ----------

    /// <summary>"Master" (đơn vị chính) hoặc "Sub" (đơn vị phụ) — truyền cho prompt đơn vị của DGNIMPORT.</summary>
    public string DgnUnits { get; set; } = "Master";

    /// <summary>"Keep" (giữ Level gốc), "Prefix" (thêm tiền tố) hoặc "Map" (bảng ánh xạ JSON).</summary>
    public string DgnLayerMode { get; set; } = "Keep";

    public string DgnLayerPrefix { get; set; } = "DGN_";

    /// <summary>Chuyển mã TCVN3 ➔ Unicode (Arial) cho DBText/MText vừa nhập.</summary>
    public bool DgnFixFonts { get; set; } = true;

    /// <summary>Explode các cell (BlockReference) vừa nhập thành đối tượng CAD thường.</summary>
    public bool DgnExplodeCells { get; set; }

    /// <summary>Xóa linetype/layer/block rỗng do DGN sinh ra.</summary>
    public bool DgnPurgeLinetypes { get; set; } = true;

    public bool DgnZoomToImport { get; set; } = true;

    public string DgnLastFolder { get; set; } = string.Empty;

    /// <summary>Batch: "Combine" (ghép vào bản vẽ hiện hành) hoặc "Separate" (mỗi file một .dwg).</summary>
    public string DgnBatchMode { get; set; } = "Combine";

    /// <summary>Batch: khi file .dwg đích đã tồn tại — "Number" (đánh số _1, _2...), "Skip" (bỏ qua), "Overwrite" (ghi đè).</summary>
    public string DgnBatchExisting { get; set; } = "Number";

    /// <summary>Giá trị DGNIMPORTMODE đặt khi nhập (2 = thẳng vào bản vẽ hiện hành); -1 = không đụng tới biến này.</summary>
    public int DgnImportMode { get; set; } = 2;

    /// <summary>Lệnh gọi import. Có thể chỉnh trong settings.json nếu Civil khác trình tự prompt.</summary>
    public string DgnCommand { get; set; } = "_.-DGNIMPORT";

    /// <summary>Danh sách tham số theo thứ tự prompt; token {path} {units} {mapping}. "" = Enter.</summary>
    public List<string> DgnCommandArgs { get; set; } = ["{path}", "", "{units}", "{mapping}"];

    /// <summary>
    /// Tên mapping setup của DGNIMPORT. Rỗng = Enter (lấy &lt;Standard&gt;). Đây là tên tự do,
    /// KHÔNG phải keyword nên không dùng tiền tố "_" (Civil 2026 báo "_Standard not found").
    /// </summary>
    public string DgnMappingSetup { get; set; } = string.Empty;

    // ---------------------------------------------------------------- CAD ➔ DGN (C2D)

    /// <summary>Phiên bản DGN đích: "V8" (mặc định) hoặc "V7".</summary>
    public string DgnExportVersion { get; set; } = "V8";

    /// <summary>Phạm vi xuất: "All" (toàn bộ Model Space) hoặc "Selected" (đối tượng đang chọn).</summary>
    public string DgnExportScope { get; set; } = "All";

    /// <summary>Dọn rác (linetype/style/layer/block không dùng) trước khi xuất — thực hiện trên bản sao tạm, không đụng bản vẽ gốc.</summary>
    public bool DgnExportPurge { get; set; } = true;

    /// <summary>Ghi đè tự động nếu file DGN đích đã tồn tại.</summary>
    public bool DgnExportOverwrite { get; set; } = true;

    public string DgnExportLastFolder { get; set; } = string.Empty;

    /// <summary>"Master" hoặc "Sub" — prompt "Specify conversion units" của -DGNEXPORT.</summary>
    public string DgnExportUnits { get; set; } = "Master";

    /// <summary>Tên mapping setup của -DGNEXPORT. Rỗng = Enter (lấy &lt;Standard&gt;).</summary>
    public string DgnExportMappingSetup { get; set; } = string.Empty;

    /// <summary>Đường dẫn seed tự chọn (ghi đè seed đi kèm bundle); rỗng = dùng seed tự động.</summary>
    public string DgnExportSeedV8 { get; set; } = string.Empty;

    public string DgnExportSeedV7 { get; set; } = string.Empty;
}

/// <summary>Hành động từ hộp thoại chèn bản đồ.</summary>
public enum MapInsertAction
{
    Insert,
    DeleteGoogleEarth
}
