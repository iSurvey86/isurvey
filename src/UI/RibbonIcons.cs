using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfColor = System.Windows.Media.Color;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfPoint = System.Windows.Point;
using WpfPen = System.Windows.Media.Pen;

namespace iSurvey.UI;

/// <summary>Icon Ribbon vector — không phụ thuộc file ngoài.</summary>
internal static class RibbonIcons
{
    public static BitmapSource MapInsert(int size) =>
        Render(size, new SolidColorBrush(WpfColor.FromRgb(0x1B, 0x5E, 0x20)), DrawMapLayers);

    public static BitmapSource MapDelete(int size) =>
        Render(size, new SolidColorBrush(WpfColor.FromRgb(0xB7, 0x1C, 0x1C)), DrawTrash);

    private static BitmapSource Render(int size, System.Windows.Media.Brush background, Action<DrawingContext, double> draw)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRoundedRectangle(background, null, new Rect(0, 0, size, size), size * 0.18, size * 0.18);
            draw(dc, size);
        }

        var bmp = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(visual);
        bmp.Freeze();
        return bmp;
    }

    /// <summary>Lớp bản đồ + chấm vị trí (Chèn Google Earth).</summary>
    private static void DrawMapLayers(DrawingContext dc, double s)
    {
        var stroke = new WpfPen(WpfBrushes.White, s * 0.04) { LineJoin = PenLineJoin.Round };

        dc.DrawRectangle(null, stroke, new Rect(s * 0.18, s * 0.30, s * 0.52, s * 0.38));
        dc.DrawRectangle(null, stroke, new Rect(s * 0.26, s * 0.22, s * 0.52, s * 0.38));
        dc.DrawRectangle(new SolidColorBrush(WpfColor.FromArgb(0x55, 0xFF, 0xFF, 0xFF)), stroke,
            new Rect(s * 0.34, s * 0.14, s * 0.52, s * 0.38));

        var pin = new PathGeometry();
        var fig = new PathFigure(new WpfPoint(s * 0.62, s * 0.26), [], false);
        fig.Segments.Add(new LineSegment(new WpfPoint(s * 0.56, s * 0.38), true));
        fig.Segments.Add(new LineSegment(new WpfPoint(s * 0.68, s * 0.38), true));
        fig.IsClosed = true;
        pin.Figures.Add(fig);
        dc.DrawGeometry(WpfBrushes.White, null, pin);
        dc.DrawEllipse(WpfBrushes.White, null, new WpfPoint(s * 0.62, s * 0.24), s * 0.05, s * 0.05);
    }

    /// <summary>Thùng rác (Xóa GE).</summary>
    private static void DrawTrash(DrawingContext dc, double s)
    {
        var pen = new WpfPen(WpfBrushes.White, s * 0.045)
        {
            LineJoin = PenLineJoin.Round,
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };

        dc.DrawLine(pen, new WpfPoint(s * 0.22, s * 0.28), new WpfPoint(s * 0.78, s * 0.28));
        dc.DrawLine(pen, new WpfPoint(s * 0.38, s * 0.22), new WpfPoint(s * 0.62, s * 0.22));

        var body = new PathGeometry();
        var fig = new PathFigure(new WpfPoint(s * 0.30, s * 0.32), [], false);
        fig.Segments.Add(new LineSegment(new WpfPoint(s * 0.34, s * 0.78), true));
        fig.Segments.Add(new LineSegment(new WpfPoint(s * 0.66, s * 0.78), true));
        fig.Segments.Add(new LineSegment(new WpfPoint(s * 0.70, s * 0.32), true));
        body.Figures.Add(fig);
        dc.DrawGeometry(null, pen, body);

        dc.DrawLine(pen, new WpfPoint(s * 0.44, s * 0.38), new WpfPoint(s * 0.46, s * 0.70));
        dc.DrawLine(pen, new WpfPoint(s * 0.56, s * 0.38), new WpfPoint(s * 0.54, s * 0.70));
    }
}
