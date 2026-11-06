using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using iSurvey.Modules.Profile;
using iSurvey.Modules.Tuyen;
using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace iSurvey.UI.Profile;

public sealed class ProfileBandRowVm : INotifyPropertyChanged
{
    private string _label = "";
    private string _content = "";
    private string _layer = iSurveyLayers.TdChu;
    private string _textStyle = ProfileTextStyles.StyleName;
    private int _colorAci = 256; // ByLayer
    private double _textHeight = 12;
    private double _rowHeight = 50;
    private string _key = "";
    private int _stt = 1;

    public int Stt
    {
        get => _stt;
        set => Set(ref _stt, value);
    }

    public string Key
    {
        get => _key;
        set => Set(ref _key, value ?? "");
    }

    public string Label
    {
        get => _label;
        set => Set(ref _label, value ?? "");
    }

    public string Content
    {
        get => _content;
        set => Set(ref _content, value ?? "");
    }

    public string Layer
    {
        get => _layer;
        set => Set(ref _layer, value ?? iSurveyLayers.TdChu);
    }

    public string TextStyle
    {
        get => _textStyle;
        set => Set(ref _textStyle, value ?? "");
    }

    public int ColorAci
    {
        get => _colorAci;
        set
        {
            if (!Set(ref _colorAci, value))
                return;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ColorBrush)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ColorText)));
        }
    }

    public string ColorText => ColorAci is >= 1 and <= 255 ? ColorAci.ToString(CultureInfo.InvariantCulture) : "ByLayer";

    public MediaBrush ColorBrush => AciToBrush(ColorAci);

    public double TextHeight
    {
        get => _textHeight;
        set => Set(ref _textHeight, value);
    }

    public double RowHeight
    {
        get => _rowHeight;
        set => Set(ref _rowHeight, value);
    }

    public static ProfileBandRowVm FromModel(ProfileBandRow row) => new()
    {
        Key = row.Key,
        Label = row.Label,
        Content = row.Content,
        Layer = row.Layer,
        TextStyle = string.IsNullOrWhiteSpace(row.TextStyle) ? ProfileTextStyles.StyleName : row.TextStyle,
        ColorAci = row.ColorAci,
        TextHeight = row.TextHeight,
        RowHeight = row.RowHeight
    };

    public ProfileBandRow ToModel() => new()
    {
        Key = string.IsNullOrWhiteSpace(Key) ? Guid.NewGuid().ToString("N")[..8] : Key,
        Label = Label,
        Content = Content,
        Layer = Layer,
        TextStyle = string.IsNullOrWhiteSpace(TextStyle) ? ProfileTextStyles.StyleName : TextStyle,
        ColorAci = ColorAci,
        TextHeight = TextHeight,
        RowHeight = RowHeight
    };

    public static MediaBrush AciToBrush(int aci)
    {
        if (aci is < 1 or > 255)
            return new SolidColorBrush(MediaColor.FromRgb(0x94, 0xA3, 0xB8));
        return aci switch
        {
            1 => MediaBrushes.Red,
            2 => MediaBrushes.Yellow,
            3 => MediaBrushes.LimeGreen,
            4 => MediaBrushes.Cyan,
            5 => MediaBrushes.Blue,
            6 => MediaBrushes.Magenta,
            7 => MediaBrushes.White,
            8 => new SolidColorBrush(MediaColor.FromRgb(0x80, 0x80, 0x80)),
            9 => MediaBrushes.Gray,
            _ => new SolidColorBrush(MediaColor.FromRgb(
                (byte)(40 + (aci * 37) % 180),
                (byte)(40 + (aci * 53) % 180),
                (byte)(40 + (aci * 71) % 180)))
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value))
            return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}

public sealed class DrawProfileViewModel : INotifyPropertyChanged
{
    private string _sampleStepText = "20";
    private string _verticalScaleText = "100";
    private string _horizontalScaleText = "1000";
    private string _datumText = "0";
    private string _labelWidthText = "250";
    private string _rowGapText = "5";
    private string _headerClearanceText = "10";
    private bool _drawMultipleRoutes;
    private bool _fillChainage = true;
    private bool _fillDatumLabels = true;
    private bool _insertLeftScaleBar = true;
    private bool _insertRightScaleBar;
    private bool _drawBandHeader = true;
    private bool _elevationsAtStationsOnly = true;
    private bool _showGrid;
    private bool _drawCrossMark = true;
    private string _templateName = "Tiêu chuẩn";
    private int _selectedRouteIndex;
    private ProfileBandRowVm? _selectedRow;
    private string _hint = "";

    public DrawProfileViewModel(IReadOnlyList<RouteRecord> routes, string? preferredRouteId, ProfileDrawOptions? saved)
    {
        Routes = routes.Where(r => r.Stations is { Count: > 0 }).ToList();
        Rows.CollectionChanged += (_, _) => RenumberRows();
        foreach (var name in new[] { "Tiêu chuẩn", "235" })
            TemplateNames.Add(name);

        var opts = saved ?? new ProfileDrawOptions();
        ApplyOptions(opts, keepRoute: false);

        if (Routes.Count == 0)
        {
            Hint = "Chưa có tuyến có cọc. Tạo tuyến và phát sinh cọc (có Z) trước.";
            return;
        }

        var index = 0;
        if (!string.IsNullOrWhiteSpace(preferredRouteId))
        {
            for (var i = 0; i < Routes.Count; i++)
            {
                if (Routes[i].Id == preferredRouteId)
                {
                    index = i;
                    break;
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(opts.RouteId))
        {
            for (var i = 0; i < Routes.Count; i++)
            {
                if (Routes[i].Id == opts.RouteId)
                {
                    index = i;
                    break;
                }
            }
        }

        SelectedRouteIndex = index;
        LoadTextStyles();
        RefreshDatumSuggestion();
    }

    public void LoadTextStyles()
    {
        ProfileTextStyles.EnsureInActiveDocument();
        TextStyleNames.Clear();
        TextStyleNames.Add(ProfileTextStyles.StyleName);
        try
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc is null)
                return;
            using var tr = doc.TransactionManager.StartTransaction();
            var table = (Autodesk.AutoCAD.DatabaseServices.TextStyleTable)tr.GetObject(
                doc.Database.TextStyleTableId, Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead);
            foreach (Autodesk.AutoCAD.DatabaseServices.ObjectId id in table)
            {
                if (tr.GetObject(id, Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead)
                    is not Autodesk.AutoCAD.DatabaseServices.TextStyleTableRecord rec)
                    continue;
                if (string.IsNullOrWhiteSpace(rec.Name) || rec.IsShapeFile)
                    continue;
                if (string.Equals(rec.Name, ProfileTextStyles.StyleName, StringComparison.OrdinalIgnoreCase))
                    continue;
                TextStyleNames.Add(rec.Name);
            }

            tr.Commit();
        }
        catch
        {
            // giữ iSurvey_TD
        }

        foreach (var row in Rows)
        {
            if (string.IsNullOrWhiteSpace(row.TextStyle))
                row.TextStyle = ProfileTextStyles.StyleName;
        }
    }

    public List<RouteRecord> Routes { get; }

    public ObservableCollection<string> TemplateNames { get; } = new();

    public ObservableCollection<string> TextStyleNames { get; } = new();

    public ObservableCollection<ProfileBandRowVm> Rows { get; } = new();

    public IEnumerable<string> RouteNames => Routes.Select(r => r.Name);

    public int SelectedRouteIndex
    {
        get => _selectedRouteIndex;
        set
        {
            if (!Set(ref _selectedRouteIndex, value))
                return;
            RefreshDatumSuggestion();
        }
    }

    public RouteRecord? SelectedRoute =>
        SelectedRouteIndex >= 0 && SelectedRouteIndex < Routes.Count ? Routes[SelectedRouteIndex] : null;

    public ProfileBandRowVm? SelectedRow
    {
        get => _selectedRow;
        set => Set(ref _selectedRow, value);
    }

    public string SampleStepText
    {
        get => _sampleStepText;
        set => Set(ref _sampleStepText, value ?? "");
    }

    public string VerticalScaleText
    {
        get => _verticalScaleText;
        set => Set(ref _verticalScaleText, value ?? "");
    }

    public string HorizontalScaleText
    {
        get => _horizontalScaleText;
        set => Set(ref _horizontalScaleText, value ?? "");
    }

    public string DatumText
    {
        get => _datumText;
        set => Set(ref _datumText, value ?? "");
    }

    public string LabelWidthText
    {
        get => _labelWidthText;
        set => Set(ref _labelWidthText, value ?? "");
    }

    public string RowGapText
    {
        get => _rowGapText;
        set => Set(ref _rowGapText, value ?? "");
    }

    public string HeaderClearanceText
    {
        get => _headerClearanceText;
        set => Set(ref _headerClearanceText, value ?? "");
    }

    public bool DrawMultipleRoutes
    {
        get => _drawMultipleRoutes;
        set => Set(ref _drawMultipleRoutes, value);
    }

    public bool FillChainage
    {
        get => _fillChainage;
        set => Set(ref _fillChainage, value);
    }

    public bool FillDatumLabels
    {
        get => _fillDatumLabels;
        set => Set(ref _fillDatumLabels, value);
    }

    public bool InsertLeftScaleBar
    {
        get => _insertLeftScaleBar;
        set => Set(ref _insertLeftScaleBar, value);
    }

    public bool InsertRightScaleBar
    {
        get => _insertRightScaleBar;
        set => Set(ref _insertRightScaleBar, value);
    }

    public bool DrawBandHeader
    {
        get => _drawBandHeader;
        set => Set(ref _drawBandHeader, value);
    }

    public bool ElevationsAtStationsOnly
    {
        get => _elevationsAtStationsOnly;
        set => Set(ref _elevationsAtStationsOnly, value);
    }

    public bool ShowGrid
    {
        get => _showGrid;
        set => Set(ref _showGrid, value);
    }

    public bool DrawCrossMark
    {
        get => _drawCrossMark;
        set => Set(ref _drawCrossMark, value);
    }

    public string TemplateName
    {
        get => _templateName;
        set => Set(ref _templateName, value ?? "Tiêu chuẩn");
    }

    public string Hint
    {
        get => _hint;
        set => Set(ref _hint, value ?? "");
    }

    public void AddRow()
    {
        var row = new ProfileBandRowVm
        {
            Key = "row_" + (Rows.Count + 1),
            Label = "Hàng mới",
            Content = "empty",
            Layer = iSurveyLayers.TdChu,
            TextStyle = ProfileTextStyles.StyleName,
            ColorAci = 256,
            TextHeight = 12,
            RowHeight = 50
        };
        Rows.Add(row);
        SelectedRow = row;
    }

    public void RemoveSelectedRow()
    {
        if (SelectedRow is null)
            return;
        var idx = Rows.IndexOf(SelectedRow);
        Rows.Remove(SelectedRow);
        if (Rows.Count == 0)
            return;
        var pick = idx < 0 ? 0 : (idx > Rows.Count - 1 ? Rows.Count - 1 : idx);
        SelectedRow = Rows[pick];
    }

    public void ResetDefaultRows()
    {
        Rows.Clear();
        foreach (var row in ProfileBandRow.DefaultRows)
            Rows.Add(ProfileBandRowVm.FromModel(row));
        TemplateName = "Tiêu chuẩn";
    }

    public void LoadTemplate(ProfileTemplateIO.TemplateFile file)
    {
        TemplateName = string.IsNullOrWhiteSpace(file.Name) ? TemplateName : file.Name;
        if (file.LabelWidth > 0)
            LabelWidthText = file.LabelWidth.ToString("0.###", CultureInfo.InvariantCulture);
        Rows.Clear();
        foreach (var row in file.Rows)
            Rows.Add(ProfileBandRowVm.FromModel(row));
        if (!TemplateNames.Contains(TemplateName))
            TemplateNames.Add(TemplateName);
    }

    private void RenumberRows()
    {
        for (var i = 0; i < Rows.Count; i++)
            Rows[i].Stt = i + 1;
    }

    public void RefreshDatumSuggestion()
    {
        var route = SelectedRoute;
        if (route?.Stations is null || route.Stations.Count == 0)
            return;
        if (!TryInt(VerticalScaleText, out var v) || !TryInt(HorizontalScaleText, out var h))
            return;
        var bandH = 0.0;
        foreach (var row in Rows)
            bandH += Math.Max(1, row.RowHeight);
        if (bandH <= 0)
            bandH = ProfileBandRow.TotalHeight(ProfileBandRow.DefaultRows);

        var fake = new List<ProfileSampleService.SamplePoint>();
        foreach (var st in route.Stations)
        {
            if (!st.HasZ)
                continue;
            fake.Add(new ProfileSampleService.SamplePoint
            {
                Chainage = st.Chainage,
                X = st.X,
                Y = st.Y,
                Z = st.Z,
                HasZ = true,
                IsStation = true,
                Name = st.Name
            });
        }

        if (fake.Count < 2)
        {
            Hint = "Tuyến này chưa đủ cọc có Z. Phát sinh cọc sau khi gắn mô hình.";
            return;
        }

        // Chỉ gợi ý nếu ô mức so sánh đang trống / chưa hợp lệ — không ghi đè số người dùng đã gõ (kể cả âm).
        if (!TryDouble(DatumText, out _))
        {
            var datum = ProfileDrawService.SuggestDatum(fake, h, v, bandH);
            DatumText = datum.ToString("0.###", CultureInfo.InvariantCulture);
        }

        if (!TryDouble(DatumText, out var z0))
            z0 = ProfileDrawService.SuggestDatum(fake, h, v, bandH);
        var segs = ProfileDrawService.PlanSegments(fake, z0, h, v, bandH);
        var tip = ProfileDrawService.FormatSuggestion(segs);
        Hint = string.IsNullOrEmpty(tip)
            ? "Sau khi bấm Vẽ, chỉ điểm chèn góc dưới-trái cột tên trên Model."
            : tip;
    }

    public bool TryBuildOptions(out ProfileDrawOptions options, out string error)
    {
        options = new ProfileDrawOptions();
        error = "";
        if (SelectedRoute is null)
        {
            error = "Chọn một tuyến có cọc.";
            return false;
        }

        if (!TryInt(VerticalScaleText, out var v) || v <= 0 || !TryInt(HorizontalScaleText, out var h) || h <= 0)
        {
            error = "Tỷ lệ đứng/ngang phải là số nguyên dương.";
            return false;
        }

        if (!TryDouble(DatumText, out var datum))
        {
            error = "Cao độ min (mức so sánh) phải là số.";
            return false;
        }

        if (!TryDouble(LabelWidthText, out var lw) || lw < 20)
        {
            error = "Rộng đầu bảng phải ≥ 20.";
            return false;
        }

        TryDouble(HeaderClearanceText, out var headerClearance);

        // Hard-code chốt UI: Z step 20; chữ thước/MSS=12; rộng vạch thước=1;
        // luôn MSS + đầu bảng + thước trái; tắt thước phải; RowGap=0, ShowGrid=false.
        options = new ProfileDrawOptions
        {
            RouteId = SelectedRoute.Id,
            ProfileName = "MCD " + SelectedRoute.Name,
            SampleStepM = 20,
            DrawMultipleRoutes = DrawMultipleRoutes,
            VerticalScale = v,
            HorizontalScale = h,
            Datum = datum,
            ProfileTextHeight = 12,
            LabelWidth = lw,
            LabelInset = 40,
            HeaderClearance = headerClearance < 0 ? 0 : headerClearance,
            ScaleBarWidth = 1,
            RowGap = 0,
            FillChainage = FillChainage,
            FillDatumLabels = true,
            InsertLeftScaleBar = true,
            InsertRightScaleBar = false,
            DrawBandHeader = true,
            ElevationsAtStationsOnly = ElevationsAtStationsOnly,
            ShowGrid = false,
            DrawCrossMark = DrawCrossMark,
            TemplateName = TemplateName,
            Rows = new List<ProfileBandRow>()
        };
        foreach (var row in Rows)
        {
            var model = row.ToModel();
            // Nội dung UI chỉ đụng Label; Content type giữ nguyên từ hàng.
            // Màu luôn ByLayer khi vẽ (ACI trong file mẫu vẫn xuất/nhập được).
            model.ColorAci = 256;
            options.Rows.Add(model);
        }
        if (options.Rows.Count == 0)
            options.Rows = ProfileBandRow.CreateDefaultList();
        return true;
    }

    public ProfileDrawOptions SnapshotSettings()
    {
        TryBuildOptions(out var opts, out _);
        return opts;
    }

    private void ApplyOptions(ProfileDrawOptions opts, bool keepRoute)
    {
        SampleStepText = opts.SampleStepM.ToString("0.###", CultureInfo.InvariantCulture);
        VerticalScaleText = opts.VerticalScale.ToString(CultureInfo.InvariantCulture);
        HorizontalScaleText = opts.HorizontalScale.ToString(CultureInfo.InvariantCulture);
        DatumText = opts.Datum.ToString("0.###", CultureInfo.InvariantCulture);
        LabelWidthText = opts.LabelWidth.ToString("0.###", CultureInfo.InvariantCulture);
        RowGapText = opts.RowGap.ToString("0.###", CultureInfo.InvariantCulture);
        HeaderClearanceText = opts.HeaderClearance.ToString("0.###", CultureInfo.InvariantCulture);
        DrawMultipleRoutes = opts.DrawMultipleRoutes;
        FillChainage = opts.FillChainage;
        FillDatumLabels = opts.FillDatumLabels;
        InsertLeftScaleBar = opts.InsertLeftScaleBar;
        InsertRightScaleBar = opts.InsertRightScaleBar;
        DrawBandHeader = opts.DrawBandHeader;
        ElevationsAtStationsOnly = opts.ElevationsAtStationsOnly;
        ShowGrid = opts.ShowGrid;
        DrawCrossMark = opts.DrawCrossMark;
        TemplateName = string.IsNullOrWhiteSpace(opts.TemplateName) ? "Tiêu chuẩn" : opts.TemplateName;
        if (!TemplateNames.Contains(TemplateName))
            TemplateNames.Add(TemplateName);

        Rows.Clear();
        var source = opts.Rows is { Count: > 0 } ? opts.Rows : ProfileBandRow.CreateDefaultList();
        foreach (var row in source)
            Rows.Add(ProfileBandRowVm.FromModel(row));
        _ = keepRoute;
    }

    private static bool TryDouble(string text, out double value)
    {
        var raw = (text ?? "").Trim().Replace(',', '.');
        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryInt(string text, out int value)
    {
        var raw = (text ?? "").Trim();
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value))
            return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
