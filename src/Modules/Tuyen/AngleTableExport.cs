using System.Globalization;
using System.Text;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace iSurvey.Modules.Tuyen;

/// <summary>
/// Xuất mô tả tuyến (.txt, UTF-8) sau lệnh GOC — nhẹ, dễ copy.
/// Phần đầu: tổng chiều dài, số góc, góc nhỏ/lớn (RP/RT). Sau đó từng đoạn góc → góc kế.
/// </summary>
internal static class AngleTableExport
{
    private const string MotaPlaceholder = "[Kỹ sư điền mô tả tuyến thực địa]";

    /// <summary>Nhãn góc trên bản vẽ: G0 = điểm đầu/đấu nối; góc cuối = điểm cuối.</summary>
    public static string[] BuildLabels(int vertexCount, string prefix, int startIndex)
    {
        var labels = new string[vertexCount];
        for (var i = 0; i < vertexCount; i++)
            labels[i] = $"{prefix}{startIndex + i}";
        return labels;
    }

    /// <summary>Hộp lưu .txt UTF-8; mở bằng app mặc định. Trả về đường dẫn hoặc null nếu hủy.</summary>
    public static string? PromptSaveWriteAndOpen(Editor ed, IReadOnlyList<Point3d> vertices, string prefix, int startIndex)
    {
        if (vertices.Count < 2)
            return null;

        var stamp = DateTime.Now.ToString("ddMMyyyy-HHmm", CultureInfo.InvariantCulture);
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Lưu mô tả tuyến",
            Filter = "Text (*.txt)|*.txt",
            DefaultExt = "txt",
            FileName = $"MoTaTuyen-{stamp}.txt",
            AddExtension = true,
            OverwritePrompt = true
        };
        if (dlg.ShowDialog() != true || string.IsNullOrWhiteSpace(dlg.FileName))
        {
            ed.WriteMessage("\n[iSurvey] Đã bỏ qua lưu mô tả tuyến.");
            return null;
        }

        var labels = BuildLabels(vertices.Count, prefix, startIndex);
        var text = BuildMoTaTuyen(vertices, labels);
        File.WriteAllText(dlg.FileName, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] Không mở được file: {ex.Message}");
        }

        return dlg.FileName;
    }

    internal static string BuildMoTaTuyen(IReadOnlyList<Point3d> vertices, IReadOnlyList<string> labels)
    {
        var vi = CultureInfo.GetCultureInfo("vi-VN");
        var sb = new StringBuilder();
        sb.AppendLine("MÔ TẢ TUYẾN");
        sb.AppendLine();

        // --- Tổng hợp ---
        var totalLen = 0.0;
        for (var i = 0; i < vertices.Count - 1; i++)
            totalLen += Dist2d(vertices[i], vertices[i + 1]);

        // Cột góc = các đỉnh giữa (không tính điểm đầu / điểm cuối).
        var gocCount = Math.Max(0, vertices.Count - 2);

        var turns = new List<(string Label, double Deg, bool Left, string Dms)>();
        for (var i = 1; i < vertices.Count - 1; i++)
        {
            if (TryTurn(vertices[i - 1], vertices[i], vertices[i + 1], out var left, out var deg, out var dms))
                turns.Add((labels[i], deg, left, dms));
        }

        sb.AppendLine(PadStat("+ Tổng chiều dài toàn tuyến đường dây", totalLen.ToString("0.00", vi) + " m"));
        sb.AppendLine(PadStat("+ Tổng số góc đường dây", gocCount.ToString(vi) + " vị trí cột góc"));

        if (turns.Count > 0)
        {
            var min = turns[0];
            var max = turns[0];
            foreach (var t in turns)
            {
                if (t.Deg < min.Deg) min = t;
                if (t.Deg > max.Deg) max = t;
            }

            sb.AppendLine(PadStat("   - Góc nhỏ nhất (" + min.Label + ")", RpRt(min.Left) + " " + min.Dms));
            sb.AppendLine(PadStat("   - Góc lớn nhất (" + max.Label + ")", RpRt(max.Left) + " " + max.Dms));
        }

        sb.AppendLine("Mô tả:");
        sb.AppendLine(
            "(Phần trong ngoặc vuông do kỹ sư điền tay theo thực địa. " +
            labels[0] + " = điểm đầu; " + labels[^1] + " = điểm cuối.)");
        sb.AppendLine();

        // --- Từng đoạn ---
        for (var i = 0; i < vertices.Count - 1; i++)
        {
            var from = labels[i];
            var to = labels[i + 1];
            var distText = Dist2d(vertices[i], vertices[i + 1]).ToString("0.00", vi) + "m";

            sb.Append("- Từ vị trí ").Append(from).Append(", ");

            if (i > 0 && i < vertices.Count - 1 &&
                TryTurn(vertices[i - 1], vertices[i], vertices[i + 1], out var left, out _, out var dms))
            {
                var huongRe = left ? "rẽ trái" : "rẽ phải";
                sb.Append("tuyến ").Append(huongRe).Append(" một góc ").Append(dms).Append(", ");
            }

            sb.Append(MotaPlaceholder)
                .Append(" đến vị trí ").Append(to)
                .Append(", chiều dài: ").Append(distText)
                .Append('.')
                .AppendLine();
        }

        return sb.ToString();
    }

    private static string RpRt(bool left) => left ? "RT" : "RP"; // RT = rẽ trái, RP = rẽ phải

    private static string PadStat(string label, string value)
    {
        // Căn cột ':' gần giống mẫu hồ sơ (nhãn ~48 ký tự).
        const int width = 48;
        if (label.Length >= width)
            return label + " : " + value;
        return label.PadRight(width) + " : " + value;
    }

    private static bool TryTurn(
        Point3d prev, Point3d at, Point3d next, out bool left, out double deg, out string dms)
    {
        left = false;
        deg = 0;
        dms = "";
        var vin = Unit(prev, at);
        var vout = Unit(at, next);
        var cross = vin.X * vout.Y - vin.Y * vout.X;
        var dot = vin.X * vout.X + vin.Y * vout.Y;
        deg = Math.Atan2(Math.Abs(cross), dot) * 180.0 / Math.PI;
        if (deg < 1.0 / 60.0)
            return false;

        left = cross > 0;
        dms = ToDmsSymbols(deg);
        return true;
    }

    /// <summary>Độ phút giây Unicode, ví dụ 105°51′12″ (UTF-8 trong file .txt).</summary>
    private static string ToDmsSymbols(double degrees)
    {
        if (degrees < 0)
            degrees = 0;
        var d = (int)Math.Floor(degrees);
        var minFull = (degrees - d) * 60.0;
        var m = (int)Math.Floor(minFull + 1e-9);
        var s = (int)Math.Round((minFull - m) * 60.0, MidpointRounding.AwayFromZero);
        if (s >= 60)
        {
            s = 0;
            m++;
        }

        if (m >= 60)
        {
            m = 0;
            d++;
        }

        // Độ/phút/giây đều 2 chữ số: 09°34′01″ (góc < 10° có số 0 đứng trước).
        return $"{d:00}\u00B0{m:00}\u2032{s:00}\u2033";
    }

    private static double Dist2d(Point3d a, Point3d b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static Vector2d Unit(Point3d a, Point3d b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-12)
            return new Vector2d(1, 0);
        return new Vector2d(dx / len, dy / len);
    }
}
