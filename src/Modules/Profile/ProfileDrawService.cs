using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using iSurvey.Modules.Tuyen;

namespace iSurvey.Modules.Profile;

/// <summary>
/// Vẽ đường đất + đầu bảng trên Model. X = mét lý trình; Y = (Z − Z0) × (H/V) phía trên bảng.
/// Gốc chèn = góc dưới-trái cột tên = góc RAIMCD chọn trên hàng cộng dồn.
/// </summary>
internal static class ProfileDrawService
{
    public const double DefaultLabelWidth = 250;
    public const double DefaultRowHeight = 5;
    public const double DefaultTextHeight = 2;
    public const double SegmentGap = 40;

    /// <summary>Chiều cao ô bản đồ A3 (mm) — dùng để gợi ý mức so sánh.</summary>
    public const double PaperMapHeightA3Mm = 277;

    public static double Exaggerate(int horizontalScale, int verticalScale) =>
        horizontalScale / (double)verticalScale;

    /// <summary>Chiều cao vùng vẽ A3 (đơn vị bản vẽ) ở tỷ lệ ngang đã chọn.</summary>
    public static double ViewHeightA3(int horizontalScale) =>
        PaperMapHeightA3Mm * horizontalScale / 1000.0;

    public static double SuggestDatum(IReadOnlyList<StationRecord> stations, int hScale, int vScale, double rowHeight)
    {
        var withZ = StationsWithZ(stations);
        if (withZ.Count == 0)
            return 0;

        var zMin = withZ.Min(s => s.Z);
        var zMax = withZ.Max(s => s.Z);
        var bandH = ProfileBandRow.DefaultRows.Count * rowHeight;
        var room = Math.Max(40, ViewHeightA3(hScale) - bandH);
        var maxSpan = room / Exaggerate(hScale, vScale);
        var z0 = FloorNice(zMin);
        if (zMax - z0 <= maxSpan)
            return z0;

        // Đặt mức để đỉnh vừa vào một tờ A3.
        return FloorNice(zMax - maxSpan);
    }

    public static List<(double From, double To, double Datum)> PlanSegments(
        IReadOnlyList<StationRecord> stations,
        double datum,
        int hScale,
        int vScale,
        double rowHeight)
    {
        var withZ = StationsWithZ(stations);
        var result = new List<(double From, double To, double Datum)>();
        if (withZ.Count == 0)
            return result;

        var bandH = ProfileBandRow.DefaultRows.Count * rowHeight;
        var room = Math.Max(40, ViewHeightA3(hScale) - bandH);
        var maxSpan = room / Exaggerate(hScale, vScale);

        var start = 0;
        while (start < withZ.Count)
        {
            var z0 = start == 0 ? datum : FloorNice(withZ[start].Z);
            // Một điểm vẫn cao hơn tờ A3: hạ mức để đỉnh vừa vào.
            if (withZ[start].Z - z0 > maxSpan)
                z0 = FloorNice(withZ[start].Z - maxSpan);

            var end = start;
            var zHi = withZ[start].Z;
            while (end + 1 < withZ.Count)
            {
                var next = withZ[end + 1];
                var hi = Math.Max(zHi, next.Z);
                if (hi - z0 > maxSpan)
                    break;
                end++;
                zHi = hi;
            }

            result.Add((withZ[start].Chainage, withZ[end].Chainage, z0));
            start = end + 1;
        }

        return result;
    }

    public static string FormatSuggestion(IReadOnlyList<(double From, double To, double Datum)> segs)
    {
        if (segs.Count == 0)
            return "";
        if (segs.Count == 1)
            return "Gợi ý mức so sánh " + segs[0].Datum.ToString("0.###", CultureInfo.InvariantCulture) + " m để vừa một tờ A3.";

        var parts = new List<string>();
        foreach (var s in segs)
        {
            parts.Add(
                FormatChainage(s.From) + "→" + FormatChainage(s.To)
                + " mức " + s.Datum.ToString("0.###", CultureInfo.InvariantCulture));
        }

        return "Đoạn cao hơn tờ A3. Gợi ý tách: " + string.Join("; ", parts) + ".";
    }

    public static ProfileRecord Draw(
        Document doc,
        RouteRecord route,
        string name,
        int hScale,
        int vScale,
        double datum,
        Point3d insert,
        double labelWidth,
        double rowHeight,
        double textHeight)
    {
        var stations = StationsWithZ(route.Stations);
        if (stations.Count < 2)
            throw new InvalidOperationException("Cần ít nhất hai cọc có cao độ Z.");

        var segs = PlanSegments(stations, datum, hScale, vScale, rowHeight);
        if (segs.Count == 0)
            throw new InvalidOperationException("Không lập được đoạn mặt cắt.");

        var handles = new List<string>();
        var origin = insert;
        using (var tr = doc.TransactionManager.StartTransaction())
        {
            var db = doc.Database;
            EnsureLayer(tr, db, iSurveyLayers.TdBang, 7);
            EnsureLayer(tr, db, iSurveyLayers.TdChu, 7);
            EnsureLayer(tr, db, iSurveyLayers.TdDat, 3);
            EnsureLayer(tr, db, iSurveyLayers.TdLuoi, 8);

            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var space = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
            var styleId = db.Textstyle;

            for (var i = 0; i < segs.Count; i++)
            {
                var seg = segs[i];
                var slice = stations.Where(s => s.Chainage >= seg.From - 1e-6 && s.Chainage <= seg.To + 1e-6).ToList();
                if (slice.Count < 2)
                    continue;
                DrawOneStrip(
                    tr, space, styleId,
                    slice, route.StartChainage,
                    hScale, vScale, seg.Datum,
                    origin, labelWidth, rowHeight, textHeight,
                    handles);

                var bandH = ProfileBandRow.DefaultRows.Count * rowHeight;
                var phong = Exaggerate(hScale, vScale);
                var zTop = slice.Max(s => s.Z);
                var groundTop = bandH + Math.Max(0, (zTop - seg.Datum) * phong);
                origin = new Point3d(insert.X, origin.Y + groundTop + SegmentGap, 0);
            }

            tr.Commit();
        }

        return new ProfileRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name,
            RouteId = route.Id,
            VerticalScale = vScale,
            HorizontalScale = hScale,
            Datum = segs[0].Datum,
            InsertX = insert.X,
            InsertY = insert.Y,
            LabelWidth = labelWidth,
            RowHeight = rowHeight,
            TextHeight = textHeight,
            EntityHandles = handles
        };
    }

    private static void DrawOneStrip(
        Transaction tr,
        BlockTableRecord space,
        ObjectId styleId,
        List<StationRecord> stations,
        double routeStart,
        int hScale,
        int vScale,
        double datum,
        Point3d origin,
        double labelWidth,
        double rowHeight,
        double textHeight,
        List<string> handles)
    {
        var rows = ProfileBandRow.DefaultRows;
        var bandH = rows.Count * rowHeight;
        var phong = Exaggerate(hScale, vScale);
        var l0 = stations[0].Chainage;
        var l1 = stations[stations.Count - 1].Chainage;
        var length = Math.Max(1, l1 - l0);
        var xSplit = origin.X + labelWidth;
        var xRight = xSplit + length;
        var yCut = origin.Y;
        var yBandTop = yCut + bandH;

        // Khung đầu bảng + cột tên.
        AddLine(tr, space, iSurveyLayers.TdBang, origin.X, yCut, xRight, yCut, handles);
        AddLine(tr, space, iSurveyLayers.TdBang, origin.X, yBandTop, xRight, yBandTop, handles);
        AddLine(tr, space, iSurveyLayers.TdBang, origin.X, yCut, origin.X, yBandTop, handles);
        AddLine(tr, space, iSurveyLayers.TdBang, xSplit, yCut, xSplit, yBandTop, handles);
        AddLine(tr, space, iSurveyLayers.TdBang, xRight, yCut, xRight, yBandTop, handles);

        for (var i = 1; i < rows.Count; i++)
        {
            var y = yCut + i * rowHeight;
            AddLine(tr, space, iSurveyLayers.TdBang, origin.X, y, xRight, y, handles);
        }

        for (var i = 0; i < rows.Count; i++)
        {
            var yMid = yCut + i * rowHeight + rowHeight * 0.5;
            AddText(tr, space, styleId, iSurveyLayers.TdChu,
                new Point3d(origin.X + 2, yMid - textHeight * 0.35, 0),
                rows[i].Title, textHeight, handles);
        }

        // Vạch đứng tại cọc + số liệu.
        for (var i = 0; i < stations.Count; i++)
        {
            var st = stations[i];
            var x = xSplit + (st.Chainage - l0);
            AddLine(tr, space, iSurveyLayers.TdBang, x, yCut, x, yBandTop, handles);

            var prev = i == 0 ? routeStart : stations[i - 1].Chainage;
            var le = st.Chainage - prev;
            var cong = st.Chainage - routeStart;
            WriteCell(tr, space, styleId, x, yCut + 0 * rowHeight, rowHeight, textHeight,
                cong.ToString("0.##", CultureInfo.InvariantCulture), handles);
            WriteCell(tr, space, styleId, x, yCut + 1 * rowHeight, rowHeight, textHeight,
                le.ToString("0.##", CultureInfo.InvariantCulture), handles);
            WriteCell(tr, space, styleId, x, yCut + 2 * rowHeight, rowHeight, textHeight,
                st.Name, handles);
            WriteCell(tr, space, styleId, x, yCut + 3 * rowHeight, rowHeight, textHeight,
                FormatChainage(st.Chainage), handles);
            WriteCell(tr, space, styleId, x, yCut + 4 * rowHeight, rowHeight, textHeight,
                st.Z.ToString("0.##", CultureInfo.InvariantCulture), handles);
        }

        // Đường đất + vạch đứng lên đỉnh.
        var ground = new Point2dCollection();
        double yGroundMax = yBandTop;
        foreach (var st in stations)
        {
            var x = xSplit + (st.Chainage - l0);
            var y = yBandTop + (st.Z - datum) * phong;
            if (y > yGroundMax)
                yGroundMax = y;
            ground.Add(new Point2d(x, y));
            AddLine(tr, space, iSurveyLayers.TdLuoi, x, yBandTop, x, y, handles);
            AddText(tr, space, styleId, iSurveyLayers.TdChu,
                new Point3d(x + textHeight * 0.2, y + textHeight * 0.2, 0),
                st.Z.ToString("0.##", CultureInfo.InvariantCulture), textHeight * 0.85, handles);
        }

        if (ground.Count >= 2)
        {
            var pl = new Polyline();
            for (var i = 0; i < ground.Count; i++)
                pl.AddVertexAt(i, ground[i], 0, 0, 0);
            pl.Layer = iSurveyLayers.TdDat;
            pl.ColorIndex = 3;
            space.AppendEntity(pl);
            tr.AddNewlyCreatedDBObject(pl, true);
            handles.Add(pl.Handle.ToString());
        }

        // Thước cao độ bên trái vùng đất.
        var stepZ = NiceStep((yGroundMax - yBandTop) / phong);
        if (stepZ > 0)
        {
            var zStart = Math.Ceiling(datum / stepZ) * stepZ;
            for (var z = zStart; z <= datum + (yGroundMax - yBandTop) / phong + 1e-6; z += stepZ)
            {
                var y = yBandTop + (z - datum) * phong;
                AddLine(tr, space, iSurveyLayers.TdLuoi, origin.X, y, xSplit, y, handles);
                AddText(tr, space, styleId, iSurveyLayers.TdChu,
                    new Point3d(origin.X + 2, y - textHeight * 0.35, 0),
                    z.ToString("0.##", CultureInfo.InvariantCulture), textHeight, handles);
            }

            AddLine(tr, space, iSurveyLayers.TdBang, origin.X, yBandTop, origin.X, yGroundMax, handles);
            AddLine(tr, space, iSurveyLayers.TdBang, xSplit, yBandTop, xSplit, yGroundMax, handles);
        }
    }

    private static void WriteCell(
        Transaction tr, BlockTableRecord space, ObjectId styleId,
        double x, double yBottom, double rowHeight, double textHeight, string text, List<string> handles)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;
        AddText(tr, space, styleId, iSurveyLayers.TdChu,
            new Point3d(x + textHeight * 0.15, yBottom + rowHeight * 0.5 - textHeight * 0.35, 0),
            text, textHeight * 0.9, handles);
    }

    private static void AddLine(
        Transaction tr, BlockTableRecord space, string layer,
        double x0, double y0, double x1, double y1, List<string> handles)
    {
        var line = new Line(new Point3d(x0, y0, 0), new Point3d(x1, y1, 0))
        {
            Layer = layer
        };
        space.AppendEntity(line);
        tr.AddNewlyCreatedDBObject(line, true);
        handles.Add(line.Handle.ToString());
    }

    private static void AddText(
        Transaction tr, BlockTableRecord space, ObjectId styleId, string layer,
        Point3d position, string content, double height, List<string> handles)
    {
        var text = new DBText
        {
            Position = position,
            Height = Math.Max(0.5, height),
            TextString = content,
            Layer = layer,
            TextStyleId = styleId
        };
        space.AppendEntity(text);
        tr.AddNewlyCreatedDBObject(text, true);
        handles.Add(text.Handle.ToString());
    }

    private static List<StationRecord> StationsWithZ(IReadOnlyList<StationRecord>? stations)
    {
        var list = new List<StationRecord>();
        if (stations is null)
            return list;
        foreach (var s in stations)
        {
            if (s.HasZ)
                list.Add(s);
        }

        list.Sort((a, b) => a.Chainage.CompareTo(b.Chainage));
        return list;
    }

    public static string FormatChainage(double chainage)
    {
        var km = Math.Floor(chainage / 1000.0);
        var m = chainage - km * 1000.0;
        return km.ToString("0", CultureInfo.InvariantCulture) + "+"
            + m.ToString("000.00", CultureInfo.InvariantCulture);
    }

    private static double FloorNice(double z)
    {
        if (double.IsNaN(z) || double.IsInfinity(z))
            return 0;
        var step = z >= 100 ? 10 : 5;
        return Math.Floor(z / step) * step;
    }

    private static double NiceStep(double span)
    {
        if (span <= 0)
            return 5;
        var raw = span / 8.0;
        var pow = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        var n = raw / pow;
        var nice = n < 1.5 ? 1 : n < 3.5 ? 2 : n < 7.5 ? 5 : 10;
        return nice * pow;
    }

    private static void EnsureLayer(Transaction tr, Database db, string name, short color)
    {
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (lt.Has(name))
            return;
        lt.UpgradeOpen();
        var layer = new LayerTableRecord
        {
            Name = name,
            Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, color)
        };
        lt.Add(layer);
        tr.AddNewlyCreatedDBObject(layer, true);
    }
}
