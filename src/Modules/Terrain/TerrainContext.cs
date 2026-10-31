namespace iSurvey.Modules.Terrain;

/// <summary>Bảng Khảo sát ghi mô hình đang chọn trước khi gửi lệnh.</summary>
public static class TerrainContext
{
    public static string? ModelId { get; set; }

    /// <summary>point, contour hoặc boundary.</summary>
    public static string? AddKind { get; set; }

    public static string? TakeModel()
    {
        var id = ModelId;
        ModelId = null;
        return id;
    }

    public static string? TakeKind()
    {
        var kind = AddKind;
        AddKind = null;
        return kind;
    }
}
