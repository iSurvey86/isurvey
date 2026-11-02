namespace iSurvey.Modules.Tuyen;

/// <summary>Kiểu vẽ cọc — lưu máy người dùng, áp dụng khi phát sinh cọc.</summary>
public sealed class StationStyleSettings
{
    /// <summary>Chiều dài gạch cọc (m). 0 = tự động theo khung nhìn.</summary>
    public double TickLength { get; set; }

    /// <summary>Cao chữ lý trình (m). 0 = tự động theo khung nhìn.</summary>
    public double TextHeight { get; set; }

    /// <summary>Khoảng cách từ tâm tuyến tới điểm neo chữ (m). 0 = tự động.</summary>
    public double TextOffset { get; set; }

    /// <summary>Nếu tim polyline có độ rộng: lấy chiều dài cọc theo độ rộng × hệ số.</summary>
    public bool UsePolylineWidth { get; set; }

    /// <summary>Hệ số nhân độ rộng polyline khi <see cref="UsePolylineWidth"/>.</summary>
    public double WidthFactor { get; set; } = 1.0;
}
