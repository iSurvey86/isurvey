namespace iSurvey.Modules.DiemDo;

/// <summary>Cách tách cột và dấu thập phân của một tệp tọa độ.</summary>
public readonly struct PointFileFormat
{
    public PointFileFormat(char delimiter, bool decimalComma)
    {
        Delimiter = delimiter;
        DecimalComma = decimalComma;
    }

    /// <summary>Ký tự phân cách. Khoảng trắng dùng '\0'.</summary>
    public char Delimiter { get; }

    public bool DecimalComma { get; }

    public bool IsWhitespace => Delimiter == '\0';

    public string DelimiterLabel => Delimiter switch
    {
        '\0' => "khoảng trắng",
        ';' => "chấm phẩy",
        ',' => "phẩy",
        '\t' => "tab",
        ' ' => "dấu cách",
        _ => Delimiter.ToString()
    };

    public string DecimalLabel => DecimalComma ? "phẩy" : "chấm";

    /// <summary>Ghi tệp: bỏ chọn thì chấm phẩy và thập phân là chấm. Phân cách là phẩy thì thập phân vẫn là chấm.</summary>
    public static bool TryForWrite(bool custom, string? customText, out PointFileFormat format, out string error)
    {
        format = default;
        error = "";
        if (!custom)
        {
            format = new PointFileFormat(';', decimalComma: false);
            return true;
        }

        if (string.IsNullOrEmpty(customText))
        {
            error = "Nhập một ký tự phân cách.";
            return false;
        }

        var delimiter = customText[0];
        format = new PointFileFormat(delimiter, decimalComma: false);
        return true;
    }
}
