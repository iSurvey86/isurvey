using System.Globalization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using iSurvey.UI.Terrain;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcadColor = Autodesk.AutoCAD.Colors.Color;
using ColorMethod = Autodesk.AutoCAD.Colors.ColorMethod;

namespace iSurvey.Modules.Terrain;

/// <summary>Gán cao độ (Elevation / Z) cho đường đồng mức 2D — lệnh GHISODM.</summary>
public static class ContourElevationService
{
    private const double ElevEps = 1e-9;
    public const string LabelLayer = "iSurvey_NhanDDM";

    /// <summary>True nếu polyline đã có cao độ dùng được cho TIN (Elevation hoặc Z đỉnh).</summary>
    public static bool HasElevation(Transaction tr, Entity entity, out double z)
    {
        z = 0;
        if (entity is Polyline polyline)
        {
            if (Math.Abs(polyline.Elevation) >= ElevEps)
            {
                z = polyline.Elevation;
                return true;
            }

            for (var i = 0; i < polyline.NumberOfVertices; i++)
            {
                var p = polyline.GetPoint3dAt(i);
                if (Math.Abs(p.Z) >= ElevEps)
                {
                    z = p.Z;
                    return true;
                }
            }

            return false;
        }

        if (entity is Polyline2d poly2)
        {
            if (Math.Abs(poly2.Elevation) >= ElevEps)
            {
                z = poly2.Elevation;
                return true;
            }

            foreach (ObjectId id in poly2)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is not Vertex2d v)
                    continue;
                if (Math.Abs(v.Position.Z) >= ElevEps)
                {
                    z = v.Position.Z;
                    return true;
                }
            }

            return false;
        }

        if (entity is Polyline3d poly3)
        {
            var any = false;
            foreach (ObjectId id in poly3)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is not PolylineVertex3d v)
                    continue;
                if (Math.Abs(v.Position.Z) < ElevEps)
                    continue;
                z = v.Position.Z;
                any = true;
                break;
            }

            return any;
        }

        return false;
    }

    public static int CountWithoutElevation(
        Autodesk.AutoCAD.ApplicationServices.Document doc,
        IEnumerable<string> handles)
    {
        var missing = 0;
        using var tr = doc.Database.TransactionManager.StartTransaction();
        foreach (var handle in handles)
        {
            if (string.IsNullOrWhiteSpace(handle))
                continue;
            if (!TryGetEntity(tr, doc.Database, handle, out var entity))
                continue;
            if (!HasElevation(tr, entity, out _))
                missing++;
        }

        tr.Commit();
        return missing;
    }

    public static int CountSelectionWithoutElevation(Transaction tr, SelectionSet set)
    {
        var missing = 0;
        foreach (SelectedObject item in set)
        {
            if (item is null)
                continue;
            if (tr.GetObject(item.ObjectId, OpenMode.ForRead) is not Entity entity || entity.IsErased)
                continue;
            if (entity is not Polyline and not Polyline2d and not Polyline3d)
                continue;
            if (!HasElevation(tr, entity, out _))
                missing++;
        }

        return missing;
    }

    public static void RunCommand()
    {
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        ContourElevOptions? options;
        try
        {
            options = ContourElevWindow.Prompt();
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Không mở được hộp thoại: " + ex.Message);
            return;
        }

        if (options is null)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy ghi số đồng mức.");
            return;
        }

        switch (options.Mode)
        {
            case ContourElevMode.FixedNumber:
                AssignByNumber(doc, options.BaseZ);
                break;
            case ContourElevMode.Sequential:
                AssignSequential(doc, options.BaseZ, options.Step, options.Increasing);
                break;
            default:
                AssignFromNearestText(doc);
                break;
        }
    }

    private static void AssignByNumber(Autodesk.AutoCAD.ApplicationServices.Document doc, double z)
    {
        var editor = doc.Editor;
        var selection = editor.GetSelection(
            new PromptSelectionOptions { MessageForAdding = "\nChọn đường đồng mức (polyline): " },
            new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "LWPOLYLINE,POLYLINE") }));
        if (selection.Status != PromptStatus.OK || selection.Value is null)
            return;

        var n = ApplyElevation(doc, selection.Value, _ => z, label: true);
        editor.WriteMessage("\n[iSurvey] Đã gán Z=" + FormatZ(z) + " cho " + n + " đường đồng mức.");
    }

    private static void AssignFromNearestText(Autodesk.AutoCAD.ApplicationServices.Document doc)
    {
        var editor = doc.Editor;
        editor.WriteMessage("\n[iSurvey] Chọn đường đồng mức và chữ số cao độ (TEXT/MTEXT).");
        var selection = editor.GetSelection(
            new PromptSelectionOptions { MessageForAdding = "\nChọn polyline + chữ cao độ: " },
            new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Operator, "<OR"),
                new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
                new TypedValue((int)DxfCode.Start, "POLYLINE"),
                new TypedValue((int)DxfCode.Start, "TEXT"),
                new TypedValue((int)DxfCode.Start, "MTEXT"),
                new TypedValue((int)DxfCode.Operator, "OR>")
            }));
        if (selection.Status != PromptStatus.OK || selection.Value is null)
            return;

        var labels = new List<(Point3d At, double Z)>();
        var polys = new List<ObjectId>();
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            foreach (SelectedObject item in selection.Value)
            {
                if (item is null)
                    continue;
                var ent = tr.GetObject(item.ObjectId, OpenMode.ForRead);
                if (ent is Polyline or Polyline2d or Polyline3d)
                    polys.Add(item.ObjectId);
                else if (ent is Entity textEnt && TryReadNumericLabel(textEnt, out var at, out var z))
                    labels.Add((at, z));
            }

            tr.Commit();
        }

        if (polys.Count == 0)
        {
            editor.WriteMessage("\n[iSurvey] Chưa chọn đường đồng mức nào.");
            return;
        }

        if (labels.Count == 0)
        {
            editor.WriteMessage("\n[iSurvey] Chưa có chữ số cao độ trong lựa chọn. Chạy lại GHISODM → Số cố định hoặc Click tuần tự.");
            return;
        }

        var assigned = 0;
        var skipped = 0;
        using (doc.LockDocument())
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            EnsureLabelLayer(tr, doc.Database);
            foreach (var id in polys)
            {
                if (tr.GetObject(id, OpenMode.ForWrite) is not Entity entity || entity.IsErased)
                    continue;
                if (!TryClosestLabel(tr, entity, labels, out var z, out var at))
                {
                    skipped++;
                    continue;
                }

                if (!SetElevation(tr, entity, z))
                {
                    skipped++;
                    continue;
                }

                PlaceLabel(tr, doc.Database, at, z);
                assigned++;
            }

            tr.Commit();
        }

        editor.WriteMessage(
            "\n[iSurvey] Đã gán cao độ từ chữ gần nhất: " + assigned + " đường."
            + (skipped > 0 ? " Bỏ qua " + skipped + "." : ""));
    }

    private static void AssignSequential(
        Autodesk.AutoCAD.ApplicationServices.Document doc,
        double startZ,
        double step,
        bool increasing)
    {
        var editor = doc.Editor;
        if (step <= 0)
            step = 5;
        var z = startZ;
        editor.WriteMessage(
            "\n[iSurvey] Click lần lượt từng đường đồng mức (Z bắt đầu "
            + FormatZ(z) + ", bước " + FormatZ(step)
            + (increasing ? " tăng" : " giảm") + "). Enter/ESC để xong.");
        var n = 0;
        using (doc.LockDocument())
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            EnsureLabelLayer(tr, doc.Database);
            while (true)
            {
                var peo = new PromptEntityOptions("\nChọn đường đồng mức: ")
                {
                    AllowNone = true
                };
                peo.SetRejectMessage("\nChỉ chọn polyline.");
                peo.AddAllowedClass(typeof(Polyline), exactMatch: false);
                peo.AddAllowedClass(typeof(Polyline2d), exactMatch: false);
                peo.AddAllowedClass(typeof(Polyline3d), exactMatch: false);
                var per = editor.GetEntity(peo);
                if (per.Status == PromptStatus.None || per.Status == PromptStatus.Cancel)
                    break;
                if (per.Status != PromptStatus.OK)
                    continue;

                if (tr.GetObject(per.ObjectId, OpenMode.ForWrite) is not Entity entity || entity.IsErased)
                    continue;
                if (!SetElevation(tr, entity, z))
                {
                    editor.WriteMessage("\n[iSurvey] Không gán được đường này.");
                    continue;
                }

                PlaceLabel(tr, doc.Database, per.PickedPoint, z);
                n++;
                editor.WriteMessage("\n[iSurvey] Z=" + FormatZ(z));
                z = increasing ? z + step : z - step;
            }

            tr.Commit();
        }

        editor.WriteMessage("\n[iSurvey] Đã gán tuần tự " + n + " đường đồng mức.");
    }

    private static int ApplyElevation(
        Autodesk.AutoCAD.ApplicationServices.Document doc,
        SelectionSet set,
        Func<Entity, double> elevOf,
        bool label)
    {
        var n = 0;
        using (doc.LockDocument())
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            if (label)
                EnsureLabelLayer(tr, doc.Database);
            foreach (SelectedObject item in set)
            {
                if (item is null)
                    continue;
                if (tr.GetObject(item.ObjectId, OpenMode.ForWrite) is not Entity entity || entity.IsErased)
                    continue;
                var z = elevOf(entity);
                if (!SetElevation(tr, entity, z))
                    continue;
                if (label)
                    PlaceLabel(tr, doc.Database, ApproximateCenter(tr, entity), z);
                n++;
            }

            tr.Commit();
        }

        return n;
    }

    public static bool SetElevation(Transaction tr, Entity entity, double z)
    {
        var ok = false;
        if (entity is Polyline polyline)
        {
            polyline.Elevation = z;
            ok = true;
        }
        else if (entity is Polyline2d poly2)
        {
            poly2.Elevation = z;
            ok = true;
        }
        else if (entity is Polyline3d poly3)
        {
            foreach (ObjectId id in poly3)
            {
                if (tr.GetObject(id, OpenMode.ForWrite) is not PolylineVertex3d v)
                    continue;
                var p = v.Position;
                v.Position = new Point3d(p.X, p.Y, z);
            }

            ok = true;
        }

        if (ok)
            entity.Color = AcadColor.FromColorIndex(ColorMethod.ByAci, 6); // Magenta — đã gán
        return ok;
    }

    private static bool TryClosestLabel(
        Transaction tr,
        Entity entity,
        List<(Point3d At, double Z)> labels,
        out double z,
        out Point3d labelAt)
    {
        z = 0;
        labelAt = Point3d.Origin;
        Curve? curve = entity as Curve;
        if (curve is null)
            return false;

        var best = double.PositiveInfinity;
        var found = false;
        foreach (var (at, elev) in labels)
        {
            try
            {
                var on = curve.GetClosestPointTo(new Point3d(at.X, at.Y, curve.StartPoint.Z), false);
                var dx = on.X - at.X;
                var dy = on.Y - at.Y;
                var d = dx * dx + dy * dy;
                if (d >= best)
                    continue;
                best = d;
                z = elev;
                labelAt = at;
                found = true;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception)
            {
                // bỏ chữ không chiếu được lên đường
            }
        }

        return found;
    }

    private static bool TryReadNumericLabel(Entity entity, out Point3d at, out double z)
    {
        at = Point3d.Origin;
        z = 0;
        string raw;
        if (entity is DBText text)
        {
            raw = text.TextString ?? "";
            at = text.HorizontalMode == TextHorizontalMode.TextLeft
                && text.VerticalMode == TextVerticalMode.TextBase
                ? text.Position
                : text.AlignmentPoint;
        }
        else if (entity is MText mtext)
        {
            raw = mtext.Contents ?? mtext.Text ?? "";
            // Bỏ mã format MTEXT đơn giản.
            raw = System.Text.RegularExpressions.Regex.Replace(raw, @"\\[A-Za-z][^;]*;", "");
            raw = raw.Replace("{", "").Replace("}", "");
            at = mtext.Location;
        }
        else
            return false;

        return TryParseElevation(raw, out z);
    }

    private static bool TryParseElevation(string text, out double value)
    {
        var label = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (label.Length == 0)
        {
            value = 0;
            return false;
        }

        if (double.TryParse(label, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            return true;
        if (double.TryParse(label, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            return true;
        // Lấy số đầu trong chuỗi (vd. "12.5 m").
        var span = label.AsSpan();
        var start = -1;
        for (var i = 0; i < span.Length; i++)
        {
            var c = span[i];
            if (c is '-' or '+' || char.IsDigit(c))
            {
                start = i;
                break;
            }
        }

        if (start < 0)
        {
            value = 0;
            return false;
        }

        var end = start + 1;
        var seenDot = false;
        while (end < span.Length)
        {
            var c = span[end];
            if (char.IsDigit(c))
            {
                end++;
                continue;
            }

            if ((c == '.' || c == ',') && !seenDot)
            {
                seenDot = true;
                end++;
                continue;
            }

            break;
        }

        var token = label.Substring(start, end - start).Replace(',', '.');
        return double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static Point3d ApproximateCenter(Transaction tr, Entity entity)
    {
        if (entity is Curve curve)
        {
            try
            {
                var mid = (curve.StartParam + curve.EndParam) * 0.5;
                return curve.GetPointAtParameter(mid);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception)
            {
                return curve.StartPoint;
            }
        }

        return Point3d.Origin;
    }

    private static void EnsureLabelLayer(Transaction tr, Database db)
    {
        var table = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (table.Has(LabelLayer))
            return;
        table.UpgradeOpen();
        var layer = new LayerTableRecord
        {
            Name = LabelLayer,
            Color = AcadColor.FromColorIndex(ColorMethod.ByAci, 5)
        };
        table.Add(layer);
        tr.AddNewlyCreatedDBObject(layer, true);
    }

    private static void PlaceLabel(Transaction tr, Database db, Point3d at, double z)
    {
        var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
        var text = new DBText();
        text.SetDatabaseDefaults();
        text.Layer = LabelLayer;
        text.Height = 1.0;
        text.TextString = FormatZ(z);
        text.Color = AcadColor.FromColorIndex(ColorMethod.ByAci, 5);
        text.Justify = AttachmentPoint.MiddleCenter;
        text.AlignmentPoint = new Point3d(at.X, at.Y, 0);
        space.AppendEntity(text);
        tr.AddNewlyCreatedDBObject(text, true);
    }

    private static bool TryGetEntity(Transaction tr, Database db, string handle, out Entity entity)
    {
        entity = null!;
        if (!long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            return false;
        var id = db.GetObjectId(false, new Handle(value), 0);
        if (!id.IsValid || id.IsErased)
            return false;
        if (tr.GetObject(id, OpenMode.ForRead) is not Entity ent || ent.IsErased)
            return false;
        entity = ent;
        return true;
    }

    private static string FormatZ(double z) =>
        z.ToString("0.###", CultureInfo.CurrentCulture);
}
