using System.Text.Json;

namespace iSurvey.Modules.Ai;

/// <summary>
/// Cấu hình Trợ lý AI — %AppData%\iSurvey\ai.config.json (không commit).
/// Provider mặc định: Claude. MCP servers: process stdio (npx / node / exe).
/// </summary>
public sealed class AiConfig
{
    /// <summary>Claude | Gemini</summary>
    public string Provider { get; set; } = "Claude";

    public string? ApiKey { get; set; }

    /// <summary>Claude: claude-sonnet-5-5 · Gemini: gemini-3.8-flash</summary>
    public string Model { get; set; } = "claude-sonnet-5-5";

    /// <summary>Danh sách MCP server (stdio). Để trống = chỉ tool CAD nội bộ iSurvey.</summary>
    public List<McpServerConfig> McpServers { get; set; } = [];

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);

    public bool IsClaude =>
        string.Equals(Provider?.Trim(), "Claude", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Provider?.Trim(), "Anthropic", StringComparison.OrdinalIgnoreCase);

    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "iSurvey", "ai.config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    public static AiConfig Load(string? path = null)
    {
        try
        {
            path ??= DefaultPath;
            if (!File.Exists(path))
                return new AiConfig();
            return JsonSerializer.Deserialize<AiConfig>(File.ReadAllText(path), JsonOptions) ?? new AiConfig();
        }
        catch
        {
            return new AiConfig();
        }
    }

    public void Save(string? path = null)
    {
        path ??= DefaultPath;
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
    }

    public static void EnsureExampleFile()
    {
        var path = DefaultPath;
        if (File.Exists(path))
            return;
        new AiConfig
        {
            Provider = "Claude",
            ApiKey = "",
            Model = "claude-sonnet-5-5",
            McpServers =
            [
                new McpServerConfig
                {
                    Name = "example-filesystem",
                    Enabled = false, // MCP ngoài cần Node — tắt mặc định; tool file nội bộ không cần Node
                    Command = "npx.cmd",
                    Args = ["-y", "@modelcontextprotocol/server-filesystem", "%USERPROFILE%"]
                }
            ]
        }.Save(path);
    }
}

public sealed class McpServerConfig
{
    public string Name { get; set; } = "server";
    public bool Enabled { get; set; } = true;
    public string Command { get; set; } = "";
    public List<string> Args { get; set; } = [];
    public Dictionary<string, string>? Env { get; set; }
}
