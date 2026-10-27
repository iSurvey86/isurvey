using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using iSurvey.Auth;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.UI.Account;

/// <summary>Hộp thoại cập nhật: phiên bản, tính năng mới, tải gói, rồi thoát CAD để cài.</summary>
public partial class UpdateWindow : Window
{
    private const int PreviewCount = 4;

    private readonly UpdateCheckService.Manifest _man;
    private readonly IReadOnlyList<ReleaseNote> _notes;
    private readonly bool _force;
    private bool _expanded;
    private bool _busy;
    private DateTime _downloadStart;

    public string? SetupDir { get; private set; }

    private UpdateWindow(UpdateCheckService.Manifest man, bool force)
    {
        _man = man;
        _force = force;
        _notes = ParseNotes(man.Notes);
        InitializeComponent();

        CurrentText.Text = FormatVersion(AuthManager.AppVersion);
        LatestText.Text = FormatVersion(man.Latest, keepPrefix: false);
        if (_force)
        {
            ForceText.Text = "Phiên bản hiện tại thấp hơn mức tối thiểu — nên cập nhật ngay.";
            ForceText.Visibility = Visibility.Visible;
        }

        if (_notes.Count > 0)
        {
            NotesTitle.Visibility = Visibility.Visible;
            RenderNotes();
        }

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && !_busy)
            {
                e.Handled = true;
                Close();
            }
        };
    }

    /// <summary>Một dòng tính năng: tên lệnh (nếu có) và ý nghĩa cho người dùng.</summary>
    internal readonly record struct ReleaseNote(string? Command, string Text);

    /// <summary>Hiện hộp thoại. Trả về thư mục Setup đã giải nén nếu người dùng bấm Cập nhật ngay và tải xong; null nếu hủy hoặc lỗi.</summary>
    public static string? ShowAvailable(UpdateCheckService.Manifest man, bool force)
    {
        var w = new UpdateWindow(man, force);
        AcadApp.ShowModalWindow(w);
        return w.SetupDir;
    }

    internal static IReadOnlyList<ReleaseNote> ParseNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return [];

        var lines = notes.Replace("\r\n", "\n").Split('\n')
            .Select(l => l.Trim().TrimStart('-', '•', '*', '–').Trim())
            .Where(l => l.Length > 0)
            .ToList();

        if (lines.Count == 1)
        {
            var parts = lines[0].Split(["; ", " • "], StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();
            if (parts.Count > 1)
                lines = parts;
        }

        var list = new List<ReleaseNote>();
        foreach (var line in lines)
        {
            if (IsAdminOnly(line))
                continue;
            list.Add(ToNote(line));
        }

        return list;
    }

    /// <summary>Dòng chỉ dành cho Admin (đăng xuất từ xa, OTP, trợ lý AI) không hiện cho người dùng.</summary>
    private static bool IsAdminOnly(string line)
    {
        if (Regex.IsMatch(line, @"^\s*Admin\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            return true;
        if (Regex.IsMatch(line, "đăng xuất từ xa", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
            || Regex.IsMatch(line, "dang xuat tu xa", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            return true;

        var cmd = Regex.Match(line, @"^[A-Z][A-Z0-9_]{1,24}");
        if (!cmd.Success)
            return false;
        return cmd.Value is "AIA" or "AICFG" or "GENOTP" or "ISURVEY_GENOTP";
    }

    /// <summary>Đưa về dạng tên lệnh + ý nghĩa. Dòng không bắt đầu bằng lệnh thì giữ nguyên câu.</summary>
    private static ReleaseNote ToNote(string line)
    {
        var m = Regex.Match(line, @"^(?<cmd>[A-Z][A-Z0-9_]{1,24})(?:\s*[:：]\s*|\s+)(?<rest>\S.*)$");
        if (!m.Success)
            return new ReleaseNote(null, line);
        return new ReleaseNote(m.Groups["cmd"].Value, m.Groups["rest"].Value.Trim());
    }

    private static string FormatVersion(string v, bool keepPrefix = true)
    {
        v = (v ?? "").Trim();
        if (v.Length == 0)
            return "";
        if (v.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            v = v[1..];
        return keepPrefix ? "v" + v : v;
    }

    private void RenderNotes()
    {
        NotesList.Children.Clear();
        var ink = (System.Windows.Media.Brush)FindResource("Ink");
        var accent = (System.Windows.Media.Brush)FindResource("Accent");
        var take = _expanded || _notes.Count <= PreviewCount ? _notes.Count : PreviewCount;
        for (var i = 0; i < take; i++)
        {
            var note = _notes[i];
            var text = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                Foreground = ink,
                Margin = new Thickness(0, 3, 0, 3)
            };
            text.Inlines.Add(new Run("•  ") { Foreground = accent });
            if (!string.IsNullOrEmpty(note.Command))
            {
                text.Inlines.Add(new Run(note.Command) { FontWeight = FontWeights.SemiBold });
                text.Inlines.Add(new Run(": "));
            }
            text.Inlines.Add(new Run(note.Text));
            NotesList.Children.Add(text);
        }

        if (_notes.Count > PreviewCount)
        {
            DetailLink.Visibility = Visibility.Visible;
            DetailLink.Text = _expanded ? "Thu gọn" : "Chi tiết";
        }
        else
        {
            DetailLink.Visibility = Visibility.Collapsed;
        }
    }

    private void Detail_Click(object sender, MouseButtonEventArgs e)
    {
        _expanded = !_expanded;
        RenderNotes();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (_busy)
            return;
        Close();
    }

    private void Update_Click(object sender, RoutedEventArgs e)
    {
        if (_busy)
            return;
        _busy = true;
        OfferPanel.Visibility = Visibility.Collapsed;
        ProgressPanel.Visibility = Visibility.Visible;
        CloseButton.Visibility = Visibility.Collapsed;
        UpdateButton.Visibility = Visibility.Collapsed;
        SetProgress("Đang tải gói cài đặt...", "Bắt đầu tải...", 0);
        _downloadStart = DateTime.UtcNow;
        UpdateApplyService.Trace("download_start latest=" + _man.Latest);

        var progress = new Progress<UpdateApplyService.UpdateProgress>(p =>
        {
            SetProgress(p.Title, p.Detail, p.Percent);
        });

        Task.Run(() => UpdateApplyService.DownloadAndStage(_man, progress))
            .ContinueWith(t => Dispatcher.Invoke(() => OnDownloaded(t)));
    }

    private void SetProgress(string title, string? detail, double? percent)
    {
        ProgressTitle.Text = title;
        ProgressStatus.Text = detail ?? "";
        if (percent is null)
        {
            Bar.IsIndeterminate = true;
            PercentText.Text = "";
        }
        else
        {
            Bar.IsIndeterminate = false;
            Bar.Value = Math.Max(0, Math.Min(100, percent.Value));
            PercentText.Text = percent.Value.ToString("0") + "%";
        }
    }

    private void OnDownloaded(Task<UpdateApplyService.ApplyResult> task)
    {
        UpdateApplyService.ApplyResult staged;
        try
        {
            staged = task.IsFaulted
                ? new UpdateApplyService.ApplyResult(false, task.Exception?.GetBaseException().Message ?? "Lỗi không xác định.")
                : task.Result;
        }
        catch (Exception ex)
        {
            staged = new UpdateApplyService.ApplyResult(false, ex.Message);
        }

        var elapsed = (int)(DateTime.UtcNow - _downloadStart).TotalMilliseconds;
        var detail = new
        {
            current = AuthManager.AppVersion,
            latest = _man.Latest,
            ok = staged.Ok,
            message = staged.Message,
            bytes = staged.Bytes,
            elapsedMs = elapsed,
            stage = "download"
        };
        UpdateApplyService.Trace($"download_done ok={staged.Ok} bytes={staged.Bytes} ms={elapsed} {staged.Message}");
        try
        {
            var access = AuthManager.AcquireAccessToken();
            if (access is not null)
                ActivityLogger.SendNow(access, "update_download", "UPD", detail, null, CancellationToken.None);
            else
                ActivityLogger.Enqueue("update_download", "UPD", detail);
        }
        catch
        {
            ActivityLogger.Enqueue("update_download", "UPD", detail);
        }

        if (!staged.Ok || string.IsNullOrEmpty(staged.SetupDir))
        {
            _busy = false;
            SetProgress("Không tải được bản mới", staged.Message, null);
            ErrorClose.Visibility = Visibility.Visible;
            AuthManager.Print("\n[iSurvey] " + staged.Message);
            return;
        }

        // Đóng hộp thoại trước, rồi lệnh UPD mới thoát CAD — Quit khi hộp thoại modal còn mở làm Civil treo.
        SetupDir = staged.SetupDir;
        Close();
    }
}
