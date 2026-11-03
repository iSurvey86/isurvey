using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using iSurvey.Auth;
using iSurvey.Modules.Tuyen;
using iSurvey.UI.Profile;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Modules.Profile;

public sealed class ProfileModule
{
    [CommandMethod("ISURVEY_PROFILE_DRAW")]
    public void DrawCommand() => Draw();

    [CommandMethod("VEMCD")]
    public void DrawAlias() => Draw();

    private static void Draw()
    {
        if (!AuthManager.EnsureAuthenticated("VEMCD"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;
        var ed = doc.Editor;
        if (doc.Database.TileMode == false)
        {
            ed.WriteMessage("\n[iSurvey] VEMCD chỉ vẽ trên Model Space.");
            return;
        }

        var seeded = TrySeedDemoRoute(doc, ed);

        List<RouteRecord> routes;
        try
        {
            routes = RouteStore.Load(doc.Database);
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage("\n[iSurvey] Không đọc được tuyến: " + ex.Message);
            return;
        }

        if (routes.Count == 0)
        {
            ed.WriteMessage("\n[iSurvey] Chưa có tuyến. Chạy TAOTUYEN rồi PHATSINHCOC trước.");
            return;
        }

        DrawProfileRequest? request;
        Point3d insert;
        if (seeded)
        {
            var route = routes[0];
            var datum = ProfileDrawService.SuggestDatum(
                route.Stations ?? new List<StationRecord>(), 5000, 500, ProfileDrawService.DefaultRowHeight);
            request = new DrawProfileRequest
            {
                Route = route,
                Name = "MCD " + route.Name,
                HorizontalScale = 5000,
                VerticalScale = 500,
                Datum = datum
            };
            insert = Point3d.Origin;
            ed.WriteMessage("\n[iSurvey] Thử nhanh: vẽ tại gốc (0,0), mức " + datum.ToString("0.###") + ".");
        }
        else
        {
            try
            {
                request = DrawProfileWindow.Prompt(routes, RouteContext.TakeRoute());
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n[iSurvey] Không mở được hộp thoại: " + ex.Message);
                return;
            }

            if (request is null)
            {
                ed.WriteMessage("\n[iSurvey] Đã hủy vẽ mặt cắt.");
                return;
            }

            var pOpts = new PromptPointOptions("\niSurvey: Chỉ góc dưới-trái cột tên (hàng cộng dồn) trên Model: ")
            {
                AllowNone = false
            };
            var pRes = ed.GetPoint(pOpts);
            if (pRes.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\n[iSurvey] Đã hủy — chưa chọn điểm chèn.");
                return;
            }

            insert = pRes.Value;
        }

        var withZ = request.Route.Stations?.Count(s => s.HasZ) ?? 0;
        if (withZ < 2)
        {
            ed.WriteMessage("\n[iSurvey] Tuyến chưa đủ cọc có Z. Gắn mô hình và chạy PHATSINHCOC.");
            return;
        }

        var segs = ProfileDrawService.PlanSegments(
            request.Route.Stations!,
            request.Datum,
            request.HorizontalScale,
            request.VerticalScale,
            ProfileDrawService.DefaultRowHeight);
        var tip = ProfileDrawService.FormatSuggestion(segs);
        if (!string.IsNullOrEmpty(tip))
            ed.WriteMessage("\n[iSurvey] " + tip);

        try
        {
            var profile = ProfileDrawService.Draw(
                doc,
                request.Route,
                request.Name,
                request.HorizontalScale,
                request.VerticalScale,
                request.Datum,
                insert,
                ProfileDrawService.DefaultLabelWidth,
                ProfileDrawService.DefaultRowHeight,
                ProfileDrawService.DefaultTextHeight);
            ProfileStore.Save(doc.Database, profile);
            ed.WriteMessage(
                "\n[iSurvey] Đã vẽ «" + profile.Name + "». "
                + profile.Summary()
                + " Rải khung bằng RAIMCD tại góc dưới-trái hàng cộng dồn.");
            ActivityLogger.Result("ISURVEY_PROFILE_DRAW", profile.Name + " " + profile.Summary());
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage("\n[iSurvey] Lỗi vẽ mặt cắt: " + ex.Message);
            ActivityLogger.TrackError(ex, "ISURVEY_PROFILE_DRAW");
        }
    }

    /// <summary>
    /// Chỉ khi có file cờ %TEMP%\isurvey-vemcd-seed.flag: tạo tuyến mẫu có Z để thử VEMCD trên bản vẽ trắng.
    /// </summary>
    private static bool TrySeedDemoRoute(Autodesk.AutoCAD.ApplicationServices.Document doc, Editor ed)
    {
        var flag = Path.Combine(Path.GetTempPath(), "isurvey-vemcd-seed.flag");
        if (!File.Exists(flag))
            return false;
        try
        {
            File.Delete(flag);
        }
        catch
        {
            return false;
        }

        try
        {
            if (RouteStore.Load(doc.Database).Count > 0)
                return true;

            ObjectId polyId;
            using (var tr = doc.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var space = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var pl = new Polyline();
                pl.AddVertexAt(0, new Point2d(0, 0), 0, 0, 0);
                pl.AddVertexAt(1, new Point2d(200, 50), 0, 0, 0);
                pl.AddVertexAt(2, new Point2d(450, 20), 0, 0, 0);
                pl.AddVertexAt(3, new Point2d(700, 80), 0, 0, 0);
                space.AppendEntity(pl);
                tr.AddNewlyCreatedDBObject(pl, true);
                polyId = pl.ObjectId;
                tr.Commit();
            }

            var route = new RouteRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "Tuyen thu VEMCD",
                PolylineHandle = polyId.Handle.ToString(),
                StartChainage = 0,
                Layer = "iSurvey_Tuyen",
                Stations =
                [
                    new StationRecord { Name = "C0", Chainage = 0, X = 0, Y = 0, Z = 12.5, HasZ = true },
                    new StationRecord { Name = "C1", Chainage = 100, X = 100, Y = 25, Z = 14.2, HasZ = true },
                    new StationRecord { Name = "C2", Chainage = 250, X = 250, Y = 40, Z = 18.0, HasZ = true },
                    new StationRecord { Name = "C3", Chainage = 450, X = 450, Y = 20, Z = 15.5, HasZ = true },
                    new StationRecord { Name = "C4", Chainage = 700, X = 700, Y = 80, Z = 22.0, HasZ = true }
                ]
            };
            RouteStore.Save(doc.Database, route);
            ed.WriteMessage("\n[iSurvey] Đã tạo tuyến mẫu để thử VEMCD.");
            return true;
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage("\n[iSurvey] Không tạo được tuyến mẫu: " + ex.Message);
            return false;
        }
    }
}
