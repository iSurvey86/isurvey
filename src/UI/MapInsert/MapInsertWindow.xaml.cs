using System.Globalization;

using System.IO;

using System.Text.Json;

using System.Windows;

using System.Windows.Controls;

using System.Windows.Media;

using iSurvey.Core;

using iSurvey.Models;

using iSurvey.Modules.Map;



namespace iSurvey.UI.MapInsert;



public partial class MapInsertWindow : Window

{

    private readonly ProvinceCatalog _catalog = new();

    private readonly MapUserSettings _savedSettings;

    private double _provinceMeridian = 105;

    private bool _updating;



    public MapInsertSettings? Settings { get; private set; }



    public MapInsertWindow(string? drawingPath, MapInsertAction defaultAction)

    {

        InitializeComponent();

        _savedSettings = UserSettingsStore.Load(drawingPath);

        Loaded += OnLoaded;



        if (defaultAction == MapInsertAction.DeleteGoogleEarth)

            Title = "iSurvey — Xóa Google Earth";

    }



    private void OnLoaded(object sender, RoutedEventArgs e)

    {

        ApplyLightComboTheme(ProvinceCombo);

        ApplyLightComboTheme(AreaCombo);

        ApplyLightComboTheme(BasemapCombo);



        BasemapCombo.ItemsSource = LoadBasemapSources();

        BasemapCombo.SelectedValue = _savedSettings.BasemapId is { Length: > 0 } id

            ? id

            : TileCacheService.DefaultSourceId;

        BoundaryRadio.IsChecked = _savedSettings.UseBoundaryClip;
        ViewportRadio.IsChecked = !_savedSettings.UseBoundaryClip;

        if (_savedSettings.ZoneWidthDegrees == 6)
            Zone6Radio.IsChecked = true;
        else
            Zone3Radio.IsChecked = true;

        var sorted = _catalog.Groups

            .OrderBy(g => g.ProvinceName, StringComparer.CurrentCultureIgnoreCase)

            .ToList();

        ProvinceCombo.ItemsSource = sorted;

        RestoreSavedProvince(sorted);

    }



    private static List<MapSourceEntry> LoadBasemapSources()

    {

        var asmDir = Path.GetDirectoryName(typeof(MapInsertWindow).Assembly.Location)

                     ?? AppContext.BaseDirectory;

        var path = Path.Combine(asmDir, "Data", "isurvey_map_sources.json");

        if (!File.Exists(path))

            path = Path.Combine(AppContext.BaseDirectory, "Data", "isurvey_map_sources.json");



        var json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<List<MapSourceEntry>>(json) ?? [];

    }



    private void RestoreSavedProvince(IReadOnlyList<ProvinceCrsGroup> sorted)

    {

        ProvinceCrsGroup? group = null;

        if (!string.IsNullOrWhiteSpace(_savedSettings.ProvinceName))

        {

            group = sorted.FirstOrDefault(g =>

                g.ProvinceName.Equals(_savedSettings.ProvinceName, StringComparison.CurrentCultureIgnoreCase));

        }



        group ??= sorted.FirstOrDefault(g =>

                      g.ProvinceName.Contains("Hà Nội", StringComparison.OrdinalIgnoreCase))

                  ?? sorted.FirstOrDefault();



        if (group is null)

            return;



        ProvinceCombo.SelectedItem = group;



        if (!string.IsNullOrWhiteSpace(_savedSettings.AreaLabel)

            && group.LegacyAreas.Count > 1

            && AreaCombo.ItemsSource is IEnumerable<LegacyAreaEntry> areas)

        {

            var area = areas.FirstOrDefault(a =>

                a.Label.Equals(_savedSettings.AreaLabel, StringComparison.CurrentCultureIgnoreCase));

            if (area is not null)

                AreaCombo.SelectedItem = area;

        }



        if (_savedSettings.CentralMeridian is >= 102 and <= 111)

        {

            MeridianBox.Text = _savedSettings.CentralMeridian.ToString("0.##", CultureInfo.InvariantCulture);

        }

    }



    private static void ApplyLightComboTheme(System.Windows.Controls.ComboBox combo)

    {

        var bg = System.Windows.Media.Brushes.White;
        var fg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x29, 0x37));
        var hi = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xDB, 0xEA, 0xFE));
        combo.Resources[System.Windows.SystemColors.WindowBrushKey] = bg;
        combo.Resources[System.Windows.SystemColors.WindowTextBrushKey] = fg;
        combo.Resources[System.Windows.SystemColors.ControlBrushKey] = bg;
        combo.Resources[System.Windows.SystemColors.ControlTextBrushKey] = fg;
        combo.Resources[System.Windows.SystemColors.HighlightBrushKey] = hi;
        combo.Resources[System.Windows.SystemColors.HighlightTextBrushKey] = fg;

    }



    private void ProvinceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)

    {

        if (_updating || ProvinceCombo.SelectedItem is not ProvinceCrsGroup group)

            return;



        _updating = true;

        try

        {

            var areas = group.LegacyAreas;

            if (areas.Count > 1)

            {

                AreaLabel.Visibility = Visibility.Visible;

                AreaCombo.Visibility = Visibility.Visible;

                AreaCombo.ItemsSource = areas;

                AreaCombo.SelectedIndex = 0;

            }

            else

            {

                AreaLabel.Visibility = Visibility.Collapsed;

                AreaCombo.Visibility = Visibility.Collapsed;

                AreaCombo.ItemsSource = null;

                if (areas.Count == 1)

                    ApplyMeridian(areas[0]);

            }

        }

        finally

        {

            _updating = false;

        }

    }



    private void AreaCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)

    {

        if (_updating || AreaCombo.SelectedItem is not LegacyAreaEntry area)

            return;



        ApplyMeridian(area);

    }



    private void ApplyMeridian(LegacyAreaEntry area)

    {

        try

        {

            _provinceMeridian = _catalog.GetCentralMeridian(area.SourceProvinceKey);
            ApplyMeridianValue(_provinceMeridian, area.Label);

        }

        catch (System.Exception ex)

        {

            MeridianHint.Text = ex.Message;

        }

    }

    private void Zone_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        var label = AreaCombo.SelectedItem is LegacyAreaEntry a
            ? a.Label
            : (ProvinceCombo.SelectedItem as ProvinceCrsGroup)?.ProvinceName ?? "";
        ApplyMeridianValue(_provinceMeridian, label);
    }

    private void ApplyMeridianValue(double provinceMeridian, string label)
    {
        _provinceMeridian = provinceMeridian;
        var zone = Zone6Radio.IsChecked == true ? 6 : 3;
        var meridian = zone == 6
            ? CoordinateService.SnapToTm6CentralMeridian(provinceMeridian)
            : provinceMeridian;

        MeridianBox.Text = meridian.ToString("0.##", CultureInfo.InvariantCulture);
        MeridianHint.Text = zone == 6
            ? $"{label} — TM-6, kinh tuyến {meridian:0.##}° (99/105/111)"
            : $"{label} — TM-3, kinh tuyến trục {meridian:0.##}°";
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)

    {

        DialogResult = false;

        Close();

    }



    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();

        var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
        if (doc is not null)
            GeDeleteWorkflow.Run(doc);
    }



    private void Apply_Click(object sender, RoutedEventArgs e)

    {

        if (ProvinceCombo.SelectedItem is not ProvinceCrsGroup group)

        {

            System.Windows.MessageBox.Show(this, "Vui lòng chọn tỉnh / thành phố.", "iSurvey",

                MessageBoxButton.OK, MessageBoxImage.Warning);

            return;

        }



        if (!TryParseMeridian(out var meridian))

            return;

        var zone = Zone6Radio.IsChecked == true ? 6 : 3;
        if (zone == 6)
            meridian = CoordinateService.SnapToTm6CentralMeridian(meridian);

        LegacyAreaEntry? area = group.LegacyAreas.Count > 1

            ? AreaCombo.SelectedItem as LegacyAreaEntry

            : group.LegacyAreas.FirstOrDefault();



        Settings = new MapInsertSettings

        {

            ProvinceName = group.ProvinceName,

            AreaLabel = area?.Label ?? group.ProvinceName,

            CentralMeridian = meridian,

            ZoneWidthDegrees = zone,

            BasemapId = BasemapCombo.SelectedValue as string ?? TileCacheService.DefaultSourceId,

            AutoRefresh = true,

            UseBoundaryClip = BoundaryRadio.IsChecked == true,

            Action = MapInsertAction.Insert

        };



        DialogResult = true;

        Close();

    }



    private bool TryParseMeridian(out double meridian)

    {

        meridian = 0;

        if (!double.TryParse(MeridianBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out meridian)

            && !double.TryParse(MeridianBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out meridian))

        {

            System.Windows.MessageBox.Show(this, "Kinh tuyến trục không hợp lệ.", "iSurvey",

                MessageBoxButton.OK, MessageBoxImage.Warning);

            return false;

        }



        if (meridian is < 99 or > 111)

        {

            System.Windows.MessageBox.Show(this, "Kinh tuyến trục nên trong khoảng 99°–111°.", "iSurvey",

                MessageBoxButton.OK, MessageBoxImage.Warning);

            return false;

        }



        return true;

    }

}

