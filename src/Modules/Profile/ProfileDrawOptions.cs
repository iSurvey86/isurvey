namespace iSurvey.Modules.Profile;

/// <summary>Tham số vẽ mặt cắt dọc tự nhiên (UI + engine).</summary>
public sealed class ProfileDrawOptions
{
    public string RouteId { get; set; } = "";

    public string ProfileName { get; set; } = "";

    /// <summary>Bước lấy mẫu Z bổ sung dọc tim (m). 0 = chỉ cọc (+ giao TIN nếu có).</summary>
    public double SampleStepM { get; set; } = 20;

    public bool DrawMultipleRoutes { get; set; }

    public int VerticalScale { get; set; } = 100;

    public int HorizontalScale { get; set; } = 1000;

    public double Datum { get; set; } = 0;

    public double ProfileTextHeight { get; set; } = 12;

    public double LabelWidth { get; set; } = 250;

    /// <summary>Cách mép trái cột tên khi ghi nhãn hàng đầu bảng (đơn vị bản vẽ).</summary>
    public double LabelInset { get; set; } = 40;

    /// <summary>
    /// Cách đầu bảng (ngang): khoảng từ thước tỷ lệ (mép phải cột nhãn) tới đầu đường đất /
    /// đường tím nét đứt đứng. Khoảng đứng đầu bảng luôn 0 (đất sát mép trên bảng).
    /// </summary>
    public double HeaderClearance { get; set; } = 10;

    public double ScaleBarWidth { get; set; } = 1;

    public double RowGap { get; set; } = 5;

    public bool FillChainage { get; set; } = true;

    public bool FillDatumLabels { get; set; } = true;

    public bool InsertLeftScaleBar { get; set; } = true;

    public bool InsertRightScaleBar { get; set; }

    public bool DrawBandHeader { get; set; } = true;

    public bool ElevationsAtStationsOnly { get; set; } = true;

    public bool ShowGrid { get; set; }

    /// <summary>Vẽ chữ thập đỏ tại đầu mặt cắt × trung điểm hàng Sơ họa mặt bằng.</summary>
    public bool DrawCrossMark { get; set; } = true;

    public string TemplateName { get; set; } = "Tiêu chuẩn";

    public List<ProfileBandRow> Rows { get; set; } = ProfileBandRow.CreateDefaultList();

    public ProfileDrawOptions Clone()
    {
        var copy = new ProfileDrawOptions
        {
            RouteId = RouteId,
            ProfileName = ProfileName,
            SampleStepM = SampleStepM,
            DrawMultipleRoutes = DrawMultipleRoutes,
            VerticalScale = VerticalScale,
            HorizontalScale = HorizontalScale,
            Datum = Datum,
            ProfileTextHeight = ProfileTextHeight,
            LabelWidth = LabelWidth,
            LabelInset = LabelInset,
            HeaderClearance = HeaderClearance,
            ScaleBarWidth = ScaleBarWidth,
            RowGap = RowGap,
            FillChainage = FillChainage,
            FillDatumLabels = FillDatumLabels,
            InsertLeftScaleBar = InsertLeftScaleBar,
            InsertRightScaleBar = InsertRightScaleBar,
            DrawBandHeader = DrawBandHeader,
            ElevationsAtStationsOnly = ElevationsAtStationsOnly,
            ShowGrid = ShowGrid,
            DrawCrossMark = DrawCrossMark,
            TemplateName = TemplateName,
            Rows = new List<ProfileBandRow>(Rows.Count)
        };
        foreach (var row in Rows)
            copy.Rows.Add(row.Clone());
        return copy;
    }
}
