using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using Autodesk.AutoCAD.DatabaseServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using MediaFonts = System.Windows.Media.Fonts;

namespace iSurvey.UI.Sequence;

public sealed class SequenceRequest
{
    public string Prefix { get; init; } = "";
    public string Suffix { get; init; } = "";
    public int Start { get; init; }
    public int Step { get; init; } = 1;
    public double Height { get; init; } = 5;
    public double WidthFactor { get; init; } = 1;
    public bool DrawCircle { get; init; } = true;
    public bool UseWipeout { get; init; } = true;
    public bool UseCurrentStyle { get; init; }
    public string StyleName { get; init; } = "";
    public string WindowsFont { get; init; } = "";
}

public sealed class FontChoice
{
    public string Label { get; init; } = "";
    public bool UseCurrentStyle { get; init; }
    public string StyleName { get; init; } = "";
    public string WindowsFont { get; init; } = "";
}

public partial class SequenceWindow : Window
{
    private readonly SequenceForm _form = new();

    public SequenceWindow()
    {
        InitializeComponent();
        var fonts = LoadFonts(out var selected);
        _form.SetFonts(fonts, selected);
        DataContext = _form;
    }

    public SequenceRequest? Request { get; private set; }

    public static SequenceRequest? Prompt()
    {
        var window = new SequenceWindow();
        AcadApp.ShowModalWindow(window);
        return window.Request;
    }

    private static List<FontChoice> LoadFonts(out FontChoice? selected)
    {
        var styleName = "";
        var typeFace = "";
        try
        {
            var db = AcadApp.DocumentManager.MdiActiveDocument?.Database;
            if (db is not null)
            {
                using var tr = db.TransactionManager.StartTransaction();
                var record = (TextStyleTableRecord)tr.GetObject(db.Textstyle, OpenMode.ForRead);
                styleName = record.Name;
                typeFace = record.Font.TypeFace ?? "";
                tr.Commit();
            }
        }
        catch
        {
            // không đọc được style thì chỉ liệt kê font Windows
        }

        var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var family in MediaFonts.SystemFontFamilies)
        {
            try
            {
                var name = FamilyName(family);
                if (string.IsNullOrWhiteSpace(name) || name[0] == '@')
                    continue;
                names.Add(name.Trim());
            }
            catch
            {
                // font hỏng trên máy, bỏ qua
            }
        }

        var choices = new List<FontChoice>();
        selected = null;
        var currentIsWindows = typeFace.Length > 0 && names.Contains(typeFace);
        if (!currentIsWindows && styleName.Length > 0)
        {
            var label = names.Contains(styleName) ? styleName + " (style bản vẽ)" : styleName;
            var current = new FontChoice { Label = label, UseCurrentStyle = true, StyleName = styleName };
            choices.Add(current);
            selected = current;
        }

        foreach (var name in names)
        {
            var item = new FontChoice { Label = name, WindowsFont = name };
            choices.Add(item);
            if (selected is null && typeFace.Equals(name, StringComparison.OrdinalIgnoreCase))
                selected = item;
        }

        selected ??= choices.FirstOrDefault();
        return choices;
    }

    private static string FamilyName(System.Windows.Media.FontFamily family)
    {
        var english = XmlLanguage.GetLanguage("en-us");
        if (family.FamilyNames.TryGetValue(english, out var name) && !string.IsNullOrWhiteSpace(name))
            return name;
        foreach (var value in family.FamilyNames.Values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return family.Source;
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        if (!_form.TryBuild(out var request, out var error))
        {
            System.Windows.MessageBox.Show(this, error, "iSurvey", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Request = request;
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
}

public sealed class SequenceForm : INotifyPropertyChanged
{
    private string _prefix = "D";
    private string _suffix = "";
    private string _startText = "1";
    private string _stepText = "1";
    private string _heightText = "5.0";
    private string _widthText = "1.0";
    private bool _drawCircle = true;
    private bool _useWipeout = true;
    private IReadOnlyList<FontChoice> _fonts = [];
    private FontChoice? _selectedFont;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Prefix
    {
        get => _prefix;
        set => Set(ref _prefix, Upper(value));
    }

    public string Suffix
    {
        get => _suffix;
        set => Set(ref _suffix, Upper(value));
    }

    public string StartText
    {
        get => _startText;
        set => Set(ref _startText, value ?? "");
    }

    public string StepText
    {
        get => _stepText;
        set => Set(ref _stepText, value ?? "");
    }

    public string HeightText
    {
        get => _heightText;
        set => Set(ref _heightText, value ?? "");
    }

    public string WidthText
    {
        get => _widthText;
        set => Set(ref _widthText, value ?? "");
    }

    public bool DrawCircle
    {
        get => _drawCircle;
        set => Set(ref _drawCircle, value);
    }

    public bool UseWipeout
    {
        get => _useWipeout;
        set => Set(ref _useWipeout, value);
    }

    public IReadOnlyList<FontChoice> Fonts => _fonts;

    public FontChoice? SelectedFont
    {
        get => _selectedFont;
        set => Set(ref _selectedFont, value);
    }

    public void SetFonts(IReadOnlyList<FontChoice> fonts, FontChoice? selected)
    {
        _fonts = fonts;
        _selectedFont = selected ?? fonts.FirstOrDefault();
    }

    public string PreviewNumbers
    {
        get
        {
            if (!int.TryParse(StartText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var start)
                || !int.TryParse(StepText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var step))
                return "—";
            return $"{Label(start)}, {Label(start + step)}, {Label(start + 2L * step)}...";
        }
    }

    public bool TryBuild(out SequenceRequest request, out string error)
    {
        request = new SequenceRequest();
        error = "";
        if (!int.TryParse(StartText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var start))
        {
            error = "Số bắt đầu phải là số nguyên.";
            return false;
        }

        if (!int.TryParse(StepText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var step) || step == 0)
        {
            error = "Bước nhảy phải là số nguyên khác 0.";
            return false;
        }

        if (!TryDouble(HeightText, out var height) || height <= 0)
        {
            error = "Chiều cao chữ phải lớn hơn 0.";
            return false;
        }

        if (!TryDouble(WidthText, out var width) || width <= 0)
        {
            error = "Độ hẹp phải lớn hơn 0.";
            return false;
        }

        if (SelectedFont is null || (SelectedFont.WindowsFont.Length == 0 && !SelectedFont.UseCurrentStyle))
        {
            error = "Chọn font chữ.";
            return false;
        }

        request = new SequenceRequest
        {
            Prefix = Prefix,
            Suffix = Suffix,
            Start = start,
            Step = step,
            Height = height,
            WidthFactor = width,
            DrawCircle = DrawCircle,
            UseWipeout = UseWipeout,
            UseCurrentStyle = SelectedFont?.UseCurrentStyle == true,
            StyleName = SelectedFont?.StyleName ?? "",
            WindowsFont = SelectedFont?.WindowsFont ?? ""
        };
        return true;
    }

    private string Label(long number) => Prefix + number.ToString(CultureInfo.InvariantCulture) + Suffix;

    private static string Upper(string? value) =>
        (value ?? "").ToUpper(CultureInfo.CurrentCulture);

    private static bool TryDouble(string text, out double value)
    {
        var normalized = text.Trim().Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        if (name is nameof(Prefix) or nameof(Suffix) or nameof(StartText) or nameof(StepText))
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PreviewNumbers)));
    }
}
