using System.Globalization;
using System.Text;

namespace iSurvey.Modules.DiemDo;

public static class PointFileWriter
{
    public static void Write(
        string path,
        IReadOnlyList<SurveyPoint> points,
        PointFileFormat format,
        int decimals,
        bool swapAxes)
    {
        var ordered = points
            .OrderBy(point => point.Name, NameOrder.Instance)
            .ThenBy(point => swapAxes ? point.Y : point.X)
            .ThenBy(point => swapAxes ? point.X : point.Y)
            .ThenBy(point => point.Z)
            .ToList();
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        using var writer = new StreamWriter(path, false, encoding);
        writer.WriteLine(string.Join(Separator(format), "Ten", "X", "Y", "Z", "Ma"));
        foreach (var point in ordered)
        {
            var x = swapAxes ? point.Y : point.X;
            var y = swapAxes ? point.X : point.Y;
            writer.WriteLine(string.Join(Separator(format),
                Cell(point.Name, format),
                Number(x, decimals, format),
                Number(y, decimals, format),
                Number(point.Z, decimals, format),
                Cell(point.Code, format)));
        }
    }

    private static string Separator(PointFileFormat format) =>
        format.IsWhitespace ? " " : format.Delimiter.ToString();

    public static string Number(double value, int decimals, PointFileFormat format)
    {
        var digits = Math.Max(0, decimals);
        var rounded = Math.Round(value, digits, MidpointRounding.AwayFromZero);
        var pattern = digits == 0 ? "0" : "0." + new string('0', digits);
        var text = rounded.ToString(pattern, CultureInfo.InvariantCulture);
        return format.DecimalComma ? text.Replace('.', ',') : text;
    }

    private static string Cell(string text, PointFileFormat format)
    {
        var value = text ?? "";
        var delimiter = format.IsWhitespace ? ' ' : format.Delimiter;
        if (value.Contains(delimiter) || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    /// <summary>Số đứng trước chữ. Số so theo giá trị (2 trước 10). Chữ so A-Z, không phân biệt hoa thường.</summary>
    internal static int CompareNames(string? left, string? right)
    {
        var a = left ?? "";
        var b = right ?? "";
        var i = 0;
        var j = 0;
        while (i < a.Length && j < b.Length)
        {
            var aDigit = char.IsDigit(a[i]);
            var bDigit = char.IsDigit(b[j]);
            if (aDigit != bDigit)
                return aDigit ? -1 : 1;

            if (aDigit)
            {
                var aStart = i;
                var bStart = j;
                while (i < a.Length && char.IsDigit(a[i])) i++;
                while (j < b.Length && char.IsDigit(b[j])) j++;
                var aNum = Significant(a.Substring(aStart, i - aStart));
                var bNum = Significant(b.Substring(bStart, j - bStart));
                var byLength = aNum.Length.CompareTo(bNum.Length);
                if (byLength != 0)
                    return byLength;
                var byDigits = string.CompareOrdinal(aNum, bNum);
                if (byDigits != 0)
                    return byDigits;
            }
            else
            {
                var aStart = i;
                var bStart = j;
                while (i < a.Length && !char.IsDigit(a[i])) i++;
                while (j < b.Length && !char.IsDigit(b[j])) j++;
                var byText = string.Compare(
                    a.Substring(aStart, i - aStart),
                    b.Substring(bStart, j - bStart),
                    CultureInfo.CurrentCulture,
                    CompareOptions.IgnoreCase);
                if (byText != 0)
                    return byText;
            }
        }

        return (a.Length - i).CompareTo(b.Length - j);
    }

    private static string Significant(string digits)
    {
        var trimmed = digits.TrimStart('0');
        return trimmed.Length == 0 ? "0" : trimmed;
    }

    private sealed class NameOrder : IComparer<string>
    {
        public static readonly NameOrder Instance = new();

        public int Compare(string? x, string? y) => CompareNames(x, y);
    }
}
