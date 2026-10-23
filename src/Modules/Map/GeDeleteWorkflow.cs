using iSurvey.Auth;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace iSurvey.Modules.Map;

/// <summary>Xóa toàn bộ tile Google Earth — hỏi xác nhận Yes/No.</summary>
public static class GeDeleteWorkflow
{
    public static void Run(Document doc)
    {
        var ed = doc.Editor;
        var db = doc.Database;

        if (!MapModule.IsModelSpace(db))
        {
            ed.WriteMessage("\n[iSurvey] Hãy mở tab Model hoặc vào Model Space trong viewport (lệnh MSPACE).");
            return;
        }

        var confirm = System.Windows.MessageBox.Show(
            "Bạn có chắc chắn muốn xóa toàn bộ ảnh Google Earth (iSurvey) trong bản vẽ?",
            "iSurvey — Xóa ảnh",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question,
            System.Windows.MessageBoxResult.No);

        if (confirm != System.Windows.MessageBoxResult.Yes)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy xóa ảnh.");
            ActivityLogger.Result("ISURVEY_DELETE_GE", "đã hủy, chưa xóa, bản vẽ " + ActivityLogger.FileName(db.Filename));
            return;
        }

        try
        {
            var count = RasterDeleteService.DeleteAll(db);
            BasemapSession.Deactivate();
            AutoRefreshController.ResetViewSignature();

            if (count > 0)
                ed.Regen();

            ed.WriteMessage(count > 0
                ? $"\n[iSurvey] Đã xóa {count} tile GE. AutoRefresh đã tắt."
                : "\n[iSurvey] Không có tile iSurvey trong bản vẽ.");
            ActivityLogger.Result("ISURVEY_DELETE_GE",
                count > 0
                    ? $"đã xóa {count} ảnh tile, bản vẽ {ActivityLogger.FileName(db.Filename)}"
                    : $"không có ảnh để xóa, bản vẽ {ActivityLogger.FileName(db.Filename)}");
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] Lỗi xóa tile: {ex.Message}");
        }
    }
}
