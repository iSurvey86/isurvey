using System.Globalization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AcadColor = Autodesk.AutoCAD.Colors.Color;
using ColorMethod = Autodesk.AutoCAD.Colors.ColorMethod;
using iSurvey.Modules.Terrain;

namespace iSurvey.Modules.Tuyen;

/// <summary>Chọn tuyến, đầu tuyến, phát sinh cọc vuông góc và Z từ TIN.</summary>
public static class RouteService
{
    public const string AppName = "ISURVEY_ROUTE";

    /// <summary>Lớp riêng cho gạch cọc và chữ lý trình.</summary>
    public const string StationLayer = "isurvey_Coctuyen";

    public static RouteRecord? Choose(Editor editor, Database db)
    {
        var routes = RouteStore.Load(db);
        if (routes.Count == 0)
        {
            editor.WriteMessage("\n[iSurvey] Chưa có tuyến. Hãy tạo tuyến trước.");
            return null;
        }

        var pending = RouteContext.TakeRoute();
        if (!string.IsNullOrWhiteSpace(pending))
        {
            foreach (var route in routes)
            {
                if (string.Equals(route.Id, pending, StringComparison.OrdinalIgnoreCase))
                    return route;
            }
        }

        if (routes.Count == 1)
            return routes[0];

        var options = new PromptKeywordOptions("\nChọn tuyến [");
        for (var i = 0; i < routes.Count; i++)
        {
            if (i > 0)
                options.Message += "/";
            options.Message += (i + 1) + "=" + routes[i].Name;
            options.Keywords.Add((i + 1).ToString(CultureInfo.InvariantCulture));
        }

        options.Message += "]";
        options.AllowNone = false;
        var picked = editor.GetKeywords(options);
        if (picked.Status != PromptStatus.OK)
            return null;
        if (!int.TryParse(picked.StringResult, out var index) || index < 1 || index > routes.Count)
            return null;
        return routes[index - 1];
    }

    /// <summary>Chọn điểm gần một đầu mút. Trả về true nếu đảo so với chiều polyline.</summary>
    public static bool? ResolveStart(
        Editor editor,
        Database db,
        string polylineHandle,
        out Point3d startPoint,
        out Point3d endPoint)
    {
        startPoint = default;
        endPoint = default;
        using var tr = db.TransactionManager.StartTransaction();
        var curve = OpenCurve(tr, db, polylineHandle);
        if (curve is null)
        {
            tr.Commit();
            return null;
        }

        var first = curve.StartPoint;
        var last = curve.EndPoint;
        tr.Commit();

        var pick = editor.GetPoint(new PromptPointOptions("\nChọn điểm đầu tuyến (gần một đầu mút của polyline): ")
        {
            AllowNone = false
        });
        if (pick.Status != PromptStatus.OK)
            return null;

        var at = pick.Value;
        var dFirst = Dist2(at, first);
        var dLast = Dist2(at, last);
        if (Math.Abs(dFirst - dLast) < 1e-6)
        {
            editor.WriteMessage("\n[iSurvey] Điểm đầu không xác định (cách đều hai đầu). Chọn lại gần một đầu hơn.");
            return null;
        }

        var reversed = dLast < dFirst;
        startPoint = reversed ? last : first;
        endPoint = reversed ? first : last;
        return reversed;
    }

    public static bool TryLength(Database db, RouteRecord route, out double length)
    {
        length = 0;
        using var tr = db.TransactionManager.StartTransaction();
        var curve = OpenCurve(tr, db, route.PolylineHandle);
        if (curve is null)
        {
            tr.Commit();
            return false;
        }

        length = CurveLength(curve);
        tr.Commit();
        return length > 1e-6;
    }

    public static bool Reverse(Autodesk.AutoCAD.ApplicationServices.Document doc, RouteRecord route)
    {
        route.Reversed = !route.Reversed;
        ClearStations(doc, route);
        RouteStore.Save(doc.Database, route);
        return route.Reversed;
    }

    public static int BuildStations(
        Autodesk.AutoCAD.ApplicationServices.Document doc,
        RouteRecord route,
        double fromChainage,
        double toChainage,
        double step,
        out int missingZ)
    {
        missingZ = 0;
        if (step <= 1e-6)
            return 0;
        if (toChainage < fromChainage)
            (fromChainage, toChainage) = (toChainage, fromChainage);

        var terrain = ResolveTerrain(doc.Database, route);
        var stations = new List<StationRecord>();
        using (doc.LockDocument())
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            var curve = OpenCurve(tr, doc.Database, route.PolylineHandle);
            if (curve is null)
            {
                tr.Commit();
                return 0;
            }

            var length = CurveLength(curve);
            var start = route.StartChainage;
            var endChain = start + length;
            var from = Math.Max(fromChainage, start);
            var to = Math.Min(toChainage, endChain);
            if (to < from)
            {
                tr.Commit();
                return 0;
            }

            EraseStationEntities(tr, doc.Database, route);
            EnsureApp(tr, doc.Database);
            EnsureLayer(tr, doc.Database, route.Layer);
            EnsureLayer(tr, doc.Database, StationLayer);
            var space = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);
            ResolveStationStyle(doc, curve, out var height, out var tick, out var textOffset);

            for (var chain = from; chain <= to + 1e-6; chain += step)
            {
                var use = Math.Min(chain, to);
                AddStation(tr, doc, space, curve, route, terrain, stations, use, start, length, height, tick, textOffset, ref missingZ, false);
                if (Math.Abs(use - to) <= 1e-6)
                    break;
            }

            if (stations.Count == 0 || Math.Abs(stations[stations.Count - 1].Chainage - to) > 1e-4)
                AddStation(tr, doc, space, curve, route, terrain, stations, to, start, length, height, tick, textOffset, ref missingZ, false);

            // Bắt buộc có cọc tại mọi đỉnh polyline nằm trong khoảng lý trình.
            foreach (var alongCurve in VertexDistancesAlong(tr, curve))
            {
                var distance = route.Reversed ? length - alongCurve : alongCurve;
                var chainage = start + distance;
                if (chainage < from - 1e-6 || chainage > to + 1e-6)
                    continue;
                AddStation(tr, doc, space, curve, route, terrain, stations, chainage, start, length, height, tick, textOffset, ref missingZ, false);
            }

            stations.Sort((a, b) => a.Chainage.CompareTo(b.Chainage));
            tr.Commit();
        }

        route.Stations = stations;
        if (terrain is not null && string.IsNullOrWhiteSpace(route.TerrainId))
            route.TerrainId = terrain.Id;
        RouteStore.Save(doc.Database, route);
        doc.Editor.Regen();
        return stations.Count;
    }

    public static int ClearStations(Autodesk.AutoCAD.ApplicationServices.Document doc, RouteRecord route)
    {
        var count = route.Stations?.Count ?? 0;
        using (doc.LockDocument())
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            EraseStationEntities(tr, doc.Database, route);
            tr.Commit();
        }

        route.Stations = new List<StationRecord>();
        RouteStore.Save(doc.Database, route);
        doc.Editor.Regen();
        return count;
    }

    public static RouteRecord? FindById(Database db, string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;
        foreach (var route in RouteStore.Load(db))
        {
            if (string.Equals(route.Id, id, StringComparison.OrdinalIgnoreCase))
                return route;
        }

        return null;
    }

    /// <summary>Xóa một cọc theo gạch hoặc chữ đã chọn trên bản vẽ.</summary>
    public static bool RemoveStationByEntity(
        Autodesk.AutoCAD.ApplicationServices.Document doc,
        ObjectId entityId,
        out string routeName,
        out string stationName)
    {
        routeName = "";
        stationName = "";
        RouteRecord? route = null;
        StationRecord? station = null;
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            if (tr.GetObject(entityId, OpenMode.ForRead) is not Entity entity || entity.IsErased)
            {
                tr.Commit();
                return false;
            }

            var handle = entity.Handle.ToString();
            foreach (var item in RouteStore.Load(doc.Database))
            {
                if (item.Stations is null)
                    continue;
                foreach (var candidate in item.Stations)
                {
                    if (!string.Equals(candidate.MarkerHandle, handle, StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(candidate.LabelHandle, handle, StringComparison.OrdinalIgnoreCase))
                        continue;
                    route = item;
                    station = candidate;
                    break;
                }

                if (station is not null)
                    break;
            }

            tr.Commit();
        }

        if (route is null || station is null)
            return false;
        routeName = route.Name;
        stationName = station.Name;
        return RemoveStation(doc, route, station);
    }

    public static bool RemoveStation(
        Autodesk.AutoCAD.ApplicationServices.Document doc,
        RouteRecord route,
        StationRecord station)
    {
        route.Stations ??= new List<StationRecord>();
        var index = -1;
        for (var i = 0; i < route.Stations.Count; i++)
        {
            if (ReferenceEquals(route.Stations[i], station)
                || (Math.Abs(route.Stations[i].Chainage - station.Chainage) <= 1e-4
                    && string.Equals(route.Stations[i].MarkerHandle, station.MarkerHandle, StringComparison.OrdinalIgnoreCase)))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
            return false;

        using (doc.LockDocument())
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            EraseHandle(tr, doc.Database, route.Stations[index].MarkerHandle);
            EraseHandle(tr, doc.Database, route.Stations[index].LabelHandle);
            tr.Commit();
        }

        route.Stations.RemoveAt(index);
        RouteStore.Save(doc.Database, route);
        doc.Editor.Regen();
        return true;
    }

    /// <summary>Thêm cọc tại điểm pick: chiếu lên tim, lấy lý trình và Z.</summary>
    public static bool TryAddStationAtPoint(
        Autodesk.AutoCAD.ApplicationServices.Document doc,
        RouteRecord route,
        Point3d pick,
        bool replaceNear,
        out double chainage,
        out int missingZ)
    {
        chainage = 0;
        missingZ = 0;
        route.Stations ??= new List<StationRecord>();
        using (doc.LockDocument())
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            var curve = OpenCurve(tr, doc.Database, route.PolylineHandle);
            if (curve is null)
            {
                tr.Commit();
                return false;
            }

            var length = CurveLength(curve);
            Point3d on;
            double along;
            try
            {
                on = curve.GetClosestPointTo(pick, false);
                along = curve.GetDistAtPoint(on);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception)
            {
                tr.Commit();
                return false;
            }

            along = Math.Max(0, Math.Min(length, along));
            var distance = route.Reversed ? length - along : along;
            chainage = route.StartChainage + distance;

            EnsureApp(tr, doc.Database);
            EnsureLayer(tr, doc.Database, StationLayer);
            var space = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);
            ResolveStationStyle(doc, curve, out var height, out var tick, out var textOffset);
            var terrain = ResolveTerrain(doc.Database, route);
            var before = route.Stations.Count;
            AddStation(
                tr,
                doc,
                space,
                curve,
                route,
                terrain,
                route.Stations,
                chainage,
                route.StartChainage,
                length,
                height,
                tick,
                textOffset,
                ref missingZ,
                replaceNear);
            var chain = chainage;
            var added = route.Stations.Count > before
                || route.Stations.Exists(item => Math.Abs(item.Chainage - chain) <= 1e-4);
            tr.Commit();
            if (!added)
                return false;
        }

        route.Stations.Sort((a, b) => a.Chainage.CompareTo(b.Chainage));
        if (ResolveTerrain(doc.Database, route) is { } terrainKeep
            && string.IsNullOrWhiteSpace(route.TerrainId))
            route.TerrainId = terrainKeep.Id;
        RouteStore.Save(doc.Database, route);
        doc.Editor.Regen();
        return true;
    }

    public static void ZoomTo(Autodesk.AutoCAD.ApplicationServices.Document doc, double x, double y)
    {
        try
        {
            var view = doc.Editor.GetCurrentView();
            view.CenterPoint = new Point2d(x, y);
            doc.Editor.SetCurrentView(view);
            doc.Editor.Regen();
        }
        catch (System.Exception)
        {
            // Bỏ qua nếu không đổi được view.
        }
    }

    private static TerrainRecord? ResolveTerrain(Database db, RouteRecord route)
    {
        var models = TerrainStore.Load(db);
        if (!string.IsNullOrWhiteSpace(route.TerrainId))
        {
            foreach (var model in models)
            {
                if (string.Equals(model.Id, route.TerrainId, StringComparison.OrdinalIgnoreCase))
                    return model;
            }
        }

        TerrainRecord? sole = null;
        foreach (var model in models)
        {
            if (model.TriangleCount == 0)
                continue;
            if (sole is not null)
                return null;
            sole = model;
        }

        return sole;
    }

    private static void AddStation(
        Transaction tr,
        Autodesk.AutoCAD.ApplicationServices.Document doc,
        BlockTableRecord space,
        Curve curve,
        RouteRecord route,
        TerrainRecord? terrain,
        List<StationRecord> stations,
        double chainage,
        double start,
        double length,
        double height,
        double tick,
        double textOffset,
        ref int missingZ,
        bool replaceExisting)
    {
        for (var i = stations.Count - 1; i >= 0; i--)
        {
            if (Math.Abs(stations[i].Chainage - chainage) > 1e-4)
                continue;
            if (!replaceExisting)
                return;
            EraseHandle(tr, doc.Database, stations[i].MarkerHandle);
            EraseHandle(tr, doc.Database, stations[i].LabelHandle);
            stations.RemoveAt(i);
        }

        var distance = chainage - start;
        if (distance < -1e-6 || distance > length + 1e-6)
            return;
        var along = route.Reversed ? length - distance : distance;
        along = Math.Max(0, Math.Min(length, along));

        Point3d point;
        Vector3d tangent;
        try
        {
            point = curve.GetPointAtDist(along);
            var param = curve.GetParameterAtDistance(along);
            tangent = curve.GetFirstDerivative(param);
        }
        catch (Autodesk.AutoCAD.Runtime.Exception)
        {
            return;
        }

        var hasZ = false;
        var z = 0.0;
        if (terrain is not null && TerrainService.TryQuery(doc, terrain, point.X, point.Y, out var tinZ))
        {
            z = tinZ;
            hasZ = true;
        }
        else
            missingZ++;

        var name = FormatChainage(chainage);
        var at = new Point3d(point.X, point.Y, hasZ ? z : point.Z);
        var flat = new Vector2d(tangent.X, tangent.Y);
        if (flat.Length <= 1e-9)
            flat = new Vector2d(1, 0);
        else
            flat = flat.GetNormal();
        var perp = new Vector2d(-flat.Y, flat.X);
        var half = tick * 0.5;
        var a = new Point3d(at.X - perp.X * half, at.Y - perp.Y * half, at.Z);
        var b = new Point3d(at.X + perp.X * half, at.Y + perp.Y * half, at.Z);
        var marker = new Line(a, b);
        marker.SetDatabaseDefaults();
        marker.Layer = StationLayer;
        marker.Color = AcadColor.FromColorIndex(ColorMethod.ByAci, 3);
        space.AppendEntity(marker);
        tr.AddNewlyCreatedDBObject(marker, true);
        Tag(marker, route.Id);

        // Chữ dọc theo gạch cọc (vuông góc tuyến), luôn đọc được (không úp).
        var textDir = perp;
        var rotation = Math.Atan2(textDir.Y, textDir.X);
        if (rotation > Math.PI / 2.0 || rotation <= -Math.PI / 2.0)
        {
            rotation += Math.PI;
            textDir = new Vector2d(-textDir.X, -textDir.Y);
        }

        var labelOffset = textDir * textOffset;
        var label = new DBText();
        label.SetDatabaseDefaults();
        label.Layer = StationLayer;
        label.Height = height;
        label.Rotation = rotation;
        label.TextString = hasZ
            ? name + "  Z=" + z.ToString("0.000", CultureInfo.CurrentCulture)
            : name + "  (chưa có Z)";
        label.Color = AcadColor.FromColorIndex(ColorMethod.ByAci, 3);
        label.Justify = AttachmentPoint.MiddleLeft;
        label.AlignmentPoint = new Point3d(at.X + labelOffset.X, at.Y + labelOffset.Y, at.Z);
        space.AppendEntity(label);
        tr.AddNewlyCreatedDBObject(label, true);
        Tag(label, route.Id);

        stations.Add(new StationRecord
        {
            Name = name,
            Chainage = chainage,
            X = at.X,
            Y = at.Y,
            Z = z,
            HasZ = hasZ,
            MarkerHandle = marker.Handle.ToString(),
            LabelHandle = label.Handle.ToString()
        });
    }

    public static string FormatChainage(double metres)
    {
        var sign = metres < 0 ? "-" : "";
        var abs = Math.Abs(metres);
        var km = Math.Floor(abs / 1000.0);
        var rem = abs - km * 1000.0;
        return sign + "Km" + km.ToString("0", CultureInfo.InvariantCulture)
            + "+" + rem.ToString("000.000", CultureInfo.InvariantCulture);
    }

    private static double CurveLength(Curve curve) =>
        curve.GetDistanceAtParameter(curve.EndParam) - curve.GetDistanceAtParameter(curve.StartParam);

    /// <summary>Khoảng cách dọc curve tại từng đỉnh polyline (không trùng điểm đầu/cuối khép kín).</summary>
    private static List<double> VertexDistancesAlong(Transaction tr, Curve curve)
    {
        var list = new List<double>();
        var length = CurveLength(curve);
        void AddDist(Point3d point)
        {
            try
            {
                var on = curve.GetClosestPointTo(point, false);
                var dist = curve.GetDistAtPoint(on);
                foreach (var existing in list)
                {
                    if (Math.Abs(existing - dist) <= 1e-4)
                        return;
                }

                if (dist < -1e-6 || dist > length + 1e-6)
                    return;
                list.Add(Math.Max(0, Math.Min(length, dist)));
            }
            catch (Autodesk.AutoCAD.Runtime.Exception)
            {
                // Bỏ đỉnh không nằm trên curve.
            }
        }

        if (curve is Polyline polyline)
        {
            var count = polyline.NumberOfVertices;
            for (var i = 0; i < count; i++)
            {
                if (polyline.Closed && i == count - 1 && count > 1
                    && Dist2(polyline.GetPoint3dAt(0), polyline.GetPoint3dAt(i)) <= 1e-8)
                    continue;
                AddDist(polyline.GetPoint3dAt(i));
            }
        }
        else if (curve is Polyline3d poly3)
        {
            foreach (ObjectId id in poly3)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is PolylineVertex3d vertex)
                    AddDist(vertex.Position);
            }
        }
        else if (curve is Polyline2d poly2)
        {
            foreach (ObjectId id in poly2)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is Vertex2d vertex)
                    AddDist(vertex.Position);
            }
        }
        else
        {
            AddDist(curve.StartPoint);
            AddDist(curve.EndPoint);
        }

        list.Sort();
        return list;
    }

    private static double Dist2(Point3d a, Point3d b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    private static Curve? OpenCurve(Transaction tr, Database db, string handle)
    {
        if (!long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            return null;
        var id = db.GetObjectId(false, new Handle(value), 0);
        if (!id.IsValid || id.IsErased)
            return null;
        return tr.GetObject(id, OpenMode.ForRead) as Curve;
    }

    private static void EraseStationEntities(Transaction tr, Database db, RouteRecord route)
    {
        if (route.Stations is null)
            return;
        foreach (var station in route.Stations)
        {
            EraseHandle(tr, db, station.MarkerHandle);
            EraseHandle(tr, db, station.LabelHandle);
        }
    }

    private static void EraseHandle(Transaction tr, Database db, string handle)
    {
        if (string.IsNullOrWhiteSpace(handle))
            return;
        if (!long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            return;
        var id = db.GetObjectId(false, new Handle(value), 0);
        if (!id.IsValid || id.IsErased)
            return;
        if (tr.GetObject(id, OpenMode.ForWrite, true) is Entity entity && !entity.IsErased)
            entity.Erase();
    }

    private static void Tag(Entity entity, string routeId)
    {
        entity.XData = new ResultBuffer(
            new TypedValue((int)DxfCode.ExtendedDataRegAppName, AppName),
            new TypedValue((int)DxfCode.ExtendedDataAsciiString, Clip(routeId)));
    }

    private static void EnsureApp(Transaction tr, Database db)
    {
        var table = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
        if (table.Has(AppName))
            return;
        table.UpgradeOpen();
        var record = new RegAppTableRecord { Name = AppName };
        table.Add(record);
        tr.AddNewlyCreatedDBObject(record, true);
    }

    private static void EnsureLayer(Transaction tr, Database db, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            name = "iSurvey_Tuyen";
        var table = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (table.Has(name))
            return;
        table.UpgradeOpen();
        var layer = new LayerTableRecord { Name = name };
        table.Add(layer);
        tr.AddNewlyCreatedDBObject(layer, true);
    }

    private static void ResolveStationStyle(
        Autodesk.AutoCAD.ApplicationServices.Document doc,
        Curve curve,
        out double height,
        out double tick,
        out double textOffset)
    {
        var style = StationStyleStore.Load();
        height = style.TextHeight > 1e-9 ? style.TextHeight : EstimateTextHeight(doc);
        tick = style.TickLength > 1e-9 ? style.TickLength : Math.Max(height * 2.5, 1.0);
        if (style.UsePolylineWidth)
        {
            var width = TryPolylineWidth(curve);
            var factor = style.WidthFactor > 1e-9 ? style.WidthFactor : 1.0;
            if (width > 1e-9)
                tick = width * factor;
        }

        textOffset = style.TextOffset > 1e-9
            ? style.TextOffset
            : tick * 0.55 + height * 0.35;
    }

    /// <summary>Độ rộng polyline tim (ConstantWidth hoặc trung bình Start/EndWidth).</summary>
    private static double TryPolylineWidth(Curve curve)
    {
        if (curve is not Polyline polyline)
            return 0;
        if (polyline.ConstantWidth > 1e-9)
            return polyline.ConstantWidth;
        if (polyline.NumberOfVertices < 1)
            return 0;
        var sum = 0.0;
        var count = 0;
        for (var i = 0; i < polyline.NumberOfVertices; i++)
        {
            try
            {
                var start = polyline.GetStartWidthAt(i);
                var end = polyline.GetEndWidthAt(i);
                var mid = (start + end) * 0.5;
                if (mid > 1e-9)
                {
                    sum += mid;
                    count++;
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception)
            {
                // Bỏ đỉnh không đọc được độ rộng.
            }
        }

        return count == 0 ? 0 : sum / count;
    }

    private static double EstimateTextHeight(Autodesk.AutoCAD.ApplicationServices.Document doc)
    {
        try
        {
            var view = doc.Editor.GetCurrentView();
            return Math.Max(view.Height / 50.0, 0.2);
        }
        catch (System.Exception)
        {
            return 1.0;
        }
    }

    private static string Clip(string text) =>
        text.Length <= 255 ? text : text.Substring(0, 255);
}
