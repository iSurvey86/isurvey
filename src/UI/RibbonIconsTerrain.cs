using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfPoint = System.Windows.Point;

namespace iSurvey.UI;

/// <summary>Icon riêng cho từng lệnh Biên tập địa hình — không dùng lại glyph của panel khác.</summary>
internal static partial class RibbonIcons
{
    public static BitmapSource BienDiem(int size) => Render(size, Solid(0xEA580C), DrawSurveyCluster);
    public static BitmapSource ImportPoints(int size) => Render(size, Grad(0xC2410C, 0x9A3412), DrawXyzIn);
    public static BitmapSource ExportPoints(int size) => Render(size, Grad(0x1D4ED8, 0x1E3A8A), DrawXyzOut);
    public static BitmapSource NangCao(int size) => Render(size, Solid(0x15803D), DrawElevate);
    public static BitmapSource TaoDiemCad(int size) => Render(size, Solid(0x0F766E), DrawEntityToPoint);
    public static BitmapSource HienDiemMa(int size) => Render(size, Solid(0x0369A1), DrawCodedPoints);
    public static BitmapSource LocDiemMa(int size) => Render(size, Solid(0xB45309), DrawFilterCode);
    public static BitmapSource ChenDiavat(int size) => Render(size, Solid(0x166534), DrawBlockOnPoint);
    public static BitmapSource TatDiemBd(int size) => Render(size, Solid(0x6B7280), DrawHidePoints);
    public static BitmapSource HienDiemTat(int size) => Render(size, Solid(0x2563EB), DrawShowPoints);
    public static BitmapSource XoaDiemVung(int size) => Render(size, Solid(0xB91C1C), DrawLassoDelete);
    public static BitmapSource NoiDiem(int size) => Render(size, Solid(0x0F766E), DrawJoinPoints);
    public static BitmapSource GanCao(int size) => Render(size, Solid(0x047857), DrawAssignZ);
    public static BitmapSource PhatSinhDiem(int size) => Render(size, Solid(0x7C3AED), DrawScatter);
    public static BitmapSource DinhViMia(int size) => Render(size, Solid(0xB45309), DrawRod);
    public static BitmapSource XoayMia(int size) => Render(size, Solid(0xA16207), DrawRotateRod);
    public static BitmapSource LocGianDiem(int size) => Render(size, Solid(0xF77F00), DrawThinPoints);

    private static void Dot(DrawingContext dc, double s, double x, double y, double k = 0.055) =>
        dc.DrawEllipse(WpfBrushes.White, null, new WpfPoint(s * x, s * y), Math.Max(1.2, s * k), Math.Max(1.2, s * k));

    private static void DrawSurveyCluster(DrawingContext dc, double s)
    {
        Dot(dc, s, 0.32, 0.62, 0.08);
        Dot(dc, s, 0.50, 0.28, 0.08);
        Dot(dc, s, 0.72, 0.58, 0.08);
        Seg(dc, Stroke(s, 0.05), s, 0.32, 0.62, 0.50, 0.28);
        Seg(dc, Stroke(s, 0.05), s, 0.50, 0.28, 0.72, 0.58);
    }

    /// <summary>Cột XYZ + mũi tên vào — nhập điểm đo.</summary>
    private static void DrawXyzIn(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.055);
        Box(dc, null, pen, s, 0.14, 0.18, 0.40, 0.64);
        Seg(dc, pen, s, 0.14, 0.40, 0.54, 0.40);
        Seg(dc, pen, s, 0.14, 0.62, 0.54, 0.62);
        Seg(dc, pen, s, 0.62, 0.50, 0.86, 0.50);
        Poly(dc, WpfBrushes.White, null, s, true, 0.72, 0.36, 0.72, 0.64, 0.90, 0.50);
    }

    /// <summary>Cột XYZ + mũi tên ra — xuất điểm đo.</summary>
    private static void DrawXyzOut(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.055);
        Box(dc, null, pen, s, 0.46, 0.18, 0.40, 0.64);
        Seg(dc, pen, s, 0.46, 0.40, 0.86, 0.40);
        Seg(dc, pen, s, 0.46, 0.62, 0.86, 0.62);
        Seg(dc, pen, s, 0.14, 0.50, 0.38, 0.50);
        Poly(dc, WpfBrushes.White, null, s, true, 0.28, 0.36, 0.28, 0.64, 0.10, 0.50);
    }

    private static void DrawElevate(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.07);
        Dot(dc, s, 0.50, 0.50, 0.08);
        Seg(dc, pen, s, 0.50, 0.34, 0.50, 0.16);
        Poly(dc, WpfBrushes.White, null, s, true, 0.38, 0.22, 0.62, 0.22, 0.50, 0.08);
        Seg(dc, pen, s, 0.50, 0.66, 0.50, 0.84);
        Poly(dc, WpfBrushes.White, null, s, true, 0.38, 0.78, 0.62, 0.78, 0.50, 0.92);
    }

    private static void DrawEntityToPoint(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.06);
        Box(dc, null, pen, s, 0.14, 0.28, 0.36, 0.44);
        Seg(dc, pen, s, 0.50, 0.50, 0.68, 0.50);
        Dot(dc, s, 0.78, 0.50, 0.08);
    }

    private static void DrawCodedPoints(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.05);
        Dot(dc, s, 0.28, 0.38);
        Dot(dc, s, 0.52, 0.62);
        Dot(dc, s, 0.74, 0.30);
        Seg(dc, pen, s, 0.34, 0.28, 0.52, 0.28);
        Seg(dc, pen, s, 0.58, 0.52, 0.78, 0.52);
    }

    private static void DrawFilterCode(DrawingContext dc, double s)
    {
        Poly(dc, WpfBrushes.White, null, s, true, 0.16, 0.16, 0.84, 0.16, 0.58, 0.46, 0.58, 0.72, 0.42, 0.64, 0.42, 0.46);
        Box(dc, null, Stroke(s, 0.05), s, 0.62, 0.58, 0.24, 0.24);
        Seg(dc, Stroke(s, 0.05), s, 0.68, 0.70, 0.80, 0.70);
    }

    private static void DrawBlockOnPoint(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.06);
        Dot(dc, s, 0.50, 0.72, 0.07);
        Box(dc, null, pen, s, 0.32, 0.18, 0.36, 0.36);
        Seg(dc, pen, s, 0.32, 0.36, 0.68, 0.36);
        Seg(dc, pen, s, 0.50, 0.18, 0.50, 0.54);
        Seg(dc, pen, s, 0.50, 0.54, 0.50, 0.66);
    }

    private static void DrawHidePoints(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.06);
        var fig = new PathFigure { StartPoint = new WpfPoint(s * 0.14, s * 0.50), IsClosed = true };
        fig.Segments.Add(new QuadraticBezierSegment(new WpfPoint(s * 0.50, s * 0.18), new WpfPoint(s * 0.86, s * 0.50), true));
        fig.Segments.Add(new QuadraticBezierSegment(new WpfPoint(s * 0.50, s * 0.82), new WpfPoint(s * 0.14, s * 0.50), true));
        var geo = new PathGeometry();
        geo.Figures.Add(fig);
        dc.DrawGeometry(null, pen, geo);
        dc.DrawEllipse(null, pen, new WpfPoint(s * 0.50, s * 0.50), s * 0.10, s * 0.10);
        Seg(dc, Stroke(s, 0.08), s, 0.22, 0.78, 0.78, 0.22);
    }

    private static void DrawShowPoints(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.06);
        var fig = new PathFigure { StartPoint = new WpfPoint(s * 0.14, s * 0.50), IsClosed = true };
        fig.Segments.Add(new QuadraticBezierSegment(new WpfPoint(s * 0.50, s * 0.18), new WpfPoint(s * 0.86, s * 0.50), true));
        fig.Segments.Add(new QuadraticBezierSegment(new WpfPoint(s * 0.50, s * 0.82), new WpfPoint(s * 0.14, s * 0.50), true));
        var geo = new PathGeometry();
        geo.Figures.Add(fig);
        dc.DrawGeometry(null, pen, geo);
        dc.DrawEllipse(WpfBrushes.White, null, new WpfPoint(s * 0.50, s * 0.50), s * 0.10, s * 0.10);
    }

    private static void DrawLassoDelete(DrawingContext dc, double s)
    {
        var dashed = Stroke(s, 0.05);
        dashed.DashStyle = new DashStyle(new double[] { 2.4, 2 }, 0);
        var fig = new PathFigure { StartPoint = new WpfPoint(s * 0.22, s * 0.38), IsClosed = true };
        fig.Segments.Add(new QuadraticBezierSegment(new WpfPoint(s * 0.20, s * 0.14), new WpfPoint(s * 0.50, s * 0.16), true));
        fig.Segments.Add(new QuadraticBezierSegment(new WpfPoint(s * 0.86, s * 0.22), new WpfPoint(s * 0.80, s * 0.52), true));
        fig.Segments.Add(new QuadraticBezierSegment(new WpfPoint(s * 0.70, s * 0.84), new WpfPoint(s * 0.38, s * 0.78), true));
        var geo = new PathGeometry();
        geo.Figures.Add(fig);
        dc.DrawGeometry(null, dashed, geo);
        Seg(dc, Stroke(s, 0.08), s, 0.34, 0.34, 0.66, 0.66);
        Seg(dc, Stroke(s, 0.08), s, 0.66, 0.34, 0.34, 0.66);
    }

    private static void DrawJoinPoints(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.07);
        Dot(dc, s, 0.18, 0.70);
        Dot(dc, s, 0.42, 0.28);
        Dot(dc, s, 0.68, 0.58);
        Dot(dc, s, 0.86, 0.26);
        Poly(dc, null, pen, s, false, 0.18, 0.70, 0.42, 0.28, 0.68, 0.58, 0.86, 0.26);
    }

    private static void DrawAssignZ(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.055);
        Poly(dc, null, pen, s, false, 0.12, 0.78, 0.32, 0.46, 0.52, 0.60, 0.78, 0.28);
        Dot(dc, s, 0.52, 0.60, 0.07);
        Seg(dc, pen, s, 0.52, 0.48, 0.52, 0.22);
        Poly(dc, WpfBrushes.White, null, s, true, 0.42, 0.28, 0.62, 0.28, 0.52, 0.12);
    }

    private static void DrawScatter(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.05);
        Box(dc, null, pen, s, 0.16, 0.16, 0.68, 0.68);
        foreach (var (x, y) in new[] { (0.30, 0.34), (0.48, 0.52), (0.66, 0.30), (0.38, 0.68), (0.70, 0.62) })
            Dot(dc, s, x, y);
    }

    private static void DrawRod(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.07);
        Seg(dc, pen, s, 0.50, 0.16, 0.50, 0.78);
        Dot(dc, s, 0.50, 0.82, 0.07);
        Seg(dc, Stroke(s, 0.05), s, 0.38, 0.28, 0.62, 0.28);
        Seg(dc, Stroke(s, 0.05), s, 0.40, 0.44, 0.60, 0.44);
    }

    private static void DrawRotateRod(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.06);
        Seg(dc, pen, s, 0.22, 0.70, 0.78, 0.38);
        Seg(dc, pen, s, 0.50, 0.54, 0.62, 0.22);
        var fig = new PathFigure { StartPoint = new WpfPoint(s * 0.70, s * 0.22), IsClosed = false };
        fig.Segments.Add(new ArcSegment(new WpfPoint(s * 0.82, s * 0.48),
            new System.Windows.Size(s * 0.16, s * 0.16), 0, false, SweepDirection.Clockwise, true));
        var geo = new PathGeometry();
        geo.Figures.Add(fig);
        dc.DrawGeometry(null, pen, geo);
        Poly(dc, WpfBrushes.White, null, s, true, 0.76, 0.42, 0.90, 0.46, 0.80, 0.58);
    }

    private static void DrawThinPoints(DrawingContext dc, double s)
    {
        Dot(dc, s, 0.22, 0.28, 0.04);
        Dot(dc, s, 0.38, 0.22, 0.04);
        Dot(dc, s, 0.28, 0.48, 0.04);
        Dot(dc, s, 0.44, 0.42, 0.04);
        Dot(dc, s, 0.34, 0.68, 0.04);
        Dot(dc, s, 0.70, 0.36, 0.09);
        Dot(dc, s, 0.78, 0.68, 0.09);
        Seg(dc, Stroke(s, 0.05), s, 0.52, 0.20, 0.52, 0.80);
        Poly(dc, WpfBrushes.White, null, s, true, 0.52, 0.50, 0.64, 0.42, 0.64, 0.58);
    }
}
