using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.GraphicsInterface;

namespace iSurvey.Modules.Profile;

/// <summary>
/// Kiểu chữ Unicode mặc định cho đầu bảng trắc dọc.
/// Arial có trên mọi Windows và hỗ trợ tiếng Việt (UTF-16 trong DBText).
/// </summary>
public static class ProfileTextStyles
{
    public const string StyleName = "iSurvey_TD";

    public const string WindowsFont = "Arial";

    public static ObjectId Ensure(Transaction tr, Database db)
    {
        var table = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
        if (table.Has(StyleName))
        {
            var existing = (TextStyleTableRecord)tr.GetObject(table[StyleName], OpenMode.ForRead);
            if (!string.Equals(existing.Font.TypeFace, WindowsFont, StringComparison.OrdinalIgnoreCase))
            {
                existing.UpgradeOpen();
                existing.Font = new FontDescriptor(WindowsFont, false, false, 0, 0);
            }

            return table[StyleName];
        }

        table.UpgradeOpen();
        var created = new TextStyleTableRecord
        {
            Name = StyleName,
            Font = new FontDescriptor(WindowsFont, false, false, 0, 0)
        };
        var id = table.Add(created);
        tr.AddNewlyCreatedDBObject(created, true);
        return id;
    }

    /// <summary>Đảm bảo style tồn tại trên bản vẽ đang mở (gọi từ UI).</summary>
    public static void EnsureInActiveDocument()
    {
        try
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc is null)
                return;
            using (doc.LockDocument())
            using (var tr = doc.TransactionManager.StartTransaction())
            {
                Ensure(tr, doc.Database);
                tr.Commit();
            }
        }
        catch
        {
            // bỏ qua nếu chưa có bản vẽ
        }
    }
}
