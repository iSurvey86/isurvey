using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using iSurvey.Modules.Frame;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using DataGrid = System.Windows.Controls.DataGrid;
using DataGridCell = System.Windows.Controls.DataGridCell;
using TextBox = System.Windows.Controls.TextBox;

namespace iSurvey.UI.Frame;

/// <summary>Dữ liệu cấp cho hộp thoại DANTRANG (hàm tính số mảnh theo khổ/tỷ lệ/chồng gối lấy từ lệnh).</summary>
public sealed class RouteLayoutDialogInput
{
    public required double RouteLengthM { get; init; }
    public required string[] Sizes { get; init; }
    public required int[] Scales { get; init; }
    public required RouteLayoutOptions Initial { get; init; }
    public required Dictionary<string, string> TemplateDefaults { get; init; }

    /// <summary>(khổ, mẫu số tỷ lệ, chồng gối m) → số mảnh.</summary>
    public required Func<string, int, double, int> CountPieces { get; init; }

    /// <summary>(khổ, mẫu số tỷ lệ) → chiều dài vùng bản đồ (m).</summary>
    public required Func<string, int, double> MapWidthM { get; init; }

    /// <summary>Số mảnh → (hàng, cột) gợi ý.</summary>
    public required Func<int, (int rows, int cols)> SuggestGrid { get; init; }
}

public sealed class FieldRow : INotifyPropertyChanged
{
    private string _value = "";

    public FieldRow(FrameField field)
    {
        Field = field;
    }

    public FrameField Field { get; }

    public int Stt => Field.Stt;

    public string Label => Field.Label;

    public string Value
    {
        get => _value;
        set
        {
            if (_value == value)
                return;
            _value = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public partial class RouteLayoutWindow : Window
{
    private static readonly Dictionary<string, string> PaperText = new()
    {
        ["A0"] = "1189 x 841",
        ["A1"] = "841 x 594",
        ["A2"] = "594 x 420",
        ["A3"] = "420 x 297",
        ["A4"] = "210 x 297"
    };

    private readonly RouteLayoutDialogInput _in;
    private readonly List<FieldRow> _rows = [];
    private bool _loading = true;
    private bool _gridTouched;
    private bool _overlapTouched;
    private int _pieces = 1;
    private string? _logoOwner;
    private string? _logoDesign;
    private bool _hideOwner;
    private bool _hideDesign;
    private readonly Dictionary<string, string> _signatures = new(StringComparer.OrdinalIgnoreCase);
    private bool _allowEdit;

    public RouteLayoutWindow(RouteLayoutDialogInput input)
    {
        _in = input;
        InitializeComponent();

        foreach (var s in input.Sizes)
            SizeBox.Items.Add(PaperText.TryGetValue(s, out var t) ? $"{s} ({t})" : s);
        foreach (var n in input.Scales)
            ScaleBox.Items.Add($"1/{n}");

        var init = input.Initial;
        SizeBox.SelectedIndex = Math.Max(0, Array.FindIndex(input.Sizes, s => s.Equals(init.Size, StringComparison.OrdinalIgnoreCase)));
        ScaleBox.SelectedIndex = Math.Max(0, Array.IndexOf(input.Scales, init.Scale));
        OverlapBox.Text = Fmt(init.OverlapM);
        RowsBox.Text = init.Rows.ToString(CultureInfo.InvariantCulture);
        ColsBox.Text = init.Cols.ToString(CultureInfo.InvariantCulture);
        GapBox.Text = Fmt(init.GapMm);
        PdfCheck.IsChecked = init.PlotPdf;
        _hideOwner = init.HideOwnerLogo;
        _hideDesign = init.HideDesignLogo;

        foreach (var f in FrameFields.All)
        {
            var row = new FieldRow(f);
            if (f.Auto)
                row.Value = "i/N  (tự động)";
            else if (init.Attributes.TryGetValue(f.Tag, out var v))
                row.Value = FrameFields.Clean(v);
            row.PropertyChanged += Row_PropertyChanged;
            _rows.Add(row);
        }

        LeftGrid.ItemsSource = _rows.Where(r => r.Stt <= 13).ToList();
        RightGrid.ItemsSource = _rows.Where(r => r.Stt > 13).ToList();
        _loading = false;
        RecalcPlan();
        UpdateContentEditor();
        UpdateLogoNote();
    }

    public RouteLayoutOptions? Result { get; private set; }

    public static RouteLayoutOptions? Show(RouteLayoutDialogInput input)
    {
        var w = new RouteLayoutWindow(input);
        AcadApp.ShowModalWindow(w);
        return w.Result;
    }

 private static string Fmt(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    private static bool TryNum(string text, out double v) =>
        double.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out v);

    private string CurrentSize => _in.Sizes[Math.Max(0, SizeBox.SelectedIndex)];

    private int CurrentScale => _in.Scales[Math.Max(0, ScaleBox.SelectedIndex)];

    private FieldRow? Selected => (LeftGrid.SelectedItem ?? RightGrid.SelectedItem) as FieldRow;

    // ---- Bố cục ----
    private void Plan_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading)
            return;
        if (!_overlapTouched)
        {
            _loading = true;
            OverlapBox.Text = Fmt(Math.Max(1, Math.Round(_in.MapWidthM(CurrentSize, CurrentScale) * 0.1)));
            _loading = false;
        }

        RecalcPlan();
    }

    private void Overlap_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading)
            return;
        _overlapTouched = true;
        RecalcPlan();
    }

    private void Grid_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading)
            return;
        _gridTouched = true;
        RecalcPlan();
    }

    private void RecalcPlan()
    {
        if (_loading)
            return;
        if (!TryNum(OverlapBox.Text, out var ov) || ov < 0)
        {
            ShowWarn("Chồng gối phải là số mét ≥ 0.");
            return;
        }

        var size = CurrentSize;
        var scale = CurrentScale;
        var wMeters = _in.MapWidthM(size, scale);
        _pieces = Math.Max(1, _in.CountPieces(size, scale, ov));
        InfoText.Text =
            $"Tuyến dài {_in.RouteLengthM:0.#} m  →  {_pieces} tờ khổ {size} tỷ lệ 1/{scale}   (vùng bản đồ mỗi tờ dài {wMeters:0.#} m, chồng gối {ov:0.#} m)";

        if (!_gridTouched)
        {
            var (r, c) = _in.SuggestGrid(_pieces);
            _loading = true;
            RowsBox.Text = r.ToString(CultureInfo.InvariantCulture);
            ColsBox.Text = c.ToString(CultureInfo.InvariantCulture);
            _loading = false;
        }

        if (int.TryParse(RowsBox.Text, out var rows) && int.TryParse(ColsBox.Text, out var cols) && rows >= 1 && cols >= 1)
        {
            if (rows > 99 || cols > 99)
                ShowWarn("Hàng và Cột tối đa 99.");
            else if (rows * cols < _pieces)
                ShowWarn($"Lưới {rows}×{cols} = {rows * cols} ô, chưa đủ chỗ cho {_pieces} tờ. Tăng Hàng/Cột.");
            else
                ShowWarn(null);
        }
        else
        {
            ShowWarn("Hàng và Cột phải là số nguyên ≥ 1.");
        }
    }

    private void ShowWarn(string? text)
    {
        WarnText.Text = text ?? "";
        WarnText.Visibility = text is null ? Visibility.Collapsed : Visibility.Visible;
        OkButton.IsEnabled = text is null;
    }

    // ---- Bảng khung tên ----
    private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading)
            return;
        // chỉ một bảng có dòng chọn tại một thời điểm
        _loading = true;
        if (ReferenceEquals(sender, LeftGrid) && LeftGrid.SelectedItem is not null)
            RightGrid.SelectedItem = null;
        else if (ReferenceEquals(sender, RightGrid) && RightGrid.SelectedItem is not null)
            LeftGrid.SelectedItem = null;
        _loading = false;
        UpdateContentEditor();
    }

    private void UpdateContentEditor()
    {
        var row = Selected;
        _loading = true;
        if (row is null)
        {
            ContentLabel.Text = "Nội dung:";
            ContentBox.Text = "";
            ContentBox.IsEnabled = false;
            SignPanel.Visibility = Visibility.Collapsed;
            LogoPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            ContentLabel.Text = $"Nội dung ({row.Stt}. {row.Label}):";
            ContentBox.Text = row.Field.Auto ? "Tự động: số thứ tự tờ / tổng số tờ (ví dụ 3/9)" : row.Value;
            ContentBox.IsEnabled = !row.Field.Auto;
            var sign = IsSignRow(row);
            SignPanel.Visibility = sign ? Visibility.Visible : Visibility.Collapsed;
            if (sign)
                SignClearButton.Visibility = _signatures.ContainsKey(row.Field.Tag) ? Visibility.Visible : Visibility.Collapsed;
            LogoPanel.Visibility = row.Stt is 1 or 5 ? Visibility.Visible : Visibility.Collapsed;
            if (row.Stt is 1 or 5)
            {
                LogoClearButton.Visibility = PendingLogo(row.Stt == 1) is null ? Visibility.Collapsed : Visibility.Visible;
                HideButton.Content = IsHidden(row.Stt == 1) ? "Hiện logo" : "Ẩn logo";
            }
        }

        _loading = false;
    }

    private void Content_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading)
            return;
        var row = Selected;
        if (row is not null && !row.Field.Auto)
            row.Value = ContentBox.Text;
    }

    private void Row_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // sửa trong ô bảng → thanh "Nội dung" phía dưới theo kịp
        if (_loading || !ReferenceEquals(sender, Selected) || ContentBox.IsKeyboardFocused)
            return;
        var row = (FieldRow)sender!;
        if (row.Field.Auto || ContentBox.Text == row.Value)
            return;
        _loading = true;
        ContentBox.Text = row.Value;
        _loading = false;
    }

    private void Content_LostFocus(object sender, RoutedEventArgs e)
    {
        var row = Selected;
        if (row is null || row.Field.Auto)
            return;
        row.Value = FrameFields.Clean(row.Value);
        _loading = true;
        ContentBox.Text = row.Value;
        _loading = false;
    }

    // ---- Sửa trực tiếp trong ô (kích đúp hoặc F2) ----
    private static T? FindParent<T>(DependencyObject? d) where T : DependencyObject
    {
        while (d is not null && d is not T)
        {
            d = d is System.Windows.Media.Visual or System.Windows.Media.Media3D.Visual3D
                ? System.Windows.Media.VisualTreeHelper.GetParent(d)
                : LogicalTreeHelper.GetParent(d);
        }

        return d as T;
    }

    private void StartEdit(DataGrid grid, FieldRow row)
    {
        if (row.Field.Auto)
            return;
        grid.SelectedItem = row;
        grid.CurrentCell = new DataGridCellInfo(row, grid.Columns[2]);
        _allowEdit = true;
        if (!grid.BeginEdit())
            _allowEdit = false;
    }

    private void Grid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var grid = (DataGrid)sender;
        var cell = FindParent<DataGridCell>(e.OriginalSource as DependencyObject);
        if (cell is null || cell.IsEditing || cell.DataContext is not FieldRow row)
            return;
        StartEdit(grid, row);
        e.Handled = true;
    }

    private void Grid_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.F2)
            return;
        var grid = (DataGrid)sender;
        if (grid.SelectedItem is FieldRow row && !IsEditingCell())
        {
            StartEdit(grid, row);
            e.Handled = true;
        }
    }

    private void Grid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
        // chỉ cho sửa khi người dùng chủ động (kích đúp / F2), chỉ cột Nội dung, không sửa dòng tự động
        if (!_allowEdit || e.Row.Item is not FieldRow row || row.Field.Auto || e.Column.DisplayIndex != 2)
            e.Cancel = true;
    }

    private void Grid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        _allowEdit = false;
        if (e.EditAction == DataGridEditAction.Commit && e.EditingElement is TextBox tb)
            tb.Text = FrameFields.Clean(tb.Text);
    }

    private static bool IsEditingCell() =>
        FindParent<DataGridCell>(System.Windows.Input.Keyboard.FocusedElement as DependencyObject)?.IsEditing == true;

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // Esc đang sửa ô: chỉ hủy sửa ô, không đóng hộp thoại
        if (e.Key == System.Windows.Input.Key.Escape && !IsEditingCell())
        {
            e.Handled = true;
            Cancel_Click(sender, e);
        }
    }

    // ---- Logo ----
    private bool IsHidden(bool owner) => owner ? _hideOwner : _hideDesign;

    private void HideLogo_Click(object sender, RoutedEventArgs e)
    {
        var row = Selected;
        if (row is null || row.Stt is not (1 or 5))
            return;
        var owner = row.Stt == 1;
        var hide = !IsHidden(owner);
        if (owner)
        {
            _hideOwner = hide;
            if (hide)
                _logoOwner = null;
        }
        else
        {
            _hideDesign = hide;
            if (hide)
                _logoDesign = null;
        }

        UpdateLogoNote();
        UpdateContentEditor();
    }

    private string? PendingLogo(bool owner) => owner ? _logoOwner : _logoDesign;

    private void Logo_Click(object sender, RoutedEventArgs e)
    {
        var row = Selected;
        if (row is null || row.Stt is not (1 or 5))
            return;
        var owner = row.Stt == 1;
        using var dlg = new System.Windows.Forms.OpenFileDialog
        {
            Title = owner ? "Chọn ảnh logo chủ đầu tư" : "Chọn ảnh logo đơn vị thiết kế",
            Filter = LogoReplaceService.ImageFilter,
            CheckFileExists = true
        };
        if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;
        if (owner)
        {
            _logoOwner = dlg.FileName;
            _hideOwner = false;
        }
        else
        {
            _logoDesign = dlg.FileName;
            _hideDesign = false;
        }

        UpdateLogoNote();
        UpdateContentEditor();
    }

    private void LogoClear_Click(object sender, RoutedEventArgs e)
    {
        var row = Selected;
        if (row is null)
            return;
        if (row.Stt == 1)
            _logoOwner = null;
        else if (row.Stt == 5)
            _logoDesign = null;
        UpdateLogoNote();
        UpdateContentEditor();
    }

    private void UpdateLogoNote()
    {
        var parts = new List<string>();
        if (_hideOwner)
            parts.Add("logo chủ đầu tư: ẨN");
        else if (_logoOwner is not null)
            parts.Add("logo chủ đầu tư: thay bằng " + System.IO.Path.GetFileName(_logoOwner));
        if (_hideDesign)
            parts.Add("logo đơn vị thiết kế: ẨN");
        else if (_logoDesign is not null)
            parts.Add("logo đơn vị thiết kế: thay bằng " + System.IO.Path.GetFileName(_logoDesign));
        foreach (var kv in _signatures)
            parts.Add("chữ ký " + kv.Key + ": " + System.IO.Path.GetFileName(kv.Value));
        LogoNote.Text = parts.Count == 0 ? "" : string.Join("; ", parts) + " (áp dụng khi bấm Tạo Layout).";
    }

    private static bool IsSignRow(FieldRow row) =>
        row.Field.Tag.StartsWith("TEN_", StringComparison.OrdinalIgnoreCase)
        && row.Field.Tag.Length == 5
        && row.Field.Tag[4] is >= '1' and <= '5';

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        using var dlg = new System.Windows.Forms.OpenFileDialog
        {
            Title = "Nhập thông tin khung tên",
            Filter = "TXT hoặc CSV (*.txt;*.csv)|*.txt;*.csv|Tất cả (*.*)|*.*",
            CheckFileExists = true
        };
        if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;

        string[] lines;
        try
        {
            lines = System.IO.File.ReadAllLines(dlg.FileName);
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show(this, "Không đọc được file: " + ex.Message, "iSurvey", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var filled = 0;
        var skipped = 0;
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("//"))
                continue;
            if (!SplitField(line, out var key, out var value))
            {
                skipped++;
                continue;
            }

            var row = FindRow(key);
            if (row is null || row.Field.Auto)
            {
                skipped++;
                continue;
            }

            row.Value = FrameFields.Clean(value);
            filled++;
        }

        UpdateContentEditor();
        System.Windows.MessageBox.Show(this, $"Đã điền {filled} ô." + (skipped > 0 ? $" Bỏ qua {skipped} dòng." : ""), "iSurvey", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private FieldRow? FindRow(string key)
    {
        if (int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stt))
            return _rows.FirstOrDefault(r => r.Stt == stt);
        return _rows.FirstOrDefault(r =>
            r.Field.Tag.Equals(key, StringComparison.OrdinalIgnoreCase)
            || r.Label.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    private static bool SplitField(string line, out string key, out string value)
    {
        key = "";
        value = "";
        var cut = line.IndexOf(';');
        if (cut < 0)
            cut = line.IndexOf('\t');
        if (cut < 0)
            cut = line.IndexOf(',');
        if (cut <= 0 || cut >= line.Length - 1)
            return false;
        key = line[..cut].Trim().Trim('"');
        value = line[(cut + 1)..].Trim().Trim('"');
        return key.Length > 0;
    }

    private void Sign_Click(object sender, RoutedEventArgs e)
    {
        var row = Selected;
        if (row is null || !IsSignRow(row))
            return;
        using var dlg = new System.Windows.Forms.OpenFileDialog
        {
            Title = "Chọn bản vẽ chữ ký cho " + row.Label,
            Filter = "Bản vẽ chữ ký (*.dwg)|*.dwg",
            CheckFileExists = true
        };
        if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;
        _signatures[row.Field.Tag] = dlg.FileName;
        UpdateLogoNote();
        UpdateContentEditor();
    }

    private void SignClear_Click(object sender, RoutedEventArgs e)
    {
        var row = Selected;
        if (row is null)
            return;
        _signatures.Remove(row.Field.Tag);
        UpdateLogoNote();
        UpdateContentEditor();
    }
    // ---- Nút ----
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (!TryNum(OverlapBox.Text, out var ov) || ov < 0)
        {
            ShowWarn("Chồng gối phải là số mét ≥ 0.");
            return;
        }

        if (!TryNum(GapBox.Text, out var gap) || gap < 0)
        {
            System.Windows.MessageBox.Show(this, "Khoảng cách giữa các tờ (mm) phải là số ≥ 0.", "iSurvey", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(RowsBox.Text, out var rows) || !int.TryParse(ColsBox.Text, out var cols) || rows < 1 || cols < 1 || rows > 99 || cols > 99)
        {
            ShowWarn("Hàng và Cột phải là số nguyên từ 1 đến 99.");
            return;
        }

        var attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in _rows)
        {
            if (!r.Field.Auto)
                attrs[r.Field.Tag] = FrameFields.Clean(r.Value);
        }

        Result = new RouteLayoutOptions
        {
            Size = CurrentSize,
            Scale = CurrentScale,
            OverlapM = ov,
            Rows = rows,
            Cols = cols,
            GapMm = gap,
            PlotPdf = PdfCheck.IsChecked == true,
            Attributes = attrs,
            LogoOwnerPath = _logoOwner,
            LogoDesignPath = _logoDesign,
            HideOwnerLogo = _hideOwner,
            HideDesignLogo = _hideDesign,
            Signatures = new Dictionary<string, string>(_signatures, StringComparer.OrdinalIgnoreCase)
        };
        Close();
    }
}








