using System.Globalization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace iSurvey.Modules.DiemDo;

/// <summary>Chọn Point, Block, Text, MText. null nghĩa là hủy, danh sách rỗng nghĩa là không trúng đối tượng.</summary>
public static class PointSelection
{
    private static readonly HashSet<string> NameTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "TEN", "NAME", "TENDIEM", "TEN_DIEM", "SOHIEU", "SO_HIEU", "SH", "ID", "DIEM", "SODIEM",
        "POINT", "POINTNAME", "POINT_NAME", "SOHIEUDIEM"
    };

    private static readonly HashSet<string> CodeTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "MA", "CODE", "KYHIEU", "KY_HIEU", "DESC", "MOTA", "DESCRIPTION", "LOAI"
    };

    public static List<SurveyPoint>? Select(Editor editor, Database db, string? layer)
    {
        var options = new PromptSelectionOptions
        {
            MessageForAdding = "\nChọn điểm, block hoặc chữ: "
        };
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
        var layerName = entity.Layer;
        tr.Commit();
        return layerName;
    }

    private static SelectionFilter Filter(string? layer)
    {
        var values = new List<TypedValue>();
        if (!string.IsNullOrWhiteSpace(layer))
            values.Add(new TypedValue((int)DxfCode.Operator, "<AND"));
        values.Add(new TypedValue((int)DxfCode.Start, "POINT,INSERT,TEXT,MTEXT"));
        if (!string.IsNullOrWhiteSpace(layer))
        {
            values.Add(new TypedValue((int)DxfCode.LayerName, layer));
            values.Add(new TypedValue((int)DxfCode.Operator, "AND>"));
        }

        return new SelectionFilter(values.ToArray());
    }

    private static List<SurveyPoint> Read(Database db, SelectionSet set)
    {
        var points = new List<SurveyPoint>();
        using var tr = db.TransactionManager.StartTransaction();
        foreach (SelectedObject item in set)
        {
            if (item is null)
                continue;
            var entity = tr.GetObject(item.ObjectId, OpenMode.ForRead);
            SurveyPoint? point = entity switch
            {
                DBPoint dbPoint => FromPoint(dbPoint),
                BlockReference block => FromBlock(tr, block),
                DBText text => FromText(text),
                MText mtext => FromMText(mtext),
                _ => null
            };
            if (point is not null)
                points.Add(point);
        }

        tr.Commit();
        return points;
    }

    private static SurveyPoint FromPoint(DBPoint dbPoint)
    {
        PointDrawService.ReadTags(dbPoint, out var name, out var code);
        return new SurveyPoint
        {
            Name = name,
            X = dbPoint.Position.X,
            Y = dbPoint.Position.Y,
            Z = dbPoint.Position.Z,
            Code = code
        };
    }

    private static SurveyPoint FromBlock(Transaction tr, BlockReference block)
    {
        var point = new SurveyPoint
        {
            X = block.Position.X,
            Y = block.Position.Y,
            Z = block.Position.Z
        };
        foreach (ObjectId id in block.AttributeCollection)
        {
            if (tr.GetObject(id, OpenMode.ForRead) is not AttributeReference attribute)
                continue;
            var value = (attribute.TextString ?? "").Trim();
            if (value.Length == 0)
                continue;
            var tag = Normalize(attribute.Tag);
            if (point.Name.Length == 0 && NameTags.Contains(tag))
                point.Name = value;
            else if (point.Code.Length == 0 && CodeTags.Contains(tag))
                point.Code = value;
        }

        return point;
    }

    private static SurveyPoint FromText(DBText text)
    {
        var anchor = text.HorizontalMode == TextHorizontalMode.TextLeft
            && text.VerticalMode == TextVerticalMode.TextBase
            ? text.Position
            : text.AlignmentPoint;
        return FromLabel(anchor, text.TextString);
    }

    private static SurveyPoint FromMText(MText text) => FromLabel(text.Location, text.Text);

    private static SurveyPoint FromLabel(Point3d anchor, string? raw)
    {
        var point = new SurveyPoint { X = anchor.X, Y = anchor.Y, Z = anchor.Z };
        var label = (raw ?? "").Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (TryElevation(label, out var elevation))
        {
            point.Z = elevation;
            return point;
        }

        point.Name = label;
        return point;
    }

    private static bool TryElevation(string text, out double value)
    {
        if (text.Length == 0)
        {
            value = 0;
            return false;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            return true;
        var normalized = text.Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static string Normalize(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return "";
        return tag.Replace(" ", "").Replace("-", "").ToUpperInvariant();
    }
}
