using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using iSurvey.Models;
using iSurvey.Modules.Import;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using WinFormsDialog = System.Windows.Forms.OpenFileDialog;

namespace iSurvey.UI.Import;

public sealed class TabImportOptions
{
    public required string Path { get; init; }
    public required string LabelField { get; init; }
    public bool DrawLabels { get; init; }
    public double TextHeight { get; init; }
    public bool DrawHatch { get; init; }
    public bool DrawOutline { get; init; }
    public int FeatureCount { get; init; }
    public bool NeedsTransform { get; init; }
    public string SrsText { get; init; } = "";
    public string TargetText { get; init; } = "";
}

public partial class TabImportWindow : Window
{
    private readonly MapUserSettings _settings;
    private TabFileSummary? _summary;

    public TabImportWindow(MapUserSettings settings)
    {
        _settings = settings;
        InitializeComponent();
    }

    public bool Accepted { get; private set; }

    public TabImportOptions? Options { get; private set; }

    public static TabImportOptions? Show(MapUserSettings settings)
    {
        var window = new TabImportWindow(settings);
        AcadApp.ShowModalWindow(window);
        return window.Accepted ? window.Options : null;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WinFormsDialog
        {
            Title = "Chọn file MapInfo TAB",
            Filter = "MapInfo TAB (*.tab)|*.tab",
            CheckFileExists = true
        };
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;

        PathBox.Text = dialog.FileName;
        Probe(dialog.FileName);
    }

    private void Probe(string path)
    {
        OkButton.IsEnabled = false;
        LabelField.Items.Clear();
        _summary = null;
        try
        {
            var summary = TabReader.Probe(path, _settings);
            _summary = summary;
            foreach (var field in summary.Fields)
                LabelField.Items.Add(field);
            if (summary.DefaultLabelField is not null)
                LabelField.SelectedItem = summary.DefaultLabelField;
            LabelField.IsEnabled = summary.Fields.Count > 0;
            SetStatus(summary.StatusText + " Layer tô và viền: TAB_[MDSD] (trống → TAB_KHAC). Nhãn: TAB_LABELS.", false);
            OkButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            var root = ex.GetBaseException().Message;
            SetStatus(root == ex.Message
                ? "Không đọc được file: " + ex.Message
                : "Không đọc được file: " + ex.Message + " — " + root, true);
        }
    }

    private void SetStatus(string text, bool error)
    {
        Status.Text = text;
        Status.Foreground = new SolidColorBrush(error
            ? System.Windows.Media.Color.FromRgb(0xB9, 0x1C, 0x1C)
            : System.Windows.Media.Color.FromRgb(0x33, 0x41, 0x55));
        StatusCard.Background = new SolidColorBrush(error
            ? System.Windows.Media.Color.FromRgb(0xFE, 0xF2, 0xF2)
            : System.Windows.Media.Color.FromRgb(0xF8, 0xFA, 0xFC));
        StatusCard.BorderBrush = new SolidColorBrush(error
            ? System.Windows.Media.Color.FromRgb(0xFE, 0xCA, 0xCA)
            : System.Windows.Media.Color.FromRgb(0xE5, 0xE7, 0xEB));
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (_summary is null)
            return;
        if (!double.TryParse(TextHeight.Text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var height)
            || height <= 0)
        {
            SetStatus("Chiều cao chữ phải là số dương.", true);
            return;
        }

        var field = LabelField.SelectedItem as string ?? _summary.DefaultLabelField ?? "";
        Options = new TabImportOptions
        {
            Path = _summary.Path,
            LabelField = field,
            DrawLabels = DrawLabels.IsChecked == true,
            TextHeight = height,
            DrawHatch = DrawHatch.IsChecked == true,
            DrawOutline = DrawOutline.IsChecked == true,
            FeatureCount = _summary.FeatureCount,
            NeedsTransform = _summary.NeedsTransform,
            SrsText = _summary.SrsText,
            TargetText = _summary.TargetText
        };
        Accepted = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    private void Header_Drag(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;
        Close();
    }
}
