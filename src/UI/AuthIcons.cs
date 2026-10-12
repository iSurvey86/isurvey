using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;
using WpfPen = System.Windows.Media.Pen;
using WpfPoint = System.Windows.Point;

namespace iSurvey.UI;

/// <summary>Icon Panel 6 (Tài khoản &amp; Hệ thống) — cùng phong cách RibbonIcons: nền bo góc, nét trắng.</summary>
internal static class AuthIcons
{
    public static BitmapSource UserLocked(int size) => Render(size, Grad(0xF77F00, 0xD62828), DrawUserWithLock);
    public static BitmapSource UserActive(int size) => Render(size, Grad(0x10B981, 0x047857), DrawUserWithDot);
    public static BitmapSource Refresh(int size) => Render(size, Solid(0x0096C7), DrawRefresh);
    public static BitmapSource Info(int size) => Render(size, Solid(0x7209B7), DrawInfo);
    public static BitmapSource Power(int size) => Render(size, Solid(0xB82318), DrawPower);

    // ---------- Helpers ----------
    private static WpfColor Hex(int rgb) =>
        WpfColor.FromRgb((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF));

    private static WpfBrush Solid(int rgb)
    {
        var b = new SolidColorBrush(Hex(rgb));
        b.Freeze();
        return b;
    }

    private static WpfBrush Grad(int from, int to)
    {
        var b = new LinearGradientBrush(Hex(from), Hex(to), new WpfPoint(0, 0), new WpfPoint(1, 1));
        b.Freeze();
        return b;
    }

    private static BitmapSource Render(int size, WpfBrush background, Action<DrawingContext, double> draw)
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

    private static WpfPen Stroke(double s, double k = 0.07) =>
        new(WpfBrushes.White, Math.Max(1.1, s * k))
        {
            LineJoin = PenLineJoin.Round,
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };

    private static WpfPoint P(double s, double x, double y) => new(s * x, s * y);

    /// <summary>Người dùng (đầu tròn + vai bán nguyệt) quanh tâm (cx,cy), hệ số k.</summary>
    private static void DrawUser(DrawingContext dc, double s, double cx, double cy, double k)
    {
        dc.DrawEllipse(WpfBrushes.White, null, P(s, cx, cy - 0.13 * k), s * 0.11 * k, s * 0.11 * k);

        var fig = new PathFigure { StartPoint = P(s, cx - 0.23 * k, cy + 0.30 * k), IsClosed = true, IsFilled = true };
        fig.Segments.Add(new ArcSegment(P(s, cx + 0.23 * k, cy + 0.30 * k), new System.Windows.Size(s * 0.23 * k, s * 0.23 * k),
            0, false, SweepDirection.Clockwise, true));
        var geo = new PathGeometry();
        geo.Figures.Add(fig);
        dc.DrawGeometry(WpfBrushes.White, null, geo);
    }

    private static void DrawUserWithLock(DrawingContext dc, double s)
    {
        DrawUser(dc, s, 0.38, 0.42, 0.85);

        // Ổ khóa góc dưới phải
        var pen = Stroke(s, 0.06);
        var shackle = new PathFigure { StartPoint = P(s, 0.60, 0.60), IsClosed = false, IsFilled = false };
        shackle.Segments.Add(new LineSegment(P(s, 0.60, 0.53), true));
        shackle.Segments.Add(new ArcSegment(P(s, 0.80, 0.53), new System.Windows.Size(s * 0.10, s * 0.10), 0, false,
            SweepDirection.Clockwise, true));
        shackle.Segments.Add(new LineSegment(P(s, 0.80, 0.60), true));
        var g = new PathGeometry();
        g.Figures.Add(shackle);
        dc.DrawGeometry(null, pen, g);

        dc.DrawRoundedRectangle(WpfBrushes.White, null, new Rect(s * 0.54, s * 0.58, s * 0.32, s * 0.26), s * 0.04, s * 0.04);
        dc.DrawEllipse(new SolidColorBrush(Hex(0xD62828)), null, P(s, 0.70, 0.69), s * 0.035, s * 0.035);
    }

    private static void DrawUserWithDot(DrawingContext dc, double s)
    {
        DrawUser(dc, s, 0.5, 0.42, 1.0);

        // Chấm trạng thái "đã kích hoạt" ở góc dưới phải
        var ring = new WpfPen(WpfBrushes.White, Math.Max(1.0, s * 0.045));
        dc.DrawEllipse(new SolidColorBrush(Hex(0x2EC4B6)), ring, P(s, 0.79, 0.79), s * 0.12, s * 0.12);
    }

    private static void DrawRefresh(DrawingContext dc, double s)
    {
        const double cx = 0.5, cy = 0.5, r = 0.26;
        var startDeg = -50.0;
        var endDeg = 230.0; // quét 280° theo chiều kim đồng hồ

        static WpfPoint At(double s, double cx, double cy, double r, double deg) =>
            new(s * (cx + r * Math.Cos(deg * Math.PI / 180)), s * (cy + r * Math.Sin(deg * Math.PI / 180)));

        var pen = Stroke(s, 0.075);
        var arc = new PathFigure { StartPoint = At(s, cx, cy, r, startDeg), IsClosed = false, IsFilled = false };
        arc.Segments.Add(new ArcSegment(At(s, cx, cy, r, endDeg), new System.Windows.Size(s * r, s * r), 0, true,
            SweepDirection.Clockwise, true));
        var g = new PathGeometry();
        g.Figures.Add(arc);
        dc.DrawGeometry(null, pen, g);

        // Đầu mũi tên tại điểm cuối, hướng theo tiếp tuyến
        var end = At(s, cx, cy, r, endDeg);
        var rad = endDeg * Math.PI / 180;
        var dir = new Vector(-Math.Sin(rad), Math.Cos(rad));
        var normal = new Vector(-dir.Y, dir.X);
        var tip = end + dir * (s * 0.13);
        var b1 = end + normal * (s * 0.10);
        var b2 = end - normal * (s * 0.10);
        var head = new PathFigure { StartPoint = tip, IsClosed = true, IsFilled = true };
        head.Segments.Add(new LineSegment(b1, true));
        head.Segments.Add(new LineSegment(b2, true));
        var hg = new PathGeometry();
        hg.Figures.Add(head);
        dc.DrawGeometry(WpfBrushes.White, null, hg);
    }

    private static void DrawInfo(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.07);
        dc.DrawEllipse(null, pen, P(s, 0.5, 0.5), s * 0.30, s * 0.30);
        dc.DrawEllipse(WpfBrushes.White, null, P(s, 0.5, 0.34), s * 0.045, s * 0.045);
        dc.DrawLine(pen, P(s, 0.5, 0.46), P(s, 0.5, 0.70));
    }

    private static void DrawPower(DrawingContext dc, double s)
    {
        const double cx = 0.5, cy = 0.55, r = 0.25;
        var pen = Stroke(s, 0.075);

        static WpfPoint At(double s, double cx, double cy, double r, double deg) =>
            new(s * (cx + r * Math.Cos(deg * Math.PI / 180)), s * (cy + r * Math.Sin(deg * Math.PI / 180)));

        // Cung hở ở đỉnh: từ -60° quét 300° theo chiều kim đồng hồ tới 240°
        var arc = new PathFigure { StartPoint = At(s, cx, cy, r, -60), IsClosed = false, IsFilled = false };
        arc.Segments.Add(new ArcSegment(At(s, cx, cy, r, 240), new System.Windows.Size(s * r, s * r), 0, true,
            SweepDirection.Clockwise, true));
        var g = new PathGeometry();
        g.Figures.Add(arc);
        dc.DrawGeometry(null, pen, g);

        dc.DrawLine(pen, P(s, 0.5, 0.20), P(s, 0.5, 0.52));
    }
}
