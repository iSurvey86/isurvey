using System.Text;
using System.Text.Json;
using Autodesk.AutoCAD.DatabaseServices;

namespace iSurvey.Modules.Ai;

/// <summary>
/// Trả lời nhanh câu hỏi CAD thường gặp ngay trên máy — không cần Gemini.
/// Giảm phụ thuộc API (503 / mạng) cho các hỏi đếm đối tượng, layer, tên bản vẽ.
/// </summary>
public static class AiLocalRouter
{
    public static bool TryAnswer(AiToolContext ctx, string userText, out string reply)
    {
        reply = "";
        var q = Normalize(userText);
        if (q.Length == 0)
            return false;

        // Đếm line / polyline / circle / text / hatch / tổng
        if (LooksLikeCountQuestion(q))
        {
            reply = AnswerCount(ctx, q);
            return true;
        }

        if (q.Contains("layer") || q.Contains("lop"))
        {
            var json = AiToolRegistry.Execute("list_layers", ctx, null);
            reply = FormatLayers(json);
            return true;
        }

        if ((q.Contains("ten") || q.Contains("file") || q.Contains("ban ve")) &&
            (q.Contains("gi") || q.Contains("nao") || q.Contains("ten ban") || q.Contains("dang mo")))
        {
            var json = AiToolRegistry.Execute("get_drawing_info", ctx, null);
            reply = FormatDrawingInfo(json);
            return true;
        }

        if (q.Contains("lenh") && (q.Contains("isurvey") || q.Contains("co nhung") || q.Contains("danh sach")))
        {
            reply =
                "Lệnh iSurvey thường dùng:\n" +
                "• GOC — đánh số góc tuyến\n" +
                "• UAVCLIP — cắt ảnh UAV\n" +
                "• D2C / C2D — nhập/xuất DGN\n" +
                "• INK / EXK — nhập/xuất KML\n" +
                "• IG / SG — chèn Google Earth / Satellite\n" +
                "• CBD — cắt bình đồ · SCN — AutoScan · UPD — cập nhật\n" +
                "Gõ rõ hơn nếu cần chạy một lệnh (có xác nhận).";
            return true;
        }

        return false;
    }

    private static string Normalize(string s)
    {
        s = s.Trim().ToLowerInvariant();
        // Bỏ dấu tiếng Việt đơn giản để match keyword
        var formD = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static bool LooksLikeCountQuestion(string q)
    {
        if (!(q.Contains("may") || q.Contains("bao nhieu") || q.Contains("so luong") || q.Contains("dem") ||
              q.Contains("co may") || q.Contains("co bao")))
            return false;
        return q.Contains("line") || q.Contains("polyline") || q.Contains("pl") ||
               q.Contains("circle") || q.Contains("text") || q.Contains("hatch") ||
               q.Contains("doi tuong") || q.Contains("entity") || q.Contains("ban ve");
    }

    private static string AnswerCount(AiToolContext ctx, string q)
    {
        var counts = CountModelSpace(ctx);
        var wantLine = q.Contains("line") && !q.Contains("polyline") && !q.Contains("pline");
        var wantPline = q.Contains("polyline") || q.Contains("pline") || q.Contains("pl ");
        // "line" trong tiếng Việt user thường gồm cả Line + Polyline
        if (q.Contains("line") && !wantPline)
            wantLine = true;

        if (wantPline && !wantLine)
        {
            var n = Get(counts, "Polyline") + Get(counts, "Polyline2d") + Get(counts, "Polyline3d");
            return $"Trong Model Space hiện có **{n}** polyline (Polyline / 2d / 3d).\n(Đếm cục bộ — không qua Gemini.)";
        }

        if (wantLine || (q.Contains("line") && !q.Contains("circle")))
        {
            var lines = Get(counts, "Line");
            var pl = Get(counts, "Polyline") + Get(counts, "Polyline2d") + Get(counts, "Polyline3d");
            return
                $"Trong Model Space:\n" +
                $"• Line: **{lines}**\n" +
                $"• Polyline (gồm 2d/3d): **{pl}**\n" +
                $"• Tổng Line+Polyline: **{lines + pl}**\n" +
                $"(Đếm cục bộ trên bản vẽ — không cần Gemini.)";
        }

        if (q.Contains("circle"))
            return $"Circle trong Model Space: **{Get(counts, "Circle")}**.\n(Đếm cục bộ.)";

        // Tổng quan
        var total = counts.Values.Sum();
        var top = counts.OrderByDescending(kv => kv.Value).Take(8)
            .Select(kv => $"• {kv.Key}: {kv.Value}");
        return $"Model Space có **{total}** đối tượng.\n" + string.Join("\n", top) +
               "\n(Đếm cục bộ — không qua Gemini.)";
    }

    private static int Get(Dictionary<string, int> counts, string key) =>
        counts.TryGetValue(key, out var n) ? n : 0;

    private static Dictionary<string, int> CountModelSpace(AiToolContext ctx)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        using (ctx.Doc.LockDocument())
        using (var tr = ctx.Doc.Database.TransactionManager.StartTransaction())
        {
            var bt = (BlockTable)tr.GetObject(ctx.Doc.Database.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            foreach (ObjectId id in ms)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is not Entity ent)
                    continue;
                var key = ent.GetType().Name;
                counts[key] = counts.TryGetValue(key, out var n) ? n + 1 : 1;
            }
            tr.Commit();
        }
        return counts;
    }

    private static string FormatLayers(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var count = doc.RootElement.GetProperty("count").GetInt32();
            var layers = doc.RootElement.GetProperty("layers").EnumerateArray()
                .Select(e => e.GetString()).Where(s => !string.IsNullOrEmpty(s)).Take(40);
            return $"Có **{count}** layer (hiển thị tối đa 40):\n• " + string.Join("\n• ", layers) +
                   "\n(Đọc cục bộ.)";
        }
        catch
        {
            return json;
        }
    }

    private static string FormatDrawingInfo(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var file = doc.RootElement.GetProperty("file").GetString();
            var n = doc.RootElement.GetProperty("model_space_entities").GetInt64();
            return $"Bản vẽ đang mở: **{file}**\nSố đối tượng Model Space (ước lượng): **{n}**\n(Đọc cục bộ.)";
        }
        catch
        {
            return json;
        }
    }
}
