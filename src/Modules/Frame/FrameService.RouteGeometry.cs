using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace iSurvey.Modules.Frame;

/// <summary>Hình học tuyến: phẳng hóa, chia mảnh theo khung (DANTRANG).</summary>
public sealed partial class FrameService
{
    /// <summary>Tuyến chỉ được chiếm tối đa phần này chiều cao vùng bản đồ (còn chỗ hiển thị hai bên tuyến).</summary>
    private const double MaxLateralFraction = 0.6;

    /// <summary>Chừa lề hai đầu mảnh (phần chiều dài vùng bản đồ).</summary>
    private const double EndMarginFraction = 0.04;

    private sealed record Piece(int Index, double S0, double S1, Point2d Center, double Phi, double Extent);

    // -------------------------------------------------------------------------------------------------
    //  Hình học tuyến
    // -------------------------------------------------------------------------------------------------
    /// <summary>Tuyến phẳng hóa (XY) với khoảng cách tích lũy; hỗ trợ nội suy điểm theo chiều dài.</summary>
    private sealed class RouteGeom
    {
        private readonly Point2d[] _p;
        private readonly double[] _c;

        public RouteGeom(IReadOnlyList<Point2d> pts)
        {
            var list = new List<Point2d>();
            foreach (var p in pts)
            {
                if (list.Count == 0 || list[^1].GetDistanceTo(p) > 1e-9)
                    list.Add(p);
            }

            _p = list.ToArray();
            _c = new double[_p.Length];
            for (var i = 1; i < _p.Length; i++)
                _c[i] = _c[i - 1] + _p[i - 1].GetDistanceTo(_p[i]);
            Length = _p.Length > 0 ? _c[^1] : 0;
        }

        public double Length { get; }

        public Point2d At(double d)
        {
            if (d <= 0)
                return _p[0];
            if (d >= Length)
                return _p[^1];
            var i = Array.BinarySearch(_c, d);
            if (i >= 0)
                return _p[i];
            i = ~i; // phần tử đầu tiên > d
            var t = (d - _c[i - 1]) / (_c[i] - _c[i - 1]);
            return new Point2d(_p[i - 1].X + (_p[i].X - _p[i - 1].X) * t, _p[i - 1].Y + (_p[i].Y - _p[i - 1].Y) * t);
        }

        /// <summary>Hướng tiếp tuyến tại khoảng cách d (radian).</summary>
        public double DirAt(double d)
        {
            var a = At(Math.Max(0, d - 1e-6 * Math.Max(1, Length)));
            var b = At(Math.Min(Length, d + 1e-6 * Math.Max(1, Length)));
            if (a.GetDistanceTo(b) < 1e-12)
                a = _p[0];
            return Math.Atan2(b.Y - a.Y, b.X - a.X);
        }

        /// <summary>Các điểm của tuyến trong [s, e], có cả hai đầu mút nội suy.</summary>
        public List<Point2d> Sub(double s, double e)
        {
            var res = new List<Point2d> { At(s) };
            for (var i = 0; i < _p.Length; i++)
            {
                if (_c[i] > s + 1e-12 && _c[i] < e - 1e-12)
                    res.Add(_p[i]);
            }

            res.Add(At(e));
            return res;
        }
    }

    /// <summary>Phẳng hóa tuyến thành chuỗi điểm XY. Null nếu đối tượng không phải đường.</summary>
    private static List<Point2d>? FlattenRoute(Entity ent, Transaction tr, out bool closed)
    {
        closed = false;

        // Civil 3D Alignment: dùng đường nền (BaseCurve) nếu có
        if (ent.GetType().Name == "Alignment")
        {
            try
            {
                var prop = ent.GetType().GetProperty("BaseCurve");
                if (prop?.GetValue(ent) is ObjectId bid && !bid.IsNull
                    && tr.GetObject(bid, OpenMode.ForRead) is Entity baseEnt && baseEnt is not null)
                {
                    var r = FlattenRoute(baseEnt, tr, out closed);
                    if (r is not null && r.Count >= 2)
                        return r;
                }
            }
            catch
            {
                // thử đọc trực tiếp như Curve
            }
        }

        var pts = new List<Point2d>();
        switch (ent)
        {
            case Autodesk.AutoCAD.DatabaseServices.Polyline pl:
            {
                closed = pl.Closed;
                var n = pl.NumberOfVertices;
                if (n < 2)
                    return null;
                var segs = closed ? n : n - 1;
                pts.Add(pl.GetPoint2dAt(0));
                for (var i = 0; i < segs; i++)
                {
                    var j = (i + 1) % n;
                    var bulge = pl.GetBulgeAt(i);
                    if (Math.Abs(bulge) < 1e-9)
                    {
                        pts.Add(pl.GetPoint2dAt(j));
                        continue;
                    }

                    var d0 = pl.GetDistanceAtParameter(i);
                    var d1 = i + 1 < n ? pl.GetDistanceAtParameter(i + 1) : pl.Length;
                    var ang = 4 * Math.Atan(Math.Abs(bulge));
                    var k = Math.Max(2, (int)Math.Ceiling(ang / (Math.PI / 90.0)));
                    for (var m = 1; m <= k; m++)
                    {
                        var q = pl.GetPointAtDist(d0 + (d1 - d0) * m / k);
                        pts.Add(new Point2d(q.X, q.Y));
                    }
                }

                return pts;
            }
            case Polyline2d p2 when p2.PolyType == Poly2dType.SimplePoly:
                closed = p2.Closed;
                foreach (ObjectId id in p2)
                {
                    if (tr.GetObject(id, OpenMode.ForRead) is Vertex2d vx && Math.Abs(vx.Bulge) < 1e-9)
                        pts.Add(new Point2d(vx.Position.X, vx.Position.Y));
                    else if (tr.GetObject(id, OpenMode.ForRead) is Vertex2d)
                        return SampleCurve(p2);
                }

                if (closed && pts.Count > 0)
                    pts.Add(pts[0]);
                return pts.Count >= 2 ? pts : null;
            case Polyline3d p3 when p3.PolyType == Poly3dType.SimplePoly:
                closed = p3.Closed;
                foreach (ObjectId id in p3)
                {
                    if (tr.GetObject(id, OpenMode.ForRead) is PolylineVertex3d vx)
                        pts.Add(new Point2d(vx.Position.X, vx.Position.Y));
                }

                if (closed && pts.Count > 0)
                    pts.Add(pts[0]);
                return pts.Count >= 2 ? pts : null;
            case Line ln:
                return [new Point2d(ln.StartPoint.X, ln.StartPoint.Y), new Point2d(ln.EndPoint.X, ln.EndPoint.Y)];
            case Curve c:
                closed = c.Closed;
                return SampleCurve(c);
            default:
                return null;
        }
    }

    private static List<Point2d>? SampleCurve(Curve c)
    {
        try
        {
            var len = c.GetDistAtPoint(c.EndPoint);
            if (len < 1e-9)
                return null;
            const int n = 1000;
            var res = new List<Point2d>(n + 1);
            for (var i = 0; i <= n; i++)
            {
                var q = c.GetPointAtDist(len * i / n);
                res.Add(new Point2d(q.X, q.Y));
            }

            return res;
        }
        catch
        {
            return null;
        }
    }

    private static bool Fits(RouteGeom g, double s, double e, double wm, double hm, out double phi, out Point2d center)
    {
        var sub = g.Sub(s, e);
        var a = sub[0];
        var b = sub[^1];
        phi = a.GetDistanceTo(b) > 1e-9 ? Math.Atan2(b.Y - a.Y, b.X - a.X) : g.DirAt(s);
        var ux = Math.Cos(phi);
        var uy = Math.Sin(phi);
        double minU = 0, maxU = 0, minV = 0, maxV = 0;
        foreach (var p in sub)
        {
            var dx = p.X - a.X;
            var dy = p.Y - a.Y;
            var u = dx * ux + dy * uy;
            var v = -dx * uy + dy * ux;
            minU = Math.Min(minU, u);
            maxU = Math.Max(maxU, u);
            minV = Math.Min(minV, v);
            maxV = Math.Max(maxV, v);
        }

        var cu = (minU + maxU) / 2;
        var cv = (minV + maxV) / 2;
        center = new Point2d(a.X + cu * ux - cv * uy, a.Y + cu * uy + cv * ux);
        return (maxU - minU) <= wm * (1 - 2 * EndMarginFraction) && (maxV - minV) <= hm * MaxLateralFraction;
    }

    private static List<Piece> Segment(RouteGeom g, double wm, double hm, double overlap)
    {
        var pieces = new List<Piece>();
        var total = g.Length;
        var delta = wm / 200.0;
        var s = 0.0;
        for (var guard = 0; guard < 5000; guard++)
        {
            double e;
            if (Fits(g, s, total, wm, hm, out _, out _))
            {
                e = total;
            }
            else
            {
                var lo = s;
                var hi = total;
                for (var d = s + delta; d < total; d += delta)
                {
                    if (Fits(g, s, d, wm, hm, out _, out _))
                    {
                        lo = d;
                    }
                    else
                    {
                        hi = d;
                        break;
                    }
                }

                for (var it = 0; it < 14; it++)
                {
                    var mid = (lo + hi) / 2;
                    if (Fits(g, s, mid, wm, hm, out _, out _))
                        lo = mid;
                    else
                        hi = mid;
                }

                e = lo > s + 1e-9 ? lo : Math.Min(s + delta, total);
            }

            Fits(g, s, e, wm, hm, out var phi, out var center);
            var sub = g.Sub(s, e);
            var ux = Math.Cos(phi);
            var uy = Math.Sin(phi);
            double minU = double.MaxValue, maxU = double.MinValue;
            foreach (var p in sub)
            {
                var u = (p.X - sub[0].X) * ux + (p.Y - sub[0].Y) * uy;
                minU = Math.Min(minU, u);
                maxU = Math.Max(maxU, u);
            }

            pieces.Add(new Piece(pieces.Count + 1, s, e, center, phi, maxU - minU));
            if (e >= total - 1e-9)
                break;
            s = Math.Max(e - overlap, s + 0.5 * (e - s));
        }

        return pieces;
    }

}

