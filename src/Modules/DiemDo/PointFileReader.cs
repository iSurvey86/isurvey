using System.Globalization;
using System.Text;

namespace iSurvey.Modules.DiemDo;

public sealed class PointFileContent
{
    public List<SurveyPoint> Points { get; init; } = [];
}

/// <summary>
/// Đọc CSV/TXT trắc địa. X là số có 7 chữ số (Bắc), Y là số có 6 chữ số (Đông),
/// kể cả khi file đảo cột hoặc chèn mã điểm ở giữa.
/// </summary>
public static class PointFileReader
{
    public static PointFileContent Read(string path)
    {
        var text = ReadText(path);
        var points = new List<SurveyPoint>();
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim('\r').Trim();
            if (line.Length == 0)
                continue;
            if (TryParseLine(line, out var point))
                points.Add(point);
        }

        return new PointFileContent { Points = points };
    }

    private static string ReadText(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length >= 2 && bytes[0] == 0x50 && bytes[1] == 0x4B)
            throw new InvalidOperationException("File này là Excel. Hãy lưu lại dạng CSV rồi mở.");
        string text;
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            text = Encoding.Unicode.GetString(bytes);
        else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            text = Encoding.BigEndianUnicode.GetString(bytes);
        else
        {
            var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            try
            {
                text = utf8.GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
#if NETFRAMEWORK
                text = Encoding.GetEncoding(1258).GetString(bytes);
#else
                text = Encoding.Latin1.GetString(bytes);
#endif
            }
        }

        return text.TrimStart('\uFEFF');
    }

    private static bool TryParseLine(string line, out SurveyPoint point)
    {
        point = new SurveyPoint();
        var fields = Split(line);
        var numbers = new List<NumberToken>();
        for (var i = 0; i < fields.Count; i++)
        {
            if (TryNumber(fields[i], out var value, out var digits))
                numbers.Add(new NumberToken(i, value, digits));
        }

        var coords = numbers.Where(n => n.Digits is 6 or 7).ToList();
        if (coords.Count < 2)
            return false;

        var sevens = coords.Where(n => n.Digits == 7).ToList();
        var sixes = coords.Where(n => n.Digits == 6).ToList();
        NumberToken xToken;
        NumberToken yToken;
        if (sevens.Count > 0 && sixes.Count > 0)
        {
            xToken = sevens[0];
            yToken = sixes[0];
        }
        else
        {
            var pair = coords.Take(2).ToList();
            if (Math.Abs(pair[0].Value) >= Math.Abs(pair[1].Value))
            {
                xToken = pair[0];
                yToken = pair[1];
            }
            else
            {
                xToken = pair[1];
                yToken = pair[0];
            }
        }

        var after = Math.Max(xToken.Index, yToken.Index);
        var height = numbers.FirstOrDefault(n => n.Index > after && n.Digits <= 4);
        var before = Math.Min(xToken.Index, yToken.Index);
        var leading = new List<string>();
        for (var i = 0; i < before; i++)
        {
            if (i == xToken.Index || i == yToken.Index)
                continue;
            if (fields[i].Length > 0)
                leading.Add(fields[i]);
        }

        var name = "";
        var code = "";
        if (leading.Count > 0)
        {
            name = leading[0];
            if (leading.Count > 1)
                code = string.Join(" ", leading.Skip(1));
        }

        var trailing = new List<string>();
        for (var i = after + 1; i < fields.Count; i++)
        {
            if (height is not null && i == height.Index)
                continue;
            if (fields[i].Length == 0)
                continue;
            if (TryNumber(fields[i], out _, out _))
                continue;
            trailing.Add(fields[i]);
        }

        if (trailing.Count is > 0 and <= 2)
            code = string.Join(" ", new[] { code, string.Join(" ", trailing) }.Where(part => part.Length > 0));

        point.Name = name;
        point.X = xToken.Value;
        point.Y = yToken.Value;
        point.Z = height?.Value ?? 0;
        point.Code = code;
        return true;
    }

    private static List<string> Split(string line)
    {
        var tabs = Count(line, '\t');
        var semis = Count(line, ';');
        var commas = Count(line, ',');
        if (semis > 0 && semis >= commas && semis >= tabs)
            return SplitDelimited(line, ';');
        if (tabs > 0 && tabs >= commas)
            return SplitDelimited(line, '\t');
        if (commas > 0)
            return SplitDelimited(line, ',');
        return line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Trim())
            .Where(part => part.Length > 0)
            .ToList();
    }

    private static int Count(string line, char ch)
    {
        var count = 0;
        foreach (var item in line)
        {
            if (item == ch)
                count++;
        }

        return count;
    }

    private static List<string> SplitDelimited(string line, char delimiter)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    quoted = !quoted;
                }

                continue;
            }

            if (ch == delimiter && !quoted)
            {
                fields.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        fields.Add(current.ToString().Trim());
        return fields;
    }

    private static bool TryNumber(string text, out double value, out int digits)
    {
        value = 0;
        digits = 0;
        var sample = text.Trim();
        if (sample.Length == 0)
            return false;

        var negative = sample[0] == '-';
        var body = sample[0] is '-' or '+' ? sample[1..] : sample;
        if (body.Length == 0)
            return false;

        var sep = body.LastIndexOfAny(new[] { '.', ',' });
        string whole;
        string normalized;
        if (sep >= 0)
        {
            whole = body[..sep];
            var fraction = body[(sep + 1)..];
            if (whole.Length == 0 || fraction.Length == 0)
                return false;
            if (!AllDigits(whole) || !AllDigits(fraction))
                return false;
            if (body.IndexOfAny(new[] { '.', ',' }) != sep)
                return false;
            normalized = whole + "." + fraction;
        }
        else
        {
            if (!AllDigits(body))
                return false;
            whole = body;
            normalized = body;
        }

        var trimmed = whole.TrimStart('0');
        digits = trimmed.Length == 0 ? 1 : trimmed.Length;
        if (negative)
            normalized = "-" + normalized;
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static bool AllDigits(string text)
    {
        foreach (var ch in text)
        {
            if (!char.IsDigit(ch))
                return false;
        }

        return text.Length > 0;
    }

    private sealed record NumberToken(int Index, double Value, int Digits);
}
