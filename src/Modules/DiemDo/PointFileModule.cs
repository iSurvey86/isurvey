using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using iSurvey.Auth;
using iSurvey.UI.DiemDo;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Modules.DiemDo;

public sealed class PointFileModule
{
    [CommandMethod("NHAPDIEM")]
    public void ImportPointsCommand()
    {
        if (!AuthManager.EnsureAuthenticated("NHAPDIEM"))
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;
        if (!IsModelSpace())
        {
            doc.Editor.WriteMessage("\n[iSurvey] Hãy mở Model Space trước khi nhập điểm.");
            return;
        }

        ImportPointsRequest? request;
        try
        {
            request = ImportPointsWindow.Prompt();
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage($"\n[iSurvey] Không mở được hộp thoại: {ex.Message}");
            return;
        }

        if (request is null)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy nhập điểm.");
            return;
        }

        try
        {
            var made = PointDrawService.Draw(doc, request.Points, request.TextHeight);
            doc.Editor.WriteMessage($"\n[iSurvey] Đã tạo {made} điểm trên layer {iSurveyLayers.DiemDo}.");
            if (made > 0)
            {
                ZoomExtents(doc.Editor);
                ActivityLogger.Result("NHAPDIEM", $"đã tạo {made} điểm trên layer {iSurveyLayers.DiemDo}");
            }
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage($"\n[iSurvey] Không tạo được điểm: {ex.Message}");
        }
    }

    [CommandMethod("XUATDIEM")]
    public void ExportPointsCommand()
    {
        if (!AuthManager.EnsureAuthenticated("XUATDIEM"))
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        ExportPointsRequest? request;
        try
        {
            request = ExportPointsWindow.Prompt();
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage($"\n[iSurvey] Không mở được hộp thoại: {ex.Message}");
            return;
        }

        if (request is null)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy xuất điểm.");
            return;
        }

        doc.Editor.WriteMessage($"\n[iSurvey] Đã ghi {request.Count} điểm ra {request.FileName}.");
        ActivityLogger.Result("XUATDIEM", $"đã ghi {request.Count} điểm ra {request.FileName}");
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
                // Điểm đã tạo. Không chặn lệnh nếu không đổi được khung nhìn.
            }
        }
    }

    private static bool IsModelSpace()
    {
        try
        {
            if (Convert.ToInt16(AcadApp.GetSystemVariable("TILEMODE")) == 1)
                return true;
            return Convert.ToInt16(AcadApp.GetSystemVariable("MSPACE")) == 1;
        }
        catch
        {
            return true;
        }
    }
}
