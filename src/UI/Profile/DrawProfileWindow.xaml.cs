using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using iSurvey.Modules.Profile;
using iSurvey.Modules.Tuyen;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.UI.Profile;

public sealed class DrawProfileRequest
{
    public RouteRecord Route { get; init; } = null!;

    public string Name { get; init; } = "";

    public int HorizontalScale { get; init; }

    public int VerticalScale { get; init; }

    public double Datum { get; init; }
}

public partial class DrawProfileWindow : Window
{
    private readonly List<RouteRecord> _routes;
    private bool _busy;

    public DrawProfileRequest? Request { get; private set; }

    public DrawProfileWindow(IReadOnlyList<RouteRecord> routes, string? preferredRouteId)
    {
        InitializeComponent();
        _routes = routes.Where(r => r.Stations is { Count: > 0 }).ToList();
        foreach (var route in _routes)
            RouteBox.Items.Add(route.Name);

        ScaleBox.Items.Add("1/500; 1/5000");
        ScaleBox.Items.Add("1/200; 1/2000");
        ScaleBox.SelectedIndex = 0;

        if (_routes.Count == 0)
        {
            HintText.Text = "Chưa có tuyến có cọc. Tạo tuyến và phát sinh cọc (có Z) trước.";
            return;
        }

        var index = 0;
        if (!string.IsNullOrWhiteSpace(preferredRouteId))
        {
            for (var i = 0; i < _routes.Count; i++)
            {
                if (_routes[i].Id == preferredRouteId)
                {
                    index = i;
                    break;
                }
            }
        }

        RouteBox.SelectedIndex = index;
        RefreshForRoute();
    }

    public static DrawProfileRequest? Prompt(IReadOnlyList<RouteRecord> routes, string? preferredRouteId)
    {
        var window = new DrawProfileWindow(routes, preferredRouteId);
        AcadApp.ShowModalWindow(window);
        return window.Request;
    }

    private void RouteBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshForRoute();

    private void ScaleBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshForRoute();

    private void RefreshForRoute()
    {
        if (_busy || RouteBox.SelectedIndex < 0 || RouteBox.SelectedIndex >= _routes.Count)
            return;
        _busy = true;
        try
        {
            var route = _routes[RouteBox.SelectedIndex];
            if (string.IsNullOrWhiteSpace(NameBox.Text) || NameBox.Text.StartsWith("MCD ", StringComparison.Ordinal))
                NameBox.Text = "MCD " + route.Name;

            GetScales(out var h, out var v);
            var withZ = 0;
            if (route.Stations is not null)
            {
                foreach (var s in route.Stations)
                {
                    if (s.HasZ)
                        withZ++;
                }
            }

            var datum = ProfileDrawService.SuggestDatum(
                route.Stations ?? new List<StationRecord>(), h, v, ProfileDrawService.DefaultRowHeight);
            DatumBox.Text = datum.ToString("0.###", CultureInfo.InvariantCulture);

            var segs = ProfileDrawService.PlanSegments(
                route.Stations ?? new List<StationRecord>(), datum, h, v, ProfileDrawService.DefaultRowHeight);
            var tip = ProfileDrawService.FormatSuggestion(segs);
            HintText.Text = withZ < 2
                ? "Tuyến này chưa đủ cọc có Z. Phát sinh cọc sau khi gắn mô hình."
                : (string.IsNullOrEmpty(tip)
                    ? "Hàng dưới cùng là Khoảng cách cộng dồn. Sau khi bấm Vẽ, chỉ điểm chèn trên Model."
                    : tip + " Hàng dưới cùng là Khoảng cách cộng dồn.");
        }
        finally
        {
            _busy = false;
        }
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (RouteBox.SelectedIndex < 0 || RouteBox.SelectedIndex >= _routes.Count)
        {
            System.Windows.MessageBox.Show(this, "Chọn một tuyến có cọc.", "iSurvey");
            return;
        }

        var route = _routes[RouteBox.SelectedIndex];
        var withZ = route.Stations?.Count(s => s.HasZ) ?? 0;
        if (withZ < 2)
        {
            System.Windows.MessageBox.Show(this, "Cần ít nhất hai cọc có cao độ Z.", "iSurvey");
            return;
        }

        if (!TryNumber(DatumBox.Text, out var datum))
        {
            System.Windows.MessageBox.Show(this, "Mức so sánh phải là số.", "iSurvey");
            return;
        }

        GetScales(out var h, out var v);
        var name = string.IsNullOrWhiteSpace(NameBox.Text) ? "MCD " + route.Name : NameBox.Text.Trim();
        Request = new DrawProfileRequest
        {
            Route = route,
            Name = name,
            HorizontalScale = h,
            VerticalScale = v,
            Datum = datum
        };
        Close();
    }

    private void GetScales(out int h, out int v)
    {
        if (ScaleBox.SelectedIndex == 1)
        {
            v = 200;
            h = 2000;
        }
        else
        {
            v = 500;
            h = 5000;
        }
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
