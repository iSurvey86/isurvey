namespace iSurvey.Modules.Profile;

/// <summary>Một hàng đầu bảng. Hàng dưới cùng phải là «Khoảng cách cộng dồn» để RAIMCD chọn góc.</summary>
public sealed class ProfileBandRow
{
    public string Key { get; init; } = "";

    public string Title { get; init; } = "";

    /// <summary>
    /// Năm hàng mặc định, từ dưới lên: cộng dồn → lẻ → tên → lý trình → cao độ.
    /// RAIMCD bấm góc dưới-trái hàng cộng dồn.
    /// </summary>
    public static IReadOnlyList<ProfileBandRow> DefaultRows { get; } =
    [
        new() { Key = "cong_don", Title = "Khoảng cách cộng dồn" },
        new() { Key = "le", Title = "Khoảng cách lẻ" },
        new() { Key = "ten", Title = "Tên cọc" },
        new() { Key = "ly_trinh", Title = "Lý trình" },
        new() { Key = "cao_do", Title = "Cao độ tự nhiên" }
    ];
}
