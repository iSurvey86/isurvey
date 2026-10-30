using System.IO;
using System.Windows;
using System.Windows.Controls;
using Autodesk.AutoCAD.DatabaseServices;
using iSurvey.Modules.Frame;
using iSurvey.UI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.UI.Frame;

/// <summary>Lựa chọn lúc INTUYEN. Chữ ký và mộc chỉ lên PDF, rồi được gỡ khỏi bản vẽ.</summary>
internal sealed class PlotPrintChoice
{
    public bool Monochrome { get; init; }

    public bool InsertSignatures { get; init; }

    public Dictionary<string, string> Signatures { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public string? StampPath { get; init; }
}

internal sealed class PlotPrintWindow : Window
{
    private readonly System.Windows.Controls.ComboBox _style = new() { MinWidth = 180 };
    private readonly System.Windows.Controls.CheckBox _signCheck = new() { Content = "Chèn chữ ký", Margin = new Thickness(0, 12, 0, 6) };
    private readonly StackPanel _signRows = new();
    private readonly System.Windows.Controls.CheckBox _stampCheck = new() { Content = "Đóng mộc đỏ", Margin = new Thickness(0, 12, 0, 6) };
    private readonly TextBlock _stampName = new() { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0), TextWrapping = TextWrapping.Wrap };
    private readonly Dictionary<string, TextBlock> _signNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _signFiles = new(StringComparer.OrdinalIgnoreCase);
    private string? _stampPath;

    private PlotPrintWindow(Database db, IReadOnlyDictionary<string, string> roles)
    {
        Title = "iSurvey — In các tờ đã rải";
        Width = 520;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;
        Background = System.Windows.Media.Brushes.White;

        var settings = RouteLayoutSettings.Load();
        var memory = FrameMemory.Load(db);
        _style.Items.Add("In màu");
        _style.Items.Add("In đen trắng");
        _style.SelectedIndex = settings.Monochrome ? 1 : 0;

        _signCheck.IsChecked = memory.SignFiles.Count > 0;
        foreach (var field in FrameFields.All)
        {
            if (field.Tag is not ("TEN_1" or "TEN_2" or "TEN_3" or "TEN_4" or "TEN_5"))
                continue;
            if (!roles.TryGetValue(field.Tag, out var role) || string.IsNullOrWhiteSpace(role))
                continue;
            memory.SignFiles.TryGetValue(field.Tag, out var known);
            if (!string.IsNullOrWhiteSpace(known) && File.Exists(known))
                _signFiles[field.Tag] = known;
            _signRows.Children.Add(SignRow(field.Tag, role.Trim()));
        }

        _stampCheck.IsChecked = memory.StampOnPdf && !string.IsNullOrWhiteSpace(memory.StampPath) && File.Exists(memory.StampPath);
        if (!string.IsNullOrWhiteSpace(memory.StampPath) && File.Exists(memory.StampPath))
            _stampPath = memory.StampPath;
        RefreshStampName();
        _signCheck.Checked += (_, _) => RefreshSignRows();
        _signCheck.Unchecked += (_, _) => RefreshSignRows();
        RefreshSignRows();

        var stampRow = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        var pickStamp = new System.Windows.Controls.Button { Content = "Chọn ảnh (.png)", Style = Cta.Style("CtaSoft") };
        pickStamp.Click += (_, _) => PickStamp();
        stampRow.Children.Add(pickStamp);
        var clearStamp = new System.Windows.Controls.Button { Content = "Xóa", Margin = new Thickness(6, 0, 0, 0), Style = Cta.Style("CtaSoft") };
        clearStamp.Click += (_, _) =>
        {
            _stampPath = null;
            _stampCheck.IsChecked = false;
            RefreshStampName();
        };
        stampRow.Children.Add(clearStamp);
        stampRow.Children.Add(_stampName);

        var buttons = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        var cancel = new System.Windows.Controls.Button { Content = "Hủy", MinWidth = 88, Margin = new Thickness(0, 0, 8, 0), Style = Cta.Style("CtaSoft"), IsCancel = true };
        cancel.Click += (_, _) => { DialogResult = false; Close(); };
        var ok = new System.Windows.Controls.Button { Content = "In PDF", MinWidth = 120, Style = Cta.Style("CtaPrimary"), IsDefault = true };
        ok.Click += (_, _) => { DialogResult = true; Close(); };
        buttons.Children.Add(cancel);
        buttons.Children.Add(ok);

        var root = new StackPanel { Margin = new Thickness(16) };
        var styleRow = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        styleRow.Children.Add(new TextBlock { Text = "Kiểu in", Width = 88, VerticalAlignment = VerticalAlignment.Center });
        styleRow.Children.Add(_style);
        root.Children.Add(styleRow);
        root.Children.Add(_signCheck);
        root.Children.Add(_signRows);
        root.Children.Add(_stampCheck);
        root.Children.Add(stampRow);
        root.Children.Add(buttons);
        Content = root;
    }

    public static PlotPrintChoice? Show(Database db, IReadOnlyDictionary<string, string> roles)
    {
        var w = new PlotPrintWindow(db, roles);
        var ok = AcadApp.ShowModalWindow(w) == true;
        if (!ok)
            return null;

        var signs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (w._signCheck.IsChecked == true)
        {
            foreach (var kv in w._signFiles)
            {
                if (File.Exists(kv.Value))
                    signs[kv.Key] = kv.Value;
            }
        }

        var stamp = w._stampCheck.IsChecked == true && !string.IsNullOrWhiteSpace(w._stampPath) && File.Exists(w._stampPath)
            ? w._stampPath
            : null;
        var choice = new PlotPrintChoice
        {
            Monochrome = w._style.SelectedIndex == 1,
            InsertSignatures = signs.Count > 0,
            Signatures = signs,
            StampPath = stamp
        };
        Remember(db, choice, w._stampCheck.IsChecked == true);
        return choice;
    }

    private static void Remember(Database db, PlotPrintChoice choice, bool stampOn)
    {
        try
        {
            var settings = RouteLayoutSettings.Load();
            settings.Monochrome = choice.Monochrome;
            settings.PlotPdf = false;
            settings.Save();
        }
        catch
        {
            // không chặn in
        }

        try
        {
            var data = FrameMemory.Load(db);
            data.StampOnPdf = stampOn && choice.StampPath is not null;
            if (choice.StampPath is not null)
                data.StampPath = choice.StampPath;
            if (choice.InsertSignatures)
            {
                foreach (var kv in choice.Signatures)
                    data.SignFiles[kv.Key] = kv.Value;
            }

            FrameMemory.Save(db, data);
        }
        catch
        {
            // không chặn in
        }
    }

    private UIElement SignRow(string tag, string label)
    {
        var row = new Grid { Margin = new Thickness(16, 0, 0, 6) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(168) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var name = new TextBlock
        {
            Text = label,
            ToolTip = label,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var file = new TextBlock { VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(6, 0, 6, 0) };
        _signNames[tag] = file;
        file.Text = _signFiles.TryGetValue(tag, out var path) ? Path.GetFileName(path) : "Chưa chọn";
        var pick = new System.Windows.Controls.Button { Content = "Chọn", MinWidth = 72, Style = Cta.Style("CtaSoft"), Tag = tag };
        pick.Click += (_, _) => PickSign(tag);
        Grid.SetColumn(file, 1);
        Grid.SetColumn(pick, 2);
        row.Children.Add(name);
        row.Children.Add(file);
        row.Children.Add(pick);
        return row;
    }

    private void RefreshSignRows()
    {
        _signRows.IsEnabled = _signCheck.IsChecked == true;
    }

    private void PickSign(string tag)
    {
        using var dlg = new System.Windows.Forms.OpenFileDialog
        {
            Title = "Chọn bản vẽ chữ ký",
            Filter = "Bản vẽ chữ ký (*.dwg)|*.dwg",
            CheckFileExists = true
        };
        if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;
        _signFiles[tag] = dlg.FileName;
        _signCheck.IsChecked = true;
        if (_signNames.TryGetValue(tag, out var label))
            label.Text = Path.GetFileName(dlg.FileName);
    }

    private void PickStamp()
    {
        using var dlg = new System.Windows.Forms.OpenFileDialog
        {
            Title = "Chọn ảnh mộc đỏ",
            Filter = "Ảnh PNG (*.png)|*.png",
            CheckFileExists = true
        };
        if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;
        _stampPath = dlg.FileName;
        _stampCheck.IsChecked = true;
        RefreshStampName();
    }

    private void RefreshStampName()
    {
        _stampName.Text = string.IsNullOrWhiteSpace(_stampPath) ? "Chưa chọn ảnh" : Path.GetFileName(_stampPath);
    }
}
