using System.Windows;
using iSurvey.Core;
using iSurvey.Models;
using Win32SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace iSurvey.Dgn;

public partial class DgnExportWindow : Window
{
    private readonly MapUserSettings _settings;
    private readonly string? _drawingPath;
    private readonly int _selectedCount;

    /// <summary>Tùy chọn người dùng chốt (null nếu hủy).</summary>
    public DgnExportOptions? Result { get; private set; }

    public DgnExportWindow(string? drawingPath, int selectedCount)
    {
        InitializeComponent();
        _drawingPath = drawingPath;
        _selectedCount = selectedCount;
        _settings = UserSettingsStore.Load(drawingPath);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_selectedCount > 0)
        {
            SelectedInfo.Text = $"Đang chọn {_selectedCount} đối tượng.";
            if (DgnExportOptions.ParseScope(_settings.DgnExportScope) == DgnExportScope.Selected)
                RbSelected.IsChecked = true;
        }
        else
        {
            RbSelected.IsEnabled = false;
            SelectedInfo.Text = "Chưa chọn đối tượng nào — hãy chọn trước rồi gõ lại lệnh C2D để dùng mục này.";
        }

        if (DgnExportOptions.ParseVersion(_settings.DgnExportVersion) == DgnExportVersion.V7)
            RbV7.IsChecked = true;
        else
            RbV8.IsChecked = true;

        ChkPurge.IsChecked = _settings.DgnExportPurge;
        ChkOverwrite.IsChecked = _settings.DgnExportOverwrite;
        UpdateSeedInfo();
    }

    private DgnExportVersion SelectedVersion => RbV7.IsChecked == true ? DgnExportVersion.V7 : DgnExportVersion.V8;

    private void Version_Changed(object sender, RoutedEventArgs e) => UpdateSeedInfo();

    private void UpdateSeedInfo()
    {
        if (SeedInfo is null)
            return;

        var version = SelectedVersion;
        var seed = DgnSeedLocator.Resolve(
            version, version == DgnExportVersion.V7 ? _settings.DgnExportSeedV7 : _settings.DgnExportSeedV8);
        SeedInfo.Text = seed is null
            ? "⚠ Không tìm thấy file Seed phù hợp"
            : "Seed: " + seed.Source;
    }

    private void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        var scope = RbSelected.IsChecked == true ? DgnExportScope.Selected : DgnExportScope.All;
        var version = SelectedVersion;
        var overwrite = ChkOverwrite.IsChecked == true;

        var seed = DgnSeedLocator.Resolve(
            version, version == DgnExportVersion.V7 ? _settings.DgnExportSeedV7 : _settings.DgnExportSeedV8);
        if (seed is null)
        {
            System.Windows.MessageBox.Show(this,
                $"Không tìm thấy file Seed cho {(version == DgnExportVersion.V7 ? "V7" : "V8")}.\n\n" +
                "Hãy đặt file VN2000_Seed2D_V7.dgn / VN2000_Seed2D_V8.dgn vào thư mục Contents\\Seeds của bundle iSurvey.",
                "iSurvey", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var defaultName = string.IsNullOrWhiteSpace(_drawingPath)
            ? "Drawing1"
            : Path.GetFileNameWithoutExtension(_drawingPath);

        var initial = !string.IsNullOrWhiteSpace(_settings.DgnExportLastFolder) && Directory.Exists(_settings.DgnExportLastFolder)
            ? _settings.DgnExportLastFolder
            : !string.IsNullOrWhiteSpace(_drawingPath) ? Path.GetDirectoryName(_drawingPath) : null;

        var dialog = new Win32SaveFileDialog
        {
            Title = "Lưu file MicroStation DGN",
            Filter = "MicroStation DGN (*.dgn)|*.dgn",
            DefaultExt = "dgn",
            AddExtension = true,
            FileName = defaultName + ".dgn",
            OverwritePrompt = !overwrite
        };
        if (!string.IsNullOrEmpty(initial))
            dialog.InitialDirectory = initial;

        if (dialog.ShowDialog(this) != true)
            return;

        _settings.DgnExportScope = scope == DgnExportScope.Selected ? "Selected" : "All";
        _settings.DgnExportVersion = version == DgnExportVersion.V7 ? "V7" : "V8";
        _settings.DgnExportPurge = ChkPurge.IsChecked == true;
        _settings.DgnExportOverwrite = overwrite;
        var folder = Path.GetDirectoryName(dialog.FileName);
        if (!string.IsNullOrEmpty(folder))
            _settings.DgnExportLastFolder = folder;
        UserSettingsStore.Save(_drawingPath, _settings);

        Result = DgnExportOptions.FromSettings(_settings);
        Result.OutputPath = dialog.FileName;
        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
