using System.Globalization;
using System.Windows;
using System.Windows.Input;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.UI.Tuyen;

public sealed class CreateStationsRequest
{
    public double FromChainage { get; init; }

    public double ToChainage { get; init; }

    public double Step { get; init; }
}

public partial class CreateStationsWindow : Window
{
    public CreateStationsRequest? Request { get; private set; }

    public CreateStationsWindow(string routeName, double from, double to, bool hasTerrain)
    {
        InitializeComponent();
        RouteText.Text = "Tuyến: " + routeName;
        FromBox.Text = from.ToString("0.###", CultureInfo.InvariantCulture);
        ToBox.Text = to.ToString("0.###", CultureInfo.InvariantCulture);
        HintText.Text = hasTerrain
            ? "Cọc đầu, cuối và mọi đỉnh tuyến luôn được phát sinh. Chữ vuông góc với tuyến. Cao độ Z lấy từ mô hình đã gắn."
            : "Cọc đầu, cuối và mọi đỉnh tuyến luôn được phát sinh. Chữ vuông góc với tuyến. Tuyến chưa gắn mô hình nên chưa có Z; gắn mô hình rồi phát sinh lại.";
    }

    public static CreateStationsRequest? Prompt(string routeName, double from, double to, bool hasTerrain)
    {
        var window = new CreateStationsWindow(routeName, from, to, hasTerrain);
        AcadApp.ShowModalWindow(window);
        return window.Request;
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        if (!TryNumber(FromBox.Text, out var from) || !TryNumber(ToBox.Text, out var to) || !TryNumber(StepBox.Text, out var step))
        {
            System.Windows.MessageBox.Show(this, "Lý trình và bước phải là số.", "iSurvey");
            return;
        }

        if (step <= 0)
        {
            System.Windows.MessageBox.Show(this, "Bước cọc phải lớn hơn 0.", "iSurvey");
            return;
        }

        Request = new CreateStationsRequest
        {
            FromChainage = from,
            ToChainage = to,
            Step = step
        };
        Close();
    }

    private static bool TryNumber(string text, out double value)
    {
        var raw = text.Trim().Replace(',', '.');
        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Request = null;
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
        Request = null;
        Close();
    }
}
