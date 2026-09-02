using Autodesk.AutoCAD.Geometry;

namespace iSurvey.Modules.Map;

/// <summary>Trạng thái basemap đang bật — dùng cho AutoRefresh và clip đường bao.</summary>
public static class BasemapSession
{
    public static bool IsActive { get; private set; }
    public static double CentralMeridian { get; private set; }
    public static string BasemapId { get; private set; } = string.Empty;
    public static bool AutoRefresh { get; private set; }

    /// <summary>Đa giác clip WCS — null = theo khung nhìn.</summary>
    public static IReadOnlyList<Point2d>? ClipPolygonWcs { get; private set; }

    public static bool HasBoundaryClip => ClipPolygonWcs is { Count: >= 3 };

    public static void Activate(
        double centralMeridian,
        string basemapId,
        bool autoRefresh = true,
        IReadOnlyList<Point2d>? clipPolygonWcs = null)
    {
        CentralMeridian = centralMeridian;
        BasemapId = basemapId;
        AutoRefresh = autoRefresh;
        ClipPolygonWcs = clipPolygonWcs is { Count: >= 3 }
            ? clipPolygonWcs.ToList()
            : null;
        IsActive = true;
    }

    public static void Deactivate()
    {
        IsActive = false;
        BasemapId = string.Empty;
        AutoRefresh = false;
        ClipPolygonWcs = null;
    }
}
