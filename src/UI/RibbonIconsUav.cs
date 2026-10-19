using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfPoint = System.Windows.Point;

namespace iSurvey.UI;

/// <summary>Icon panel UAV &amp; LiDAR (vector, đồng bộ kiểu RibbonIcons).</summary>
internal static partial class RibbonIcons
{
    // Nhóm ảnh UAV — xanh dương
    public static BitmapSource UavInsert(int size) => Render(size, Grad(0x2563EB, 0x1D4ED8), DrawUavImage);
    public static BitmapSource UavClip(int size) => Render(size, Grad(0x0EA5E9, 0x0369A1), DrawScissors);
    public static BitmapSource UavFrame(int size) => Render(size, Solid(0x0284C7), DrawImageFrame);
    public static BitmapSource UavSendBack(int size) => Render(size, Solid(0x0369A1), DrawSendBack);

    // Nhóm bề mặt — cam
    public static BitmapSource UavDem(int size) => Render(size, Grad(0xF77F00, 0xD62828), DrawDem);
    public static BitmapSource UavContour(int size) => Render(size, Solid(0xE09F3E), DrawContourLabel);
    public static BitmapSource UavCrs(int size) => Render(size, Solid(0xF59E0B), DrawCrs);

    // Point cloud — xám xanh
    public static BitmapSource PcImport(int size) => Render(size, Grad(0x64748B, 0x334155), DrawPointCloud);
    public static BitmapSource PcFilter(int size) => Render(size, Solid(0x475569), DrawFunnel);
    public static BitmapSource PcGrid(int size) => Render(size, Solid(0x334155), DrawPointGrid);

    /// <summary>Ảnh chữ nhật + cánh quạt UAV nhỏ.</summary>
    private static void DrawUavImage(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.055);
        Box(dc, null, pen, s, 0.14, 0.22, 0.72, 0.52);
        Seg(dc, pen, s, 0.14, 0.38, 0.86, 0.38);
        Seg(dc, pen, s, 0.40, 0.22, 0.40, 0.74);
        // cánh quạt
        dc.DrawEllipse(null, pen, new WpfPoint(s * 0.50, s * 0.84), s * 0.10, s * 0.06);
        Seg(dc, pen, s, 0.50, 0.74, 0.50, 0.80);
    }

    private static void DrawImageFrame(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.07);
        Box(dc, null, pen, s, 0.18, 0.18, 0.64, 0.64);
        Box(dc, null, pen, s, 0.30, 0.30, 0.40, 0.40);
    }

    private static void DrawSendBack(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.07);
        Box(dc, null, pen, s, 0.22, 0.18, 0.56, 0.36);
        Box(dc, WpfBrushes.White, null, s, 0.30, 0.42, 0.56, 0.36);
        Seg(dc, pen, s, 0.50, 0.28, 0.50, 0.68);
        Poly(dc, WpfBrushes.White, null, s, true, 0.38, 0.58, 0.62, 0.58, 0.50, 0.76);
    }

    private static void DrawDem(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.06);
        Poly(dc, null, pen, s, true, 0.10, 0.72, 0.28, 0.40, 0.48, 0.56, 0.68, 0.24, 0.90, 0.48, 0.90, 0.84, 0.10, 0.84);
        Seg(dc, pen, s, 0.10, 0.72, 0.90, 0.48);
    }

    private static void DrawCrs(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.07);
        dc.DrawEllipse(null, pen, new WpfPoint(s * 0.50, s * 0.50), s * 0.30, s * 0.30);
        Seg(dc, pen, s, 0.20, 0.50, 0.80, 0.50);
        Seg(dc, pen, s, 0.50, 0.20, 0.50, 0.80);
        Seg(dc, pen, s, 0.28, 0.32, 0.72, 0.68);
    }

    private static void DrawPointCloud(DrawingContext dc, double s)
    {
        var r = Math.Max(1.2, s * 0.045);
        foreach (var (x, y) in new[]
                 {
                     (0.22, 0.30), (0.40, 0.22), (0.58, 0.34), (0.74, 0.26),
                     (0.28, 0.50), (0.48, 0.48), (0.66, 0.54), (0.80, 0.46),
                     (0.24, 0.72), (0.44, 0.70), (0.60, 0.76), (0.76, 0.68)
                 })
            dc.DrawEllipse(WpfBrushes.White, null, new WpfPoint(s * x, s * y), r, r);
    }

    private static void DrawPointGrid(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.05);
        for (var i = 0; i < 4; i++)
        {
            var t = 0.22 + i * 0.18;
            Seg(dc, pen, s, 0.18, t, 0.82, t);
            Seg(dc, pen, s, t, 0.18, t, 0.82);
        }

        var r = Math.Max(1.1, s * 0.04);
        for (var i = 0; i < 4; i++)
        for (var j = 0; j < 4; j++)
            dc.DrawEllipse(WpfBrushes.White, null,
                new WpfPoint(s * (0.22 + i * 0.18), s * (0.22 + j * 0.18)), r, r);
    }
}
