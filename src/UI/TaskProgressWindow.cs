using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using WpfButton = System.Windows.Controls.Button;
using WpfColor = System.Windows.Media.Color;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfProgressBar = System.Windows.Controls.ProgressBar;
using WpfSolid = System.Windows.Media.SolidColorBrush;
using WpfStack = System.Windows.Controls.StackPanel;
using WpfText = System.Windows.Controls.TextBlock;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.UI;

/// <summary>
/// Hộp thoại tiến trình modeless dùng chung. Đóng bằng <see cref="ForceClose"/> khi xong.
/// Nút Hủy bỏ / đóng cửa sổ chỉ đặt <see cref="Cancelled"/> — lệnh gọi phải dừng khi thấy cờ.
/// </summary>
public sealed class TaskProgressWindow : Window
{
    private readonly WpfText _heading;
    private readonly WpfText _detail;
    private readonly WpfProgressBar _bar;
    private bool _allowClose;

    public bool Cancelled { get; private set; }

    private TaskProgressWindow(string title, string heading)
    {
        Title = title;
        Width = 460;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;
        WindowStyle = WindowStyle.ToolWindow;
        Topmost = true;
        ShowInTaskbar = false;
        Background = new WpfSolid(WpfColor.FromRgb(0xF3, 0xF5, 0xF8));
        FontFamily = new WpfFontFamily("Segoe UI");
        FontSize = 13;

        _heading = new WpfText
        {
            Text = heading,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = new WpfSolid(WpfColor.FromRgb(0x24, 0x30, 0x40))
        };
        _detail = new WpfText
        {
            Text = "Đang chuẩn bị...",
            Margin = new Thickness(0, 8, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            Foreground = new WpfSolid(WpfColor.FromRgb(0x5C, 0x6B, 0x7A))
        };
        _bar = new WpfProgressBar
        {
            Height = 12,
            Margin = new Thickness(0, 14, 0, 0),
            Minimum = 0,
            Maximum = 100,
            IsIndeterminate = true
        };
        var cancel = new WpfButton
        {
            Content = "Hủy bỏ",
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0),
            MinWidth = 88,
            IsCancel = true,
            Style = Cta.Style("CtaDanger")
        };
        cancel.Click += (_, _) => Cancelled = true;

        Content = new WpfStack
        {
            Margin = new Thickness(20),
            Children = { _heading, _detail, _bar, cancel }
        };
        Closing += OnClosing;
    }

    public static TaskProgressWindow ShowNew(string title, string heading, string? firstDetail = null)
    {
        var window = new TaskProgressWindow(title, heading);
        AcadApp.ShowModelessWindow(window);
        window.Report(0, 0, firstDetail ?? "Đang chuẩn bị...");
        return window;
    }

    public void SetHeading(string heading) => _heading.Text = heading;

    /// <param name="total">≤ 0 thì thanh không xác định; &gt; 0 thì done/total.</param>
    public void Report(int done, int total, string text)
    {
        if (Cancelled && !_allowClose)
            return;

        _detail.Text = text;
        if (total <= 0)
        {
            _bar.IsIndeterminate = true;
        }
        else
        {
            _bar.IsIndeterminate = false;
            _bar.Minimum = 0;
            _bar.Maximum = Math.Max(1, total);
            _bar.Value = Math.Max(0, Math.Min(done, total));
        }

        Pump();
    }

    public void ForceClose()
    {
        _allowClose = true;
        try
        {
            Close();
        }
        catch
        {
            // cửa sổ có thể đã đóng
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
            return;
        Cancelled = true;
        e.Cancel = true;
    }

    private void Pump()
    {
        Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
        System.Windows.Forms.Application.DoEvents();
    }
}
