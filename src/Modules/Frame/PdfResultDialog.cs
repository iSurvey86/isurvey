using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using iSurvey.UI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Button = System.Windows.Controls.Button;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Orientation = System.Windows.Controls.Orientation;

namespace iSurvey.Modules.Frame;

/// <summary>Báo in PDF xong. Có file thì thêm nút mở thư mục và chọn sẵn file đó.</summary>
internal static class PdfResultDialog
{
    public static void Show(string message, string? pdfPath)
    {
        var hasFile = !string.IsNullOrWhiteSpace(pdfPath) && File.Exists(pdfPath);
        var window = new Window
        {
            Title = "iSurvey",
            Width = 480,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize,
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            FontSize = 13
        };

        var text = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(16, 16, 16, 8)
        };
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(16, 8, 16, 16)
        };

        if (hasFile)
        {
            var open = new Button
            {
                Content = "Mở thư mục",
                MinWidth = 110,
                Margin = new Thickness(0, 0, 8, 0),
                Style = Cta.Style("CtaSoft")
            };
            open.Click += (_, _) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = "/select,\"" + pdfPath + "\"",
                        UseShellExecute = true
                    });
                }
                catch
                {
                    // không chặn việc đóng hộp
                }

                window.Close();
            };
            buttons.Children.Add(open);
        }

        var ok = new Button
        {
            Content = "OK",
            MinWidth = 88,
            IsDefault = true,
            IsCancel = true,
            Style = Cta.Style("CtaPrimary")
        };
        ok.Click += (_, _) => window.Close();
        buttons.Children.Add(ok);

        var root = new StackPanel();
        root.Children.Add(text);
        root.Children.Add(buttons);
        window.Content = root;
        AcadApp.ShowModalWindow(window);
    }
}
