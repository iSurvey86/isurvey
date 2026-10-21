using System.Text.Json;

namespace iSurvey.Modules.Ai;

/// <summary>Gộp tool CAD nội bộ + MCP thành schema Claude tools[].</summary>
public static class AiToolBridge
{
    public static List<object> BuildClaudeTools(McpHub? hub)
    {
        var tools = new List<object>();

        foreach (var t in AiToolRegistry.All)
        {
            object schema;
            try
            {
                using var doc = JsonDocument.Parse(t.ParametersJsonSchema);
                schema = JsonSerializer.Deserialize<object>(doc.RootElement.GetRawText())!;
            }
            catch
            {
                schema = new { type = "object", properties = new { } };
            }

            tools.Add(new
            {
                name = "isurvey_" + t.Name,
                description = "[iSurvey CAD] " + t.Description,
                input_schema = schema
            });
        }

        if (hub is not null)
        {
            foreach (var (claudeName, server, tool, info) in hub.EnumerateTools())
            {
                object schema = new { type = "object", properties = new { } };
                if (info.InputSchema.ValueKind == JsonValueKind.Object)
                {
                    try
                    {
                        schema = JsonSerializer.Deserialize<object>(info.InputSchema.GetRawText())!;
                    }
                    catch { /* keep empty */ }
                }

                tools.Add(new
                {
                    name = claudeName,
                    description = $"[MCP:{server}] {info.Description}",
                    input_schema = schema
                });
            }
        }

        return tools;
    }

    public static async Task<string> ExecuteAsync(
        string toolName,
        JsonElement? args,
        AiToolContext cadCtx,
        McpHub? hub,
        Func<string, JsonElement?, string> runCadOnUi,
        CancellationToken ct)
    {
        if (toolName.StartsWith("isurvey_", StringComparison.OrdinalIgnoreCase))
        {
            var local = toolName["isurvey_".Length..];
            return runCadOnUi(local, args);
        }

        if (toolName.StartsWith("mcp__", StringComparison.OrdinalIgnoreCase))
        {
            if (hub is null)
                return JsonSerializer.Serialize(new { error = "MCP chưa kết nối." });
            return await hub.CallAsync(toolName, args, ct).ConfigureAwait(false);
        }

        // backward: tên tool CAD không prefix
        if (AiToolRegistry.Find(toolName) is not null)
            return runCadOnUi(toolName, args);

        return JsonSerializer.Serialize(new { error = "unknown_tool", name = toolName });
    }
}
