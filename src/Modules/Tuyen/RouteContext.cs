namespace iSurvey.Modules.Tuyen;

/// <summary>Bảng Khảo sát ghi tuyến đang chọn trước khi gửi lệnh.</summary>
public static class RouteContext
{
    public static string? RouteId { get; set; }

    public static string? TakeRoute()
    {
        var id = RouteId;
        RouteId = null;
        return id;
    }
}
