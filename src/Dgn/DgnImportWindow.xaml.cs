using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using iSurvey.Core;
using iSurvey.Models;

namespace iSurvey.Dgn;

public partial class DgnImportWindow : Window
{
    private readonly string? _drawingPath;
    private readonly ObservableCollection<DgnFileItem> _files = [];

    public DgnImportOptions? Result { get; private set; }

    public DgnImportWindow(string? drawingPath)
    {
        _drawingPath = drawingPath;
        InitializeComponent();
        FileGrid.ItemsSource = _files;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var s = UserSettingsStore.Load(_drawingPath);

        if (string.Equals(s.DgnBatchMode, "Separate", StringComparison.OrdinalIgnoreCase))
            SeparateRadio.IsChecked = true;
        else
            BatchRadio.IsChecked = true;

        if (string.Equals(s.DgnLayerMode, "Prefix", StringComparison.OrdinalIgnoreCase))
            PrefixRadio.IsChecked = true;
        else
            KeepRadio.IsChecked = true;

        PrefixBox.Text = string.IsNullOrWhiteSpace(s.DgnLayerPrefix) || s.DgnLayerPrefix.Trim() == "DGN_"
            ? "iSurvey_"
            : s.DgnLayerPrefix.Trim();

        FontCheck.IsChecked = s.DgnFixFonts;
        ExplodeCheck.IsChecked = s.DgnExplodeCells;

        RefreshTotal();
        UpdateMoveButtons();
        PaintSheet();
    }

    private void FileGrid_SizeChanged(object sender, SizeChangedEventArgs e) => PaintSheet();

    /// <summary>Kẻ ô trống phía dưới danh sách, nét mờ giống Excel.</summary>
    private void PaintSheet()
    {
        if (SheetHost is null || FileGrid.ActualHeight <= 0)
            return;

        var header = FindVisualChild<System.Windows.Controls.Primitives.DataGridColumnHeadersPresenter>(FileGrid);
        var headerHeight = header?.ActualHeight ?? 0;
        if (headerHeight <= 0)
            headerHeight = 34;

        var scroll = FindVisualChild<ScrollViewer>(FileGrid);
        var bar = scroll?.ComputedVerticalScrollBarVisibility == Visibility.Visible
            ? SystemParameters.VerticalScrollBarWidth
            : 0;
        SheetHost.Margin = new Thickness(0, headerHeight, bar, 0);

        const double rowHeight = 32;
        var rows = Math.Max(1, (int)Math.Ceiling(Math.Max(0, SheetHost.ActualHeight) / rowHeight));
        if (SheetHost.RowDefinitions.Count == rows)
            return;

        SheetHost.RowDefinitions.Clear();
        SheetHost.Children.Clear();
        var line = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE1, 0xE6, 0xEC));
        line.Freeze();
        for (var r = 0; r < rows; r++)
        {
            SheetHost.RowDefinitions.Add(new RowDefinition { Height = new GridLength(rowHeight) });
            for (var c = 0; c < 4; c++)
            {
                var cell = new Border
                {
                    BorderBrush = line,
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Background = System.Windows.Media.Brushes.White
                };
                Grid.SetRow(cell, r);
                Grid.SetColumn(cell, c);
                SheetHost.Children.Add(cell);
            }
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T found)
                return found;
            var nested = FindVisualChild<T>(child);
            if (nested is not null)
                return nested;
        }

        return null;
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "MicroStation DGN (*.dgn)|*.dgn|Tất cả (*.*)|*.*",
            Title = "Chọn các tệp DGN",
            Multiselect = true,
            CheckFileExists = true
        };

        var start = UserSettingsStore.Load(_drawingPath).DgnLastFolder;
        if (!string.IsNullOrWhiteSpace(start) && System.IO.Directory.Exists(start))
            dialog.InitialDirectory = start;
        else if (!string.IsNullOrWhiteSpace(_drawingPath))
        {
            var folder = System.IO.Path.GetDirectoryName(_drawingPath);
            if (!string.IsNullOrWhiteSpace(folder) && System.IO.Directory.Exists(folder))
                dialog.InitialDirectory = folder;
        }

        if (dialog.ShowDialog() != true)
            return;

        var known = new HashSet<string>(_files.Select(f => f.FullPath), StringComparer.OrdinalIgnoreCase);
        foreach (var path in dialog.FileNames)
        {
            if (!known.Add(path))
                continue;
            _files.Add(DgnFileItem.FromPath(path));
        }

        Renumber();
        if (_files.Count > 0)
            FileGrid.SelectedIndex = _files.Count - 1;
    }

    private void Up_Click(object sender, RoutedEventArgs e) => MoveSelected(-1);

    private void Down_Click(object sender, RoutedEventArgs e) => MoveSelected(1);

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (FileGrid.SelectedItem is not DgnFileItem item)
            return;

        var index = _files.IndexOf(item);
        _files.Remove(item);
        Renumber();
        if (_files.Count > 0)
            FileGrid.SelectedIndex = Math.Min(index, _files.Count - 1);
    }

    private void MoveSelected(int delta)
    {
        if (FileGrid.SelectedItem is not DgnFileItem item)
            return;

        var index = _files.IndexOf(item);
        var next = index + delta;
        if (index < 0 || next < 0 || next >= _files.Count)
            return;

        _files.Move(index, next);
        Renumber();
        FileGrid.SelectedIndex = next;
    }

    private void FileGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateMoveButtons();

    private void Renumber()
    {
        for (var i = 0; i < _files.Count; i++)
            _files[i].Index = i + 1;
        FileGrid.Items.Refresh();
        RefreshTotal();
        UpdateMoveButtons();
    }

    private void RefreshTotal() => TotalLabel.Text = $"Tổng số: {_files.Count} tệp";

    private void UpdateMoveButtons()
    {
        var index = FileGrid.SelectedIndex;
        UpBtn.IsEnabled = index > 0;
        DownBtn.IsEnabled = index >= 0 && index < _files.Count - 1;
        RemoveBtn.IsEnabled = index >= 0;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (_files.Count == 0)
        {
            System.Windows.MessageBox.Show(this, "Hãy thêm ít nhất một tệp DGN.", "iSurvey",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var prefix = PrefixRadio.IsChecked == true;
        var prefixText = (PrefixBox.Text ?? string.Empty).Trim();
        if (prefix && (prefixText.Length == 0 || prefixText.IndexOfAny(['<', '>', '/', '\\', '"', ':', ';', '?', '*', '|', '=', '`', ',']) >= 0))
        {
            System.Windows.MessageBox.Show(this, "Tiền tố layer không hợp lệ.", "iSurvey",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            PrefixBox.Focus();
            return;
        }

        var settings = UserSettingsStore.Load(_drawingPath);
        settings.DgnBatchMode = SeparateRadio.IsChecked == true ? "Separate" : "Combine";
        settings.DgnLayerMode = prefix ? "Prefix" : "Keep";
        settings.DgnLayerPrefix = prefixText.Length == 0 ? "iSurvey_" : prefixText;
        settings.DgnFixFonts = FontCheck.IsChecked == true;
        settings.DgnExplodeCells = ExplodeCheck.IsChecked == true;
        settings.DgnPurgeLinetypes = true;
        settings.DgnZoomToImport = true;
        settings.DgnUnits = "Master";
        if (_files.Count > 0)
        {
            var folder = System.IO.Path.GetDirectoryName(_files[0].FullPath);
            if (!string.IsNullOrWhiteSpace(folder))
                settings.DgnLastFolder = folder;
        }

        UserSettingsStore.Save(_drawingPath, settings);

        var options = DgnImportOptions.FromSettings(settings, _files[0].FullPath);
        options.FilePath = _files[0].FullPath;
        options.BatchFiles = _files.Select(f => f.FullPath).ToList();
        options.BatchCombine = settings.DgnBatchMode == "Combine";
        options.BatchExisting = DgnBatchExisting.Number;
        options.PurgeJunk = true;
        options.ZoomToImport = true;
        options.UseSubUnits = false;
        Result = options;
        DialogResult = true;
        Close();
    }
}

public sealed class DgnFileItem
{
    public int Index { get; set; }

    public string Name { get; init; } = string.Empty;

    public string Folder { get; init; } = string.Empty;

    public string FullPath { get; init; } = string.Empty;

    public string SizeText { get; init; } = string.Empty;

    public static DgnFileItem FromPath(string path)
    {
        long bytes = 0;
        try
        {
            bytes = new System.IO.FileInfo(path).Length;
        }
        catch
        {
            // bảng vẫn hiện tên tệp khi không đọc được kích thước
        }

        return new DgnFileItem
        {
            Name = System.IO.Path.GetFileName(path),
            Folder = System.IO.Path.GetDirectoryName(path) ?? string.Empty,
            FullPath = path,
            SizeText = FormatSize(bytes)
        };
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:0.#} KB";
        return $"{bytes / 1024.0 / 1024.0:0.#} MB";
    }
}
