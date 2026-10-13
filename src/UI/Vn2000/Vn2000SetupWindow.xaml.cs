using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using iSurvey.Core;
using iSurvey.Models;
using iSurvey.Modules.Map;
namespace iSurvey.UI.Vn2000;

public partial class Vn2000SetupWindow : Window
{
    private readonly ProvinceCatalog _catalog = new();
    private readonly MapUserSettings _saved;
    private readonly Vn2000SetupMode _mode;
    private readonly string? _drawingPath;
    private bool _filling;
    private string _areaLabel = "";

    public Vn2000SetupResult? Result { get; private set; }

    public Vn2000SetupWindow(string? drawingPath, Vn2000SetupMode mode)
    {
        InitializeComponent();
        _drawingPath = drawingPath;
        _mode = mode;
        _saved = UserSettingsStore.Load(drawingPath);

        ImportGroup.Visibility = mode == Vn2000SetupMode.Import ? Visibility.Visible : Visibility.Collapsed;
        ExportGroup.Visibility = mode == Vn2000SetupMode.Export ? Visibility.Visible : Visibility.Collapsed;
        ConfirmButton.Content = mode switch
        {
            Vn2000SetupMode.Export => "Xác nhận & Xuất File",
            Vn2000SetupMode.InsertImage => "Xác nhận & Chèn ảnh",
            _ => "Xác nhận & Nhập File"
        };
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyDarkCombo(ProvinceCombo);

        InsertPointsCheck.IsChecked = _saved.ImportInsertPoints;
        LabelCheck.IsChecked = _saved.ImportLabelNames;
        TextHeightBox.Text = _saved.ImportTextHeight.ToString("0.##", CultureInfo.InvariantCulture);
        SimplifyCheck.IsChecked = _saved.ImportSimplifyVertices;
        ToleranceBox.Text = (_saved.ImportSimplifyTolerance > 0 ? _saved.ImportSimplifyTolerance : 0.2)
            .ToString("0.##", CultureInfo.InvariantCulture);
        SelectionRadio.IsChecked = _saved.ExportSelectionOnly;
        AllModelRadio.IsChecked = !_saved.ExportSelectionOnly;
        KmzRadio.IsChecked = _saved.ExportUseKmz;
        KmlRadio.IsChecked = !_saved.ExportUseKmz;
        GroupByLayerCheck.IsChecked = _saved.ExportGroupByLayer;
        UseZCheck.IsChecked = _saved.ExportUseElevationZ;
        OpenAfterCheck.IsChecked = _saved.ExportOpenAfter;

        if (_saved.ZoneWidthDegrees == 6)
            Zone6Radio.IsChecked = true;

        var sorted = _catalog.Groups
            .OrderBy(g => g.ProvinceName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        ProvinceCombo.ItemsSource = sorted;

        var group = FindSavedGroup(sorted)
                    ?? sorted.FirstOrDefault(g => g.ProvinceName.IndexOf("Hà Nội", StringComparison.OrdinalIgnoreCase) >= 0)
                    ?? sorted.FirstOrDefault();
        if (group is not null)
            ProvinceCombo.SelectedItem = group;

        // Lần đầu chưa lưu tỉnh: giữ kinh tuyến catalog. Đã lưu thì khôi phục cả số Độ/Phút đã sửa.
        if (!string.IsNullOrWhiteSpace(_saved.ProvinceName))
            SetMeridianBoxes(_saved.CentralMeridian);
        RefreshHint();
    }

    private ProvinceCrsGroup? FindSavedGroup(IReadOnlyList<ProvinceCrsGroup> groups)
    {
        if (!string.IsNullOrWhiteSpace(_saved.ProvinceName))
        {
            var byName = groups.FirstOrDefault(g =>
                g.ProvinceName.Equals(_saved.ProvinceName, StringComparison.CurrentCultureIgnoreCase));
            if (byName is not null)
                return byName;
        }

        if (!string.IsNullOrWhiteSpace(_saved.AreaLabel))
        {
            return groups.FirstOrDefault(g =>
                g.LegacyAreas.Any(a => a.Label.Equals(_saved.AreaLabel, StringComparison.CurrentCultureIgnoreCase)));
        }

        return null;
    }

    private void ProvinceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProvinceCombo.SelectedItem is not ProvinceCrsGroup group)
            return;

        var area = ResolveArea(group);
        _areaLabel = area?.Label ?? "";
        if (area is null)
            return;

        try
        {
            SetMeridianBoxes(_catalog.GetCentralMeridian(area.SourceProvinceKey));
        }
        catch (System.Exception ex)
        {
            MeridianHint.Text = ex.Message;
        }
    }

    private LegacyAreaEntry? ResolveArea(ProvinceCrsGroup group)
    {
        if (group.LegacyAreas.Count == 0)
            return null;
        if (!string.IsNullOrWhiteSpace(_saved.AreaLabel))
        {
            var match = group.LegacyAreas.FirstOrDefault(a =>
                a.Label.Equals(_saved.AreaLabel, StringComparison.CurrentCultureIgnoreCase));
            if (match is not null)
                return match;
        }

        return group.LegacyAreas[0];
    }

    private void Zone_Changed(object sender, RoutedEventArgs e) => RefreshHint();

    private void Meridian_TextChanged(object sender, TextChangedEventArgs e) => RefreshHint();

    private void SetMeridianBoxes(double meridian)
    {
        var deg = (int)Math.Truncate(meridian);
        var minutes = (int)Math.Round((meridian - deg) * 60.0, MidpointRounding.AwayFromZero);
        if (minutes >= 60)
        {
            deg++;
            minutes -= 60;
        }

        _filling = true;
        DegreeBox.Text = deg.ToString(CultureInfo.InvariantCulture);
        MinuteBox.Text = minutes.ToString(CultureInfo.InvariantCulture);
        _filling = false;
        RefreshHint();
    }

    private bool TryReadMeridian(out double entered, out string error)
    {
        entered = 0;
        error = "";
        if (!int.TryParse(DegreeBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var deg)
            || !int.TryParse(MinuteBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes))
        {
            error = "Độ và phút phải là số nguyên.";
            return false;
        }

        if (minutes is < 0 or > 59)
        {
            error = "Phút phải từ 0 đến 59.";
            return false;
        }

        entered = deg + minutes / 60.0;
        var zone = Zone6Radio.IsChecked == true ? 6 : 3;
        if (zone == 3 && !CoordinateService.IsValidCentralMeridian(entered, 3))
        {
            error = "Kinh tuyến trục TM-3 phải từ 102° đến 117°.";
            return false;
        }

        return true;
    }

    private void RefreshHint()
    {
        if (!IsLoaded || _filling || MeridianHint is null)
            return;

        if (!TryReadMeridian(out var entered, out var error))
        {
            MeridianHint.Text = error;
            return;
        }

        var zone = Zone6Radio.IsChecked == true ? 6 : 3;
        var used = zone == 6 ? CoordinateService.SnapToTm6CentralMeridian(entered) : entered;
        MeridianHint.Text = zone == 6 && Math.Abs(used - entered) > 1e-6
            ? $"Múi 6° dùng kinh tuyến {used:0.#}° (105 / 111 / 117)."
            : $"Sẽ chiếu theo kinh tuyến {used:0.###}° (TM-{zone}).";
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (ProvinceCombo.SelectedItem is not ProvinceCrsGroup group)
        {
            System.Windows.MessageBox.Show(this, "Hãy chọn tỉnh / thành phố.", "iSurvey");
            return;
        }

        if (!TryReadMeridian(out var entered, out var error))
        {
            System.Windows.MessageBox.Show(this, error, "iSurvey");
            return;
        }

        var zone = Zone6Radio.IsChecked == true ? 6 : 3;
        var used = zone == 6 ? CoordinateService.SnapToTm6CentralMeridian(entered) : entered;

        var textHeight = _saved.ImportTextHeight;
        var tolerance = _saved.ImportSimplifyTolerance > 0 ? _saved.ImportSimplifyTolerance : 0.2;
        if (_mode == Vn2000SetupMode.Import)
        {
            if (!double.TryParse(TextHeightBox.Text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out textHeight)
                || textHeight <= 0 || textHeight > 1000)
            {
                System.Windows.MessageBox.Show(this, "Cỡ chữ phải lớn hơn 0.", "iSurvey");
                return;
            }

            if (!double.TryParse(ToleranceBox.Text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out tolerance)
                || tolerance <= 0 || tolerance > 1000)
            {
                System.Windows.MessageBox.Show(this, "Sai số lọc đỉnh phải lớn hơn 0 mét.", "iSurvey");
                return;
            }
        }

        var useKmz = KmzRadio.IsChecked == true;

        Result = new Vn2000SetupResult
        {
            ProvinceName = group.ProvinceName,
            AreaLabel = _areaLabel,
            EnteredMeridian = entered,
            CentralMeridian = used,
            ZoneWidthDegrees = zone,
            InsertPoints = InsertPointsCheck.IsChecked == true,
            LabelNames = LabelCheck.IsChecked == true,
            TextHeight = textHeight,
            SimplifyVertices = SimplifyCheck.IsChecked == true,
            SimplifyTolerance = tolerance,
            SelectionOnly = SelectionRadio.IsChecked == true,
            UseKmz = useKmz,
            GroupByLayer = GroupByLayerCheck.IsChecked == true,
            UseElevationZ = UseZCheck.IsChecked == true,
            OpenAfterExport = OpenAfterCheck.IsChecked == true,
            OutputPath = ""
        };

        Persist(Result);
        DialogResult = true;
    }

    private void Persist(Vn2000SetupResult result)
    {
        _saved.ProvinceName = result.ProvinceName;
        _saved.AreaLabel = result.AreaLabel;
        _saved.CentralMeridian = result.CentralMeridian;
        _saved.ZoneWidthDegrees = result.ZoneWidthDegrees;
        if (_mode == Vn2000SetupMode.Import)
        {
            _saved.ImportInsertPoints = result.InsertPoints;
            _saved.ImportLabelNames = result.LabelNames;
            _saved.ImportTextHeight = result.TextHeight;
            _saved.ImportSimplifyVertices = result.SimplifyVertices;
            _saved.ImportSimplifyTolerance = result.SimplifyTolerance;
        }
        else if (_mode == Vn2000SetupMode.Export)
        {
            _saved.ExportSelectionOnly = result.SelectionOnly;
            _saved.ExportUseKmz = result.UseKmz;
            _saved.ExportGroupByLayer = result.GroupByLayer;
            _saved.ExportUseElevationZ = result.UseElevationZ;
            _saved.ExportOpenAfter = result.OpenAfterExport;
        }

        UserSettingsStore.Save(_drawingPath, _saved);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private static void ApplyDarkCombo(System.Windows.Controls.ComboBox combo)
    {
        var bg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1E, 0x22, 0x2A));
        var fg = System.Windows.Media.Brushes.White;
        var hi = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1B, 0x6C, 0xA8));
        combo.Resources[System.Windows.SystemColors.WindowBrushKey] = bg;
        combo.Resources[System.Windows.SystemColors.WindowTextBrushKey] = fg;
        combo.Resources[System.Windows.SystemColors.ControlBrushKey] = bg;
        combo.Resources[System.Windows.SystemColors.ControlTextBrushKey] = fg;
        combo.Resources[System.Windows.SystemColors.HighlightBrushKey] = hi;
        combo.Resources[System.Windows.SystemColors.HighlightTextBrushKey] = fg;
    }
}
