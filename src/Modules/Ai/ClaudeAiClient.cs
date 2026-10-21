using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace iSurvey.Modules.Ai;

/// <summary>Anthropic Messages API + tool_use (builtin CAD + MCP).</summary>
public sealed class ClaudeAiClient
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(120) };

    private readonly AiConfig _config;

    public ClaudeAiClient(AiConfig config) => _config = config;

    public async Task<string> ChatAsync(
        IList<ClaudeMessage> history,
        IReadOnlyList<object> tools,
        Func<string, JsonElement?, CancellationToken, Task<string>> runTool,
        IProgress<string>? progress,
        CancellationToken ct)
    {
        if (!_config.HasApiKey)
            throw new InvalidOperationException(
                "Chưa có ApiKey Claude. Mở Cấu Hình AI → dán key Anthropic vào ai.config.json.");

        var model = string.IsNullOrWhiteSpace(_config.Model) ? "claude-sonnet-5-5" : _config.Model.Trim();
        var messages = history.Select(m => new { role = m.Role, content = m.Content }).ToList();
        const int maxRounds = 8;

        for (var round = 0; round < maxRounds; round++)
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report(round == 0 ? "Đang hỏi Claude…" : $"Claude tool (vòng {round + 1})…");

            var body = new Dictionary<string, object?>
            {
                ["model"] = model,
                ["max_tokens"] = 4096,
                ["system"] =
                    "Bạn là operator AutoCAD/Civil 3D trong iSurvey (Admin): thao tác trực tiếp trên bản vẽ, " +
                    "không hướng dẫn user tự gõ lệnh. " +
                    "Mọi yêu cầu vẽ/sửa/tra (hình bất kỳ, layer, zoom, move…) → gọi isurvey_execute_cad " +
                    "với steps = chuỗi Command Line như người dùng (vd [\"_.RECTANG\",\"0,0\",\"20,30\"], " +
                    "[\"_.CIRCLE\",\"5,5\",\"8\"], [\"_.POLYGON\",\"6\",\"0,0\",\"C\",\"10\"]). " +
                    "Không cần tool riêng theo từng hình. Shortcut draw_* chỉ khi tiện. " +
                    "Thiếu toạ độ → tự chọn mặc định hợp lý rồi thực thi. " +
                    "Đọc trạng thái: get_drawing_info / list_layers / get_selection_summary. Tiếng Việt, ngắn.",
                ["messages"] = messages,
            };
            if (tools.Count > 0)
                body["tools"] = tools;

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            req.Headers.TryAddWithoutValidation("x-api-key", _config.ApiKey!.Trim());
            req.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
            var raw = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException(FriendlyHttp((int)resp.StatusCode, raw));

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var stop = root.TryGetProperty("stop_reason", out var sr) ? sr.GetString() : "";
            var content = root.GetProperty("content");

            // Assistant turn — giữ nguyên content array
            var assistantContent = JsonSerializer.Deserialize<object>(content.GetRawText())!;
            messages.Add(new { role = "assistant", content = assistantContent });

            var toolUses = new List<(string Id, string Name, JsonElement Input)>();
            var texts = new List<string>();
            foreach (var block in content.EnumerateArray())
            {
                var type = block.GetProperty("type").GetString();
                if (type == "text")
                    texts.Add(block.GetProperty("text").GetString() ?? "");
                else if (type == "tool_use")
                {
                    toolUses.Add((
                        block.GetProperty("id").GetString() ?? "",
                        block.GetProperty("name").GetString() ?? "",
                        block.TryGetProperty("input", out var inp) ? inp.Clone() : default));
                }
            }

            if (toolUses.Count == 0 || stop == "end_turn")
                return texts.Count > 0 ? string.Join("\n", texts) : "(Không có nội dung.)";

            var toolResults = new List<object>();
            foreach (var (id, name, input) in toolUses)
            {
                progress?.Report($"Tool: {name}…");
                JsonElement? args = input.ValueKind == JsonValueKind.Undefined ? null : input;
                string result;
                try
                {
                    result = await runTool(name, args, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    result = JsonSerializer.Serialize(new { error = ex.Message });
                }

                toolResults.Add(new
                {
                    type = "tool_result",
                    tool_use_id = id,
                    content = result
                });
            }

            messages.Add(new { role = "user", content = (object)toolResults });
        }

        return "Đã đạt giới hạn vòng tool. Hãy hỏi ngắn hơn.";
    }

    private static string FriendlyHttp(int code, string raw)
    {
        if (code == 401 || code == 403)
            return "ApiKey Claude không hợp lệ hoặc hết quyền. Kiểm tra key Anthropic trong ai.config.json.";
        if (code == 429)
            return "Claude đang giới hạn tốc độ (429). Đợi một lúc rồi gửi lại.";
        if (code == 529 || code == 503)
            return "Claude đang quá tải. Đợi rồi thử lại.";
        if (raw.Length > 300) raw = raw[..300] + "…";
        return $"Claude HTTP {code}: {raw}";
    }
}

public sealed class ClaudeMessage
{
    public string Role { get; }
    public object Content { get; }

    public ClaudeMessage(string role, object content)
    {
        Role = role;
        Content = content;
    }

    public static ClaudeMessage UserText(string text) =>
        new("user", text);
}
