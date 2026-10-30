using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using iSurvey.Modules.DiemDo;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.UI.DiemDo;

public sealed class ExportPointsRequest
{
    public int Count { get; init; }
    public string FileName { get; init; } = "";
}

public partial class ExportPointsWindow : Window
{
    private readonly ExportPointsForm _form = new();

    public ExportPointsWindow()
    {
        InitializeComponent();
        DataContext = _form;
    }

    public ExportPointsRequest? Request { get; private set; }

    public static ExportPointsRequest? Prompt()
    {
        var window = new ExportPointsWindow();
        AcadApp.ShowModalWindow(window);
        return window.Request;
    }

    private void PickLayer_Click(object sender, RoutedEventArgs e)
    {
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;
        try
        {
            string? layer;
            using (doc.Editor.StartUserInteraction(this))
                layer = PointSelection.LayerOfEntity(doc.Editor, doc.Database);
            if (string.IsNullOrWhiteSpace(layer))
                return;
            _form.FilterByLayer = true;
            _form.LayerName = layer;
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show(this, ex.Message, "iSurvey");
        }
    }

    private void Pick_Click(object sender, RoutedEventArgs e)
    {
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;
        try
        {
            var layer = _form.FilterByLayer ? _form.LayerName.Trim() : null;
            if (_form.FilterByLayer && string.IsNullOrWhiteSpace(layer))
            {
                System.Windows.MessageBox.Show(this, "Nhập tên layer hoặc bỏ chọn lọc theo layer.", "iSurvey");
                return;
            }

            List<SurveyPoint>? points;
            using (doc.Editor.StartUserInteraction(this))
                points = PointSelection.Select(doc.Editor, doc.Database, layer);
            if (points is null)
                return;
            _form.SetPoints(points);
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show(this, ex.Message, "iSurvey");
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_form.Points.Count == 0)
            return;

        if (!int.TryParse(_form.DecimalsText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var digits)
            || digits < 0 || digits > 12)
        {
            System.Windows.MessageBox.Show(this, "Số lẻ phải là số nguyên từ 0 đến 12.", "iSurvey");
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv|Văn bản phân cách tab (*.txt)|*.txt",
            FileName = "DiemDo.csv",
            InitialDirectory = InitialDirectory()
        };
        if (dialog.ShowDialog(this) != true)
            return;

        var format = FormatFor(dialog.FileName);
        try
        {
            PointFileWriter.Write(dialog.FileName, _form.Points.ToList(), format, digits, swapAxes: false);
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show(this, ex.Message, "iSurvey");
            return;
        }

        System.Windows.MessageBox.Show(this, "Đã xuất thành công.\n" + dialog.FileName, "iSurvey");
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = "/select,\"" + dialog.FileName + "\"",
                UseShellExecute = true
            });
        }
        catch
        {
            // tệp đã ghi, chỉ không mở được thư mục
        }

        Request = new ExportPointsRequest
        {
            Count = _form.Points.Count,
            FileName = Path.GetFileName(dialog.FileName)
        };
        Close();
    }

    private static PointFileFormat FormatFor(string path)
    {
        if (path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            return new PointFileFormat('\t', decimalComma: false);

        var separator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
        var delimiter = separator.Length == 1 ? separator[0] : ',';
        return new PointFileFormat(delimiter, decimalComma: false);
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

public sealed class ExportPointsForm : INotifyPropertyChanged
{
    private bool _filterByLayer;
    private string _layerName = "";
    private string _decimalsText = "3";

    public ObservableCollection<SurveyPoint> Points { get; } = new();

    public bool FilterByLayer
    {
        get => _filterByLayer;
        set => Set(ref _filterByLayer, value);
    }

    public string LayerName
    {
        get => _layerName;
        set => Set(ref _layerName, value ?? "");
    }

    public string DecimalsText
    {
        get => _decimalsText;
        set => Set(ref _decimalsText, value ?? "");
    }

    public string StatusText => $"Đã chọn: {Points.Count} điểm";

    public bool CanExport => Points.Count > 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SetPoints(IReadOnlyList<SurveyPoint> points)
    {
        Points.Clear();
        foreach (var point in points)
            Points.Add(point);
        Notify(nameof(StatusText));
        Notify(nameof(CanExport));
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        Notify(name);
        return true;
    }

    private void Notify(string? name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
