namespace iSurvey.Modules.Tuyen;

/// <summary>Một cọc trên tuyến: lý trình, tọa độ và Z tự nhiên nếu có.</summary>
public sealed class StationRecord
{
    public string Name { get; set; } = "";

    /// <summary>Lý trình tuyệt đối (m), tính từ lý trình gốc của tuyến.</summary>
    public double Chainage { get; set; }

    public double X { get; set; }

    public double Y { get; set; }

    public double Z { get; set; }

    public bool HasZ { get; set; }

    public string MarkerHandle { get; set; } = "";

    public string LabelHandle { get; set; } = "";
}
