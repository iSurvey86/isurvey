namespace iSurvey.Modules.Ai;

/// <summary>Gom nhiều MCP server; tên tool Claude = mcp__{server}__{tool}.</summary>
public sealed class McpHub : IDisposable
{
    private readonly List<McpStdioClient> _clients = [];

    public IReadOnlyList<McpStdioClient> Clients => _clients;

    public static async Task<McpHub> ConnectAsync(AiConfig config, IProgress<string>? progress, CancellationToken ct)
    {
        var hub = new McpHub();
        foreach (var cfg in config.McpServers.Where(s => s.Enabled && !string.IsNullOrWhiteSpace(s.Command)))
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report($"MCP: kết nối {cfg.Name}…");
            try
            {
                var client = await McpStdioClient.StartAsync(cfg, ct).ConfigureAwait(false);
                hub._clients.Add(client);
                progress?.Report($"MCP: {cfg.Name} — {client.Tools.Count} tool(s)");
            }
            catch (Exception ex)
            {
                // MCP ngoài là tùy chọn (thường cần Node). Chat Claude + tool CAD vẫn chạy.
                var shortMsg = ex.Message;
                if (shortMsg.Length > 120) shortMsg = shortMsg[..120] + "…";
                progress?.Report($"MCP '{cfg.Name}' bỏ qua (tùy chọn) — {shortMsg}");
            }
        }
        return hub;
    }

    public IEnumerable<(string ClaudeName, string Server, string Tool, McpToolInfo Info)> EnumerateTools()
    {
        foreach (var c in _clients)
        {
            foreach (var t in c.Tools)
            {
                var claudeName = $"mcp__{Sanitize(c.ServerName)}__{Sanitize(t.Name)}";
                yield return (claudeName, c.ServerName, t.Name, t);
            }
        }
    }

    public async Task<string> CallAsync(string claudeName, System.Text.Json.JsonElement? args, CancellationToken ct)
    {
        foreach (var (name, server, tool, _) in EnumerateTools())
        {
            if (!name.Equals(claudeName, StringComparison.OrdinalIgnoreCase))
                continue;
            var client = _clients.First(c => c.ServerName == server);
            return await client.CallToolAsync(tool, args, ct).ConfigureAwait(false);
        }
        throw new InvalidOperationException($"Không tìm thấy MCP tool: {claudeName}");
    }

    private static string Sanitize(string s)
    {
        var chars = s.Select(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-' ? ch : '_').ToArray();
        return new string(chars);
    }

    public void Dispose()
    {
        foreach (var c in _clients)
            c.Dispose();
        _clients.Clear();
    }
}
