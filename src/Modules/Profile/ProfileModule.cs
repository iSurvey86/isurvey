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

        ProfileDrawOptions options;
        ProfileDialogAction action;
        Point3d insert;

        if (seeded)
        {
            var route = routes[0];
            options = ProfileSettingsStore.Load();
            options.RouteId = route.Id;
            options.ProfileName = "MCD " + route.Name;
            var samples = route.Stations!
                .Where(s => s.HasZ)
                .Select(s => new ProfileSampleService.SamplePoint
                {
                    Chainage = s.Chainage,
                    X = s.X,
                    Y = s.Y,
                    Z = s.Z,
                    HasZ = true,
                    IsStation = true,
                    Name = s.Name
                })
                .ToList();
            var bandH = ProfileBandRow.TotalHeight(options.Rows);
            options.Datum = ProfileDrawService.SuggestDatum(
                samples, options.HorizontalScale, options.VerticalScale, bandH);
            action = ProfileDialogAction.Draw;
            insert = Point3d.Origin;
            ed.WriteMessage("\n[iSurvey] Thử nhanh: vẽ tại gốc (0,0), mức " + options.Datum.ToString("0.###") + ".");
        }
        else
        {
            DrawProfileDialogResult? dialog;
            try
            {
                dialog = DrawProfileWindow.Prompt(routes, RouteContext.TakeRoute());
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n[iSurvey] Không mở được hộp thoại: " + ex.Message);
                return;
            }

            if (dialog is null || dialog.Action == ProfileDialogAction.Cancel || dialog.Options is null)
            {
                ed.WriteMessage("\n[iSurvey] Đã hủy vẽ mặt cắt.");
                return;
            }

            options = dialog.Options;
            action = dialog.Action;
            ProfileSettingsStore.Save(options);

            if (action == ProfileDialogAction.Draw)
            {
                // Luôn pick vị trí; nếu đã có MCD thì xóa bản cũ rồi vẽ tại điểm mới.
                var pOpts = new PromptPointOptions("\niSurvey: Chỉ góc dưới-trái cột tên (đáy bảng) trên Model: ")
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
            else
            {
                insert = Point3d.Origin;
            }
        }

        try
        {
            switch (action)
            {
                case ProfileDialogAction.Draw:
                    RunDraw(doc, ed, routes, options, insert);
                    break;
                case ProfileDialogAction.UpdateFromProfile:
                case ProfileDialogAction.UpdateProperties:
                case ProfileDialogAction.UpdateFromTin:
                    RunUpdate(doc, ed, routes, options, action);
                    break;
            }
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage("\n[iSurvey] Lỗi mặt cắt dọc: " + ex.Message);
            ActivityLogger.TrackError(ex, "ISURVEY_PROFILE_DRAW");
        }
    }

    private static void RunDraw(
        Autodesk.AutoCAD.ApplicationServices.Document doc,
        Editor ed,
        List<RouteRecord> routes,
        ProfileDrawOptions options,
        Point3d insert)
    {
        var targets = SelectRoutes(routes, options);
        if (targets.Count == 0)
        {
            ed.WriteMessage("\n[iSurvey] Không có tuyến phù hợp để vẽ.");
            return;
        }

        var existingProfiles = ProfileStore.Load(doc.Database);
        var origin = insert;
        var drawn = 0;
        foreach (var route in targets)
        {
            ProfileRecord? existing = null;
            foreach (var p in existingProfiles)
            {
                if (string.Equals(p.RouteId, route.Id, StringComparison.OrdinalIgnoreCase))
                {
                    existing = p;
                    break;
                }
            }

            // Đã có bản → xóa rồi vẽ lại tại điểm vừa pick (không chồng bản).
            var profile = ProfileDrawService.Draw(
                doc, route, options, origin,
                existingId: existing?.Id,
                eraseHandles: existing?.EntityHandles);
            ProfileStore.Save(doc.Database, profile);
            if (existing is not null)
            {
                ed.WriteMessage("\n[iSurvey] Đã cập nhật «" + profile.Name + "». " + profile.Summary());
                ActivityLogger.Result("ISURVEY_PROFILE_DRAW", "redraw " + profile.Name + " " + profile.Summary());
            }
            else
            {
                ed.WriteMessage(
                    "\n[iSurvey] Đã vẽ «" + profile.Name + "». "
                    + profile.Summary()
                    + " Rải khung bằng RAIMCD tại góc dưới-trái bảng.");
                ActivityLogger.Result("ISURVEY_PROFILE_DRAW", profile.Name + " " + profile.Summary());
            }

            drawn++;
            if (!options.DrawMultipleRoutes || drawn >= targets.Count)
                continue;

            // Dàn trang xuống: insert tiếp theo sao cho đỉnh MCD sau ≈ Y_min_trước − 50.
            var yMin = ProfileDrawService.GetProfileYMin(doc, profile.EntityHandles, profile.InsertY);
            var nextBandH = ProfileBandRow.TotalHeight(options.Rows);
            // Dự phòng chiều cao đất (cùng pad cũ 200) để MCD sau không chồng lên.
            var nextHeightGuess = nextBandH + 200;
            origin = new Point3d(insert.X, yMin - ProfileDrawService.BatchRouteGapY - nextHeightGuess, 0);
        }

        if (drawn == 0)
            ed.WriteMessage("\n[iSurvey] Không vẽ được mặt cắt nào.");
        else if (options.DrawMultipleRoutes && drawn > 1)
            ed.WriteMessage("\n[iSurvey] Đã vẽ " + drawn + " mặt cắt (dàn trang Y_min − 50).");
    }

    private static void RunUpdate(
        Autodesk.AutoCAD.ApplicationServices.Document doc,
        Editor ed,
        List<RouteRecord> routes,
        ProfileDrawOptions options,
        ProfileDialogAction action)
    {
        var profiles = ProfileStore.Load(doc.Database);
        if (profiles.Count == 0)
        {
            ed.WriteMessage("\n[iSurvey] Chưa có mặt cắt để cập nhật. Hãy Vẽ trắc dọc trước.");
            return;
        }

        ProfileRecord? target = null;
        foreach (var p in profiles)
        {
            if (string.Equals(p.RouteId, options.RouteId, StringComparison.OrdinalIgnoreCase))
            {
                target = p;
                break;
            }
        }

        target ??= profiles[profiles.Count - 1];

        var route = routes.FirstOrDefault(r => string.Equals(r.Id, target.RouteId, StringComparison.OrdinalIgnoreCase))
            ?? routes.FirstOrDefault(r => string.Equals(r.Id, options.RouteId, StringComparison.OrdinalIgnoreCase));
        if (route is null)
        {
            ed.WriteMessage("\n[iSurvey] Không tìm thấy tuyến của mặt cắt «" + target.Name + "».");
            return;
        }

        // UpdateProperties: giữ tham số cũ về tỷ lệ/datum nếu form chỉ đổi mẫu bảng — vẫn dùng options từ form.
        // UpdateFromTin: ép lấy mẫu lại (Draw luôn Collect từ TIN).
        // UpdateFromProfile: vẽ lại cùng điểm chèn, xóa handle cũ.
        if (action == ProfileDialogAction.UpdateProperties)
        {
            // Giữ điểm chèn + id; áp options mới từ form.
        }
        else if (action == ProfileDialogAction.UpdateFromProfile)
        {
            // Đồng bộ options từ profile cũ rồi ghi đè các tùy chọn form (đã có trong options).
            options.Datum = target.Datum;
        }

        var insert = new Point3d(target.InsertX, target.InsertY, 0);
        var profile = ProfileDrawService.Draw(
            doc, route, options, insert,
            existingId: target.Id,
            eraseHandles: target.EntityHandles);
        ProfileStore.Save(doc.Database, profile);
        ed.WriteMessage("\n[iSurvey] Đã cập nhật «" + profile.Name + "». " + profile.Summary());
        ActivityLogger.Result("ISURVEY_PROFILE_DRAW", "update " + profile.Name + " " + profile.Summary());
    }

    private static List<RouteRecord> SelectRoutes(List<RouteRecord> routes, ProfileDrawOptions options)
    {
        if (options.DrawMultipleRoutes)
            return routes.Where(r => r.Stations is { Count: > 0 }).ToList();

        var one = routes.FirstOrDefault(r => string.Equals(r.Id, options.RouteId, StringComparison.OrdinalIgnoreCase));
        return one is null ? new List<RouteRecord>() : new List<RouteRecord> { one };
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
