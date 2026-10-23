using System.Text;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using iSurvey.Auth;
using iSurvey.Core;
using iSurvey.Models;
using iSurvey.Modules.Map;
using iSurvey.UI.Import;
using OSGeo.OGR;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcadColor = Autodesk.AutoCAD.Colors.Color;
using OgrGeometry = OSGeo.OGR.Geometry;

namespace iSurvey.Modules.Import;

/// <summary>Nhập MapInfo TAB vào Model Space: màu brush MapInfo, layer theo MDSD.</summary>
public sealed class TabImportModule : ISurveyModule
{
    public string DisplayName => "Nhập TAB";

    public void Initialize() { }

    public void Terminate() { }

    [CommandMethod("ISURVEY_TAB_TO_CAD")]
    public void TabToCadCommand()
    {
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        var ed = doc.Editor;
        if (!MapModule.IsModelSpace(doc.Database))
        {
            ed.WriteMessage("\n[iSurvey] Hãy mở Model Space trước khi nhập TAB.");
            return;
        }

        var drawingPath = string.IsNullOrWhiteSpace(doc.Database.Filename) ? null : doc.Database.Filename;
        var settings = UserSettingsStore.Load(drawingPath);

        TabImportOptions? options;
        try
        {
            options = TabImportWindow.Show(settings);
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] Không mở được hộp thoại TAB: {ex.Message}");
            return;
        }

        if (options is null)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy nhập TAB.");
            return;
        }

        try
        {
            var stats = Import(doc.Database, ed, options, settings);
            ZoomExtents(ed);
            var report = BuildReport(stats, options);
            ActivityLogger.Result("ISURVEY_TAB_TO_CAD",
                $"file {ActivityLogger.FileName(options.Path)}, {stats.Hatches} hatch, {stats.Outlines} viền, {stats.Labels} nhãn, lỗi {stats.Errors}, bản vẽ {ActivityLogger.FileName(drawingPath)}");
            foreach (var line in report.Split('\n'))
                ed.WriteMessage("\n[iSurvey] " + line.TrimEnd('\r'));
            AcadApp.ShowAlertDialog(report);
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] Nhập TAB thất bại: {ex.Message}");
            ActivityLogger.TrackError(ex);
        }
    }

    private static TabImportStats Import(Database db, Editor ed, TabImportOptions options, MapUserSettings settings)
    {
        var stats = new TabImportStats();
        var hatches = new ObjectIdCollection();
        using var tr = db.TransactionManager.StartTransaction();
        var space = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
        var layers = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        var made = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var pm = new ProgressMeter();
        pm.Start("iSurvey nhập TAB");
        pm.SetLimit(Math.Max(1, options.FeatureCount));
        try
        {
            TabReader.Read(options.Path, settings, transform: true, feature =>
            {
                pm.MeterProgress();
                try
                {
                    DrawFeature(tr, space, layers, made, hatches, feature, options, stats);
                }
                catch (System.Exception ex)
                {
                    stats.Errors++;
                    stats.FirstError ??= ex.Message;
                }
            });

            if (hatches.Count > 0)
            {
                var order = (DrawOrderTable)tr.GetObject(space.DrawOrderTableId, OpenMode.ForWrite);
                order.MoveToBottom(hatches);
            }

            tr.Commit();
        }
        finally
        {
            pm.Stop();
        }

        return stats;
    }

    private static string BuildReport(TabImportStats stats, TabImportOptions options)
    {
        var text = new StringBuilder();
        text.AppendLine(stats.Errors == 0 ? "Đã nhập TAB." : "Đã nhập TAB, có lỗi.");
        text.AppendLine();
        text.AppendLine($"{stats.Hatches:N0} hatch, {stats.Outlines:N0} viền, {stats.Labels:N0} nhãn.");
        if (stats.Skipped > 0)
            text.AppendLine($"{stats.Skipped:N0} đối tượng không có hình.");
        if (stats.Errors > 0)
            text.AppendLine($"{stats.Errors:N0} lỗi. Lỗi đầu: {stats.FirstError}");
        text.AppendLine();
        text.AppendLine(options.NeedsTransform
            ? $"Đã chuyển hệ: {options.SrsText} về {options.TargetText}."
            : $"Không chuyển hệ. {options.SrsText} khớp {options.TargetText}.");
        text.Append("UNDO hoàn tác cả lượt nhập.");
        return text.ToString();
    }

    private static void ZoomExtents(Editor ed)
    {
        try
        {
            ed.Command("_.ZOOM", "_E");
        }
        catch (System.Exception)
        {
            try
            {
                var db = ed.Document.Database;
                db.UpdateExt(true);
                var view = ed.GetCurrentView();
                var width = Math.Max(db.Extmax.X - db.Extmin.X, 20);
                var height = Math.Max(db.Extmax.Y - db.Extmin.Y, 20);
                width *= 1.05;
                height *= 1.05;
                var aspect = view.Height > 1e-9 ? view.Width / view.Height : 1.6;
                if (width / height > aspect)
                    height = width / aspect;
                else
                    width = height * aspect;
                view.CenterPoint = new Point2d((db.Extmin.X + db.Extmax.X) / 2.0, (db.Extmin.Y + db.Extmax.Y) / 2.0);
                view.Width = width;
                view.Height = height;
                ed.SetCurrentView(view);
            }
            catch (System.Exception)
            {
                // Không chặn báo cáo nếu không đổi được khung nhìn.
            }
        }
    }

    private static void DrawFeature(
        Transaction tr,
        BlockTableRecord space,
        LayerTable layers,
        HashSet<string> made,
        ObjectIdCollection hatches,
        TabFeature feature,
        TabImportOptions options,
        TabImportStats stats)
    {
        if (feature.MissingGeometry || feature.Geometry is null)
        {
            stats.Skipped++;
            return;
        }

        var layer = EnsureLayer(tr, layers, made, LayerName(feature.Mdsd));
        foreach (var polygon in Polygons(feature.Geometry))
        {
            var ringIds = new List<ObjectId>();
            var rings = polygon.GetGeometryCount();
            for (var i = 0; i < rings; i++)
            {
                var pts = RingPoints(polygon.GetGeometryRef(i), feature.Map);
                if (pts.Count < 3)
                    continue;

                var positive = SignedArea(pts) > 0;
                if (positive != (i == 0))
                    pts.Reverse();

                if (!options.DrawOutline && !options.DrawHatch)
                    continue;

                ringIds.Add(AddPolyline(tr, space, pts, layer));
                if (options.DrawOutline)
                    stats.Outlines++;
            }

            if (options.DrawHatch && !feature.NoFill && ringIds.Count > 0)
            {
                hatches.Add(AddHatch(tr, space, ringIds, layer, feature.R, feature.G, feature.B));
                stats.Hatches++;
                if (!options.DrawOutline)
                {
                    foreach (var id in ringIds)
                        ((Entity)tr.GetObject(id, OpenMode.ForWrite)).Erase();
                }
            }
        }

        if (!options.DrawLabels)
            return;

        var text = feature.Label(options.LabelField);
        if (text.Length == 0 || !TryPointOnSurface(feature.Geometry, feature.Map, out var x, out var y))
            return;

        AddLabel(tr, space, EnsureLayer(tr, layers, made, "TAB_LABELS"), text, x, y, options.TextHeight);
        stats.Labels++;
    }

    private static IEnumerable<OgrGeometry> Polygons(OgrGeometry geometry)
    {
        var name = geometry.GetGeometryName() ?? "";
        if (name.StartsWith("POLYGON", StringComparison.OrdinalIgnoreCase))
        {
            yield return geometry;
            yield break;
        }

        if (!name.StartsWith("MULTI", StringComparison.OrdinalIgnoreCase)
            && !name.Equals("GEOMETRYCOLLECTION", StringComparison.OrdinalIgnoreCase))
            yield break;

        var count = geometry.GetGeometryCount();
        for (var i = 0; i < count; i++)
        {
            foreach (var part in Polygons(geometry.GetGeometryRef(i)))
                yield return part;
        }
    }

    private static List<(double X, double Y)> RingPoints(OgrGeometry ring, Func<double, double, (double X, double Y)>? map)
    {
        var count = ring.GetPointCount();
        var pts = new List<(double X, double Y)>(count);
        for (var i = 0; i < count; i++)
        {
            var x = ring.GetX(i);
            var y = ring.GetY(i);
            if (map is not null)
                (x, y) = map(x, y);
            if (pts.Count > 0 && Nearly(pts[^1].X, x) && Nearly(pts[^1].Y, y))
                continue;
            pts.Add((x, y));
        }

        if (pts.Count > 1 && Nearly(pts[0].X, pts[^1].X) && Nearly(pts[0].Y, pts[^1].Y))
            pts.RemoveAt(pts.Count - 1);
        return pts;
    }

    private static bool TryPointOnSurface(OgrGeometry geometry, Func<double, double, (double X, double Y)>? map, out double x, out double y)
    {
        x = 0;
        y = 0;
        using var point = geometry.PointOnSurface();
        if (point is null || point.IsEmpty())
            return false;
        x = point.GetX(0);
        y = point.GetY(0);
        if (map is not null)
            (x, y) = map(x, y);
        return true;
    }

    private static ObjectId AddPolyline(Transaction tr, BlockTableRecord space, List<(double X, double Y)> pts, string layer)
    {
        var pl = new Polyline(pts.Count);
        for (var i = 0; i < pts.Count; i++)
            pl.AddVertexAt(i, new Point2d(pts[i].X, pts[i].Y), 0, 0, 0);
        pl.Closed = true;
        pl.Layer = layer;
        space.AppendEntity(pl);
        tr.AddNewlyCreatedDBObject(pl, true);
        return pl.ObjectId;
    }

    private static ObjectId AddHatch(
        Transaction tr, BlockTableRecord space, List<ObjectId> rings, string layer, byte r, byte g, byte b)
    {
        var hatch = new Hatch();
        space.AppendEntity(hatch);
        tr.AddNewlyCreatedDBObject(hatch, true);
        hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
        hatch.Associative = false;
        hatch.Layer = layer;
        hatch.Color = AcadColor.FromRgb(r, g, b);
        for (var i = 0; i < rings.Count; i++)
        {
            var loop = new ObjectIdCollection { rings[i] };
            hatch.AppendLoop(i == 0 ? HatchLoopTypes.Outermost : HatchLoopTypes.Default, loop);
        }

        hatch.EvaluateHatch(true);
        return hatch.ObjectId;
    }

    private static void AddLabel(
        Transaction tr, BlockTableRecord space, string layer, string text, double x, double y, double height)
    {
        var note = new DBText
        {
            TextString = text,
            Height = height,
            Layer = layer,
            HorizontalMode = TextHorizontalMode.TextCenter,
            VerticalMode = TextVerticalMode.TextVerticalMid,
            AlignmentPoint = new Point3d(x, y, 0),
            Position = new Point3d(x, y, 0)
        };
        space.AppendEntity(note);
        tr.AddNewlyCreatedDBObject(note, true);
    }

    private static string EnsureLayer(Transaction tr, LayerTable layers, HashSet<string> made, string name)
    {
        if (!made.Contains(name))
        {
            if (!layers.Has(name))
            {
                if (!layers.IsWriteEnabled)
                    layers.UpgradeOpen();
                var record = new LayerTableRecord { Name = name };
                layers.Add(record);
                tr.AddNewlyCreatedDBObject(record, true);
            }

            made.Add(name);
        }

        return name;
    }

    private static string LayerName(string code)
    {
        var raw = code.Trim();
        if (raw.Length == 0)
            return "TAB_KHAC";

        var sb = new StringBuilder("TAB_");
        foreach (var ch in raw)
            sb.Append("<>/\\\":;?*|=`".Contains(ch) ? '_' : ch);
        var name = sb.ToString().Trim();
        if (name.Length <= 4)
            return "TAB_KHAC";
        return name.Length > 255 ? name[..255] : name;
    }

    private static double SignedArea(List<(double X, double Y)> pts)
    {
        double area = 0;
        for (var i = 0; i < pts.Count; i++)
        {
            var j = (i + 1) % pts.Count;
            area += pts[i].X * pts[j].Y - pts[j].X * pts[i].Y;
        }

        return area / 2.0;
    }

    private static bool Nearly(double a, double b) => Math.Abs(a - b) < 1e-6;

    private sealed class TabImportStats
    {
        public int Hatches;
        public int Outlines;
        public int Labels;
        public int Skipped;
        public int Errors;
        public string? FirstError;
    }
}
