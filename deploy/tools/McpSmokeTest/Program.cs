using System.Diagnostics;
using System.Text;
using System.Text.Json;

// Claude E2E mirroring iSurvey ClaudeAiClient (no AutoCAD).
var cfgPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "iSurvey", "ai.config.json");
if (!File.Exists(cfgPath))
{
    Console.WriteLine("FAIL: missing ai.config.json");
    return 1;
}

using var cfgDoc = JsonDocument.Parse(File.ReadAllText(cfgPath));
var root = cfgDoc.RootElement;
var apiKey = root.GetProperty("ApiKey").GetString() ?? "";
var model = root.TryGetProperty("Model", out var m) ? m.GetString() ?? "" : "";
var mcpOn = false;
if (root.TryGetProperty("McpServers", out var servers))
{
    foreach (var s in servers.EnumerateArray())
        if (s.TryGetProperty("Enabled", out var en) && en.GetBoolean())
            mcpOn = true;
}

Console.WriteLine($"CONFIG model={model} mcpOn={mcpOn} hasKey={!string.IsNullOrWhiteSpace(apiKey)}");
if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(model))
{
    Console.WriteLine("FAIL: config incomplete");
    return 1;
}
if (mcpOn)
{
    Console.WriteLine("FAIL: MCP still enabled (default path must not need Node)");
    return 1;
}

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };

async Task<JsonElement> MessagesAsync(object body)
{
    using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
    req.Headers.TryAddWithoutValidation("x-api-key", apiKey.Trim());
    req.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
    req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
    using var resp = await http.SendAsync(req);
    var raw = await resp.Content.ReadAsStringAsync();
    if (!resp.IsSuccessStatusCode)
        throw new Exception($"HTTP {(int)resp.StatusCode}: {raw[..Math.Min(400, raw.Length)]}");
    return JsonDocument.Parse(raw).RootElement.Clone();
}

var tools = new object[]
{
    new
    {
        name = "list_directory",
        description = "List directory under user profile",
        input_schema = new
        {
            type = "object",
            properties = new { path = new { type = "string" } },
            required = new[] { "path" }
        }
    },
    new
    {
        name = "get_drawing_info",
        description = "Drawing info",
        input_schema = new { type = "object", properties = new { } }
    }
};

// 1) Plain chat
{
    var r = await MessagesAsync(new
    {
        model,
        max_tokens = 64,
        messages = new[] { new { role = "user", content = "Reply with exactly: OK" } }
    });
    var text = string.Join("", r.GetProperty("content").EnumerateArray()
        .Where(b => b.GetProperty("type").GetString() == "text")
        .Select(b => b.GetProperty("text").GetString()));
    Console.WriteLine($"CHAT_PASS stop={r.GetProperty("stop_reason").GetString()} text={text}");
    if (string.IsNullOrWhiteSpace(text)) return 1;
}

// 2) Tool round-trip like ClaudeAiClient
var messages = new List<object>
{
    new { role = "user", content = (object)"Use list_directory on the user profile path, then say how many entries." }
};

var toolsBody = new Dictionary<string, object?>
{
    ["model"] = model,
    ["max_tokens"] = 512,
    ["system"] = "You must call list_directory when asked to list a folder. Keep answers short.",
    ["messages"] = messages,
    ["tools"] = tools
};

var round1 = await MessagesAsync(toolsBody);
var content1 = round1.GetProperty("content");
var assistantContent = JsonSerializer.Deserialize<object>(content1.GetRawText())!;
messages.Add(new { role = "assistant", content = assistantContent });

var toolUses = content1.EnumerateArray()
    .Where(b => b.GetProperty("type").GetString() == "tool_use")
    .ToList();
Console.WriteLine($"TOOL_CALL_PASS count={toolUses.Count} stop={round1.GetProperty("stop_reason").GetString()}");
if (toolUses.Count == 0) return 1;

var toolResults = new List<object>();
foreach (var tu in toolUses)
{
    var id = tu.GetProperty("id").GetString()!;
    var name = tu.GetProperty("name").GetString()!;
    var path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    if (tu.TryGetProperty("input", out var input) && input.TryGetProperty("path", out var p))
        path = p.GetString() ?? path;

    string resultJson;
    try
    {
        var entries = Directory.GetFileSystemEntries(path);
        resultJson = JsonSerializer.Serialize(new { path, count = entries.Length, sample = entries.Take(10).Select(Path.GetFileName) });
    }
    catch (Exception ex)
    {
        resultJson = JsonSerializer.Serialize(new { error = ex.Message });
    }

    Console.WriteLine($"  ran {name}");
    toolResults.Add(new { type = "tool_result", tool_use_id = id, content = resultJson });
}

messages.Add(new { role = "user", content = (object)toolResults });

var round2 = await MessagesAsync(new Dictionary<string, object?>
{
    ["model"] = model,
    ["max_tokens"] = 256,
    ["messages"] = messages,
    ["tools"] = tools
});
var finalText = string.Join("", round2.GetProperty("content").EnumerateArray()
    .Where(b => b.GetProperty("type").GetString() == "text")
    .Select(b => b.GetProperty("text").GetString()));
Console.WriteLine($"TOOL_RESULT_PASS stop={round2.GetProperty("stop_reason").GetString()} len={finalText.Length}");
if (finalText.Length < 1)
{
    Console.WriteLine("FAIL empty after tool_result");
    return 1;
}

// 3) DLL installed
foreach (var year in new[] { "2023", "2025", "2027" })
{
    var dll = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Autodesk", "ApplicationPlugins", "iSurvey.bundle", "Contents", "Win64", year, "iSurvey.dll");
    if (!File.Exists(dll))
    {
        Console.WriteLine($"FAIL missing DLL {year}");
        return 1;
    }
    var utf = Encoding.UTF8.GetString(File.ReadAllBytes(dll));
    if (!utf.Contains("TryResolveSandboxPath"))
    {
        Console.WriteLine($"FAIL DLL {year} missing sandbox tools");
        return 1;
    }
    Console.WriteLine($"DLL_OK {year} ageMin={(int)(DateTime.Now - File.GetLastWriteTime(dll)).TotalMinutes}");
}

var acad = Process.GetProcessesByName("acad");
Console.WriteLine($"ACAD_RUNNING={acad.Length}");
Console.WriteLine("OVERALL=PASS (Claude chat + tool_use + tool_result + DLL; Node not required)");
if (acad.Length > 0)
    Console.WriteLine("RESTART_ACAD_REQUIRED=yes (process still holds previous load)");
else
    Console.WriteLine("RESTART_ACAD_REQUIRED=open Civil once, then AIA");
return 0;
