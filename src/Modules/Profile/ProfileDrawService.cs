using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using iSurvey.Modules.Terrain;
using iSurvey.Modules.Tuyen;
using AcadColor = Autodesk.AutoCAD.Colors.Color;
using ColorMethod = Autodesk.AutoCAD.Colors.ColorMethod;

namespace iSurvey.Modules.Profile;

/// <summary>
/// Vẽ đường đất + đầu bảng trên Model. X = mét lý trình; Y = (Z − Z0) × (H/V) phía trên bảng.
/// Gốc chèn = góc dưới-trái cột tên (đáy bảng).
/// </summary>
internal static class ProfileDrawService
{
    public const double SegmentGap = 40;

    public const double PaperMapHeightA3Mm = 277;

    /// <summary>Cách mép trái cột tên khi ghi nhãn hàng (mặc định).</summary>
    public const double DefaultLabelInset = 40;

    public static double Exaggerate(int horizontalScale, int verticalScale) =>
        horizontalScale / (double)verticalScale;

    public static double ViewHeightA3(int horizontalScale) =>
        PaperMapHeightA3Mm * horizontalScale / 1000.0;

    /// <summary>Gợi ý mức so sánh gần Z min (làm tròn đẹp). Không giới hạn khổ giấy.</summary>
    public static double SuggestDatum(
        IReadOnlyList<ProfileSampleService.SamplePoint> samples,
        int hScale,
        int vScale,
        double bandHeight)
    {
        _ = hScale;
        _ = vScale;
        _ = bandHeight;
        if (samples.Count == 0)
            return 0;
        return FloorNice(samples.Min(s => s.Z));
    }

    /// <summary>Một đoạn duy nhất theo mức so sánh người dùng — không cắt theo A3.</summary>
    public static List<(double From, double To, double Datum)> PlanSegments(
        IReadOnlyList<ProfileSampleService.SamplePoint> samples,
        double datum,
        int hScale,
        int vScale,
        double bandHeight)
    {
        _ = hScale;
        _ = vScale;
        _ = bandHeight;
        var result = new List<(double From, double To, double Datum)>();
        if (samples.Count == 0)
            return result;

        result.Add((samples[0].Chainage, samples[samples.Count - 1].Chainage, datum));
        return result;
    }

    public static string FormatSuggestion(IReadOnlyList<(double From, double To, double Datum)> segs)
    {
        if (segs.Count == 0)
            return "";
        return "Mức so sánh "
            + segs[0].Datum.ToString("0.###", CultureInfo.InvariantCulture)
            + " m. Sau khi bấm Vẽ, chỉ điểm chèn góc dưới-trái cột tên trên Model.";
    }

    public static void EraseHandles(Document doc, IReadOnlyList<string> handles)
    {
        if (handles is null || handles.Count == 0)
            return;
        using (doc.LockDocument())
        using (var tr = doc.TransactionManager.StartTransaction())
        {
            foreach (var text in handles)
            {
                if (string.IsNullOrWhiteSpace(text))
                    continue;
                try
                {
                    var handle = new Handle(Convert.ToInt64(text, 16));
                    if (!doc.Database.TryGetObjectId(handle, out var id) || id.IsNull)
                        continue;
                    if (tr.GetObject(id, OpenMode.ForWrite, true) is not Entity ent || ent.IsErased)
                        continue;
                    ent.Erase(true);
                }
                catch
                {
                    // bỏ handle hỏng
                }
            }

            tr.Commit();
        }
    }

    public static ProfileRecord Draw(
        Document doc,
        RouteRecord route,
        ProfileDrawOptions options,
        Point3d insert,
        string? existingId = null,
        IReadOnlyList<string>? eraseHandles = null)
    {
        if (eraseHandles is { Count: > 0 })
            EraseHandles(doc, eraseHandles);

        var terrain = ResolveTerrain(doc, route);
        var samples = ProfileSampleService.Collect(doc, route, options.SampleStepM, terrain);
        if (samples.Count < 2)
            throw new InvalidOperationException("Cần ít nhất hai điểm có cao độ Z trên tuyến.");

        var rows = options.Rows is { Count: > 0 } ? options.Rows : ProfileBandRow.CreateDefaultList();
        var bandH = ProfileBandRow.TotalHeight(rows);
        var segs = PlanSegments(samples, options.Datum, options.HorizontalScale, options.VerticalScale, bandH);
        if (segs.Count == 0)
            throw new InvalidOperationException("Không lập được đoạn mặt cắt.");

        var handles = new List<string>();
        var origin = insert;
        using (doc.LockDocument())
        using (var tr = doc.TransactionManager.StartTransaction())
        {
            var db = doc.Database;
            EnsureLayer(tr, db, iSurveyLayers.TdBang, 7);
            EnsureLayer(tr, db, iSurveyLayers.TdChu, 7);
            EnsureLayer(tr, db, iSurveyLayers.TdDat, 3);
            EnsureLayer(tr, db, iSurveyLayers.TdLuoi, 8);

            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var space = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
            var styleId = ProfileTextStyles.Ensure(tr, db);

            for (var i = 0; i < segs.Count; i++)
            {
                var seg = segs[i];
                var slice = samples.Where(s => s.Chainage >= seg.From - 1e-6 && s.Chainage <= seg.To + 1e-6).ToList();
                if (slice.Count < 2)
                    continue;
                DrawOneStrip(
                    tr, space, styleId,
                    slice, route,
                    options, rows, seg.Datum,
                    origin, handles);

                var phong = Exaggerate(options.HorizontalScale, options.VerticalScale);
                var zTop = slice.Max(s => s.Z);
                // Khoảng đứng = 0: band + đất (không cộng Cách đầu bảng — đó là khoảng ngang).
                var groundTop = bandH + Math.Max(0, (zTop - seg.Datum) * phong);
                origin = new Point3d(insert.X, origin.Y + groundTop + SegmentGap + options.RowGap, 0);
            }

            tr.Commit();
        }

        var id = string.IsNullOrWhiteSpace(existingId) ? Guid.NewGuid().ToString("N") : existingId!;
        var name = string.IsNullOrWhiteSpace(options.ProfileName) ? "MCD " + route.Name : options.ProfileName.Trim();
        return ProfileRecord.FromDraw(id, name, route.Id, options, insert.X, insert.Y, handles);
    }

    private static TerrainRecord? ResolveTerrain(Document doc, RouteRecord route)
    {
        try
        {
            var models = TerrainStore.Load(doc.Database);
            if (models.Count == 0)
                return null;
            if (!string.IsNullOrWhiteSpace(route.TerrainId))
            {
                foreach (var m in models)
                {
                    if (string.Equals(m.Id, route.TerrainId, StringComparison.OrdinalIgnoreCase))
                        return m;
                }
            }

            return models[0];
        }
        catch
        {
            return null;
        }
    }

    private static void DrawOneStrip(
        Transaction tr,
        BlockTableRecord space,
        ObjectId styleId,
        List<ProfileSampleService.SamplePoint> samples,
        RouteRecord route,
        ProfileDrawOptions options,
        IReadOnlyList<ProfileBandRow> rows,
        double datum,
        Point3d origin,
        List<string> handles)
    {
        var bandH = ProfileBandRow.TotalHeight(rows);
        var phong = Exaggerate(options.HorizontalScale, options.VerticalScale);
        var l0 = samples[0].Chainage;
        var l1 = samples[samples.Count - 1].Chainage;
        var length = Math.Max(1, l1 - l0);
        var labelWidth = Math.Max(40, options.LabelWidth);
        var labelInset = options.LabelInset > 0 ? options.LabelInset : DefaultLabelInset;
        // Cách đầu bảng = khoảng ngang thước → đầu đất. Khoảng đứng luôn 0.
        var gap = Math.Max(0, options.HeaderClearance);
        var xSplit = origin.X + labelWidth;
        var xProfile0 = xSplit + gap;
        var xRight = xProfile0 + length;
        var yCut = origin.Y;
        var yBandTop = yCut + bandH;
        var yDatum = yBandTop;
        var textProfile = Math.Max(0.5, options.ProfileTextHeight);

        if (options.DrawBandHeader)
        {
            AddLine(tr, space, iSurveyLayers.TdBang, 7, origin.X, yCut, xRight, yCut, handles);
            AddLine(tr, space, iSurveyLayers.TdBang, 7, origin.X, yBandTop, xRight, yBandTop, handles);
            AddLine(tr, space, iSurveyLayers.TdBang, 7, origin.X, yCut, origin.X, yBandTop, handles);
            AddLine(tr, space, iSurveyLayers.TdBang, 7, xSplit, yCut, xSplit, yBandTop, handles);
            AddLine(tr, space, iSurveyLayers.TdBang, 7, xRight, yCut, xRight, yBandTop, handles);

            // Hàng từ trên xuống: rows[0] sát đường đất.
            var yTop = yBandTop;
            foreach (var row in rows)
            {
                var h = Math.Max(1, row.RowHeight);
                var yBottom = yTop - h;
                AddLine(tr, space, iSurveyLayers.TdBang, 7, origin.X, yBottom, xRight, yBottom, handles);
                var th = Math.Max(0.5, row.TextHeight);
                var rowStyle = ResolveTextStyle(tr, space.Database, row.TextStyle, styleId);
                AddText(tr, space, rowStyle, row.Layer, row.ColorAci,
                    new Point3d(origin.X + labelInset, yBottom + h * 0.5 - th * 0.35, 0),
                    row.Label, th, handles);
                yTop = yBottom;
            }
        }

        // Vạch đứng tại cọc + số liệu hàng.
        var stations = samples.Where(s => s.IsStation).ToList();
        if (stations.Count == 0)
            stations = samples;

        // Ghi nhớ khoảng Y từng hàng để vẽ zigzag hướng tuyến.
        var rowBands = new List<(ProfileBandRow Row, double YBottom, double Height)>();
        {
            var yTop = yBandTop;
            foreach (var row in rows)
            {
                var h = Math.Max(1, row.RowHeight);
                var yBottom = yTop - h;
                rowBands.Add((row, yBottom, h));
                yTop = yBottom;
            }
        }

        // Chỉ hàng khoảng cách lẻ có dóng dọc tại cọc; các hàng khác không kẻ dọc.
        var leBand = rowBands.FirstOrDefault(b => ContentKey(b.Row.Content) == "le");
        var hasLeBand = leBand.Row is not null;

        // Chữ thập đỏ: X = đầu mặt cắt (cách đầu bảng), Y = trung điểm hàng Sơ họa mặt bằng.
        var soHoaBand = rowBands.FirstOrDefault(b => ContentKey(b.Row.Content) == "so_hoa");
        if (options.DrawCrossMark && options.DrawBandHeader && soHoaBand.Row is not null)
        {
            var yCross = soHoaBand.YBottom + soHoaBand.Height * 0.5;
            var arm = Math.Max(textProfile * 0.55, 5.0);
            AddLine(tr, space, iSurveyLayers.TdBang, 1,
                xProfile0 - arm, yCross, xProfile0 + arm, yCross, handles);
            AddLine(tr, space, iSurveyLayers.TdBang, 1,
                xProfile0, yCross - arm, xProfile0, yCross + arm, handles);
        }

        for (var i = 0; i < stations.Count; i++)
        {
            var st = stations[i];
            var x = xProfile0 + (st.Chainage - l0);
            if (options.DrawBandHeader && hasLeBand)
                AddLine(tr, space, iSurveyLayers.TdBang, 7,
                    x, leBand.YBottom, x, leBand.YBottom + leBand.Height, handles);

            var prev = i == 0 ? route.StartChainage : stations[i - 1].Chainage;
            var le = st.Chainage - prev;
            var cong = st.Chainage - route.StartChainage;
            var xPrev = xProfile0 + (prev - l0);

            foreach (var band in rowBands)
            {
                var key = ContentKey(band.Row.Content);
                if (key is "huong" or "bearing" or "kc_100")
                    continue; // zigzag / thước 100 vẽ riêng

                var cell = ResolveCell(key, st, le, cong, 0, options);
                if (string.IsNullOrEmpty(cell))
                    continue;

                var rowStyle = ResolveTextStyle(tr, space.Database, band.Row.TextStyle, styleId);
                var th = Math.Max(0.5, band.Row.TextHeight);
                // Khoảng lẻ: chữ giữa hai vạch đứng.
                var xText = key == "le"
                    ? CellMidX(i, stations, l0, xProfile0, xPrev, x, th)
                    : x;
                if (IsVerticalCell(key))
                {
                    WriteVerticalCell(tr, space, rowStyle, band.Row.Layer, band.Row.ColorAci,
                        xText, band.YBottom, band.Height, th, cell, handles);
                }
                else
                {
                    WriteCell(tr, space, rowStyle, band.Row.Layer, band.Row.ColorAci,
                        xText, band.YBottom, band.Height, th, cell, handles);
                }
            }
        }

        foreach (var band in rowBands)
        {
            var key = ContentKey(band.Row.Content);
            if (key is not ("huong" or "bearing"))
                continue;
            var rowStyle = ResolveTextStyle(tr, space.Database, band.Row.TextStyle, styleId);
            DrawHuongZigzag(
                tr, space, rowStyle, band.Row,
                stations, l0, xProfile0, xRight,
                band.YBottom, band.Height, handles);
        }

        foreach (var band in rowBands)
        {
            if (ContentKey(band.Row.Content) != "neo")
                continue;
            var rowStyle = ResolveTextStyle(tr, space.Database, band.Row.TextStyle, styleId);
            DrawNeoSpans(
                tr, space, rowStyle, band.Row,
                stations, l0, xProfile0,
                band.YBottom, band.Height, handles);
        }

        foreach (var band in rowBands)
        {
            if (ContentKey(band.Row.Content) != "kc_100")
                continue;
            var rowStyle = ResolveTextStyle(tr, space.Database, band.Row.TextStyle, styleId);
            DrawKc100Pickets(
                tr, space, rowStyle, band.Row,
                route, l0, l1, xProfile0,
                band.YBottom, band.Height, handles);
        }

        // Đường đất: chỉ polyline bề mặt (+ vạch đứng tại cọc). Không ghi số Z trên đường đất.
        // Y sát mép trên bảng (khoảng đứng = 0). X lệch thước theo Cách đầu bảng.
        _ = textProfile;
        var ground = new Point2dCollection();
        double yGroundMax = yDatum;
        foreach (var st in samples)
        {
            var x = xProfile0 + (st.Chainage - l0);
            var y = yDatum + (st.Z - datum) * phong;
            if (y > yGroundMax)
                yGroundMax = y;
            ground.Add(new Point2d(x, y));
            if (st.IsStation)
                AddLine(tr, space, iSurveyLayers.TdLuoi, 8, x, yDatum, x, y, handles);
        }

        // Lưới tùy chọn chỉ trên phần đường đất (không xuyên đầu bảng).
        if (options.ShowGrid && samples.Count >= 2)
        {
            var step = Math.Max(10, NiceStep(length / 10.0));
            for (var c = Math.Ceiling(l0 / step) * step; c < l1 - 1e-6; c += step)
            {
                var x = xProfile0 + (c - l0);
                AddLine(tr, space, iSurveyLayers.TdLuoi, 8, x, yDatum, x, yGroundMax, handles);
            }
        }

        if (ground.Count >= 2)
        {
            var pl = new Polyline();
            for (var i = 0; i < ground.Count; i++)
                pl.AddVertexAt(i, ground[i], 0, 0, 0);
            pl.Layer = iSurveyLayers.TdDat;
            pl.Color = AcadColor.FromColorIndex(ColorMethod.ByAci, 3);
            space.AppendEntity(pl);
            tr.AddNewlyCreatedDBObject(pl, true);
            handles.Add(pl.Handle.ToString());
        }

        if (options.InsertLeftScaleBar || options.InsertRightScaleBar || options.FillDatumLabels)
            DrawScaleBars(tr, space, styleId, options, origin.X, xSplit, xRight, labelWidth, yDatum, yGroundMax, datum, phong, textProfile, handles);
    }

    /// <summary>Tọa độ X giữa ô khoảng lẻ — không cắt vào vạch đứng.</summary>
    private static double CellMidX(
        int index,
        List<ProfileSampleService.SamplePoint> stations,
        double l0,
        double xProfile0,
        double xPrev,
        double x,
        double textHeight)
    {
        // le: giữa hai vạch đứng của đoạn [prev, hiện tại].
        if (Math.Abs(x - xPrev) > 1e-3)
            return (xPrev + x) * 0.5;

        if (index + 1 < stations.Count)
        {
            var xNext = xProfile0 + (stations[index + 1].Chainage - l0);
            return x + (xNext - x) * 0.35;
        }

        return x + Math.Max(textHeight, 1.0);
    }

    /// <summary>
    /// Thước tỷ lệ đứng tại đầu cắt dọc (mép trái vùng đất = xSplit).
    /// Số cao độ / MSS nằm trong cột đầu bảng, bên trái trục — không đè đường đất.
    /// </summary>
    private static void DrawScaleBars(
        Transaction tr, BlockTableRecord space, ObjectId styleId,
        ProfileDrawOptions options,
        double xTableLeft, double xProfileLeft, double xRight, double labelWidth,
        double yBandTop, double yGroundMax, double datum, double phong, double textHeight,
        List<string> handles)
    {
        var spanZ = (yGroundMax - yBandTop) / phong;
        if (spanZ <= 1e-9)
            return;

        var stepZ = NiceStep(spanZ);
        var minStep = textHeight * 1.5 / phong;
        if (stepZ < minStep)
            stepZ = NiceStep(minStep * 8.0);
        while (stepZ > 0 && stepZ * phong < textHeight * 1.5)
            stepZ *= 2;
        if (stepZ <= 0)
            return;

        var tick = Math.Max(textHeight * 0.55, Math.Max(0.2, options.ScaleBarWidth) * textHeight * 0.5);
        tick = Math.Min(tick, Math.Max(textHeight, labelWidth * 0.12));
        var zTop = datum + spanZ;
        var zStart = Math.Ceiling(datum / stepZ) * stepZ;
        if (zStart < datum - 1e-9)
            zStart += stepZ;
        var xLabelMin = xTableLeft + (options.LabelInset > 0 ? options.LabelInset : DefaultLabelInset);

        if (options.InsertLeftScaleBar)
        {
            // Trục thước = vị trí đầu tiên của cắt dọc.
            AddLine(tr, space, iSurveyLayers.TdBang, 7, xProfileLeft, yBandTop, xProfileLeft, yGroundMax, handles);

            for (var z = zStart; z <= zTop + 1e-6; z += stepZ)
            {
                var y = yBandTop + (z - datum) * phong;
                if (y < yBandTop + textHeight * 0.45)
                    continue;
                AddLine(tr, space, iSurveyLayers.TdLuoi, 8, xProfileLeft - tick, y, xProfileLeft, y, handles);
                if (options.FillDatumLabels)
                {
                    var label = z.ToString("0.##", CultureInfo.InvariantCulture);
                    var xText = xProfileLeft - tick - textHeight * 0.5 - ApproxTextWidth(label, textHeight);
                    if (xText < xLabelMin)
                        xText = xLabelMin;
                    AddText(tr, space, styleId, iSurveyLayers.TdChu, 7,
                        new Point3d(xText, y - textHeight * 0.35, 0),
                        label, textHeight, handles);
                }
            }

            if (options.FillDatumLabels)
            {
                var mss = "MSS: " + datum.ToString("0.##", CultureInfo.InvariantCulture);
                var xMss = xProfileLeft - tick - textHeight * 0.5 - ApproxTextWidth(mss, textHeight * 0.85);
                if (xMss < xLabelMin)
                    xMss = xLabelMin;
                AddText(tr, space, styleId, iSurveyLayers.TdChu, 3,
                    new Point3d(xMss, yBandTop + textHeight * 0.25, 0),
                    mss, textHeight * 0.85, handles);
            }
        }

        if (options.InsertRightScaleBar)
        {
            AddLine(tr, space, iSurveyLayers.TdBang, 7, xRight, yBandTop, xRight, yGroundMax, handles);
            for (var z = zStart; z <= zTop + 1e-6; z += stepZ)
            {
                var y = yBandTop + (z - datum) * phong;
                if (y < yBandTop + textHeight * 0.45)
                    continue;
                AddLine(tr, space, iSurveyLayers.TdLuoi, 8, xRight, y, xRight + tick, y, handles);
                if (options.FillDatumLabels)
                {
                    AddText(tr, space, styleId, iSurveyLayers.TdChu, 7,
                        new Point3d(xRight + tick + textHeight * 0.35, y - textHeight * 0.35, 0),
                        z.ToString("0.##", CultureInfo.InvariantCulture), textHeight, handles);
                }
            }
        }
    }

    private static double ApproxTextWidth(string text, double height) =>
        Math.Max(1, (text ?? "").Length) * Math.Max(0.5, height) * 0.62;

    private static string ContentKey(string? content) =>
        (content ?? "").Trim().ToLowerInvariant();

    /// <summary>Cao độ / khoảng cách lẻ / cộng dồn / lý trình / tên cọc — chữ xoay dọc.</summary>
    private static bool IsVerticalCell(string key) =>
        key is "cao_do" or "z" or "le" or "cong_don" or "ly_trinh" or "chainage" or "ten" or "name";

    private static string ResolveCell(
        string key,
        ProfileSampleService.SamplePoint st,
        double le,
        double cong,
        double bearingDeg,
        ProfileDrawOptions options)
    {
        _ = bearingDeg;
        return key switch
        {
            "cao_do" or "z" => st.Z.ToString("0.##", CultureInfo.InvariantCulture),
            // Không ghi 0 tại đầu tuyến (tránh dính số khoảng sau).
            "le" => Math.Abs(le) < 0.05 ? "" : le.ToString("0.##", CultureInfo.InvariantCulture),
            "cong_don" => cong.ToString("0.##", CultureInfo.InvariantCulture),
            "ly_trinh" or "chainage" => options.FillChainage ? FormatChainage(st.Chainage) : "",
            "ten" or "name" => st.Name,
            "huong" or "bearing" or "kc_100" => "",
            "so_hoa" or "neo" or "cot" or "empty" => "",
            _ => ""
        };
    }

    /// <summary>
    /// Hàng hướng tuyến kiểu ADS: --/\-- = rẽ trái (RTr), --\/-- = rẽ phải (RPh); chữ góc đối diện mũi.
    /// </summary>
    private static void DrawHuongZigzag(
        Transaction tr, BlockTableRecord space, ObjectId styleId, ProfileBandRow row,
        List<ProfileSampleService.SamplePoint> stations,
        double l0, double xSplit, double xRight,
        double yBottom, double rowHeight, List<string> handles)
    {
        if (stations.Count < 2)
            return;

        // Đường hướng tuyến giữa khuông; mũi rộng–thấp; DMS giữa đường hướng tuyến và mép hàng (đối diện mũi).
        var yTop = yBottom + rowHeight;
        var yBase = yBottom + rowHeight * 0.5;
        var peakH = rowHeight * 0.16;
        var halfWWish = Math.Max(rowHeight * 0.42, Math.Max(0.5, row.TextHeight) * 1.35);
        var th = Math.Max(0.5, row.TextHeight * 0.85);
        var layer = string.IsNullOrWhiteSpace(row.Layer) ? iSurveyLayers.TdBang : row.Layer;
        var color = row.ColorAci is >= 1 and <= 255 ? (short)row.ColorAci : (short)7;

        var gaps = new List<(double Left, double Right, bool LeftTurn, double X, string Label)>();
        for (var i = 0; i < stations.Count; i++)
        {
            if (!TryDeflection(stations, i, out var left, out _, out var dms))
                continue;
            var x = xSplit + (stations[i].Chainage - l0);
            var xPrev = i > 0
                ? xSplit + (stations[i - 1].Chainage - l0)
                : xSplit;
            var xNext = i < stations.Count - 1
                ? xSplit + (stations[i + 1].Chainage - l0)
                : xRight;
            var halfW = Math.Min(halfWWish, Math.Min((x - xPrev) * 0.48, (xNext - x) * 0.48));
            if (halfW < 0.5)
                halfW = Math.Min(halfWWish, Math.Max(0.5, (xNext - xPrev) * 0.28));
            var label = (left ? "RTr = " : "RPh = ") + dms;
            gaps.Add((x - halfW, x + halfW, left, x, label));
        }

        gaps.Sort((a, b) => a.Left.CompareTo(b.Left));
        var cursor = xSplit;
        foreach (var g in gaps)
        {
            if (g.Left > cursor + 1e-9)
                AddLine(tr, space, layer, color, cursor, yBase, g.Left, yBase, handles);

            var yPeak = g.LeftTurn ? yBase + peakH : yBase - peakH;
            var v = new Polyline();
            v.AddVertexAt(0, new Point2d(g.Left, yBase), 0, 0, 0);
            v.AddVertexAt(1, new Point2d(g.X, yPeak), 0, 0, 0);
            v.AddVertexAt(2, new Point2d(g.Right, yBase), 0, 0, 0);
            v.Layer = layer;
            v.Color = AcadColor.FromColorIndex(ColorMethod.ByAci, color);
            space.AppendEntity(v);
            tr.AddNewlyCreatedDBObject(v, true);
            handles.Add(v.Handle.ToString());

            // Mũi lên → chữ giữa đường hướng tuyến và đáy hàng; mũi xuống → giữa đường và đỉnh hàng.
            var yText = g.LeftTurn
                ? (yBase + yBottom) * 0.5
                : (yBase + yTop) * 0.5;
            AddCenteredText(tr, space, styleId, layer, color,
                new Point3d(g.X, yText, 0), g.Label, th, handles);

            cursor = Math.Max(cursor, g.Right);
        }

        if (xRight > cursor + 1e-9)
            AddLine(tr, space, layer, color, cursor, yBase, xRight, yBase, handles);
    }

    /// <summary>
    /// Thước 100 m: vạch đứng tại cộng dồn 0, 100, 200…; chữ ngang giữa vạch;
    /// vạch cắt rỗng đúng chiều cao chữ (không đè số).
    /// </summary>
    private static void DrawKc100Pickets(
        Transaction tr, BlockTableRecord space, ObjectId styleId, ProfileBandRow row,
        RouteRecord route, double l0, double l1, double xProfile0,
        double yBottom, double rowHeight, List<string> handles)
    {
        var layer = string.IsNullOrWhiteSpace(row.Layer) ? iSurveyLayers.TdBang : row.Layer;
        var color = row.ColorAci is >= 1 and <= 255 ? (short)row.ColorAci : (short)7;
        var th = Math.Max(0.5, row.TextHeight);
        var yMid = yBottom + rowHeight * 0.5;
        // Rỗng qua chiều cao chữ (± nửa H + chút đệm).
        var gapHalf = th * 0.55;
        var yGapLo = yMid - gapHalf;
        var yGapHi = yMid + gapHalf;
        var yHi = yBottom + rowHeight;

        var cong0 = l0 - route.StartChainage;
        var cong1 = l1 - route.StartChainage;
        var first = Math.Ceiling(cong0 / 100.0 - 1e-9) * 100.0;
        if (first < cong0 - 1e-6)
            first += 100.0;
        if (cong0 <= 1e-6)
            first = 0.0;

        for (var cong = first; cong <= cong1 + 1e-6; cong += 100.0)
        {
            var chainage = route.StartChainage + cong;
            if (chainage < l0 - 1e-6 || chainage > l1 + 1e-6)
                continue;

            var x = xProfile0 + (chainage - l0);
            if (yGapLo > yBottom + 1e-6)
                AddLine(tr, space, layer, color, x, yBottom, x, yGapLo, handles);
            if (yHi > yGapHi + 1e-6)
                AddLine(tr, space, layer, color, x, yGapHi, x, yHi, handles);

            var label = cong.ToString("0", CultureInfo.InvariantCulture);
            AddCenteredText(tr, space, styleId, layer, color,
                new Point3d(x, yMid, 0), label, th, handles);
        }
    }

    /// <summary>
    /// Khoảng néo = khoảng cách giữa hai điểm Góc liên tiếp (đầu/cuối tuyến + đỉnh có góc).
    /// Vạch đứng tại mỗi G; số khoảng giữa hai vạch, chữ ngang.
    /// </summary>
    private static void DrawNeoSpans(
        Transaction tr, BlockTableRecord space, ObjectId styleId, ProfileBandRow row,
        List<ProfileSampleService.SamplePoint> stations,
        double l0, double xProfile0,
        double yBottom, double rowHeight, List<string> handles)
    {
        if (stations.Count < 2)
            return;

        var gIdx = new List<int>();
        for (var i = 0; i < stations.Count; i++)
        {
            if (i == 0 || i == stations.Count - 1 || TryDeflection(stations, i, out _, out _, out _))
                gIdx.Add(i);
        }

        if (gIdx.Count < 2)
            return;

        var layer = string.IsNullOrWhiteSpace(row.Layer) ? iSurveyLayers.TdBang : row.Layer;
        var color = row.ColorAci is >= 1 and <= 255 ? (short)row.ColorAci : (short)1;
        var th = Math.Max(0.5, row.TextHeight);
        var yMid = yBottom + rowHeight * 0.5;

        for (var k = 0; k < gIdx.Count; k++)
        {
            var i = gIdx[k];
            var x = xProfile0 + (stations[i].Chainage - l0);
            AddLine(tr, space, layer, color, x, yBottom, x, yBottom + rowHeight, handles);

            if (k == 0)
                continue;

            var i0 = gIdx[k - 1];
            var dist = stations[i].Chainage - stations[i0].Chainage;
            if (dist < 1e-3)
                continue;

            var x0 = xProfile0 + (stations[i0].Chainage - l0);
            var xMid = (x0 + x) * 0.5;
            var text = dist.ToString("0.##", CultureInfo.InvariantCulture);
            AddCenteredText(tr, space, styleId, layer, color,
                new Point3d(xMid, yMid, 0), text, th, handles);
        }
    }

    private static bool TryDeflection(
        List<ProfileSampleService.SamplePoint> stations, int index,
        out bool left, out double deg, out string dms)
    {
        left = false;
        deg = 0;
        dms = "";
        if (index <= 0 || index >= stations.Count - 1)
            return false;

        var a = stations[index - 1];
        var b = stations[index];
        var c = stations[index + 1];
        var vInX = b.X - a.X;
        var vInY = b.Y - a.Y;
        var vOutX = c.X - b.X;
        var vOutY = c.Y - b.Y;
        var lenIn = Math.Sqrt(vInX * vInX + vInY * vInY);
        var lenOut = Math.Sqrt(vOutX * vOutX + vOutY * vOutY);
        if (lenIn < 1e-9 || lenOut < 1e-9)
            return false;

        vInX /= lenIn;
        vInY /= lenIn;
        vOutX /= lenOut;
        vOutY /= lenOut;
        var cross = vInX * vOutY - vInY * vOutX;
        var dot = vInX * vOutX + vInY * vOutY;
        deg = Math.Atan2(Math.Abs(cross), dot) * 180.0 / Math.PI;
        if (deg < 1.0 / 60.0)
            return false;

        left = cross > 0;
        dms = FormatDms(deg);
        return true;
    }

    private static string FormatDms(double degrees)
    {
        if (degrees < 0)
            degrees = 0;
        var totalSec = (int)Math.Round(degrees * 3600.0);
        if (totalSec < 0)
            totalSec = 0;
        var d = totalSec / 3600;
        var rem = totalSec % 3600;
        var m = rem / 60;
        var s = rem % 60;

        return d.ToString("0", CultureInfo.InvariantCulture) + "°"
            + m.ToString("00", CultureInfo.InvariantCulture) + "'"
            + s.ToString("00", CultureInfo.InvariantCulture) + "\"";
    }

    private static void WriteCell(
        Transaction tr, BlockTableRecord space, ObjectId styleId,
        string layer, int colorAci,
        double x, double yBottom, double rowHeight, double textHeight, string text, List<string> handles)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;
        AddText(tr, space, styleId, layer, colorAci,
            new Point3d(x + textHeight * 0.15, yBottom + rowHeight * 0.5 - textHeight * 0.35, 0),
            text, textHeight * 0.9, handles);
    }

    private static void WriteVerticalCell(
        Transaction tr, BlockTableRecord space, ObjectId styleId,
        string layer, int colorAci,
        double x, double yBottom, double rowHeight, double textHeight, string text, List<string> handles)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;
        var th = Math.Max(0.5, textHeight * 0.9);
        var yMid = yBottom + rowHeight * 0.5;
        // Xoay 90° (đọc từ dưới lên): căn giữa sau khi vào DB — Justify trước Append không ăn.
        var dbText = new DBText
        {
            Height = th,
            TextString = text,
            Layer = string.IsNullOrWhiteSpace(layer) ? iSurveyLayers.TdChu : layer,
            TextStyleId = styleId,
            Rotation = Math.PI / 2.0,
            Position = new Point3d(x, yMid, 0)
        };
        if (colorAci is >= 1 and <= 255)
            dbText.Color = AcadColor.FromColorIndex(ColorMethod.ByAci, (short)colorAci);
        space.AppendEntity(dbText);
        tr.AddNewlyCreatedDBObject(dbText, true);
        dbText.Justify = AttachmentPoint.MiddleCenter;
        dbText.AlignmentPoint = new Point3d(x, yMid, 0);
        try
        {
            dbText.AdjustAlignment(space.Database);
        }
        catch
        {
            // một số style không Adjust được — AlignmentPoint vẫn giữ
        }

        handles.Add(dbText.Handle.ToString());
    }

    private static void AddLine(
        Transaction tr, BlockTableRecord space, string layer, short color,
        double x0, double y0, double x1, double y1, List<string> handles)
    {
        var line = new Line(new Point3d(x0, y0, 0), new Point3d(x1, y1, 0))
        {
            Layer = layer,
            Color = AcadColor.FromColorIndex(ColorMethod.ByAci, color)
        };
        space.AppendEntity(line);
        tr.AddNewlyCreatedDBObject(line, true);
        handles.Add(line.Handle.ToString());
    }

    /// <summary>Đường dóng nét đứt bằng đoạn ngắn — không phụ thuộc LTSCALE / DASHED.</summary>
    private static void AddDashedLine(
        Transaction tr, BlockTableRecord space, string layer, short color,
        double x0, double y0, double x1, double y1, double textHeight, List<string> handles)
    {
        var dx = x1 - x0;
        var dy = y1 - y0;
        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-9)
            return;

        var ux = dx / len;
        var uy = dy / len;
        var dash = Math.Max(textHeight * 1.2, 8.0);
        var gap = Math.Max(textHeight * 0.7, 4.0);
        var pos = 0.0;
        while (pos < len - 1e-9)
        {
            var end = Math.Min(pos + dash, len);
            AddLine(tr, space, layer, color,
                x0 + ux * pos, y0 + uy * pos,
                x0 + ux * end, y0 + uy * end, handles);
            pos = end + gap;
        }
    }

    private static void AddCenteredText(
        Transaction tr, BlockTableRecord space, ObjectId styleId, string layer, int colorAci,
        Point3d center, string content, double height, List<string> handles)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;
        var text = new DBText
        {
            Height = Math.Max(0.5, height),
            TextString = content,
            Layer = string.IsNullOrWhiteSpace(layer) ? iSurveyLayers.TdChu : layer,
            TextStyleId = styleId,
            Position = center
        };
        if (colorAci is >= 1 and <= 255)
            text.Color = AcadColor.FromColorIndex(ColorMethod.ByAci, (short)colorAci);
        space.AppendEntity(text);
        tr.AddNewlyCreatedDBObject(text, true);
        text.Justify = AttachmentPoint.MiddleCenter;
        text.AlignmentPoint = center;
        try
        {
            text.AdjustAlignment(space.Database);
        }
        catch
        {
            // style không Adjust được — AlignmentPoint vẫn giữ
        }

        handles.Add(text.Handle.ToString());
    }

    private static ObjectId ResolveTextStyle(Transaction tr, Database db, string? styleName, ObjectId fallback)
    {
        var name = string.IsNullOrWhiteSpace(styleName) ? ProfileTextStyles.StyleName : styleName.Trim();
        try
        {
            var table = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
            if (table.Has(name))
                return table[name];
            if (string.Equals(name, ProfileTextStyles.StyleName, StringComparison.OrdinalIgnoreCase))
                return ProfileTextStyles.Ensure(tr, db);
        }
        catch
        {
            // fallback
        }

        return fallback.IsNull ? ProfileTextStyles.Ensure(tr, db) : fallback;
    }

    private static void AddText(
        Transaction tr, BlockTableRecord space, ObjectId styleId, string layer, int colorAci,
        Point3d position, string content, double height, List<string> handles)
    {
        var text = new DBText
        {
            Position = position,
            Height = Math.Max(0.5, height),
            TextString = content,
            Layer = string.IsNullOrWhiteSpace(layer) ? iSurveyLayers.TdChu : layer,
            TextStyleId = styleId
        };
        if (colorAci is >= 1 and <= 255)
            text.Color = AcadColor.FromColorIndex(ColorMethod.ByAci, (short)colorAci);
        space.AppendEntity(text);
        tr.AddNewlyCreatedDBObject(text, true);
        handles.Add(text.Handle.ToString());
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
            Color = AcadColor.FromColorIndex(ColorMethod.ByAci, color)
        };
        lt.Add(layer);
        tr.AddNewlyCreatedDBObject(layer, true);
    }
}
