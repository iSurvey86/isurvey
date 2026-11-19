using iSurvey.Models;

namespace iSurvey.Dgn;

/// <summary>Cách đặt tên layer sau khi nhập DGN.</summary>
public enum DgnLayerMode
{
    /// <summary>Giữ nguyên tên layer do DGNIMPORT sinh ra (Level gốc).</summary>
    Keep,

    /// <summary>Thêm tiền tố (mặc định iSurvey_) vào layer mới.</summary>
    Prefix,

    /// <summary>Ánh xạ Level ➔ Layer theo dgn_layer_mapping.json (địa chính VN).</summary>
    Map
}

/// <summary>Batch: xử lý khi .dwg đích đã tồn tại.</summary>
public enum DgnBatchExisting
{
    /// <summary>Đánh số tiếp (tên_1.dwg, tên_2.dwg...).</summary>
    Number,

    /// <summary>Bỏ qua file DGN đó.</summary>
    Skip,

    /// <summary>Ghi đè .dwg cũ.</summary>
    Overwrite
}

/// <summary>Tùy chọn của một lần nhập DGN (ghép từ hộp thoại và <see cref="MapUserSettings"/>).</summary>
public sealed class DgnImportOptions
{
    public string FilePath { get; set; } = string.Empty;

    /// <summary>true = "_Sub" (đơn vị phụ); false = "_Master" (đơn vị chính).</summary>
    public bool UseSubUnits { get; set; }

    public DgnLayerMode LayerMode { get; set; } = DgnLayerMode.Keep;

    public string LayerPrefix { get; set; } = "iSurvey_";

    public bool FixFonts { get; set; } = true;

    public bool ExplodeCells { get; set; }

    public bool PurgeJunk { get; set; } = true;

    public bool ZoomToImport { get; set; } = true;

    public int ImportMode { get; set; } = 2;

    /// <summary>Danh sách file cho chế độ Batch (rỗng = nhập một file vào bản vẽ hiện hành).</summary>
    public IReadOnlyList<string> BatchFiles { get; set; } = [];

    public DgnBatchExisting BatchExisting { get; set; } = DgnBatchExisting.Number;

    /// <summary>true = ghép tất cả vào Model Space bản vẽ hiện hành; false = mỗi file một .dwg riêng.</summary>
    public bool BatchCombine { get; set; }

    public bool IsBatch => BatchFiles.Count > 0;

    public static DgnBatchExisting ParseBatchExisting(string? text) => text?.Trim().ToLowerInvariant() switch
    {
        "skip" => DgnBatchExisting.Skip,
        "overwrite" => DgnBatchExisting.Overwrite,
        _ => DgnBatchExisting.Number
    };

    public static string BatchExistingToText(DgnBatchExisting v) => v switch
    {
        DgnBatchExisting.Skip => "Skip",
        DgnBatchExisting.Overwrite => "Overwrite",
        _ => "Number"
    };

    public DgnImportOptions Clone() => (DgnImportOptions)MemberwiseClone();

    public string Command { get; set; } = "_.-DGNIMPORT";

    /// <summary>
    /// Trình tự -DGNIMPORT khi FILEDIA=0: tên file ➔ model ➔ đơn vị ➔ mapping.
    /// Gửi "Default" (không phải Enter/space): prompt model là chuỗi tự do — space/_Master bị nhận
    /// như tên model. File V7 phu cuong luôn hỏi model; "Default" là lựa chọn chuẩn.
    /// </summary>
    public IReadOnlyList<string> CommandArgs { get; set; } = ["{path}", "Default", "{units}", "{mapping}"];

    /// <summary>Rỗng ➔ "Standard" khi gọi lệnh (không gửi Enter trống).</summary>
    public string MappingSetup { get; set; } = "Standard";

    public static DgnImportOptions FromSettings(MapUserSettings s, string filePath) => new()
    {
        FilePath = filePath,
        UseSubUnits = string.Equals(s.DgnUnits, "Sub", StringComparison.OrdinalIgnoreCase),
        LayerMode = ParseLayerMode(s.DgnLayerMode),
        LayerPrefix = string.IsNullOrWhiteSpace(s.DgnLayerPrefix) || s.DgnLayerPrefix.Trim() == "DGN_"
            ? "iSurvey_"
            : s.DgnLayerPrefix.Trim(),
        FixFonts = s.DgnFixFonts,
        ExplodeCells = s.DgnExplodeCells,
        PurgeJunk = s.DgnPurgeLinetypes,
        ZoomToImport = s.DgnZoomToImport,
        ImportMode = s.DgnImportMode,
        Command = string.IsNullOrWhiteSpace(s.DgnCommand) ? "_.-DGNIMPORT" : s.DgnCommand.Trim(),
        CommandArgs = s.DgnCommandArgs is { Count: > 0 }
            ? s.DgnCommandArgs.ToList()
            : ["{path}", "Default", "{units}", "{mapping}"],
        MappingSetup = NormalizeMapping(s.DgnMappingSetup)
    };

    /// <summary>Rỗng / _Standard ➔ "Standard" (gửi rõ tên mapping, không Enter trống).</summary>
    private static string NormalizeMapping(string? value)
    {
        var v = value?.Trim() ?? string.Empty;
        if (v.Length == 0
            || v.Equals("_Standard", StringComparison.OrdinalIgnoreCase)
            || v.Equals("Standard", StringComparison.OrdinalIgnoreCase))
            return "Standard";
        return v;
    }

    public static DgnLayerMode ParseLayerMode(string? text) => text?.Trim().ToLowerInvariant() switch
    {
        "prefix" => DgnLayerMode.Prefix,
        "map" => DgnLayerMode.Map,
        _ => DgnLayerMode.Keep
    };

    public static string LayerModeToText(DgnLayerMode mode) => mode switch
    {
        DgnLayerMode.Prefix => "Prefix",
        DgnLayerMode.Map => "Map",
        _ => "Keep"
    };
}
