using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfColor = System.Windows.Media.Color;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfPoint = System.Windows.Point;
using WpfPen = System.Windows.Media.Pen;

namespace iSurvey.UI;

/// <summary>
/// Icon Ribbon vector â€” váº½ báº±ng DrawingVisual â†’ RenderTargetBitmap, khÃ´ng cáº§n file áº£nh ngoÃ i.
/// Ná»n bo gÃ³c (bÃ¡n kÃ­nh size * 0.18), nÃ©t tráº¯ng.
/// </summary>
internal static partial class RibbonIcons
{
    // ---------- Panel 1: Báº£n Ä‘á»“ (ná»n Ä‘áº·c) ----------
    public static BitmapSource MapInsert(int size) => Render(size, Solid(0x3B7D3B), DrawMapLayers);
    public static BitmapSource MapDelete(int size) => Render(size, Solid(0xB82318), DrawTrash);
    public static BitmapSource ExportGe(int size) => Render(size, Solid(0x0C59CF), DrawExport);
    public static BitmapSource ImportKml(int size) => Render(size, Grad(0x2B9348, 0x1B6CA8), DrawFileDown);
    public static BitmapSource ImportTab(int size) => Render(size, Grad(0x0F766E, 0x1B6CA8), DrawFileDown);
    public static BitmapSource ExportKml(int size) => Render(size, Grad(0x0C59CF, 0x023E8A), DrawFileUp);
    public static BitmapSource UpdateGe(int size) => Render(size, Solid(0x0096C7), DrawRefresh);

    // ---------- Panel 2: Khung & Cáº¯t BÃ¬nh Äá»“ ----------
    public static BitmapSource TaoKhung(int size) => Render(size, Grad(0x0096C7, 0x023E8A), DrawFrames);
    public static BitmapSource CatBd(int size) => Render(size, Grad(0xF77F00, 0xD62828), DrawScissors);
    public static BitmapSource RaiKhung(int size) => Render(size, Solid(0x0096C7), DrawFramesChain);
    public static BitmapSource KhungChuan(int size) => Render(size, Grad(0x1D6FA5, 0x0B3C5D), DrawStdFrame);
    public static BitmapSource KhungBanVe(int size) => Render(size, Grad(0x2A9D8F, 0x264653), DrawTechFrame);
    public static BitmapSource TileKhung(int size) => Render(size, Solid(0x2A9D8F), DrawScaleArrows);
    public static BitmapSource KhoaKhung(int size) => Render(size, Solid(0xE09F3E), DrawLock);

    // ---------- Panel 3: BiÃªn táº­p Äá»‹a hÃ¬nh ----------
    public static BitmapSource GhiSoDm(int size) => Render(size, Grad(0x2A9D8F, 0x264653), DrawContourLabel);
    public static BitmapSource LocDiem(int size) => Render(size, Solid(0xF77F00), DrawFunnel);
    public static BitmapSource AnTin(int size) => Render(size, Solid(0x0096C7), DrawTin);
    public static BitmapSource ChuyenFont(int size) => Render(size, Solid(0x7209B7), DrawLetterT);

    // ---------- Panel 4: Tuyáº¿n & Cá»™t Äiá»‡n ----------
    public static BitmapSource RaiMong(int size) => Render(size, Grad(0x7209B7, 0x3A0CA3), DrawFoundation);
    public static BitmapSource BienDuongTam(int size) => Render(size, Solid(0xE09F3E), DrawRoad);
    public static BitmapSource KeTuyen(int size) => Render(size, Solid(0xB82318), DrawLightning);
    public static BitmapSource DanhSoGoc(int size) => Render(size, Grad(0xEF4444, 0xB91C1C), DrawVertexNumbers);
    public static BitmapSource DuoiTuyen(int size) => Render(size, Grad(0x0F766E, 0x115E59), DrawStripMap);
    public static BitmapSource DanhSoCot(int size) => Render(size, Solid(0x2A9D8F), DrawTag);

    // ---------- Panel 5: Há»“ sÆ¡ & QA/QC ----------
    public static BitmapSource BangToaDo(int size) => Render(size, Grad(0xE09F3E, 0x9E2A2B), DrawTable);
    public static BitmapSource KiemTraCaoDo(int size) => Render(size, Solid(0x3B7D3B), DrawCheck);
    public static BitmapSource AutoScan(int size) => Render(size, Solid(0x0096C7), DrawMagnifier);
    public static BitmapSource XuatExcel(int size) => Render(size, Solid(0x2A9D8F), DrawDocument);

    // ---------- Panel Admin: Trợ lý AI ----------
    public static BitmapSource AiAssist(int size) => Render(size, Grad(0x7C3AED, 0x4F46E5), DrawAiSpark);
    public static BitmapSource AiSettings(int size) => Render(size, Solid(0x6366F1), DrawAiGear);

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

    /// <summary>NÃ©t tráº¯ng; tá»‘i thiá»ƒu 1.1px Ä‘á»ƒ icon 16x16 khÃ´ng bá»‹ má».</summary>
    private static WpfPen Stroke(double s, double k = 0.06) =>
        new(WpfBrushes.White, Math.Max(1.1, s * k))
        {
            LineJoin = PenLineJoin.Round,
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };

    private static void Seg(DrawingContext dc, WpfPen pen, double s, double x1, double y1, double x2, double y2) =>
        dc.DrawLine(pen, new WpfPoint(s * x1, s * y1), new WpfPoint(s * x2, s * y2));

    private static void Box(DrawingContext dc, WpfBrush? fill, WpfPen? pen, double s, double x, double y, double w, double h) =>
        dc.DrawRectangle(fill, pen, new Rect(s * x, s * y, s * w, s * h));

    /// <summary>Äa giÃ¡c/Ä‘a tuyáº¿n theo tá»a Ä‘á»™ chuáº©n hÃ³a (x0,y0,x1,y1,...).</summary>
    private static void Poly(DrawingContext dc, WpfBrush? fill, WpfPen? pen, double s, bool closed, params double[] xy)
    {
        var fig = new PathFigure { StartPoint = new WpfPoint(s * xy[0], s * xy[1]), IsClosed = closed, IsFilled = fill is not null };
        for (var i = 2; i < xy.Length; i += 2)
            fig.Segments.Add(new LineSegment(new WpfPoint(s * xy[i], s * xy[i + 1]), true));
        var geo = new PathGeometry();
        geo.Figures.Add(fig);
        dc.DrawGeometry(fill, pen, geo);
    }

    // ---------- Glyph: Panel 1 (giá»¯ nguyÃªn hÃ¬nh) ----------
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

    private static void DrawTrash(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.045);

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

    private static void DrawExport(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.045);

        dc.DrawEllipse(null, pen, new WpfPoint(s * 0.42, s * 0.48), s * 0.22, s * 0.28);
        dc.DrawLine(pen, new WpfPoint(s * 0.20, s * 0.48), new WpfPoint(s * 0.64, s * 0.48));
        dc.DrawLine(pen, new WpfPoint(s * 0.42, s * 0.20), new WpfPoint(s * 0.42, s * 0.76));

        Poly(dc, WpfBrushes.White, null, s, true, 0.58, 0.32, 0.82, 0.48, 0.58, 0.64);
    }

    /// <summary>Tờ giấy và mũi tên xuống.</summary>
    private static void DrawFileDown(DrawingContext dc, double s) => DrawFileArrow(dc, s, down: true);

    /// <summary>Tờ giấy và mũi tên lên.</summary>
    private static void DrawFileUp(DrawingContext dc, double s) => DrawFileArrow(dc, s, down: false);

    private static void DrawFileArrow(DrawingContext dc, double s, bool down)
    {
        var pen = Stroke(s, 0.045);
        Poly(dc, null, pen, s, true, 0.18, 0.14, 0.58, 0.14, 0.74, 0.30, 0.74, 0.86, 0.18, 0.86);
        Seg(dc, pen, s, 0.58, 0.14, 0.58, 0.30);
        Seg(dc, pen, s, 0.58, 0.30, 0.74, 0.30);
        if (down)
        {
            Seg(dc, pen, s, 0.46, 0.40, 0.46, 0.66);
            Poly(dc, WpfBrushes.White, null, s, true, 0.32, 0.58, 0.60, 0.58, 0.46, 0.76);
        }
        else
        {
            Seg(dc, pen, s, 0.46, 0.76, 0.46, 0.50);
            Poly(dc, WpfBrushes.White, null, s, true, 0.32, 0.52, 0.60, 0.52, 0.46, 0.34);
        }
    }

    /// <summary>Mũi tên vòng — làm mới ảnh.</summary>
    private static void DrawRefresh(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.07);
        var fig = new PathFigure { StartPoint = new WpfPoint(s * 0.72, s * 0.28), IsClosed = false };
        fig.Segments.Add(new ArcSegment(
            new WpfPoint(s * 0.28, s * 0.36),
            new System.Windows.Size(s * 0.24, s * 0.24),
            0, true, SweepDirection.Counterclockwise, true));
        var geo = new PathGeometry();
        geo.Figures.Add(fig);
        dc.DrawGeometry(null, pen, geo);
        Poly(dc, WpfBrushes.White, null, s, true, 0.62, 0.18, 0.84, 0.22, 0.70, 0.38);
    }

    // ---------- Glyph: Panel 2 ----------
    /// <summary>Hai khung lá»“ng nhau + dáº¥u + á»Ÿ tÃ¢m.</summary>
    private static void DrawFrames(DrawingContext dc, double s)
    {
        var pen = Stroke(s);
        Box(dc, null, pen, s, 0.16, 0.16, 0.68, 0.68);
        Box(dc, null, pen, s, 0.30, 0.30, 0.40, 0.40);
        Seg(dc, pen, s, 0.50, 0.40, 0.50, 0.60);
        Seg(dc, pen, s, 0.40, 0.50, 0.60, 0.50);
    }

    /// <summary>Chiáº¿c kÃ©o: 2 vÃ²ng chuÃ´i + 2 lÆ°á»¡i báº¯t chÃ©o.</summary>
    private static void DrawScissors(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.07);
        dc.DrawEllipse(null, pen, new WpfPoint(s * 0.28, s * 0.76), s * 0.10, s * 0.10);
        dc.DrawEllipse(null, pen, new WpfPoint(s * 0.72, s * 0.76), s * 0.10, s * 0.10);
        Seg(dc, pen, s, 0.33, 0.68, 0.80, 0.16);
        Seg(dc, pen, s, 0.67, 0.68, 0.20, 0.16);
    }

    /// <summary>Hai khung chá»¯ nháº­t ná»‘i tiáº¿p.</summary>
    private static void DrawFramesChain(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.08);
        Box(dc, null, pen, s, 0.10, 0.30, 0.30, 0.40);
        Box(dc, null, pen, s, 0.60, 0.30, 0.30, 0.40);
        Seg(dc, pen, s, 0.40, 0.50, 0.60, 0.50);
    }

    /// <summary>MÅ©i tÃªn phÃ³ng to / thu nhá».</summary>
    private static void DrawScaleArrows(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.08);
        Seg(dc, pen, s, 0.28, 0.72, 0.72, 0.28);
        Poly(dc, WpfBrushes.White, null, s, true, 0.84, 0.16, 0.56, 0.20, 0.80, 0.44);
        Poly(dc, WpfBrushes.White, null, s, true, 0.16, 0.84, 0.44, 0.80, 0.20, 0.56);
    }

    /// <summary>á»” khÃ³a.</summary>
    private static void DrawLock(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.09);
        Box(dc, WpfBrushes.White, null, s, 0.24, 0.48, 0.52, 0.38);

        var fig = new PathFigure { StartPoint = new WpfPoint(s * 0.36, s * 0.48), IsClosed = false, IsFilled = false };
        fig.Segments.Add(new LineSegment(new WpfPoint(s * 0.36, s * 0.36), true));
        fig.Segments.Add(new ArcSegment(new WpfPoint(s * 0.64, s * 0.36), new System.Windows.Size(s * 0.14, s * 0.14),
            0, false, SweepDirection.Clockwise, true));
        fig.Segments.Add(new LineSegment(new WpfPoint(s * 0.64, s * 0.48), true));
        var geo = new PathGeometry();
        geo.Figures.Add(fig);
        dc.DrawGeometry(null, pen, geo);
    }

    // ---------- Glyph: Panel 3 ----------
    /// <summary>Hai Ä‘Æ°á»ng Ä‘á»“ng má»©c lÆ°á»£n sÃ³ng + khung nhÃ£n chá»¯.</summary>
    private static void DrawContourLabel(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.055);
        foreach (var y in new[] { 0.26, 0.74 })
        {
            var fig = new PathFigure { StartPoint = new WpfPoint(s * 0.10, s * y), IsClosed = false, IsFilled = false };
            fig.Segments.Add(new QuadraticBezierSegment(new WpfPoint(s * 0.30, s * (y - 0.20)), new WpfPoint(s * 0.50, s * y), true));
            fig.Segments.Add(new QuadraticBezierSegment(new WpfPoint(s * 0.70, s * (y + 0.20)), new WpfPoint(s * 0.90, s * y), true));
            var geo = new PathGeometry();
            geo.Figures.Add(fig);
            dc.DrawGeometry(null, pen, geo);
        }

        Box(dc, null, pen, s, 0.30, 0.40, 0.40, 0.20);
        Seg(dc, pen, s, 0.38, 0.50, 0.62, 0.50);
    }

    /// <summary>CÃ¡i phá»…u lá»c.</summary>
    private static void DrawFunnel(DrawingContext dc, double s) =>
        Poly(dc, WpfBrushes.White, null, s, true, 0.14, 0.20, 0.86, 0.20, 0.58, 0.52, 0.58, 0.82, 0.42, 0.72, 0.42, 0.52);

    /// <summary>Tam giÃ¡c lÆ°á»›i TIN.</summary>
    private static void DrawTin(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.07);
        Poly(dc, null, pen, s, true, 0.50, 0.14, 0.88, 0.82, 0.12, 0.82);
        Seg(dc, pen, s, 0.50, 0.56, 0.50, 0.14);
        Seg(dc, pen, s, 0.50, 0.56, 0.88, 0.82);
        Seg(dc, pen, s, 0.50, 0.56, 0.12, 0.82);
    }

    /// <summary>Chá»¯ T.</summary>
    private static void DrawLetterT(DrawingContext dc, double s)
    {
        Box(dc, WpfBrushes.White, null, s, 0.20, 0.20, 0.60, 0.15);
        Box(dc, WpfBrushes.White, null, s, 0.43, 0.20, 0.14, 0.62);
    }

    /// <summary>Technical drawing frame: border, title-block table at bottom right, a small logo circle.</summary>
    private static void DrawTechFrame(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.07);
        Box(dc, null, pen, s, 0.10, 0.14, 0.80, 0.72);
        Box(dc, null, pen, s, 0.50, 0.52, 0.40, 0.34);
        Seg(dc, pen, s, 0.50, 0.69, 0.90, 0.69);
        Seg(dc, pen, s, 0.70, 0.52, 0.70, 0.69);
        var r = Math.Max(1.4, s * 0.06);
        dc.DrawEllipse(null, Stroke(s, 0.05), new WpfPoint(s * 0.70, s * 0.78), r, r);
        Seg(dc, Stroke(s, 0.05), s, 0.20, 0.30, 0.42, 0.30);
        Seg(dc, Stroke(s, 0.05), s, 0.20, 0.42, 0.38, 0.42);
    }

    /// <summary>Standard map frame: sheet with border, title-block strip on the right, grid crosses.</summary>
    private static void DrawStdFrame(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.07);
        Box(dc, null, pen, s, 0.10, 0.14, 0.80, 0.72);
        Seg(dc, pen, s, 0.68, 0.14, 0.68, 0.86);
        Seg(dc, pen, s, 0.68, 0.40, 0.90, 0.40);
        Seg(dc, pen, s, 0.68, 0.62, 0.90, 0.62);
        var thin = Stroke(s, 0.05);
        foreach (var cx in new[] { 0.26, 0.48 })
        {
            foreach (var cy in new[] { 0.34, 0.60 })
            {
                Seg(dc, thin, s, cx - 0.05, cy, cx + 0.05, cy);
                Seg(dc, thin, s, cx, cy - 0.05, cx, cy + 0.05);
            }
        }
    }

    // ---------- Glyph: Panel 4 ----------
    /// <summary>Viá»n vuÃ´ng nÃ©t Ä‘á»©t (biÃªn láº¥y Ä‘áº¥t) + vuÃ´ng gáº¡ch chÃ©o (mÃ³ng cá»™t).</summary>
    private static void DrawFoundation(DrawingContext dc, double s)
    {
        var dashed = Stroke(s, 0.05);
        dashed.DashStyle = new System.Windows.Media.DashStyle(new double[] { 2.5, 2 }, 0);
        dashed.DashCap = PenLineCap.Flat;
        Box(dc, null, dashed, s, 0.12, 0.12, 0.76, 0.76);

        var pen = Stroke(s, 0.06);
        Box(dc, null, pen, s, 0.34, 0.34, 0.32, 0.32);
        Seg(dc, pen, s, 0.34, 0.50, 0.50, 0.34);
        Seg(dc, pen, s, 0.34, 0.66, 0.66, 0.34);
        Seg(dc, pen, s, 0.50, 0.66, 0.66, 0.50);
    }

    /// <summary>Con Ä‘Æ°á»ng thu háº¹p (phá»‘i cáº£nh) + váº¡ch giá»¯a Ä‘á»©t.</summary>
    private static void DrawRoad(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.08);
        Seg(dc, pen, s, 0.16, 0.86, 0.40, 0.14);
        Seg(dc, pen, s, 0.84, 0.86, 0.60, 0.14);
        Seg(dc, pen, s, 0.50, 0.84, 0.50, 0.66);
        Seg(dc, pen, s, 0.50, 0.54, 0.50, 0.38);
    }

    /// <summary>Tia sÃ©t Ä‘iá»‡n.</summary>
    private static void DrawLightning(DrawingContext dc, double s) =>
        Poly(dc, WpfBrushes.White, null, s, true, 0.60, 0.08, 0.26, 0.52, 0.46, 0.52, 0.38, 0.92, 0.76, 0.42, 0.54, 0.42, 0.68, 0.08);

    /// <summary>Ziczac bên trái, mũi tên, đoạn thẳng bên phải.</summary>
    private static void DrawStripMap(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.075);
        Poly(dc, null, pen, s, false, 0.08, 0.74, 0.24, 0.30, 0.40, 0.70);
        Seg(dc, pen, s, 0.46, 0.50, 0.60, 0.50);
        Seg(dc, pen, s, 0.54, 0.40, 0.64, 0.50);
        Seg(dc, pen, s, 0.54, 0.60, 0.64, 0.50);
        Seg(dc, pen, s, 0.68, 0.50, 0.92, 0.50);
    }

    /// <summary>Đường gấp khúc + chấm đỉnh góc (đánh số góc tuyến).</summary>
    private static void DrawVertexNumbers(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.08);
        Poly(dc, null, pen, s, false, 0.12, 0.72, 0.38, 0.28, 0.62, 0.62, 0.88, 0.22);
        var r = Math.Max(1.4, s * 0.07);
        dc.DrawEllipse(WpfBrushes.White, null, new WpfPoint(s * 0.12, s * 0.72), r, r);
        dc.DrawEllipse(WpfBrushes.White, null, new WpfPoint(s * 0.38, s * 0.28), r, r);
        dc.DrawEllipse(WpfBrushes.White, null, new WpfPoint(s * 0.62, s * 0.62), r, r);
        dc.DrawEllipse(WpfBrushes.White, null, new WpfPoint(s * 0.88, s * 0.22), r, r);
    }

    /// <summary>Tháº» Tag.</summary>
    private static void DrawTag(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.08);
        Poly(dc, null, pen, s, true, 0.12, 0.50, 0.34, 0.24, 0.86, 0.24, 0.86, 0.76, 0.34, 0.76);
        dc.DrawEllipse(WpfBrushes.White, null, new WpfPoint(s * 0.40, s * 0.50), Math.Max(1.2, s * 0.06), Math.Max(1.2, s * 0.06));
    }

    // ---------- Glyph: Panel 5 ----------
    /// <summary>Báº£ng biá»ƒu (grid table).</summary>
    private static void DrawTable(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.05);
        Box(dc, null, pen, s, 0.14, 0.18, 0.72, 0.64);
        Seg(dc, pen, s, 0.14, 0.34, 0.86, 0.34);
        Seg(dc, pen, s, 0.14, 0.50, 0.86, 0.50);
        Seg(dc, pen, s, 0.14, 0.66, 0.86, 0.66);
        Seg(dc, pen, s, 0.40, 0.18, 0.40, 0.82);
        Seg(dc, pen, s, 0.62, 0.18, 0.62, 0.82);
    }

    /// <summary>Dáº¥u tÃ­ch V.</summary>
    private static void DrawCheck(DrawingContext dc, double s) =>
        Poly(dc, null, Stroke(s, 0.14), s, false, 0.18, 0.52, 0.42, 0.76, 0.84, 0.26);

    /// <summary>KÃ­nh lÃºp.</summary>
    private static void DrawMagnifier(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.10);
        dc.DrawEllipse(null, pen, new WpfPoint(s * 0.42, s * 0.42), s * 0.24, s * 0.24);
        Seg(dc, pen, s, 0.60, 0.60, 0.84, 0.84);
    }

    /// <summary>Trang tÃ i liá»‡u (gÃ³c gáº­p).</summary>
    private static void DrawDocument(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.07);
        Poly(dc, null, pen, s, true, 0.24, 0.12, 0.62, 0.12, 0.80, 0.30, 0.80, 0.88, 0.24, 0.88);
        Poly(dc, null, pen, s, false, 0.62, 0.12, 0.62, 0.30, 0.80, 0.30);
        Seg(dc, pen, s, 0.34, 0.52, 0.70, 0.52);
        Seg(dc, pen, s, 0.34, 0.68, 0.70, 0.68);
    }

    /// <summary>Tia AI (hình sao 4 cánh).</summary>
    private static void DrawAiSpark(DrawingContext dc, double s)
    {
        Poly(dc, WpfBrushes.White, null, s, true,
            0.50, 0.10, 0.58, 0.38, 0.88, 0.42, 0.62, 0.58, 0.70, 0.88, 0.50, 0.68, 0.30, 0.88, 0.38, 0.58, 0.12, 0.42, 0.42, 0.38);
        var r = Math.Max(1.2, s * 0.05);
        dc.DrawEllipse(WpfBrushes.White, null, new WpfPoint(s * 0.78, s * 0.22), r, r);
    }

    /// <summary>Bánh răng cấu hình AI.</summary>
    private static void DrawAiGear(DrawingContext dc, double s)
    {
        var pen = Stroke(s, 0.08);
        dc.DrawEllipse(null, pen, new WpfPoint(s * 0.50, s * 0.50), s * 0.22, s * 0.22);
        dc.DrawEllipse(WpfBrushes.White, null, new WpfPoint(s * 0.50, s * 0.50), s * 0.08, s * 0.08);
        for (var i = 0; i < 6; i++)
        {
            var a = i * Math.PI / 3.0;
            var x0 = 0.50 + 0.28 * Math.Cos(a);
            var y0 = 0.50 + 0.28 * Math.Sin(a);
            var x1 = 0.50 + 0.40 * Math.Cos(a);
            var y1 = 0.50 + 0.40 * Math.Sin(a);
            Seg(dc, pen, s, x0, y0, x1, y1);
        }
    }
}
