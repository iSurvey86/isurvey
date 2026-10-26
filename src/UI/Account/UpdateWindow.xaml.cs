using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using iSurvey.Auth;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.UI.Account;

/// <summary>Hộp thoại cập nhật: phiên bản, tính năng mới, tải gói, rồi thoát CAD để cài.</summary>
public partial class UpdateWindow : Window
{
    private const int PreviewCount = 4;

    private readonly UpdateCheckService.Manifest _man;
    private readonly IReadOnlyList<string> _notes;
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

    /// <summary>Hiện hộp thoại. Trả về thư mục Setup đã giải nén nếu người dùng bấm Cập nhật ngay và tải xong; null nếu hủy hoặc lỗi.</summary>
    public static string? ShowAvailable(UpdateCheckService.Manifest man, bool force)
    {
        var w = new UpdateWindow(man, force);
        AcadApp.ShowModalWindow(w);
        return w.SetupDir;
    }

    internal static IReadOnlyList<string> ParseNotes(string? notes)
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
                return parts;
        }

        return lines;
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
        var take = _expanded || _notes.Count <= PreviewCount ? _notes.Count : PreviewCount;
        for (var i = 0; i < take; i++)
        {
            var row = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
            row.Children.Add(new TextBlock
            {
                Text = "•",
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x38, 0xAB, 0xFF)),
                FontSize = 13,
                Margin = new Thickness(0, 0, 8, 0)
            });
            row.Children.Add(new TextBlock
            {
                Text = _notes[i],
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xD5, 0xDB, 0xE6)),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 420
            });
            NotesList.Children.Add(row);
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

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
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
        CloseX.IsEnabled = false;
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
            CloseX.IsEnabled = true;
            SetProgress("Không tải được bản mới", staged.Message, null);
            ProgressButtons.Visibility = Visibility.Visible;
            AuthManager.Print("\n[iSurvey] " + staged.Message);
            return;
        }

        // Đóng hộp thoại trước, rồi lệnh UPD mới thoát CAD — Quit khi hộp thoại modal còn mở làm Civil treo.
        SetupDir = staged.SetupDir;
        Close();
    }
}
