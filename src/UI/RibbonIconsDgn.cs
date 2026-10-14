using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfPoint = System.Windows.Point;

namespace iSurvey.UI;

/// <summary>Icon nhóm DGN ➔ CAD (cùng kiểu vẽ vector với các icon Panel khác).</summary>
internal static partial class RibbonIcons
{
    // ---------- Panel 1 / nhóm DGN ----------
    public static BitmapSource DgnToCad(int size) =>
        Render(size, Grad(0xF59E0B, 0xB45309), (dc, s) => DrawConvert(dc, s, leftIsDgn: true));

    public static BitmapSource CadToDgn(int size) =>
        Render(size, Grad(0x8B5CF6, 0x5B21B6), (dc, s) => DrawConvert(dc, s, leftIsDgn: false));

    public static BitmapSource DgnConfig(int size) =>
        Render(size, Grad(0x64748B, 0x334155), DrawGear);

    /// <summary>Hai khung + mũi tên: khung "DGN" có hai vạch ngang (level), khung "CAD" có dấu chéo.</summary>
    private static void DrawConvert(DrawingContext dc, double s, bool leftIsDgn)
    {
        var pen = Stroke(s, 0.07);

        DrawFormatBox(dc, pen, s, 0.05, isDgn: leftIsDgn);
        DrawFormatBox(dc, pen, s, 0.69, isDgn: !leftIsDgn);

        Seg(dc, pen, s, 0.36, 0.50, 0.56, 0.50);
        Poly(dc, WpfBrushes.White, null, s, true, 0.53, 0.38, 0.66, 0.50, 0.53, 0.62);
    }

    private static void DrawFormatBox(DrawingContext dc, System.Windows.Media.Pen pen, double s, double x, bool isDgn)
    {
        const double y = 0.28;
        const double w = 0.26;
        const double h = 0.44;
        Box(dc, null, pen, s, x, y, w, h);

        if (isDgn)
        {
            Seg(dc, pen, s, x + 0.05, y + 0.14, x + w - 0.05, y + 0.14);
            Seg(dc, pen, s, x + 0.05, y + h - 0.14, x + w - 0.05, y + h - 0.14);
        }
        else
        {
            Seg(dc, pen, s, x + 0.05, y + 0.08, x + w - 0.05, y + h - 0.08);
            Seg(dc, pen, s, x + w - 0.05, y + 0.08, x + 0.05, y + h - 0.08);
        }
    }

    /// <summary>Bánh răng: vòng tròn + 8 răng.</summary>
    private static void DrawGear(DrawingContext dc, double s)
    {
        var ring = Stroke(s, 0.09);
        dc.DrawEllipse(null, ring, new WpfPoint(s * 0.5, s * 0.5), s * 0.17, s * 0.17);

        var teeth = Stroke(s, 0.12);
        for (var i = 0; i < 8; i++)
        {
            var a = i * Math.PI / 4;
            var cos = Math.Cos(a);
            var sin = Math.Sin(a);
            Seg(dc, teeth, s, 0.5 + cos * 0.27, 0.5 + sin * 0.27, 0.5 + cos * 0.38, 0.5 + sin * 0.38);
        }

        dc.DrawEllipse(WpfBrushes.White, null, new WpfPoint(s * 0.5, s * 0.5), Math.Max(1.2, s * 0.05), Math.Max(1.2, s * 0.05));
    }
}
