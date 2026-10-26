using System.Text.Json;
using System.Text.Json.Serialization;

namespace iSurvey.Modules.Frame;

/// <summary>Một ô thông tin của khung tên (thuộc tính block KB_A*). <paramref name="Auto"/> = lệnh tự điền, người dùng không sửa.</summary>
public sealed record FrameField(int Stt, string Tag, string Label, bool Auto = false);

public static class FrameFields
{
    /// <summary>Thứ tự và tên hiển thị khớp bảng trong hộp thoại DANTRANG. TY_LE không có trong bảng (tự điền 1/N).</summary>
    public static readonly FrameField[] All =
    [
        new(1, "CHU_DAU_TU", "Chủ đầu tư"),
        new(2, "DAI_DIEN_CDT", "Đại diện CĐT"),
        new(3, "DU_AN", "Dự án"),
        new(4, "DIA_DIEM", "Địa điểm"),
        new(5, "DON_VI_TK", "Đơn vị thiết kế"),
        new(6, "DIA_CHI_TK", "Địa chỉ ĐVTK"),
        new(7, "VAITRO_1", "Chức danh 1"),
        new(8, "TEN_1", "Họ tên 1"),
        new(9, "VAITRO_2", "Chức danh 2"),
        new(10, "TEN_2", "Họ tên 2"),
        new(11, "VAITRO_3", "Chức danh 3"),
        new(12, "TEN_3", "Họ tên 3"),
        new(13, "VAITRO_4", "Chức danh 4"),
        new(14, "TEN_4", "Họ tên 4"),
        new(15, "VAITRO_5", "Chức danh 5"),
        new(16, "TEN_5", "Họ tên 5"),
        new(17, "GIAI_DOAN", "Giai đoạn"),
        new(18, "HANG_MUC", "Hạng mục"),
        new(19, "TEN_BAN_VE", "Tên bản vẽ"),
        new(20, "NGAY_1", "Ngày xuất bản 1"),
        new(21, "NGAY_2", "Ngày xuất bản 2"),
        new(22, "NGAY_3", "Ngày xuất bản 3"),
        new(23, "NGAY_4", "Ngày xuất bản 4"),
        new(24, "TO", "Tờ", Auto: true),
        new(25, "SO_HIEU", "Số hiệu")
    ];

    /// <summary>Bỏ khoảng trắng đầu/cuối và gộp các chỗ có từ hai khoảng trắng/tab liền nhau thành một.</summary>
    public static string Clean(string? s) =>
        string.IsNullOrEmpty(s) ? "" : System.Text.RegularExpressions.Regex.Replace(s, @"[ \t\u00A0]{2,}", " ").Trim();

    public const string ScaleTag = "TY_LE";
    public const string SheetTag = "TO";
}

/// <summary>Kết quả hộp thoại (hoặc thiết lập đã lưu khi chạy không hộp thoại).</summary>
public sealed class RouteLayoutOptions
{
    public string Size { get; set; } = "A3";
    public int Scale { get; set; } = 1000;
    public double OverlapM { get; set; } = 50;
    public int Rows { get; set; } = 1;
    public int Cols { get; set; } = 1;
    public double GapMm { get; set; } = 20;
    public bool PlotPdf { get; set; } = true;

    /// <summary>Giá trị ô khung tên theo tag (đã gồm giá trị mặc định của khung mẫu).</summary>
    public Dictionary<string, string> Attributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Ảnh logo thay thế (null = giữ logo EVN).</summary>
    public string? LogoOwnerPath { get; set; }

    public string? LogoDesignPath { get; set; }

    /// <summary>Ẩn logo chủ đầu tư (STT 1) / đơn vị thiết kế (STT 5) — tắt layer logo của khung, áp cho cả bản vẽ.</summary>
    public bool HideOwnerLogo { get; set; }

    public bool HideDesignLogo { get; set; }
}

/// <summary>Thiết lập DANTRANG nhớ giữa các lần chạy (%AppData%\iSurvey\route_layout.json).</summary>
public sealed class RouteLayoutSettings
{
    public string Size { get; set; } = "A3";
    public int Scale { get; set; } = 1000;
    public double OverlapM { get; set; } = -1;
    public double GapMm { get; set; } = 20;
    public bool PlotPdf { get; set; } = true;

    /// <summary>Chỉ các ô người dùng đã sửa khác mặc định của khung mẫu.</summary>
    public Dictionary<string, string> Attributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonIgnore]
    private static string FilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "iSurvey", "route_layout.json");

    public static RouteLayoutSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var s = JsonSerializer.Deserialize<RouteLayoutSettings>(File.ReadAllText(FilePath));
                if (s is not null)
                {
                    s.Attributes = new Dictionary<string, string>(s.Attributes ?? [], StringComparer.OrdinalIgnoreCase);
                    return s;
                }
            }
        }
        catch
        {
            // file hỏng: dùng mặc định
        }

        return new RouteLayoutSettings();
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // không chặn lệnh
        }
    }
}


