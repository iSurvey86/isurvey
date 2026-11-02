using System.Globalization;
using System.Windows;
using System.Windows.Input;
using iSurvey.Modules.Tuyen;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.UI.Tuyen;

public partial class StationStyleWindow : Window
{
    public StationStyleSettings? Result { get; private set; }

    public StationStyleWindow(StationStyleSettings current)
    {
        InitializeComponent();
        TickBox.Text = FormatOptional(current.TickLength);
        HeightBox.Text = FormatOptional(current.TextHeight);
        OffsetBox.Text = FormatOptional(current.TextOffset);
        WidthBox.IsChecked = current.UsePolylineWidth;
        FactorBox.Text = current.WidthFactor <= 1e-9
            ? "1"
            : current.WidthFactor.ToString("0.###", CultureInfo.InvariantCulture);
    }

    public static StationStyleSettings? Prompt(StationStyleSettings current)
    {
        var window = new StationStyleWindow(current);
        AcadApp.ShowModalWindow(window);
        return window.Result;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!TryOptional(TickBox.Text, out var tick)
            || !TryOptional(HeightBox.Text, out var height)
            || !TryOptional(OffsetBox.Text, out var offset))
        {
            System.Windows.MessageBox.Show(this, "Chiều dài, cao chữ và khoảng cách phải là số ≥ 0 (hoặc để trống).", "iSurvey");
            return;
        }

        if (!TryNumber(FactorBox.Text, out var factor) || factor <= 0)
        {
            System.Windows.MessageBox.Show(this, "Hệ số nhân độ rộng phải lớn hơn 0.", "iSurvey");
            return;
        }

        Result = new StationStyleSettings
        {
            TickLength = tick,
            TextHeight = height,
            TextOffset = offset,
            UsePolylineWidth = WidthBox.IsChecked == true,
            WidthFactor = factor
        };
        Close();
    }

    private static string FormatOptional(double value) =>
        value <= 1e-9 ? "" : value.ToString("0.###", CultureInfo.InvariantCulture);

    private static bool TryOptional(string text, out double value)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = 0;
            return true;
        }

        return TryNumber(text, out value) && value >= 0;
    }

    private static bool TryNumber(string text, out double value)
    {
        var raw = text.Trim().Replace(',', '.');
        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }

    private void Header_Drag(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;
        Result = null;
        Close();
    }
}
