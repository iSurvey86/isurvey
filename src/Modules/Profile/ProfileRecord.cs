namespace iSurvey.Modules.Profile;

/// <summary>Một mặt cắt dọc đã vẽ trên Model. RAIMCD chỉ rải khung lên hình này.</summary>
public sealed class ProfileRecord
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string RouteId { get; set; } = "";

    /// <summary>Mẫu số tỷ lệ đứng (500 hoặc 200).</summary>
    public int VerticalScale { get; set; } = 500;

    /// <summary>Mẫu số tỷ lệ ngang (5000 hoặc 2000).</summary>
    public int HorizontalScale { get; set; } = 5000;

    /// <summary>Mức so sánh Z0 (m). Y đất = (Z − Z0) × (H/V).</summary>
    public double Datum { get; set; }

    public double InsertX { get; set; }

    public double InsertY { get; set; }

    /// <summary>Chiều rộng cột tên (đơn vị bản vẽ = mét lý trình).</summary>
    public double LabelWidth { get; set; } = 250;

    public double RowHeight { get; set; } = 5;

    public double TextHeight { get; set; } = 2;

    public List<string> EntityHandles { get; set; } = new();

    public string Summary()
    {
        var hv = "1/" + VerticalScale + "; 1/" + HorizontalScale;
        return "Tỷ lệ " + hv + ". Mức so sánh " + Datum.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " m.";
    }
}
