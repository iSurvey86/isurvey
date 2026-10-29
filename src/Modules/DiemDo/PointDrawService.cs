using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcadColor = Autodesk.AutoCAD.Colors.Color;
using ColorMethod = Autodesk.AutoCAD.Colors.ColorMethod;

namespace iSurvey.Modules.DiemDo;

/// <summary>Vẽ dấu điểm, tên và cao độ, mỗi cái một layer và một màu.</summary>
public static class PointDrawService
{
    private const string AppName = "ISURVEY_DIEM";

    public static int Draw(Autodesk.AutoCAD.ApplicationServices.Document doc, IReadOnlyList<SurveyPoint> points, double textHeight)
    {
        try
        {
            if (Convert.ToInt32(AcadApp.GetSystemVariable("PDMODE")) == 0)
                AcadApp.SetSystemVariable("PDMODE", 35);
        }
        catch
        {
            // điểm vẫn được tạo, chỉ có thể chưa hiện dấu
        }

        var made = 0;
        using (doc.LockDocument())
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            EnsureLayer(tr, doc.Database, iSurveyLayers.DiemDo, 2);
            EnsureLayer(tr, doc.Database, iSurveyLayers.DiemDoTen, 4);
            EnsureLayer(tr, doc.Database, iSurveyLayers.DiemDoCaoDo, 3);
            EnsureApp(tr, doc.Database);
            var space = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);
            var styleId = doc.Database.Textstyle;
            foreach (var point in points)
            {
                var position = new Point3d(point.X, point.Y, point.Z);
                AddPoint(tr, space, position, point);
                var gap = textHeight * 0.4;
                if (!string.IsNullOrWhiteSpace(point.Name))
                {
                    AddText(tr, space, styleId, iSurveyLayers.DiemDoTen,
                        new Point3d(position.X + gap, position.Y + textHeight * 0.2, position.Z),
                        point.Name, textHeight);
                }

                AddText(tr, space, styleId, iSurveyLayers.DiemDoCaoDo,
                    new Point3d(position.X + gap, position.Y - textHeight * 1.1, position.Z),
                    PointFileWriter.Number(point.Z, 3, new PointFileFormat('\0', decimalComma: false)),
                    textHeight);
                made++;
            }

            tr.Commit();
        }

        doc.Editor.Regen();
        return made;
    }

    private static void AddPoint(Transaction tr, BlockTableRecord space, Point3d position, SurveyPoint source)
    {
        var point = new DBPoint(position);
        point.SetDatabaseDefaults();
        point.Layer = iSurveyLayers.DiemDo;
        point.Color = AcadColor.FromColorIndex(ColorMethod.ByLayer, 256);
        point.Position = position;
        space.AppendEntity(point);
        tr.AddNewlyCreatedDBObject(point, true);
        point.XData = new ResultBuffer(
            new TypedValue((int)DxfCode.ExtendedDataRegAppName, AppName),
            new TypedValue((int)DxfCode.ExtendedDataAsciiString, Clip(source.Name)),
            new TypedValue((int)DxfCode.ExtendedDataAsciiString, Clip(source.Code)));
    }

    private static void AddText(Transaction tr, BlockTableRecord space, ObjectId styleId, string layer, Point3d position, string content, double height)
    {
        var text = new DBText();
        text.SetDatabaseDefaults();
        text.Layer = layer;
        text.Color = AcadColor.FromColorIndex(ColorMethod.ByLayer, 256);
        text.TextStyleId = styleId;
        text.TextString = content;
        text.Height = height;
        text.Position = position;
        space.AppendEntity(text);
        tr.AddNewlyCreatedDBObject(text, true);
    }

    private static void EnsureLayer(Transaction tr, Database db, string name, short aci)
    {
        var layers = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (layers.Has(name))
            return;
        layers.UpgradeOpen();
        var record = new LayerTableRecord
        {
            Name = name,
            Color = AcadColor.FromColorIndex(ColorMethod.ByAci, aci),
            IsPlottable = true
        };
        layers.Add(record);
        tr.AddNewlyCreatedDBObject(record, true);
    }

    private static void EnsureApp(Transaction tr, Database db)
    {
        var apps = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
        if (apps.Has(AppName))
            return;
        apps.UpgradeOpen();
        var record = new RegAppTableRecord { Name = AppName };
        apps.Add(record);
        tr.AddNewlyCreatedDBObject(record, true);
    }

    internal static string Clip(string? text)
    {
        var value = text ?? "";
        return value.Length <= 255 ? value : value.Substring(0, 255);
    }

    internal static void ReadTags(DBPoint point, out string name, out string code)
    {
        name = "";
        code = "";
        var data = point.GetXDataForApplication(AppName);
        if (data is null)
            return;
        using (data)
        {
            var texts = new List<string>();
            foreach (var item in data.AsArray())
            {
                if (item.TypeCode == (int)DxfCode.ExtendedDataAsciiString && item.Value is string text)
                    texts.Add(text);
            }

            if (texts.Count > 0)
                name = texts[0];
            if (texts.Count > 1)
                code = texts[1];
        }
    }
}
