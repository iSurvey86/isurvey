using System.Text;
using System.Text.RegularExpressions;

namespace iSurvey.Dgn;

/// <summary>
/// Chuyển chuỗi TCVN3 (ABC) ➔ Unicode. DGNIMPORT đọc byte TCVN3 như ký tự Latin-1
/// (ví dụ 0xB7 ➔ "·", 0xE9 ➔ "é"), nên nhận diện theo NỘI DUNG chuỗi, không theo tên font.
/// </summary>
internal static class Tcvn3Converter
{
    // Mã byte TCVN3 (song song với chuỗi ký tự Unicode bên dưới).
    private static readonly byte[] Codes =
    [
        0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE,
        0xB5, 0xB6, 0xB7, 0xB8, 0xB9,
        0xBB, 0xBC, 0xBD, 0xBE, 0xC6,
        0xC7, 0xC8, 0xC9, 0xCA, 0xCB,
        0xCC, 0xCE, 0xCF, 0xD0, 0xD1,
        0xD2, 0xD3, 0xD4, 0xD5, 0xD6,
        0xD7, 0xD8, 0xDC, 0xDD, 0xDE,
        0xDF, 0xE1, 0xE2, 0xE3, 0xE4,
        0xE5, 0xE6, 0xE7, 0xE8, 0xE9,
        0xEA, 0xEB, 0xEC, 0xED, 0xEE,
        0xEF, 0xF1, 0xF2, 0xF3, 0xF4,
        0xF5, 0xF6, 0xF7, 0xF8, 0xF9,
        0xFA, 0xFB, 0xFC, 0xFD, 0xFE
    ];

    private const string Chars =
        "ĂÂÊÔƠƯĐăâêôơưđ" +
        "àảãáạ" + "ằẳẵắặ" + "ầẩẫấậ" +
        "èẻẽéẹ" + "ềểễếệ" +
        "ìỉĩíị" +
        "òỏõóọ" + "ồổỗốộ" + "ờởỡớợ" +
        "ùủũúụ" + "ừửữứự" +
        "ỳỷỹýỵ";

    private static readonly Dictionary<char, char> Map = BuildMap();

    // Font chữ HOA kiểu .VnTimeH / .VnArialH: cùng mã byte nhưng hiển thị chữ hoa.
    private static readonly Regex UpperFontRegex = new(@"^\.?vn.*h$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public enum Kind
    {
        /// <summary>Không phải TCVN3 (đã Unicode, hoặc không có ký tự cần đổi).</summary>
        None,

        /// <summary>Chỉ có chữ Latin-1 ở dải 0xC0–0xFF (như "Sông" Unicode) — mơ hồ.</summary>
        Weak,

        /// <summary>Có ký tự dải 0xA1–0xBF (·¸µ¹«ª...) — gần như chắc chắn TCVN3.</summary>
        Strong
    }

    /// <summary>Font họ TCVN3 (.VnTime, .VnArial, .VnTimeH ...).</summary>
    public static bool IsTcvn3Font(string? typeface) =>
        !string.IsNullOrWhiteSpace(typeface) &&
        Regex.IsMatch(typeface!.Trim(), @"^\.?vn", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static bool IsUpperFont(string? typeface) =>
        !string.IsNullOrWhiteSpace(typeface) && UpperFontRegex.IsMatch(typeface!.Trim());

    /// <summary>
    /// Chuỗi kiểu "X· TUY LéC" (font .VnTimeH): có ≥2 chữ ASCII HOA và không có chữ ASCII thường
    /// ➔ coi là chữ hoa dù tên font đã bị DGNIMPORT đổi.
    /// </summary>
    public static bool LooksAllCaps(string text)
    {
        var upper = 0;
        foreach (var c in text)
        {
            if (c is >= 'a' and <= 'z')
                return false;
            if (c is >= 'A' and <= 'Z')
                upper++;
        }

        return upper >= 2;
    }

    // DGN V8: DGNIMPORT đổi byte 0xA1–0xBF của TCVN3 thành chữ "N/64" (N = 2·byte − 319, lẻ 3..63),
    // byte 0xD8 thành '∅' (U+2205) và byte 0xC8 thành '±'. Các byte ≥ 0xC0 còn lại giữ nguyên.
    private static readonly Regex V8FractionRegex = new(@"(\d{1,2})/64", RegexOptions.CultureInvariant);

    /// <summary>Chuỗi còn dấu vết kiểu V8: "N/64", '∅', '±'.</summary>
    public static bool HasV8Artifacts(string text) =>
        text.IndexOf('\u2205') >= 0 || text.IndexOf('\u00B1') >= 0 ||
        (text.Contains("/64") && V8FractionRegex.IsMatch(text));

    /// <summary>Khôi phục byte TCVN3 gốc từ các dấu vết do DGNIMPORT (V8) tạo ra.</summary>
    public static string DecodeV8Artifacts(string text)
    {
        if (text.Contains("/64"))
        {
            text = V8FractionRegex.Replace(text, m =>
            {
                var n = int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                if (n < 3 || n > 63 || (n & 1) == 0)
                    return m.Value;
                return ((char)((n + 319) / 2)).ToString();
            });
        }

        return text.Replace('\u2205', (char)0xD8).Replace('\u00B1', (char)0xC8);
    }

    public static Kind Classify(string text)
    {
        var strong = false;
        var weak = false;
        foreach (var c in text)
        {
            if (c > 0xFF)
                return Kind.None; // Unicode thật (ạ, ế, Đ...) ➔ giữ nguyên

            if (c >= 0xA1 && Map.ContainsKey(c))
            {
                if (c < 0xC0)
                    strong = true;
                else
                    weak = true;
            }
        }

        return strong ? Kind.Strong : weak ? Kind.Weak : Kind.None;
    }

    public static string ToUnicode(string text, bool upper)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (c >= 0xA1 && c <= 0xFF && Map.TryGetValue(c, out var u))
                sb.Append(upper ? char.ToUpperInvariant(u) : u);
            else
                sb.Append(c);
        }

        return sb.ToString();
    }

    private static Dictionary<char, char> BuildMap()
    {
        if (Codes.Length != Chars.Length)
            throw new InvalidOperationException("Bảng TCVN3 không khớp độ dài.");

        var map = new Dictionary<char, char>(Codes.Length);
        for (var i = 0; i < Codes.Length; i++)
            map[(char)Codes[i]] = Chars[i];
        return map;
    }
}
