using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using iSurvey.Models;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Modules.Map;

/// <summary>Ảnh chụp khung nhìn Model Space hiện tại.</summary>
public sealed class ViewportSnapshot
{
    public required Point3d[] CornersWcs { get; init; }
    public double ViewWidth { get; init; }
    public double ViewHeight { get; init; }
    public double ScreenWidth { get; init; }
    public double ScreenHeight { get; init; }

    public static ViewportSnapshot Capture(Document doc)
    {
        var ed = doc.Editor;
        using var view = ed.GetCurrentView();

        if (view.Width <= 0 || view.Height <= 0)
            throw new InvalidOperationException("Viewport có kích thước không hợp lệ.");

        var toWcs = Matrix3d.Displacement(view.Target - Point3d.Origin)
            * Matrix3d.Rotation(-view.ViewTwist, view.ViewDirection, view.Target)
            * Matrix3d.PlaneToWorld(view.ViewDirection);

        var halfW = view.Width / 2.0;
        var halfH = view.Height / 2.0;
        var center = view.CenterPoint;

        var localCorners = new[]
        {
            new Point3d(center.X - halfW, center.Y - halfH, 0),
            new Point3d(center.X + halfW, center.Y - halfH, 0),
            new Point3d(center.X + halfW, center.Y + halfH, 0),
            new Point3d(center.X - halfW, center.Y + halfH, 0)
        };

        var wcs = new Point3d[localCorners.Length];
        for (var i = 0; i < localCorners.Length; i++)
            wcs[i] = localCorners[i].TransformBy(toWcs);

        var screenW = 1920.0;
        var screenH = 1080.0;
        try
        {
            if (AcadApp.GetSystemVariable("SCREENSIZE") is Point2d screen
                && screen.X > 0 && screen.Y > 0)
            {
                screenW = screen.X;
                screenH = screen.Y;
            }
        }
        catch
        {
            // giữ mặc định
        }

        return new ViewportSnapshot
        {
            CornersWcs = wcs,
            ViewWidth = view.Width,
            ViewHeight = view.Height,
            ScreenWidth = screenW,
            ScreenHeight = screenH
        };
    }

    public GeoBounds ToWgs84Bounds(CoordinateService coordinates, double centralMeridian)
    {
        var points = new List<GeoPoint>(CornersWcs.Length);
        foreach (var pt in CornersWcs)
        {
            var wgs = coordinates.ToWgs84(new PlanePoint(pt.X, pt.Y), centralMeridian);
            points.Add(wgs);
        }

        return TileMath.ClampBounds(new GeoBounds(
            points.Min(p => p.Longitude),
            points.Min(p => p.Latitude),
            points.Max(p => p.Longitude),
            points.Max(p => p.Latitude)));
    }
}
