using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
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
}

public partial class ImportPointsWindow : Window
{
    private readonly ImportPointsForm _form = new();

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

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Tệp tọa độ (*.csv;*.txt)|*.csv;*.txt|Tất cả (*.*)|*.*",
            InitialDirectory = InitialDirectory()
        };
        if (dialog.ShowDialog(this) != true)
            return;
        _form.FilePath = dialog.FileName;
        Reload(tellIfEmpty: true);
    }

    private void Reload(bool tellIfEmpty = false)
    {
        if (string.IsNullOrWhiteSpace(_form.FilePath) || !File.Exists(_form.FilePath))
            return;
        try
        {
            var content = PointFileReader.Read(_form.FilePath);
            _form.Replace(content, tellIfEmpty);
        }
        catch (System.Exception ex)
        {
            if (tellIfEmpty)
                System.Windows.MessageBox.Show(this, ex.Message, "iSurvey");
        }
    }

    private void Swap_Click(object sender, RoutedEventArgs e)
    {
        _form.ToggleSwap();
        SwapButton.Background = new SolidColorBrush(_form.Swapped
            ? System.Windows.Media.Color.FromRgb(253, 224, 71)
            : System.Windows.Media.Color.FromRgb(254, 249, 195));
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        if (_form.Points.Count == 0)
        {
            System.Windows.MessageBox.Show(this, "Chưa có điểm nào để tạo.", "iSurvey");
            return;
        }

        if (!_form.TryHeight(out var height, out var error))
        {
            System.Windows.MessageBox.Show(this, error, "iSurvey");
            return;
        }

        Request = new ImportPointsRequest
        {
            Points = _form.Points.Select(point => point.Copy()).ToList(),
            TextHeight = height
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

public sealed class ImportPointsForm : INotifyPropertyChanged
{
    private string _filePath = "";
    private string _heightText = "2.5";
    private string _totalText = "0";
    private bool _swapped;

    public ObservableCollection<SurveyPoint> Points { get; } = new();

    public string FilePath
    {
        get => _filePath;
        set => Set(ref _filePath, value ?? "");
    }

    public string HeightText
    {
        get => _heightText;
        set => Set(ref _heightText, value ?? "");
    }

    public string TotalText => _totalText;

    public bool Swapped => _swapped;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Replace(PointFileContent content, bool tellIfEmpty)
    {
        Points.Clear();
        var stt = 1;
        foreach (var point in content.Points)
        {
            if (_swapped)
                point.SwapAxes();
            point.Stt = stt++;
            Points.Add(point);
        }

        _totalText = Points.Count.ToString(CultureInfo.InvariantCulture);
        Notify(nameof(TotalText));
        if (tellIfEmpty && Points.Count == 0)
            System.Windows.MessageBox.Show("Không thấy dòng tọa độ nào. Cần một số 7 chữ số (X) và một số 6 chữ số (Y).", "iSurvey");
    }

    public void ToggleSwap()
    {
        _swapped = !_swapped;
        foreach (var point in Points)
            point.SwapAxes();
        Notify(nameof(Swapped));
    }

    public bool TryHeight(out double height, out string error)
    {
        height = 0;
        error = "";
        var sample = HeightText.Trim().Replace(',', '.');
        if (!double.TryParse(sample, NumberStyles.Float, CultureInfo.InvariantCulture, out height) || height <= 0)
        {
            error = "Chiều cao chữ phải lớn hơn 0.";
            return false;
        }

        return true;
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        Notify(name);
    }

    private void Notify(string? name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
