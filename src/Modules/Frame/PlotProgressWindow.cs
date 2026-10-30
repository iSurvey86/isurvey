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
using iSurvey.UI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Modules.Frame;

/// <summary>Thanh tiến trình modeless trong lúc xuất PDF. Đóng bằng ForceClose khi in xong.</summary>
internal sealed class PlotProgressWindow : Window
{
    private readonly WpfText _detail;
    private readonly WpfProgressBar _bar;
    private bool _allowClose;

    public bool Cancelled { get; private set; }

    private PlotProgressWindow()
    {
        Title = "iSurvey — Xuất PDF";
        Width = 440;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;
        WindowStyle = WindowStyle.ToolWindow;
        Topmost = true;
        ShowInTaskbar = false;
        Background = new WpfSolid(WpfColor.FromRgb(0xF3, 0xF5, 0xF8));
        FontFamily = new WpfFontFamily("Segoe UI");
        FontSize = 13;

        var title = new WpfText
        {
            Text = "Đang xuất PDF",
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
            Height = 10,
            Margin = new Thickness(0, 14, 0, 0),
            Minimum = 0,
            Maximum = 100,
            IsIndeterminate = true
        };
        var cancel = new WpfButton
        {
            Content = "Hủy",
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
            Children = { title, _detail, _bar, cancel }
        };
        Closing += OnClosing;
    }

    public static PlotProgressWindow ShowNew()
    {
        var window = new PlotProgressWindow();
        AcadApp.ShowModelessWindow(window);
        window.Report(0, 0, "Đang chuẩn bị xuất PDF...");
        return window;
    }

    public void Report(int done, int total, string text)
    {
        _detail.Text = text;
        if (total <= 0)
        {
            _bar.IsIndeterminate = true;
        }
        else
        {
            _bar.IsIndeterminate = false;
            _bar.Minimum = 0;
            _bar.Maximum = total;
            _bar.Value = done;
        }

        Pump();
    }

    public void ForceClose()
    {
        _allowClose = true;
        Close();
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
