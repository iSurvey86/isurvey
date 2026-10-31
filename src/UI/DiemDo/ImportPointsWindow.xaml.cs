using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using iSurvey.Modules.DiemDo;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.UI.DiemDo;

public sealed class ImportPointsRequest
{
    public IReadOnlyList<SurveyPoint> Points { get; init; } = [];
    public double TextHeight { get; init; } = 2.5;
    public int PointMode { get; init; }
    public double PointSizePercent { get; init; } = 5;
}

public partial class ImportPointsWindow : Window
{
    private readonly ImportPointsViewModel _form = new();

    public ImportPointsWindow()
    {
        InitializeComponent();
        DataContext = _form;
    }

    public ImportPointsRequest? Request { get; private set; }

    public static ImportPointsRequest? Prompt()
    {
        var window = new ImportPointsWindow();
        AcadApp.ShowModalWindow(window);
        return window.Request;
    }

    private void PointGrid_LayoutUpdated(object? sender, EventArgs e)
    {
        var header = FindChild<DataGridColumnHeadersPresenter>(PointGrid);
        var widths = new double[PointGrid.Columns.Count];
        for (var i = 0; i < widths.Length; i++)
            widths[i] = PointGrid.Columns[i].ActualWidth;
        GridLines.Apply(header?.ActualHeight ?? 0, widths);
    }

    private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T found)
                return found;
            var nested = FindChild<T>(child);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private async void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Tệp tọa độ (*.csv;*.txt)|*.csv;*.txt|Tất cả (*.*)|*.*",
            InitialDirectory = InitialDirectory()
        };
        if (dialog.ShowDialog(this) != true)
            return;
        var path = dialog.FileName;
        _form.FilePath = path;
        try
        {
            var content = await System.Threading.Tasks.Task.Run(() => PointFileReader.Read(path)).ConfigureAwait(false);
            await Dispatcher.InvokeAsync(() =>
            {
                if (IsLoaded)
                    _form.Replace(content, tellIfEmpty: true);
            });
        }
        catch (System.Exception ex)
        {
            if (IsLoaded)
                System.Windows.MessageBox.Show(this, ex.Message, "iSurvey");
        }
    }

    private void MarkerDot_Click(object sender, RoutedEventArgs e) => _form.Marker = PointMarker.Dot;

    private void MarkerNone_Click(object sender, RoutedEventArgs e) => _form.Marker = PointMarker.None;

    private void MarkerCross_Click(object sender, RoutedEventArgs e) => _form.Marker = PointMarker.CircleCross;

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        _form.AddPoint();
        if (_form.Selected is null)
            return;
        PointGrid.ScrollIntoView(_form.Selected);
        PointGrid.Focus();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        _form.DeletePoint();
        if (_form.Selected is not null)
            PointGrid.ScrollIntoView(_form.Selected);
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        PointGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        PointGrid.CommitEdit(DataGridEditingUnit.Row, true);
        if (_form.Points.Count == 0)
        {
            System.Windows.MessageBox.Show(this, "Chưa có điểm nào để tạo.", "iSurvey");
            return;
        }

        if (!_form.TrySettings(out var height, out var mode, out var pointSize, out var error))
        {
            System.Windows.MessageBox.Show(this, error, "iSurvey");
            return;
        }

        Request = new ImportPointsRequest
        {
            Points = _form.Points.Select(point => point.Copy()).ToList(),
            TextHeight = height,
            PointMode = mode,
            PointSizePercent = pointSize
        };
        Close();
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

    private static string InitialDirectory()
    {
        var path = AcadApp.DocumentManager.MdiActiveDocument?.Database?.Filename;
        if (!string.IsNullOrWhiteSpace(path))
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                return dir;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }
}
