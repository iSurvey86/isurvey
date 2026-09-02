using Autodesk.AutoCAD.DatabaseServices;

namespace iSurvey.Modules.Map;

/// <summary>Xóa toàn bộ tile iSurvey (và wipeout thử nghiệm cũ nếu còn).</summary>
public static class RasterDeleteService
{
    public static int DeleteAll(Database db) => RasterClearService.ClearAll(db);

    /// <summary>Giữ tương thích lệnh cũ.</summary>
    public enum DeleteScope
    {
        All
    }

    public static int Delete(Database db, DeleteScope scope)
        => scope == DeleteScope.All ? DeleteAll(db) : 0;
}
