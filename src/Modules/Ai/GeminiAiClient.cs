using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace iSurvey.Modules.Ai;

/// <summary>Gọi Gemini generateContent với function calling (vòng tool).</summary>
public sealed class GeminiAiClient
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(90) };

    private readonly AiConfig _config;

    public GeminiAiClient(AiConfig config) => _config = config;

    /// <param name="runToolOnUi">Bắt buộc marshal về luồng AutoCAD (LockDocument / Command).</param>
    public async Task<string> ChatAsync(
        IList<GeminiContent> history,
        Func<string, JsonElement?, string> runToolOnUi,
        IProgress<string>? progress,
        CancellationToken ct)
    {
        if (!_config.HasApiKey)
            throw new InvalidOperationException(
                "Chua cau hinh ApiKey. Mo ISURVEY_AI_SETTINGS hoac sua %AppData%\\iSurvey\\ai.config.json");

        var model = string.IsNullOrWhiteSpace(_config.Model) ? "gemini-3.8-flash" : _config.Model.Trim();
        var url =
            $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(_config.ApiKey!.Trim())}";

        var contents = history.Select(c => new GeminiContent(c.Role, c.Parts)).ToList();
        const int maxRounds = 6;

        for (var round = 0; round < maxRounds; round++)
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report(round == 0 ? "Đang hỏi Gemini…" : $"Đang chạy tool (vòng {round + 1})…");

            var body = new Dictionary<string, object?>
            {
                ["systemInstruction"] = new
                {
                    parts = new[]
                    {
                        new
                        {
                            text =
                                "Bạn là Trợ lý AI của add-in iSurvey trên AutoCAD/Civil 3D (Admin). " +
                                "Trả lời tiếng Việt, ngắn gọn, chuyên khảo sát/CAD. " +
                                "Khi cần dữ liệu bản vẽ hoặc chạy lệnh iSurvey, hãy gọi tool. " +
                                "Không bịa tọa độ/layer. Không chạy lệnh phá hủy nếu user không yêu cầu rõ."
                        }
                    }
                },
                ["contents"] = contents.Select(c => new { role = c.Role, parts = c.Parts }).ToList(),
                ["tools"] = new[]
                {
                    new { functionDeclarations = AiToolRegistry.ToGeminiDeclarations() }
                }
            };

            var raw = await PostWithRetryAsync(url, body, progress, ct).ConfigureAwait(false);

            using var doc = JsonDocument.Parse(raw);
            if (!doc.RootElement.TryGetProperty("candidates", out var cands) || cands.GetArrayLength() == 0)
                throw new InvalidOperationException("Gemini khong tra candidate. " + Trim(raw, 300));

            var content = cands[0].GetProperty("content");
            var parts = content.GetProperty("parts");
            var modelParts = new List<object>();
            var functionCalls = new List<(string Name, JsonElement Args)>();
            var textBits = new List<string>();

            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textEl))
                {
                    var t = textEl.GetString();
                    if (!string.IsNullOrEmpty(t))
                    {
                        textBits.Add(t!);
                        modelParts.Add(new { text = t });
                    }
                }
                else if (part.TryGetProperty("functionCall", out var fc))
                {
                    var name = fc.GetProperty("name").GetString() ?? "";
                    JsonElement args = default;
                    if (fc.TryGetProperty("args", out var a))
                        args = a.Clone();
                    functionCalls.Add((name, args));
                    modelParts.Add(new
                    {
                        functionCall = new
                        {
                            name,
                            args = args.ValueKind == JsonValueKind.Undefined
                                ? new Dictionary<string, object>()
                                : JsonSerializer.Deserialize<object>(args.GetRawText())
                        }
                    });
                }
            }

            contents.Add(new GeminiContent("model", modelParts));

            if (functionCalls.Count == 0)
                return textBits.Count > 0 ? string.Join("\n", textBits) : "(Không có nội dung trả lời.)";

            var responseParts = new List<object>();
            foreach (var (name, args) in functionCalls)
            {
                progress?.Report($"Tool: {name}…");
                JsonElement? argsEl = args.ValueKind == JsonValueKind.Undefined ? null : args;
                var result = runToolOnUi(name, argsEl);
                responseParts.Add(new
                {
                    functionResponse = new
                    {
                        name,
                        response = ParseOrWrap(result)
                    }
                });
            }

            contents.Add(new GeminiContent("user", responseParts));
        }

        return "Đã đạt giới hạn vòng tool. Hãy hỏi ngắn hơn hoặc chạy lệnh thủ công.";
    }

    private async Task<string> PostWithRetryAsync(
        string url,
        Dictionary<string, object?> body,
        IProgress<string>? progress,
        CancellationToken ct)
    {
        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
            var raw = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (resp.IsSuccessStatusCode)
                return raw;

            var code = (int)resp.StatusCode;
            var overloaded = code is 429 or 503
                             || raw.Contains("UNAVAILABLE", StringComparison.OrdinalIgnoreCase)
                             || raw.Contains("high demand", StringComparison.OrdinalIgnoreCase);

            if (overloaded && attempt < maxAttempts)
            {
                var wait = attempt * 2;
                progress?.Report($"Gemini đang quá tải — thử lại sau {wait}s ({attempt}/{maxAttempts})…");
                await Task.Delay(TimeSpan.FromSeconds(wait), ct).ConfigureAwait(false);
                continue;
            }

            if (code == 404)
            {
                throw new InvalidOperationException(
                    "Model Gemini khong ton tai. Sua Model trong ai.config.json (vd: gemini-3.8-flash).");
            }

            if (overloaded)
            {
                throw new InvalidOperationException(
                    "Gemini dang qua tai (503). Doi vai phut roi gui lai — khong phai loi ApiKey.");
            }

            throw new InvalidOperationException($"Gemini HTTP {code}.");
        }

        throw new InvalidOperationException("Gemini: het so lan thu.");
    }

    private static object ParseOrWrap(string result)
    {
        try
        {
            using var d = JsonDocument.Parse(result);
            return d.RootElement.Clone();
        }
        catch
        {
            return new { result };
        }
    }

    private static string Trim(string s, int n) =>
        s.Length <= n ? s : s[..n] + "…";
}

public sealed class GeminiContent
{
    public string Role { get; }
    public List<object> Parts { get; }

    public GeminiContent(string role, IEnumerable<object> parts)
    {
        Role = role;
        Parts = parts.ToList();
    }

    public static GeminiContent UserText(string text) =>
        new("user", new object[] { new { text } });
}
