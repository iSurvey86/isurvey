using System.Globalization;
using System.Windows;
using System.Windows.Input;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.UI.Terrain;

public enum ContourElevMode
{
    NearestText,
    FixedNumber,
    Sequential
}

public sealed class ContourElevOptions
{
    public ContourElevMode Mode { get; init; } = ContourElevMode.Sequential;

    public double BaseZ { get; init; } = 10;

    public double Step { get; init; } = 5;

    public bool Increasing { get; init; } = true;
}

public partial class ContourElevWindow : Window
{
    public ContourElevOptions? Options { get; private set; }

    public ContourElevWindow()
    {
        InitializeComponent();
        UpdateParamsEnabled();
    }

    public static ContourElevOptions? Prompt()
    {
        var window = new ContourElevWindow();
        AcadApp.ShowModalWindow(window);
        return window.Options;
    }

    private void Mode_Changed(object sender, RoutedEventArgs e) => UpdateParamsEnabled();

    private void UpdateParamsEnabled()
    {
        if (ZBox is null || StepBox is null || DirPanel is null)
            return;

        if (RadChu.IsChecked == true)
        {
            ZBox.IsEnabled = false;
            StepBox.IsEnabled = false;
            DirPanel.IsEnabled = false;
            StepLabel.Opacity = 0.45;
            DirLabel.Opacity = 0.45;
            return;
        }

        if (RadSo.IsChecked == true)
        {
            ZBox.IsEnabled = true;
            StepBox.IsEnabled = false;
            DirPanel.IsEnabled = false;
            StepLabel.Opacity = 0.45;
            DirLabel.Opacity = 0.45;
            return;
        }

        ZBox.IsEnabled = true;
        StepBox.IsEnabled = true;
        DirPanel.IsEnabled = true;
        StepLabel.Opacity = 1;
        DirLabel.Opacity = 1;
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        var mode = ContourElevMode.Sequential;
        if (RadChu.IsChecked == true)
            mode = ContourElevMode.NearestText;
        else if (RadSo.IsChecked == true)
            mode = ContourElevMode.FixedNumber;

        if (mode != ContourElevMode.NearestText)
        {
            if (!TryNumber(ZBox.Text, out var z))
            {
                System.Windows.MessageBox.Show(this, "Cao độ gốc phải là số.", "iSurvey");
                return;
            }

            var step = 5.0;
            if (mode == ContourElevMode.Sequential)
            {
                if (!TryNumber(StepBox.Text, out step) || step <= 0)
                {
                    System.Windows.MessageBox.Show(this, "Bước nhảy phải là số lớn hơn 0.", "iSurvey");
                    return;
                }
            }

            Options = new ContourElevOptions
            {
                Mode = mode,
                BaseZ = z,
                Step = step,
                Increasing = RadTang.IsChecked == true
            };
        }
        else
        {
            Options = new ContourElevOptions { Mode = ContourElevMode.NearestText };
        }

        Close();
    }

    private static bool TryNumber(string text, out double value)
    {
        var raw = text.Trim().Replace(',', '.');
        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Options = null;
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
        Options = null;
        Close();
    }
}
