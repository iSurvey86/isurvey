using System.Diagnostics;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.Runtime;
using iSurvey.Auth;
using iSurvey.UI.Account;
using iSurvey.UI.Ai;

namespace iSurvey.Modules.Ai;

/// <summary>Trợ lý AI — Admin-Only (Claude + MCP + tool CAD).</summary>
public sealed class AiAssistantModule
{
    [CommandMethod("ISURVEY_AI_ASSIST")]
    public void AssistCommand()
    {
        if (!AuthManager.EnsureAdmin()) return;
        if (AcadApp.DocumentManager.MdiActiveDocument is null)
        {
            AuthManager.Print("\n[iSurvey] Can mo mot ban ve truoc khi dung Tro ly AI.");
            return;
        }

        AiChatWindow.ShowOrFocus();
    }

    [CommandMethod("ISURVEY_AI_SETTINGS")]
    public void SettingsCommand()
    {
        if (!AuthManager.EnsureAdmin()) return;

        AiConfig.EnsureExampleFile();
        var cfg = AiConfig.Load();
        var path = AiConfig.DefaultPath;
        var mcpCount = cfg.McpServers.Count(s => s.Enabled);
        var keyHint = cfg.HasApiKey ? "ApiKey: đã có" : "ApiKey: CHƯA — dán key Anthropic (Claude)";

        DarkDialog.ShowInfo(
            "CẤU HÌNH TRỢ LÝ AI",
            "Claude + MCP",
            [
                ("File", path),
                ("Provider", cfg.Provider),
                ("Model", cfg.Model),
                ("Key", keyHint),
                ("MCP bật", mcpCount.ToString())
            ],
            note: "Provider=Claude, ApiKey=sk-ant-... từ console.anthropic.com. " +
                  "McpServers: bật Enabled=true, Command/Args (vd npx @modelcontextprotocol/server-filesystem).");

        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch
        {
            AuthManager.Print("\n[iSurvey] Mo thu cong: " + path);
        }
    }
}
