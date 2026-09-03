using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using iSurvey.Core;
using iSurvey.Models;
using iSurvey.Modules.Map;
using Win32SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;

namespace iSurvey.UI.KmlExport;

public partial class KmlExportWindow : Window
{
    private readonly ProvinceCatalog _catalog = new();
    private readonly MapUserSettings _saved;
    private double _provinceMeridian = 105;
    private double _centralMeridian = 105;

    public KmlExportSettings? Settings { get; private set; }

    public KmlExportWindow(string? drawingPath)
    {
        InitializeComponent();
        _saved = UserSettingsStore.Load(drawingPath);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyLightComboTheme(ProvinceCombo);

        if (_saved.ZoneWidthDegrees == 6)
            Zone6Radio.IsChecked = true;
        else
            Zone3Radio.IsChecked = true;

        var sorted = _catalog.Groups
            .OrderBy(g => g.ProvinceName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        ProvinceCombo.ItemsSource = sorted;
        RestoreSavedProvince(sorted);
    }

    private void RestoreSavedProvince(IReadOnlyList<ProvinceCrsGroup> sorted)
    {
        ProvinceCrsGroup? group = null;
        if (!string.IsNullOrWhiteSpace(_saved.ProvinceName))
        {
            group = sorted.FirstOrDefault(g =>
                g.ProvinceName.Equals(_saved.ProvinceName, StringComparison.CurrentCultureIgnoreCase));
        }

        group ??= sorted.FirstOrDefault(g =>
                      g.ProvinceName.Contains("Hà Nội", StringComparison.OrdinalIgnoreCase))
                  ?? sorted.FirstOrDefault();

        if (group is null)
            return;

        ProvinceCombo.SelectedItem = group;
    }

    private void ProvinceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProvinceCombo.SelectedItem is not ProvinceCrsGroup group)
            return;

        var area = ResolveArea(group);
        if (area is null)
        {
            MeridianLabel.Text = "Kinh tuyến trục: —";
            return;
        }

        try
        {
            _provinceMeridian = _catalog.GetCentralMeridian(area.SourceProvinceKey);
            RefreshMeridianLabel();
        }
        catch (System.Exception ex)
        {
            MeridianLabel.Text = ex.Message;
        }
    }

    private void Zone_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;
        RefreshMeridianLabel();
    }

    private void RefreshMeridianLabel()
    {
        var zone = Zone6Radio.IsChecked == true ? 6 : 3;
        _centralMeridian = zone == 6
            ? CoordinateService.SnapToTm6CentralMeridian(_provinceMeridian)
            : _provinceMeridian;

        var tag = zone == 6 ? "TM-6" : "TM-3";
        MeridianLabel.Text = string.Create(CultureInfo.InvariantCulture,
            $"Kinh tuyến trục: {_centralMeridian:0.##}° ({tag})");
    }

    private LegacyAreaEntry? ResolveArea(ProvinceCrsGroup group)
    {
        if (group.LegacyAreas.Count == 0)
            return null;

        if (group.LegacyAreas.Count == 1)
            return group.LegacyAreas[0];

        if (!string.IsNullOrWhiteSpace(_saved.AreaLabel))
        {
            var match = group.LegacyAreas.FirstOrDefault(a =>
                a.Label.Equals(_saved.AreaLabel, StringComparison.CurrentCultureIgnoreCase));
            if (match is not null)
                return match;
        }

        return group.LegacyAreas[0];
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (ProvinceCombo.SelectedItem is not ProvinceCrsGroup)
        {
            System.Windows.MessageBox.Show(this, "Vui lòng chọn tỉnh / thành phố.", "iSurvey",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var zone = Zone6Radio.IsChecked == true ? 6 : 3;
        RefreshMeridianLabel();

        if (_centralMeridian is < 99 or > 111)
        {
            System.Windows.MessageBox.Show(this, "Kinh tuyến trục không hợp lệ — chọn lại tỉnh.", "iSurvey",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var useKmz = KmzRadio.IsChecked == true;
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var defaultName = useKmz
            ? $"isurvey_export_{stamp}.kmz"
            : $"isurvey_export_{stamp}.kml";

        var dialog = new Win32SaveFileDialog
        {
            Title = "Lưu xuất Google Earth",
            Filter = useKmz
                ? "Google Earth KMZ (*.kmz)|*.kmz"
                : "Google Earth KML (*.kml)|*.kml",
            DefaultExt = useKmz ? ".kmz" : ".kml",
            AddExtension = true,
            FileName = defaultName
        };

        if (dialog.ShowDialog(this) != true)
            return;

        Settings = new KmlExportSettings
        {
            CentralMeridian = _centralMeridian,
            ZoneWidthDegrees = zone,
            SelectionOnly = SelectionRadio.IsChecked == true,
            UseKmz = useKmz,
            GroupByLayer = GroupByLayerCheck.IsChecked == true,
            UseElevationZ = UseZCheck.IsChecked == true,
            OpenAfterExport = OpenAfterCheck.IsChecked == true,
            OutputPath = dialog.FileName
        };

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private static void ApplyLightComboTheme(System.Windows.Controls.ComboBox combo)
    {
        var bg = WpfBrushes.White;
        var fg = new SolidColorBrush(WpfColor.FromRgb(0x1F, 0x29, 0x37));
        var hi = new SolidColorBrush(WpfColor.FromRgb(0xDB, 0xEA, 0xFE));
        combo.Resources[System.Windows.SystemColors.WindowBrushKey] = bg;
        combo.Resources[System.Windows.SystemColors.WindowTextBrushKey] = fg;
        combo.Resources[System.Windows.SystemColors.ControlBrushKey] = bg;
        combo.Resources[System.Windows.SystemColors.ControlTextBrushKey] = fg;
        combo.Resources[System.Windows.SystemColors.HighlightBrushKey] = hi;
        combo.Resources[System.Windows.SystemColors.HighlightTextBrushKey] = fg;
    }
}
