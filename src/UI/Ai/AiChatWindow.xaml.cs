using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using iSurvey.Auth;
using iSurvey.Modules.Ai;
using iSurvey.UI.Account;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;

namespace iSurvey.UI.Ai;

public partial class AiChatWindow : Window
{
    private static AiChatWindow? _open;
    private readonly List<ClaudeMessage> _history = [];
    private McpHub? _mcp;
    private CancellationTokenSource? _cts;
    private bool _busy;
    private bool _mcpStarted;

    public AiChatWindow()
    {
        InitializeComponent();
        Closed += (_, _) =>
        {
            if (ReferenceEquals(_open, this))
                _open = null;
            _cts?.Cancel();
            _mcp?.Dispose();
            _mcp = null;
        };
        AppendBubble(
            "Hệ thống",
            "Trợ lý AI iSurvey (Admin) — operator Command Line trên Civil/CAD.\n" +
            "• Bạn ra lệnh tiếng Việt → AI gửi lệnh CAD thật lên bản vẽ (hình bất kỳ, không cần tool riêng từng loại).\n" +
            "• Ví dụ: «vẽ HCN 0,0–20,30», «vẽ lục giác», «zoom extents».",
            isUser: false);
        StatusText.Text = "Admin · Claude · đang sẵn sàng…";
        _ = EnsureMcpAsync();
    }

    public static void ShowOrFocus()
    {
        if (_open is not null)
        {
            try
            {
                _open.Activate();
                _open.InputBox.Focus();
                return;
            }
            catch
            {
                _open = null;
            }
        }

        var w = new AiChatWindow();
        _open = w;
        AcadApp.ShowModelessWindow(w);
        w.InputBox.Focus();
    }

    private async Task EnsureMcpAsync()
    {
        if (_mcpStarted) return;
        _mcpStarted = true;
        try
        {
            var cfg = AiConfig.Load();
            var progress = new Progress<string>(s => StatusText.Text = s);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            var enabled = cfg.McpServers.Count(s => s.Enabled);
            if (enabled == 0)
            {
                StatusText.Text = "Admin · Claude · sẵn sàng (tool CAD + file nội bộ)";
                return;
            }

            _mcp = await McpHub.ConnectAsync(cfg, progress, cts.Token).ConfigureAwait(true);
            var n = _mcp.EnumerateTools().Count();
            StatusText.Text = n > 0
                ? $"Admin · Claude · sẵn sàng · MCP {n} tool(s)"
                : "Admin · Claude · sẵn sàng (MCP tắt/lỗi — vẫn chat + tool nội bộ)";
        }
        catch
        {
            StatusText.Text = "Admin · Claude · sẵn sàng (MCP bỏ qua — vẫn chat + tool nội bộ)";
        }
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void InputBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            e.Handled = true;
            _ = SendAsync();
        }
    }

    private void Send_Click(object sender, RoutedEventArgs e) => _ = SendAsync();

    private async Task SendAsync()
    {
        if (_busy) return;
        var text = (InputBox.Text ?? "").Trim();
        if (text.Length == 0) return;

        if (!AuthManager.IsAdmin())
        {
            AppendBubble("Hệ thống", "Phiên Admin đã hết. Đăng nhập lại bằng email Admin.", isUser: false);
            return;
        }

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
        {
            AppendBubble("Hệ thống", "Không có bản vẽ đang mở.", isUser: false);
            return;
        }

        await EnsureMcpAsync().ConfigureAwait(true);

        InputBox.Text = "";
        AppendBubble("Bạn", text, isUser: true);
        _history.Add(ClaudeMessage.UserText(text));

        _busy = true;
        SendButton.IsEnabled = false;
        InputBox.IsEnabled = false;
        _cts = new CancellationTokenSource();
        var progress = new Progress<string>(s => StatusText.Text = s);

        try
        {
            ActivityLogger.Enqueue("ai_chat", detail: new { provider = "claude", len = text.Length });
            var config = AiConfig.Load();

            if (!config.HasApiKey)
            {
                AiConfig.EnsureExampleFile();
                AppendBubble(
                    "Hệ thống",
                    "Chưa có ApiKey Claude.\n1) Cấu Hình AI → mở ai.config.json\n" +
                    "2) Provider: Claude\n3) ApiKey: key từ console.anthropic.com\n4) Model: claude-sonnet-4-5",
                    isUser: false);
                StatusText.Text = "Thiếu ApiKey Claude";
                return;
            }

            var toolCtx = new AiToolContext
            {
                Doc = doc,
                ConfirmDestructive = msg =>
                    DarkDialog.Confirm("Trợ lý AI", msg, "Chạy lệnh", "Hủy")
            };

            string RunCad(string name, JsonElement? args) =>
                Dispatcher.Invoke(() => AiToolRegistry.Execute(name, toolCtx, args));

            var tools = AiToolBridge.BuildClaudeTools(_mcp);
            var client = new ClaudeAiClient(config);

            async Task<string> RunTool(string name, JsonElement? args, CancellationToken ct) =>
                await AiToolBridge.ExecuteAsync(name, args, toolCtx, _mcp, RunCad, ct)
                    .ConfigureAwait(false);

            string reply;
            if (config.IsClaude)
            {
                reply = await client.ChatAsync(_history, tools, RunTool, progress, _cts.Token)
                    .ConfigureAwait(true);
            }
            else
            {
                // Gemini fallback (ít dùng): không gắn MCP đầy đủ
                AppendBubble("Hệ thống", "Provider đang không phải Claude — chuyển Provider=Claude trong config để dùng MCP đầy đủ. Đang thử Gemini…", isUser: false);
                var gemini = new GeminiAiClient(config);
                var gHist = new List<GeminiContent> { GeminiContent.UserText(text) };
                reply = await gemini.ChatAsync(gHist, (n, a) => RunCad(n, a), progress, _cts.Token)
                    .ConfigureAwait(true);
            }

            AppendBubble("Trợ lý", reply, isUser: false);
            _history.Add(new ClaudeMessage("assistant", reply));
            StatusText.Text = "Admin · Claude · sẵn sàng";
        }
        catch (OperationCanceledException)
        {
            AppendBubble("Hệ thống", "Đã dừng hoặc hết thời gian chờ.", isUser: false);
            StatusText.Text = "Đã dừng";
        }
        catch (Exception ex)
        {
            AppendBubble("Hệ thống", FriendlyError(ex), isUser: false);
            StatusText.Text = "Lỗi — xem tin nhắn trên";
            ActivityLogger.TrackError(ex, "AiChat");
        }
        finally
        {
            _busy = false;
            SendButton.IsEnabled = true;
            InputBox.IsEnabled = true;
            InputBox.Focus();
        }
    }

    private static string FriendlyError(Exception ex)
    {
        var m = ex.Message ?? "";
        if (m.Contains("ApiKey", StringComparison.OrdinalIgnoreCase) ||
            m.Contains("401", StringComparison.Ordinal) ||
            m.Contains("403", StringComparison.Ordinal))
            return m;
        if (m.Contains("429") || m.Contains("quá tải", StringComparison.OrdinalIgnoreCase) ||
            m.Contains("529"))
            return "Claude đang giới hạn/quá tải. Đợi rồi gửi lại.";
        if (ex is HttpRequestException)
            return "Không kết nối được Anthropic (mạng/firewall).";
        if (m.Length > 280) m = m[..280] + "…";
        return m;
    }

    private void AppendBubble(string who, string text, bool isUser)
    {
        var border = new Border
        {
            Background = isUser
                ? new SolidColorBrush(WpfColor.FromRgb(0x37, 0x30, 0xA3))
                : new SolidColorBrush(WpfColor.FromRgb(0x1E, 0x22, 0x2A)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(isUser ? 40 : 0, 0, isUser ? 0 : 40, 8),
            HorizontalAlignment = isUser
                ? System.Windows.HorizontalAlignment.Right
                : System.Windows.HorizontalAlignment.Left
        };

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = who,
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(WpfColor.FromRgb(0xA5, 0xB4, 0xFC)),
            Margin = new Thickness(0, 0, 0, 4)
        });
        stack.Children.Add(new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            Foreground = WpfBrushes.White,
            FontSize = 12.5
        });
        border.Child = stack;
        ChatPanel.Children.Add(border);
        ChatScroll.ScrollToEnd();
    }
}
