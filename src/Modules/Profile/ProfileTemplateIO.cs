using System.Globalization;
using System.Text;
using System.Text.Json;

namespace iSurvey.Modules.Profile;

/// <summary>Nhập/xuất mẫu bảng trắc dọc (JSON hoặc CSV).</summary>
public static class ProfileTemplateIO
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null
    };

    public sealed class TemplateFile
    {
        public string Name { get; set; } = "Tiêu chuẩn";

        public double LabelWidth { get; set; } = 250;

        public List<ProfileBandRow> Rows { get; set; } = ProfileBandRow.CreateDefaultList();
    }

    public static void ExportJson(string path, string name, double labelWidth, IReadOnlyList<ProfileBandRow> rows)
    {
        var file = new TemplateFile
        {
            Name = name,
            LabelWidth = labelWidth,
            Rows = new List<ProfileBandRow>(rows.Count)
        };
        foreach (var row in rows)
            file.Rows.Add(row.Clone());
        File.WriteAllText(path, JsonSerializer.Serialize(file, Json), Encoding.UTF8);
    }

    public static TemplateFile ImportJson(string path)
    {
        var text = File.ReadAllText(path);
        var file = JsonSerializer.Deserialize<TemplateFile>(text, Json)
            ?? throw new InvalidOperationException("File JSON không hợp lệ.");
        if (file.Rows is null || file.Rows.Count == 0)
            file.Rows = ProfileBandRow.CreateDefaultList();
        return file;
    }

    public static void ExportCsv(string path, IReadOnlyList<ProfileBandRow> rows)
    {
        var sep = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(sep, "Key", "Label", "Content", "Layer", "TextStyle", "ColorAci", "TextHeight", "RowHeight"));
        foreach (var row in rows)
        {
            sb.Append(Escape(row.Key, sep)).Append(sep);
            sb.Append(Escape(row.Label, sep)).Append(sep);
            sb.Append(Escape(row.Content, sep)).Append(sep);
            sb.Append(Escape(row.Layer, sep)).Append(sep);
            sb.Append(Escape(row.TextStyle, sep)).Append(sep);
            sb.Append(row.ColorAci.ToString(CultureInfo.InvariantCulture)).Append(sep);
            sb.Append(FormatNum(row.TextHeight)).Append(sep);
            sb.Append(FormatNum(row.RowHeight));
            sb.AppendLine();
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    public static List<ProfileBandRow> ImportCsv(string path)
    {
        var sep = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
        var lines = File.ReadAllLines(path);
        var rows = new List<ProfileBandRow>();
        var start = 0;
        if (lines.Length > 0 && lines[0].IndexOf("Label", StringComparison.OrdinalIgnoreCase) >= 0)
            start = 1;
        for (var i = start; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0)
                continue;
            var parts = SplitCsv(line, sep);
            if (parts.Count < 7)
                continue;
            // Bản mới: Key,Label,Content,Layer,TextStyle,ColorAci,TextHeight,RowHeight
            // Bản cũ: Key,Label,Content,Layer,ColorAci,TextHeight,RowHeight
            var hasStyle = parts.Count >= 8;
            rows.Add(new ProfileBandRow
            {
                Key = parts[0],
                Label = parts[1],
                Content = parts[2],
                Layer = string.IsNullOrWhiteSpace(parts[3]) ? iSurveyLayers.TdChu : parts[3],
                TextStyle = hasStyle ? parts[4] : "",
                ColorAci = int.TryParse(parts[hasStyle ? 5 : 4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var aci) ? aci : 256,
                TextHeight = ParseNum(parts[hasStyle ? 6 : 5], 7.5),
                RowHeight = ParseNum(parts[hasStyle ? 7 : 6], 50)
            });
        }

        if (rows.Count == 0)
            throw new InvalidOperationException("CSV không có hàng mẫu bảng.");
        return rows;
    }

    private static string FormatNum(double value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);

    private static double ParseNum(string text, double fallback)
    {
        var raw = (text ?? "").Trim().Replace(',', '.');
        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : fallback;
    }

    private static string Escape(string? value, string sep)
    {
        var t = value ?? "";
        if (t.Contains('"') || t.Contains(sep) || t.Contains('\n'))
            return "\"" + t.Replace("\"", "\"\"") + "\"";
        return t;
    }

    private static List<string> SplitCsv(string line, string sep)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                    inQuotes = !inQuotes;
                continue;
            }

            if (!inQuotes && sep.Length > 0 && line.AsSpan(i).StartsWith(sep.AsSpan(), StringComparison.Ordinal))
            {
                result.Add(sb.ToString());
                sb.Clear();
                i += sep.Length - 1;
                continue;
            }

            sb.Append(c);
        }

        result.Add(sb.ToString());
        return result;
    }
}
