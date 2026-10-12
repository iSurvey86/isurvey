using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.UI.Account;

/// <summary>Hộp thoại WPF tối (cùng phong cách LoginWindow) dùng cho About / Update / xác nhận đăng xuất.</summary>
public partial class DarkDialog : Window
{
    private DarkDialog(string title)
    {
        InitializeComponent();
        TitleText.Text = title;
        Title = title;
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Secondary_Click(this, e);
            }
        };
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void Primary_Click(object sender, RoutedEventArgs e) => Finish(true);

    private void Secondary_Click(object sender, RoutedEventArgs e) => Finish(false);

    private bool _result;

    private void Finish(bool result)
    {
        _result = result;
        Close();
    }

    private void AddRow(string label, string value, int row)
    {
        RowsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var l = new TextBlock
        {
            Text = label,
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x8C, 0x96, 0xA8)),
            FontSize = 12.5,
            Margin = new Thickness(0, 4, 18, 4)
        };
        var v = new TextBlock
        {
            Text = value,
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 4)
        };
        Grid.SetRow(l, row);
        Grid.SetColumn(l, 0);
        Grid.SetRow(v, row);
        Grid.SetColumn(v, 1);
        RowsGrid.Children.Add(l);
        RowsGrid.Children.Add(v);
        RowsGrid.Visibility = Visibility.Visible;
    }

    private static bool Show(DarkDialog dialog)
    {
        AcadApp.ShowModalWindow(dialog);
        return dialog._result;
    }

    /// <summary>Hộp thông tin: tiêu đề, dòng nhấn mạnh, bảng nhãn-giá trị, ghi chú.</summary>
    public static void ShowInfo(string title, string? headline, IReadOnlyList<(string Label, string Value)>? rows = null,
        string? message = null, string? note = null)
    {
        var d = new DarkDialog(title);

        if (!string.IsNullOrEmpty(headline))
        {
            d.HeadlineText.Text = headline;
            d.HeadlineText.Visibility = Visibility.Visible;
        }

        if (rows is not null)
        {
            for (var i = 0; i < rows.Count; i++)
                d.AddRow(rows[i].Label, rows[i].Value, i);
        }

        if (!string.IsNullOrEmpty(message))
        {
            d.MessageText.Text = message;
            d.MessageText.Visibility = Visibility.Visible;
        }

        if (!string.IsNullOrEmpty(note))
        {
            d.NoteText.Text = note;
            d.NoteText.Visibility = Visibility.Visible;
        }

        Show(d);
    }

    /// <summary>Hộp xác nhận Có/Không — true nếu bấm nút chính.</summary>
    public static bool Confirm(string title, string message, string yesText, string noText = "Hủy")
    {
        var d = new DarkDialog(title);
        d.MessageText.Text = message;
        d.MessageText.Visibility = Visibility.Visible;
        d.PrimaryButton.Content = yesText;
        d.PrimaryButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xB8, 0x23, 0x18));
        d.SecondaryButton.Content = noText;
        d.SecondaryButton.Visibility = Visibility.Visible;
        d.PrimaryButton.IsDefault = false;
        d.SecondaryButton.IsDefault = true;
        return Show(d);
    }
}
