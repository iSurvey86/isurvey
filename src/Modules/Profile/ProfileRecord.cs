namespace iSurvey.Modules.Profile;

/// <summary>Một mặt cắt dọc đã vẽ trên Model. RAIMCD chỉ rải khung lên hình này.</summary>
public sealed class ProfileRecord
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string RouteId { get; set; } = "";

    public int VerticalScale { get; set; } = 100;

    public int HorizontalScale { get; set; } = 1000;

    /// <summary>Mức so sánh Z0 (m). Y đất = (Z − Z0) × (H/V).</summary>
    public double Datum { get; set; }

    public double InsertX { get; set; }

    public double InsertY { get; set; }

    public double LabelWidth { get; set; } = 250;

    public double LabelInset { get; set; } = 40;

    public double HeaderClearance { get; set; } = 40;

    public double ProfileTextHeight { get; set; } = 12;

    public double SampleStepM { get; set; } = 20;

    public bool FillChainage { get; set; } = true;

    public bool FillDatumLabels { get; set; } = true;

    public bool InsertLeftScaleBar { get; set; } = true;

    public bool InsertRightScaleBar { get; set; }

    public bool DrawBandHeader { get; set; } = true;

    public bool ElevationsAtStationsOnly { get; set; } = true;

    public bool ShowGrid { get; set; }

    public string TemplateName { get; set; } = "Tiêu chuẩn";

    public List<ProfileBandRow> Rows { get; set; } = ProfileBandRow.CreateDefaultList();

    public List<string> EntityHandles { get; set; } = new();

    public string Summary()
    {
        var hv = "1/" + VerticalScale + "; 1/" + HorizontalScale;
        return "Tỷ lệ " + hv + ". Mức so sánh " + Datum.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " m.";
    }

    public ProfileDrawOptions ToOptions()
    {
        var opts = new ProfileDrawOptions
        {
            RouteId = RouteId,
            ProfileName = Name,
            SampleStepM = SampleStepM,
            VerticalScale = VerticalScale,
            HorizontalScale = HorizontalScale,
            Datum = Datum,
            ProfileTextHeight = ProfileTextHeight,
            LabelWidth = LabelWidth,
            LabelInset = LabelInset,
            HeaderClearance = HeaderClearance,
            FillChainage = FillChainage,
            FillDatumLabels = FillDatumLabels,
            InsertLeftScaleBar = InsertLeftScaleBar,
            InsertRightScaleBar = InsertRightScaleBar,
            DrawBandHeader = DrawBandHeader,
            ElevationsAtStationsOnly = ElevationsAtStationsOnly,
            ShowGrid = ShowGrid,
            TemplateName = TemplateName,
            Rows = new List<ProfileBandRow>()
        };
        if (Rows is not null)
        {
            foreach (var row in Rows)
                opts.Rows.Add(row.Clone());
        }

        if (opts.Rows.Count == 0)
            opts.Rows = ProfileBandRow.CreateDefaultList();
        return opts;
    }

    public static ProfileRecord FromDraw(
        string id,
        string name,
        string routeId,
        ProfileDrawOptions options,
        double insertX,
        double insertY,
        List<string> handles)
    {
        var rows = new List<ProfileBandRow>();
        foreach (var row in options.Rows)
            rows.Add(row.Clone());
        return new ProfileRecord
        {
            Id = id,
            Name = name,
            RouteId = routeId,
            VerticalScale = options.VerticalScale,
            HorizontalScale = options.HorizontalScale,
            Datum = options.Datum,
            InsertX = insertX,
            InsertY = insertY,
            LabelWidth = options.LabelWidth,
            LabelInset = options.LabelInset,
            HeaderClearance = options.HeaderClearance,
            ProfileTextHeight = options.ProfileTextHeight,
            SampleStepM = options.SampleStepM,
            FillChainage = options.FillChainage,
            FillDatumLabels = options.FillDatumLabels,
            InsertLeftScaleBar = options.InsertLeftScaleBar,
            InsertRightScaleBar = options.InsertRightScaleBar,
            DrawBandHeader = options.DrawBandHeader,
            ElevationsAtStationsOnly = options.ElevationsAtStationsOnly,
            ShowGrid = options.ShowGrid,
            TemplateName = options.TemplateName,
            Rows = rows,
            EntityHandles = handles
        };
    }
}
