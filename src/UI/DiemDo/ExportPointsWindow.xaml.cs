using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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
    private static readonly SolidColorBrush IdleFill = Freeze(248, 250, 252);
    private static readonly SolidColorBrush IdleBorder = Freeze(209, 213, 219);
    private static readonly SolidColorBrush IdleText = Freeze(31, 41, 55);
    private static readonly SolidColorBrush ChosenFill = Freeze(219, 234, 254);
    private static readonly SolidColorBrush ChosenBorder = Freeze(29, 78, 216);

    public ExportPointsWindow()
    {
        InitializeComponent();
        DataContext = _form;
        foreach (var button in new[] { WindowButton, PolyButton, DirectButton })
            Paint(button, chosen: false);
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

    private void Window_Click(object sender, RoutedEventArgs e) =>
        Pick(WindowButton, (editor, db, layer) => PointSelection.CrossingWindow(editor, db, layer));

    private void Poly_Click(object sender, RoutedEventArgs e) =>
        Pick(PolyButton, (editor, db, layer) => PointSelection.CrossingPolyline(editor, db, layer));

    private void Direct_Click(object sender, RoutedEventArgs e) =>
        Pick(DirectButton, (editor, db, layer) => PointSelection.Direct(editor, db, layer));

    private void Pick(System.Windows.Controls.Button button, Func<Autodesk.AutoCAD.EditorInput.Editor, Autodesk.AutoCAD.DatabaseServices.Database, string?, List<SurveyPoint>?> select)
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
                points = select(doc.Editor, doc.Database, layer);
            if (points is null)
                return;
            _form.SetPoints(points);
            foreach (var item in new[] { WindowButton, PolyButton, DirectButton })
                Paint(item, ReferenceEquals(item, button));
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show(this, ex.Message, "iSurvey");
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_form.Points.Count == 0)
        {
            System.Windows.MessageBox.Show(this, "Chưa chọn điểm nào.", "iSurvey");
            return;
        }

        if (!int.TryParse(_form.DecimalsText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var digits)
            || digits < 0 || digits > 12)
        {
            System.Windows.MessageBox.Show(this, "Số lẻ phải là số nguyên từ 0 đến 12.", "iSurvey");
            return;
        }

        var format = new PointFileFormat(';', decimalComma: false);
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv|Văn bản (*.txt)|*.txt",
            FileName = "DiemDo.csv",
            InitialDirectory = InitialDirectory()
        };
        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            PointFileWriter.Write(dialog.FileName, _form.Points.ToList(), format, digits, _form.SwapAxes);
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show(this, ex.Message, "iSurvey");
            return;
        }

        Request = new ExportPointsRequest
        {
            Count = _form.Points.Count,
            FileName = Path.GetFileName(dialog.FileName)
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

    private static void Paint(System.Windows.Controls.Button button, bool chosen)
    {
        button.Background = chosen ? ChosenFill : IdleFill;
        button.BorderBrush = chosen ? ChosenBorder : IdleBorder;
        button.Foreground = IdleText;
        button.BorderThickness = new Thickness(1);
        button.FontWeight = chosen ? FontWeights.SemiBold : FontWeights.Normal;
    }

    private static SolidColorBrush Freeze(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
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
    private bool _swapAxes;

    public ObservableCollection<SurveyPoint> Points { get; } = new();

    public bool FilterByLayer
    {
        get => _filterByLayer;
        set
        {
            if (Set(ref _filterByLayer, value))
                Notify(nameof(Hint));
        }
    }

    public string LayerName
    {
        get => _layerName;
        set => Set(ref _layerName, value ?? "");
    }

    public string DecimalsText
    {
        get => _decimalsText;
        set
        {
            if (Set(ref _decimalsText, value ?? ""))
                Notify(nameof(Hint));
        }
    }

    public bool SwapAxes
    {
        get => _swapAxes;
        set
        {
            if (Set(ref _swapAxes, value))
                Notify(nameof(Hint));
        }
    }

    public string Hint
    {
        get
        {
            var count = $"Đã chọn {Points.Count} điểm.";
            var digits = 3;
            if (!int.TryParse(DecimalsText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out digits))
                digits = 3;
            var swap = SwapAxes ? "Đảo X/Y. " : "";
            return $"{swap}Ghi phân cách chấm phẩy, thập phân là dấu chấm, {digits} số lẻ, xếp tên từ nhỏ đến lớn. {count}";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SetPoints(IReadOnlyList<SurveyPoint> points)
    {
        Points.Clear();
        foreach (var point in points)
            Points.Add(point);
        Notify(nameof(Hint));
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
