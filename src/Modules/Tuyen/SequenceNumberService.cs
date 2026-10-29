using System.Runtime.InteropServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using iSurvey.Auth;
using iSurvey.UI.Sequence;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcadColor = Autodesk.AutoCAD.Colors.Color;
using ColorMethod = Autodesk.AutoCAD.Colors.ColorMethod;

namespace iSurvey.Modules.Tuyen;

/// <summary>STT: đánh số thứ tự tại từng điểm click. Thay dstt.lsp.</summary>
public sealed class SequenceNumberService
{
    private const string LayerName = iSurvey.iSurveyLayers.SoThuTu;
    private const int WipeoutSides = 32;
    private const double RadiusFactor = 1.15;

    // acedUsrBrk vừa đọc vừa xóa cờ Esc. acedEatCommandThroat xóa Enter còn sót sau khi đóng hộp thoại.
    [DllImport("accore.dll", EntryPoint = "acedUsrBrk", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool ClearUserBreak();

    [DllImport("accore.dll", EntryPoint = "acedEatCommandThroat", CallingConvention = CallingConvention.Cdecl)]
    private static extern int EatPendingInput();

    [CommandMethod("STT")]
    public void SequenceCommand()
    {
        if (!AuthManager.EnsureAuthenticated("STT"))
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        var request = SequenceWindow.Prompt();
        if (request is null)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy.");
            return;
        }

        try
        {
            AcadApp.SetSystemVariable("WIPEOUTFRAME", 2);
        }
        catch
        {
            // bản CAD không có biến này vẫn đặt số được
        }

        // Đóng hộp thoại để lại Esc hoặc Enter. Nếu không xóa, GetPoint nhận ngay và lệnh thoát.
        try
        {
            ClearUserBreak();
            EatPendingInput();
        }
        catch
        {
            // bản CAD không xuất hàm này thì vòng lặp bên dưới bỏ qua tín hiệu đến trong 400 ms
        }

        var placed = 0;
        var number = request.Start;
        var ed = doc.Editor;
        var skippedPhantom = false;
        while (true)
        {
            var label = request.Prefix + number + request.Suffix;
            var ppo = new PromptPointOptions($"\niSurvey: Click điểm chèn {label} (Esc để thoát): ")
            {
                AllowNone = true
            };
            var asked = DateTime.UtcNow;
            var ppr = ed.GetPoint(ppo);
            // Đóng hộp thoại để lại Enter/Esc trong hàng đợi, GetPoint nhận ngay và thoát.
            if (!skippedPhantom
                && ppr.Status != PromptStatus.OK
                && (DateTime.UtcNow - asked).TotalMilliseconds < 400)
            {
                skippedPhantom = true;
                continue;
            }

            if (ppr.Status != PromptStatus.OK)
                break;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var space = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);
                EnsureLayer(tr, doc.Database);
                var radius = request.Height * RadiusFactor;
                if (request.UseWipeout)
                    AddWipeout(tr, space, ppr.Value, radius);
                if (request.DrawCircle)
                    AddCircle(tr, space, ppr.Value, radius);
                var styleId = EnsureTextStyle(tr, doc.Database, request);
                var textId = AddText(tr, space, ppr.Value, label, request.Height, request.WidthFactor, styleId);
                var order = (DrawOrderTable)tr.GetObject(space.DrawOrderTableId, OpenMode.ForWrite);
                order.MoveToTop(new ObjectIdCollection { textId });
                tr.Commit();
            }

            placed++;
            number += request.Step;
        }

        ed.WriteMessage(placed == 0
            ? "\n[iSurvey] Đã hủy."
            : $"\n[iSurvey] Đã ghi {placed} số trên layer {LayerName}.");
        if (placed > 0)
            ActivityLogger.Result("STT", $"đã ghi {placed} số trên layer {LayerName}");
    }

    private static void EnsureLayer(Transaction tr, Database db)
    {
        var layers = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (layers.Has(LayerName))
            return;
        layers.UpgradeOpen();
        var record = new LayerTableRecord
        {
            Name = LayerName,
            Color = AcadColor.FromColorIndex(ColorMethod.ByAci, 4),
            IsPlottable = true
        };
        layers.Add(record);
        tr.AddNewlyCreatedDBObject(record, true);
    }

    private static void AddWipeout(Transaction tr, BlockTableRecord space, Point3d center, double radius)
    {
        var pts = new Point2dCollection();
        for (var i = 0; i < WipeoutSides; i++)
        {
            var angle = 2 * Math.PI * i / WipeoutSides;
            pts.Add(new Point2d(center.X + radius * Math.Cos(angle), center.Y + radius * Math.Sin(angle)));
        }

        pts.Add(pts[0]);
        var wipe = new Wipeout { Layer = LayerName };
        space.AppendEntity(wipe);
        tr.AddNewlyCreatedDBObject(wipe, true);
        wipe.SetFrom(pts, Vector3d.ZAxis);
    }

    private static void AddCircle(Transaction tr, BlockTableRecord space, Point3d center, double radius)
    {
        var circle = new Circle(center, Vector3d.ZAxis, radius) { Layer = LayerName };
        space.AppendEntity(circle);
        tr.AddNewlyCreatedDBObject(circle, true);
    }

    private static ObjectId EnsureTextStyle(Transaction tr, Database db, SequenceRequest request)
    {
        var table = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
        if (request.UseCurrentStyle)
        {
            if (request.StyleName.Length > 0 && table.Has(request.StyleName))
                return table[request.StyleName];
            return db.Textstyle;
        }

        var font = request.WindowsFont;
        var current = (TextStyleTableRecord)tr.GetObject(db.Textstyle, OpenMode.ForRead);
        if (string.Equals(current.Font.TypeFace, font, StringComparison.OrdinalIgnoreCase))
            return db.Textstyle;

        foreach (ObjectId id in table)
        {
            var record = (TextStyleTableRecord)tr.GetObject(id, OpenMode.ForRead);
            if (string.Equals(record.Font.TypeFace, font, StringComparison.OrdinalIgnoreCase))
                return id;
        }

        var name = StyleNameFor(font);
        var suffix = 2;
        while (table.Has(name))
            name = StyleNameFor(font) + suffix++;

        table.UpgradeOpen();
        var created = new TextStyleTableRecord
        {
            Name = name,
            Font = new Autodesk.AutoCAD.GraphicsInterface.FontDescriptor(font, false, false, 0, 0)
        };
        var createdId = table.Add(created);
        tr.AddNewlyCreatedDBObject(created, true);
        return createdId;
    }

    private static string StyleNameFor(string font)
    {
        var cleaned = new string(font.Where(ch => !@"<>/\"":;?*|,=`".Contains(ch)).ToArray()).Trim();
        if (cleaned.Length == 0)
            cleaned = "KS-STT";
        return cleaned.Length <= 255 ? cleaned : cleaned.Substring(0, 255);
    }

    private static ObjectId AddText(
        Transaction tr, BlockTableRecord space, Point3d center, string label, double height, double widthFactor, ObjectId styleId)
    {
        var text = new DBText
        {
            Layer = LayerName,
            TextStyleId = styleId,
            TextString = label,
            Height = height,
            WidthFactor = widthFactor,
            HorizontalMode = TextHorizontalMode.TextCenter,
            VerticalMode = TextVerticalMode.TextVerticalMid,
            AlignmentPoint = center
        };
        space.AppendEntity(text);
        tr.AddNewlyCreatedDBObject(text, true);
        text.AlignmentPoint = center;
        return text.ObjectId;
    }
}
