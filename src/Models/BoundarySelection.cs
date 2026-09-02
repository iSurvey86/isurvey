using Autodesk.AutoCAD.Geometry;

namespace iSurvey.Models;

/// <summary>Kết quả chọn đường bao: bbox để tải tile + polygon WCS để clip raster.</summary>
public sealed class BoundarySelection
{
    public required ViewportCorners Bounds { get; init; }

    /// <summary>Đỉnh đa giác đóng trong WCS (XY), dùng clip RasterImage.</summary>
    public required IReadOnlyList<Point2d> ClipPolygonWcs { get; init; }
}
