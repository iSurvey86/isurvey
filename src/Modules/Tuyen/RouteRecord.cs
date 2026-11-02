namespace iSurvey.Modules.Tuyen;

/// <summary>Tuyến nội bộ: một polyline và lý trình gốc. Chưa phải Alignment của Civil.</summary>
public sealed class RouteRecord
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string PolylineHandle { get; set; } = "";

    /// <summary>Lý trình tại điểm đầu đã chọn, đơn vị mét.</summary>
    public double StartChainage { get; set; }

    /// <summary>
    /// false: lý trình tăng theo chiều polyline (đỉnh đầu → cuối).
    /// true: đã chọn đầu gần đỉnh cuối, lý trình tăng ngược chiều vẽ.
    /// </summary>
    public bool Reversed { get; set; }

    public string TerrainId { get; set; } = "";

    public string Layer { get; set; } = "iSurvey_Tuyen";

    public List<StationRecord> Stations { get; set; } = new();

    public string Summary()
    {
        var chain = StartChainage.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        var coc = 0;
        var zok = 0;
        if (Stations is not null)
        {
            coc = Stations.Count;
            foreach (var station in Stations)
            {
                if (station.HasZ)
                    zok++;
            }
        }

        var huong = Reversed ? "Đã đảo so với chiều vẽ." : "Theo chiều đã chọn đầu.";
        return "Lý trình gốc " + chain + " m. " + huong + "\nCọc " + coc
            + (coc == 0 ? "" : ", có Z " + zok);
    }
}
