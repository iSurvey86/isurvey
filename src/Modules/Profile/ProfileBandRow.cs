namespace iSurvey.Modules.Profile;

/// <summary>
/// Một hàng đầu bảng trắc dọc.
/// Thứ tự trong danh sách: từ trên xuống (gần đường đất → đáy bảng).
/// </summary>
public sealed class ProfileBandRow
{
    public string Key { get; set; } = "";

    /// <summary>Nhãn cột trái (đầu bảng).</summary>
    public string Label { get; set; } = "";

    /// <summary>Loại nội dung điền: cao_do, le, huong, cong_don, ly_trinh, ten, so_hoa, kc_100, neo, cot, empty…</summary>
    public string Content { get; set; } = "";

    public string Layer { get; set; } = iSurveyLayers.TdChu;

    /// <summary>Tên Text Style CAD. Mặc định iSurvey_TD (Arial Unicode Windows).</summary>
    public string TextStyle { get; set; } = ProfileTextStyles.StyleName;

    /// <summary>Màu ACI (1–255). 256 = ByLayer.</summary>
    public int ColorAci { get; set; } = 256;

    public double TextHeight { get; set; } = 7.5;

    /// <summary>Cao hàng (đơn vị bản vẽ = mét lý trình trên trục X).</summary>
    public double RowHeight { get; set; } = 50;

    public ProfileBandRow Clone() => new()
    {
        Key = Key,
        Label = Label,
        Content = Content,
        Layer = Layer,
        TextStyle = TextStyle,
        ColorAci = ColorAci,
        TextHeight = TextHeight,
        RowHeight = RowHeight
    };

    /// <summary>
    /// Mẫu mặc định Pha 3. Sơ họa 200; các hàng còn lại 50.
    /// </summary>
    public static IReadOnlyList<ProfileBandRow> DefaultRows { get; } =
    [
        new() { Key = "so_hoa", Label = "Sơ họa mặt bằng", Content = "so_hoa", Layer = iSurveyLayers.TdBang, TextStyle = ProfileTextStyles.StyleName, ColorAci = 7, TextHeight = 12, RowHeight = 200 },
        new() { Key = "cao_do", Label = "Độ cao tự nhiên", Content = "cao_do", Layer = iSurveyLayers.TdChu, TextStyle = ProfileTextStyles.StyleName, ColorAci = 7, TextHeight = 12, RowHeight = 50 },
        new() { Key = "le", Label = "Khoảng cách lẻ", Content = "le", Layer = iSurveyLayers.TdChu, TextStyle = ProfileTextStyles.StyleName, ColorAci = 7, TextHeight = 12, RowHeight = 50 },
        new() { Key = "huong", Label = "Hướng tuyến", Content = "huong", Layer = iSurveyLayers.TdChu, TextStyle = ProfileTextStyles.StyleName, ColorAci = 7, TextHeight = 12, RowHeight = 50 },
        new() { Key = "kc_100", Label = "Khoảng cách 100m", Content = "kc_100", Layer = iSurveyLayers.TdChu, TextStyle = ProfileTextStyles.StyleName, ColorAci = 7, TextHeight = 12, RowHeight = 50 },
        new() { Key = "cong_don", Label = "Khoảng cách cộng dồn", Content = "cong_don", Layer = iSurveyLayers.TdChu, TextStyle = ProfileTextStyles.StyleName, ColorAci = 7, TextHeight = 12, RowHeight = 50 },
        new() { Key = "neo", Label = "Khoảng néo", Content = "neo", Layer = iSurveyLayers.TdChu, TextStyle = ProfileTextStyles.StyleName, ColorAci = 7, TextHeight = 12, RowHeight = 50 },
        new() { Key = "cot", Label = "Khoảng cột", Content = "cot", Layer = iSurveyLayers.TdChu, TextStyle = ProfileTextStyles.StyleName, ColorAci = 7, TextHeight = 12, RowHeight = 50 }
    ];

    public static List<ProfileBandRow> CreateDefaultList()
    {
        var list = new List<ProfileBandRow>(DefaultRows.Count);
        foreach (var row in DefaultRows)
            list.Add(row.Clone());
        return list;
    }

    public static double TotalHeight(IReadOnlyList<ProfileBandRow> rows)
    {
        var sum = 0.0;
        foreach (var row in rows)
            sum += Math.Max(1, row.RowHeight);
        return sum;
    }
}
