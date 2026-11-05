using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using iSurvey.Auth;
using iSurvey.Modules.Terrain;
using iSurvey.UI.Palette;
using iSurvey.UI.Tuyen;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Modules.Tuyen;

public sealed class RouteModule
{
    [CommandMethod("ISURVEY_ROUTE_NEW")]
    public void RouteNewCommand() => New();

    [CommandMethod("TAOTUYEN")]
    public void RouteNewAlias() => New();

    [CommandMethod("ISURVEY_ROUTE_STATIONS")]
    public void StationsCommand() => Stations();

    [CommandMethod("PHATSINHCOC")]
    public void StationsAlias() => Stations();

    [CommandMethod("ISURVEY_ROUTE_CLEAR_STATIONS")]
    public void ClearStationsCommand() => ClearStations();

    [CommandMethod("XOACOC")]
    public void ClearStationsAlias() => ClearStations();

    [CommandMethod("ISURVEY_ROUTE_REVERSE")]
    public void ReverseCommand() => Reverse();

    [CommandMethod("DAODAUTUYEN")]
    public void ReverseAlias() => Reverse();

    [CommandMethod("ISURVEY_STATION_STYLE")]
    public void StationStyleCommand() => StationStyle();

    [CommandMethod("THIETLAPCOC")]
    public void StationStyleAlias() => StationStyle();

    [CommandMethod("ISURVEY_STATION_REMOVE_ONE")]
    public void RemoveOneCommand() => RemoveOne();

    [CommandMethod("XOACOC1")]
    public void RemoveOneAlias() => RemoveOne();

    [CommandMethod("ISURVEY_STATION_ADD_ONE")]
    public void AddOneCommand() => AddOne();

    [CommandMethod("THEMCOC")]
    public void AddOneAlias() => AddOne();

    [CommandMethod("ISURVEY_STATION_LIST")]
    public void ListCommand() => ListStations();

    [CommandMethod("BANGCOC")]
    public void ListAlias() => ListStations();

    private static void StationStyle()
    {
        if (!AuthManager.EnsureAuthenticated("THIETLAPCOC"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        StationStyleSettings? saved;
        try
        {
            saved = StationStyleWindow.Prompt(StationStyleStore.Load());
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Không mở được hộp thoại: " + ex.Message);
            return;
        }

        if (saved is null)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy thiết lập kiểu cọc.");
            return;
        }

        StationStyleStore.Save(saved);
        doc.Editor.WriteMessage(
            "\n[iSurvey] Đã lưu kiểu cọc. Chạy PHATSINHCOC để vẽ lại theo thông số mới.");
    }

    private static void New()
    {
        if (!AuthManager.EnsureAuthenticated("TAOTUYEN"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null || !ModelSpace(doc.Editor))
            return;

        var count = 1;
        try
        {
            count = RouteStore.Load(doc.Database).Count + 1;
        }
        catch (System.Exception)
        {
            count = 1;
        }

        var models = new List<RouteModelChoice>();
        string? preferredTerrainId = null;
        try
        {
            foreach (var model in TerrainStore.Load(doc.Database))
                models.Add(new RouteModelChoice { Id = model.Id, Name = model.Name });
            preferredTerrainId = TerrainStore.LastCreated(doc.Database)?.Id;
        }
        catch (System.Exception)
        {
            models.Clear();
            preferredTerrainId = null;
        }

        CreateRouteRequest? request;
        try
        {
            request = CreateRouteWindow.Prompt("Tuyến " + count, models, preferredTerrainId);
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Không mở được hộp thoại: " + ex.Message);
            return;
        }

        if (request is null)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy tạo tuyến.");
            return;
        }

        var picked = doc.Editor.GetEntity(new PromptEntityOptions("\nChọn polyline tim tuyến: ")
        {
            AllowNone = false
        });
        if (picked.Status != PromptStatus.OK)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy chọn tim tuyến.");
            return;
        }

        string handle;
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            if (tr.GetObject(picked.ObjectId, OpenMode.ForRead) is not Entity entity
                || entity is not (Polyline or Polyline2d or Polyline3d))
            {
                doc.Editor.WriteMessage("\n[iSurvey] Hãy chọn một polyline.");
                tr.Commit();
                return;
            }

            handle = entity.Handle.ToString();
            tr.Commit();
        }

        var reversed = RouteService.ResolveStart(doc.Editor, doc.Database, handle, out var startPt, out var endPt);
        if (reversed is null)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy chọn điểm đầu tuyến.");
            return;
        }

        var route = new RouteRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = request.Name,
            PolylineHandle = handle,
            StartChainage = request.StartChainage,
            TerrainId = request.TerrainId,
            Reversed = reversed.Value
        };
        RouteStore.Save(doc.Database, route);
        var chain = request.StartChainage.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        doc.Editor.WriteMessage(
            "\n[iSurvey] Đã tạo tuyến \"" + route.Name + "\", lý trình gốc " + chain
            + " m. Đầu (" + startPt.X.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
            + ", " + startPt.Y.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
            + ") → cuối (" + endPt.X.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
            + ", " + endPt.Y.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + ")"
            + (reversed.Value ? " (đảo so với chiều vẽ)." : "."));
        SurveyPaletteManager.Reload();
    }

    private static void Stations()
    {
        if (!AuthManager.EnsureAuthenticated("PHATSINHCOC"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null || !ModelSpace(doc.Editor))
            return;
        var route = RouteService.Choose(doc.Editor, doc.Database);
        if (route is null)
            return;
        if (!RouteService.TryLength(doc.Database, route, out var length))
        {
            doc.Editor.WriteMessage("\n[iSurvey] Không đọc được polyline tim tuyến.");
            return;
        }

        var from = route.StartChainage;
        var to = route.StartChainage + length;
        CreateStationsRequest? request;
        try
        {
            request = CreateStationsWindow.Prompt(route.Name, from, to);
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Không mở được hộp thoại: " + ex.Message);
            return;
        }

        if (request is null)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy phát sinh cọc.");
            return;
        }

        try
        {
            var count = RouteService.BuildStations(
                doc,
                route,
                request.FromChainage,
                request.ToChainage,
                request.Step,
                out var missingZ);
            if (count == 0)
            {
                doc.Editor.WriteMessage("\n[iSurvey] Không phát sinh được cọc trong khoảng lý trình đã chọn.");
                return;
            }

            var msg = "\n[iSurvey] Đã phát sinh " + count + " cọc trên \"" + route.Name + "\".";
            if (missingZ > 0)
            {
                msg += " " + missingZ + " cọc chưa lấy được Z từ mô hình.";
                if (string.IsNullOrWhiteSpace(route.TerrainId))
                    msg += " Hãy gắn mô hình khi tạo tuyến hoặc chỉ còn một mô hình đã dựng TIN.";
                else
                    msg += " Kiểm tra mô hình đã Xây dựng TIN và tuyến nằm trong đường bao.";
            }

            doc.Editor.WriteMessage(msg);
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Không phát sinh được cọc: " + ex.Message);
        }

        SurveyPaletteManager.Reload();
    }

    private static void Reverse()
    {
        if (!AuthManager.EnsureAuthenticated("DAODAUTUYEN"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null || !ModelSpace(doc.Editor))
            return;
        var route = RouteService.Choose(doc.Editor, doc.Database);
        if (route is null)
            return;
        var nowReversed = RouteService.Reverse(doc, route);
        doc.Editor.WriteMessage(
            "\n[iSurvey] Đã đảo đầu tuyến \"" + route.Name + "\". "
            + (nowReversed ? "Lý trình tăng ngược chiều vẽ polyline." : "Lý trình tăng theo chiều vẽ polyline.")
            + " Cọc cũ đã xóa — hãy phát sinh cọc lại. Sau khi vẽ MCD cũng đảo lại bằng lệnh này nếu cần.");
        SurveyPaletteManager.Reload();
    }

    private static void ClearStations()
    {
        if (!AuthManager.EnsureAuthenticated("XOACOC"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null || !ModelSpace(doc.Editor))
            return;
        var route = RouteService.Choose(doc.Editor, doc.Database);
        if (route is null)
            return;
        if (route.Stations is null || route.Stations.Count == 0)
        {
            doc.Editor.WriteMessage("\n[iSurvey] \"" + route.Name + "\" chưa có cọc.");
            return;
        }

        var removed = RouteService.ClearStations(doc, route);
        doc.Editor.WriteMessage("\n[iSurvey] Đã xóa " + removed + " cọc của \"" + route.Name + "\".");
        SurveyPaletteManager.Reload();
    }

    private static void RemoveOne()
    {
        if (!AuthManager.EnsureAuthenticated("XOACOC1"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null || !ModelSpace(doc.Editor))
            return;

        var options = new PromptEntityOptions("\nChọn gạch cọc hoặc chữ cọc cần xóa: ")
        {
            AllowNone = false
        };
        options.SetRejectMessage("\nChỉ chọn Line, Text hoặc MText của cọc.");
        options.AddAllowedClass(typeof(Line), exactMatch: false);
        options.AddAllowedClass(typeof(DBText), exactMatch: false);
        options.AddAllowedClass(typeof(MText), exactMatch: false);
        var picked = doc.Editor.GetEntity(options);
        if (picked.Status != PromptStatus.OK)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy xóa cọc.");
            return;
        }

        if (!RouteService.RemoveStationByEntity(doc, picked.ObjectId, out var routeName, out var stationName))
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đối tượng không phải cọc iSurvey đã lưu.");
            return;
        }

        doc.Editor.WriteMessage("\n[iSurvey] Đã xóa cọc " + stationName + " trên \"" + routeName + "\".");
        SurveyPaletteManager.Reload();
    }

    private static void AddOne()
    {
        if (!AuthManager.EnsureAuthenticated("THEMCOC"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null || !ModelSpace(doc.Editor))
            return;
        var route = RouteService.Choose(doc.Editor, doc.Database);
        if (route is null)
            return;

        var point = doc.Editor.GetPoint(new PromptPointOptions("\nChọn vị trí thêm cọc (gần tim tuyến): "));
        if (point.Status != PromptStatus.OK)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy thêm cọc.");
            return;
        }

        if (!RouteService.TryAddStationAtPoint(doc, route, point.Value, replaceNear: true, out var chainage, out var missingZ))
        {
            doc.Editor.WriteMessage("\n[iSurvey] Không thêm được cọc tại điểm đã chọn.");
            return;
        }

        var msg = "\n[iSurvey] Đã thêm cọc " + RouteService.FormatChainage(chainage)
            + " trên \"" + route.Name + "\".";
        if (missingZ > 0)
            msg += " Chưa lấy được Z từ mô hình.";
        doc.Editor.WriteMessage(msg);
        SurveyPaletteManager.Reload();
    }

    private static void ListStations()
    {
        if (!AuthManager.EnsureAuthenticated("BANGCOC"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null || !ModelSpace(doc.Editor))
            return;
        var route = RouteService.Choose(doc.Editor, doc.Database);
        if (route is null)
            return;

        while (true)
        {
            StationListResult result;
            try
            {
                result = StationListWindow.Prompt(doc, route);
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage("\n[iSurvey] Không mở được danh sách cọc: " + ex.Message);
                return;
            }

            SurveyPaletteManager.Reload();
            if (!result.AddRequested)
                return;

            var point = doc.Editor.GetPoint(new PromptPointOptions("\nChọn vị trí thêm cọc (gần tim tuyến): "));
            if (point.Status != PromptStatus.OK)
            {
                doc.Editor.WriteMessage("\n[iSurvey] Đã hủy thêm cọc.");
                return;
            }

            if (!RouteService.TryAddStationAtPoint(doc, route, point.Value, replaceNear: true, out var chainage, out var missingZ))
            {
                doc.Editor.WriteMessage("\n[iSurvey] Không thêm được cọc tại điểm đã chọn.");
                continue;
            }

            var msg = "\n[iSurvey] Đã thêm cọc " + RouteService.FormatChainage(chainage) + ".";
            if (missingZ > 0)
                msg += " Chưa lấy được Z từ mô hình.";
            doc.Editor.WriteMessage(msg);
            // Mở lại bảng sau khi thêm.
        }
    }

    private static bool ModelSpace(Editor editor)
    {
        try
        {
            if (Convert.ToInt16(AcadApp.GetSystemVariable("TILEMODE")) == 1)
                return true;
            if (Convert.ToInt16(AcadApp.GetSystemVariable("CVPORT")) != 1)
                return true;
        }
        catch (System.Exception)
        {
            return true;
        }

        editor.WriteMessage("\n[iSurvey] Hãy mở Model Space trước khi làm tuyến.");
        return false;
    }
}
