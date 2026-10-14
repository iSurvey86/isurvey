using System.Diagnostics;
using System.Windows;
using iSurvey.Core;
using iSurvey.Models;
using Win32OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace iSurvey.Dgn;

public partial class DgnImportWindow : Window
{
    private static readonly char[] InvalidLayerChars = ['<', '>', '/', '\\', '"', ':', ';', '?', '*', '|', '=', '`', ','];

    private readonly MapUserSettings _settings;
    private readonly string? _drawingPath;
    private readonly bool _configOnly;
    private readonly List<string> _batchFiles = [];

    /// <summary>Tùy chọn người dùng chốt (null nếu hủy hoặc ở chế độ chỉ cấu hình).</summary>
    public DgnImportOptions? Result { get; private set; }

    public DgnImportWindow(string? drawingPath, bool configOnly)
    {
        InitializeComponent();
        _drawingPath = drawingPath;
        _configOnly = configOnly;
        _settings = UserSettingsStore.Load(drawingPath);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (string.Equals(_settings.DgnUnits, "Sub", StringComparison.OrdinalIgnoreCase))
            UnitsSubRadio.IsChecked = true;
        else
            UnitsMasterRadio.IsChecked = true;

        switch (DgnImportOptions.ParseLayerMode(_settings.DgnLayerMode))
        {
            case DgnLayerMode.Prefix:
                LayerPrefixRadio.IsChecked = true;
                break;
            case DgnLayerMode.Map:
                LayerMapRadio.IsChecked = true;
                break;
            default:
                LayerKeepRadio.IsChecked = true;
                break;
        }

        PrefixBox.Text = string.IsNullOrWhiteSpace(_settings.DgnLayerPrefix) ? "DGN_" : _settings.DgnLayerPrefix;
        FixFontCheck.IsChecked = _settings.DgnFixFonts;
        ExplodeCheck.IsChecked = _settings.DgnExplodeCells;
        PurgeCheck.IsChecked = _settings.DgnPurgeLinetypes;
        ZoomCheck.IsChecked = _settings.DgnZoomToImport;

        switch (DgnImportOptions.ParseBatchExisting(_settings.DgnBatchExisting))
        {
            case DgnBatchExisting.Skip:
                ExistSkipRadio.IsChecked = true;
                break;
            case DgnBatchExisting.Overwrite:
                ExistOverwriteRadio.IsChecked = true;
                break;
            default:
                ExistNumberRadio.IsChecked = true;
                break;
        }

        if (string.Equals(_settings.DgnBatchMode, "Combine", StringComparison.OrdinalIgnoreCase))
            BatchCombineRadio.IsChecked = true;
        else
            BatchSeparateRadio.IsChecked = true;
        BatchKind_Changed(this, new RoutedEventArgs());

        if (_configOnly)
        {
            Title = "iSurvey — Cấu hình DGN";
            FileGroup.Visibility = Visibility.Collapsed;
            ConfirmButton.Content = "✓ Lưu cấu hình";
            ConfirmButton.MinWidth = 160;
        }

        RefreshMapInfo();
    }

    private void OnActivated(object? sender, EventArgs e) => RefreshMapInfo();

    private void RefreshMapInfo()
    {
        try
        {
            var map = DgnLayerMap.Load();
            var source = map.SourcePath is null
                ? "không đọc được file"
                : map.SourcePath.StartsWith(Path.GetDirectoryName(DgnLayerMap.UserPath) ?? "\u0001",
                    StringComparison.OrdinalIgnoreCase)
                    ? "bản của bạn trong %AppData%\\iSurvey"
                    : "bản mặc định đi kèm";
            MapInfoText.Text = $"{map.Levels.Count} Level đã khai báo — {source}.";
        }
        catch (System.Exception ex)
        {
            MapInfoText.Text = "Không đọc được bảng ánh xạ: " + ex.Message;
        }
    }

    private bool IsBatchMode => ModeBatchRadio?.IsChecked == true;

    private void BatchKind_Changed(object sender, RoutedEventArgs e)
    {
        if (ExistPanel is null || BatchCombineRadio is null)
            return;

        // Tùy chọn "đã có .dwg" chỉ áp dụng khi không ghép (mỗi file một .dwg).
        ExistPanel.Visibility = BatchCombineRadio.IsChecked == true ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Mode_Changed(object sender, RoutedEventArgs e)
    {
        // Sự kiện Checked bắn cả lúc InitializeComponent, khi các control phía sau chưa tạo.
        if (BatchPanel is null || PathBox is null || ConfirmButton is null)
            return;

        var batch = IsBatchMode;
        BatchPanel.Visibility = batch ? Visibility.Visible : Visibility.Collapsed;
        PathBox.IsReadOnly = batch;
        PathBox.Text = string.Empty;
        _batchFiles.Clear();
        PathBox.ToolTip = batch
            ? "Bấm \"Chọn file...\" rồi chọn nhiều file .dgn (Ctrl+A để chọn hết trong thư mục)"
            : "Đường dẫn file .dgn (MicroStation V7 hoặc V8)";
        if (!_configOnly)
            ConfirmButton.Content = batch ? "✓ Chạy Batch" : "✓ Thực hiện Nhập DGN";
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var batch = IsBatchMode;
        var dialog = new Win32OpenFileDialog
        {
            Title = batch ? "Chọn nhiều file MicroStation DGN" : "Chọn file MicroStation DGN",
            Filter = "MicroStation (*.dgn)|*.dgn",
            CheckFileExists = true,
            Multiselect = batch
        };

        if (Directory.Exists(_settings.DgnLastFolder))
            dialog.InitialDirectory = _settings.DgnLastFolder;

        if (dialog.ShowDialog(this) != true)
            return;

        if (!batch)
        {
            PathBox.Text = dialog.FileName;
            return;
        }

        _batchFiles.Clear();
        _batchFiles.AddRange(dialog.FileNames.OrderBy(f => f, StringComparer.OrdinalIgnoreCase));
        PathBox.Text = _batchFiles.Count == 1
            ? _batchFiles[0]
            : $"{_batchFiles.Count} file — {Path.GetDirectoryName(_batchFiles[0])}";
        PathBox.ToolTip = string.Join(Environment.NewLine, _batchFiles.Select(Path.GetFileName));
    }

    private void OpenMap_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DgnLayerMap.EnsureUserCopy();
            Process.Start(new ProcessStartInfo("notepad.exe", $"\"{path}\"") { UseShellExecute = true });
            LayerMapRadio.IsChecked = true;
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show(this, "Không mở được bảng ánh xạ: " + ex.Message, "iSurvey",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        var path = PathBox.Text.Trim().Trim('"');
        if (!_configOnly && IsBatchMode)
        {
            if (_batchFiles.Count == 0)
            {
                System.Windows.MessageBox.Show(this, "Hãy chọn ít nhất một file .dgn cho Batch.", "iSurvey",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var missing = _batchFiles.Where(f => !File.Exists(f)).ToList();
            if (missing.Count > 0)
            {
                System.Windows.MessageBox.Show(this, "Không tìm thấy file: " + missing[0], "iSurvey",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            path = _batchFiles[0];
        }
        else if (!_configOnly)
        {
            if (path.Length == 0 || !File.Exists(path))
            {
                System.Windows.MessageBox.Show(this, "Hãy chọn một file .dgn tồn tại.", "iSurvey",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.Equals(Path.GetExtension(path), ".dgn", StringComparison.OrdinalIgnoreCase))
            {
                System.Windows.MessageBox.Show(this, "File phải có đuôi .dgn.", "iSurvey",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        var mode = LayerPrefixRadio.IsChecked == true ? DgnLayerMode.Prefix
            : LayerMapRadio.IsChecked == true ? DgnLayerMode.Map
            : DgnLayerMode.Keep;

        var prefix = PrefixBox.Text.Trim();
        if (mode == DgnLayerMode.Prefix && (prefix.Length == 0 || prefix.IndexOfAny(InvalidLayerChars) >= 0))
        {
            System.Windows.MessageBox.Show(this, "Tiền tố layer trống hoặc chứa ký tự không hợp lệ.", "iSurvey",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _settings.DgnUnits = UnitsSubRadio.IsChecked == true ? "Sub" : "Master";
        _settings.DgnLayerMode = DgnImportOptions.LayerModeToText(mode);
        if (prefix.Length > 0 && prefix.IndexOfAny(InvalidLayerChars) < 0)
            _settings.DgnLayerPrefix = prefix;
        _settings.DgnFixFonts = FixFontCheck.IsChecked == true;
        _settings.DgnExplodeCells = ExplodeCheck.IsChecked == true;
        _settings.DgnPurgeLinetypes = PurgeCheck.IsChecked == true;
        _settings.DgnZoomToImport = ZoomCheck.IsChecked == true;
        var existing = ExistSkipRadio.IsChecked == true ? DgnBatchExisting.Skip
            : ExistOverwriteRadio.IsChecked == true ? DgnBatchExisting.Overwrite
            : DgnBatchExisting.Number;
        _settings.DgnBatchExisting = DgnImportOptions.BatchExistingToText(existing);
        var combine = BatchCombineRadio.IsChecked == true;
        _settings.DgnBatchMode = combine ? "Combine" : "Separate";

        if (!_configOnly)
        {
            var folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder))
                _settings.DgnLastFolder = folder;
        }

        UserSettingsStore.Save(_drawingPath, _settings);

        if (!_configOnly)
        {
            Result = DgnImportOptions.FromSettings(_settings, path);
            if (IsBatchMode)
            {
                Result.BatchFiles = _batchFiles.ToList();
                Result.BatchExisting = existing;
                Result.BatchCombine = combine;
            }
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
