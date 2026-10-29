using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace iSurvey.Modules.DiemDo;

/// <summary>Chọn DBPoint trên bản vẽ. null nghĩa là người dùng hủy, danh sách rỗng nghĩa là không trúng điểm nào.</summary>
public static class PointSelection
{
    public static List<SurveyPoint>? CrossingWindow(Editor editor, Database db, string? layer)
    {
        var first = editor.GetPoint(new PromptPointOptions("\nGóc thứ nhất của vùng chữ nhật: ") { AllowNone = true });
        if (first.Status != PromptStatus.OK)
            return null;
        var second = editor.GetCorner("\nGóc đối diện: ", first.Value);
        if (second.Status != PromptStatus.OK)
            return null;
        var picked = editor.SelectCrossingWindow(first.Value, second.Value, Filter(layer));
        if (picked.Status != PromptStatus.OK || picked.Value is null)
            return [];
        return Read(db, picked.Value);
    }

    public static List<SurveyPoint>? CrossingPolyline(Editor editor, Database db, string? layer)
    {
        var options = new PromptEntityOptions("\nChọn polyline kín: ") { AllowNone = true };
        options.SetRejectMessage("\nChỉ nhận polyline.");
        options.AddAllowedClass(typeof(Polyline), exactMatch: true);
        var picked = editor.GetEntity(options);
        if (picked.Status != PromptStatus.OK)
            return null;

        Point3dCollection? polygon = null;
        var closed = false;
        using (var tr = db.TransactionManager.StartTransaction())
        {
            if (tr.GetObject(picked.ObjectId, OpenMode.ForRead) is Polyline polyline && polyline.NumberOfVertices >= 3)
            {
                var first = polyline.GetPoint2dAt(0);
                var last = polyline.GetPoint2dAt(polyline.NumberOfVertices - 1);
                closed = polyline.Closed || first.GetDistanceTo(last) <= 1e-6;
                if (closed)
                {
                    polygon = new Point3dCollection();
                    for (var i = 0; i < polyline.NumberOfVertices; i++)
                        polygon.Add(polyline.GetPoint3dAt(i));
                }
            }

            tr.Commit();
        }

        if (!closed || polygon is null)
            throw new InvalidOperationException("Polyline phải kín.");

        var result = editor.SelectCrossingPolygon(polygon, Filter(layer));
        if (result.Status != PromptStatus.OK || result.Value is null)
            return [];
        return Read(db, result.Value);
    }

    public static List<SurveyPoint>? Direct(Editor editor, Database db, string? layer)
    {
        var options = new PromptSelectionOptions { MessageForAdding = "\nChọn điểm đo: " };
        var picked = editor.GetSelection(options, Filter(layer));
        if (picked.Status != PromptStatus.OK || picked.Value is null)
            return null;
        return Read(db, picked.Value);
    }

    public static string? LayerOfEntity(Editor editor, Database db)
    {
        var options = new PromptEntityOptions("\nChọn một đối tượng để lấy layer: ") { AllowNone = true };
        var picked = editor.GetEntity(options);
        if (picked.Status != PromptStatus.OK)
            return null;
        using var tr = db.TransactionManager.StartTransaction();
        var entity = (Entity)tr.GetObject(picked.ObjectId, OpenMode.ForRead);
        var layer = entity.Layer;
        tr.Commit();
        return layer;
    }

    private static SelectionFilter Filter(string? layer)
    {
        var values = new List<TypedValue> { new((int)DxfCode.Start, "POINT") };
        if (!string.IsNullOrWhiteSpace(layer))
            values.Add(new TypedValue((int)DxfCode.LayerName, layer));
        return new SelectionFilter(values.ToArray());
    }

    private static List<SurveyPoint> Read(Database db, SelectionSet set)
    {
        var points = new List<SurveyPoint>();
        using var tr = db.TransactionManager.StartTransaction();
        foreach (SelectedObject item in set)
        {
            if (item is null || tr.GetObject(item.ObjectId, OpenMode.ForRead) is not DBPoint dbPoint)
                continue;
            PointDrawService.ReadTags(dbPoint, out var name, out var code);
            points.Add(new SurveyPoint
            {
                Name = name,
                X = dbPoint.Position.X,
                Y = dbPoint.Position.Y,
                Z = dbPoint.Position.Z,
                Code = code
            });
        }

        tr.Commit();
        return points;
    }
}
