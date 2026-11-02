using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using iSurvey.Modules.Tuyen;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.UI.Tuyen;

public sealed class StationListRow
{
    public StationRecord Station { get; init; } = new();

    public string Name => Station.Name;

    public string ChainageText => Station.Chainage.ToString("0.000", CultureInfo.InvariantCulture);

    public string XText => Station.X.ToString("0.000", CultureInfo.InvariantCulture);

    public string YText => Station.Y.ToString("0.000", CultureInfo.InvariantCulture);

    public string ZText => Station.HasZ
        ? Station.Z.ToString("0.000", CultureInfo.InvariantCulture)
        : "—";
}

public sealed class StationListResult
{
    public bool AddRequested { get; init; }
}

public partial class StationListWindow : Window
{
    private readonly Autodesk.AutoCAD.ApplicationServices.Document _doc;
    private readonly RouteRecord _route;
    private readonly ObservableCollection<StationListRow> _rows = new();

    public StationListResult Result { get; private set; } = new();

    public StationListWindow(Autodesk.AutoCAD.ApplicationServices.Document doc, RouteRecord route)
    {
        InitializeComponent();
        _doc = doc;
        _route = route;
        TitleText.Text = "iSurvey — Cọc: " + route.Name;
        ReloadRows();
        Grid.ItemsSource = _rows;
    }

    public static StationListResult Prompt(Autodesk.AutoCAD.ApplicationServices.Document doc, RouteRecord route)
    {
        var window = new StationListWindow(doc, route);
        AcadApp.ShowModalWindow(window);
        return window.Result;
    }

    private void ReloadRows()
    {
        _rows.Clear();
        if (_route.Stations is null)
            return;
        var ordered = _route.Stations.OrderBy(item => item.Chainage).ToList();
        foreach (var station in ordered)
            _rows.Add(new StationListRow { Station = station });
    }

    private void Zoom_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not StationListRow row)
        {
            System.Windows.MessageBox.Show(this, "Hãy chọn một cọc trên bảng.", "iSurvey");
            return;
        }

        RouteService.ZoomTo(_doc, row.Station.X, row.Station.Y);
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not StationListRow row)
        {
            System.Windows.MessageBox.Show(this, "Hãy chọn một cọc trên bảng.", "iSurvey");
            return;
        }

        if (!RouteService.RemoveStation(_doc, _route, row.Station))
        {
            System.Windows.MessageBox.Show(this, "Không xóa được cọc đã chọn.", "iSurvey");
            return;
        }

        ReloadRows();
        TitleText.Text = "iSurvey — Cọc: " + _route.Name + " (" + _rows.Count + ")";
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        Result = new StationListResult { AddRequested = true };
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Result = new StationListResult();
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
        Result = new StationListResult();
        Close();
    }
}
