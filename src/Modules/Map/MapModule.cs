using Autodesk.AutoCAD.ApplicationServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using iSurvey.Auth;
using iSurvey.Core;
using iSurvey.Models;
using iSurvey.UI.MapInsert;
using iSurvey.UI.Vn2000;

namespace iSurvey.Modules.Map;

/// <summary>Module bản đồ vệ tinh Google — lệnh ISURVEY_MAP / ISURVEY_DELETE_GE.</summary>
public sealed class MapModule : ISurveyModule
{
    private readonly BasemapRefreshService _refresh = new();

    public string DisplayName => "Bản đồ";

    public void Initialize() => AutoRefreshController.Subscribe();

    public void Terminate() => AutoRefreshController.Unsubscribe();

    [CommandMethod("ISURVEY_MAP")]
    public void InsertMapCommand()
    {
        if (!AuthManager.EnsureAuthenticated()) return;
        RunMapWorkflow(MapInsertAction.Insert);
    }

    [CommandMethod("ISURVEY_MAP_SAT")]
    public void InsertSatelliteCommand()
    {
        if (!AuthManager.EnsureAuthenticated()) return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null) return;

        if (!IsModelSpace(doc.Database))
        {
            doc.Editor.WriteMessage("\n[iSurvey] Hãy mở Model Space.");
            return;
        }

        if (!EnsureProvince(doc))
            return;

        var mode = PromptSatelliteMode(doc.Editor);
        if (mode is null)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy.");
            return;
        }

        if (mode == 2)
            HdMosaicWorkflow.Run(doc);
        else
            ApplyBasemap("google-satellite");
    }

    private static bool EnsureProvince(Document doc)
    {
        var drawingPath = string.IsNullOrWhiteSpace(doc.Database.Filename) ? null : doc.Database.Filename;
        var saved = UserSettingsStore.Load(drawingPath);
        if (!string.IsNullOrWhiteSpace(saved.ProvinceName))
            return true;

        Vn2000SetupResult? setup;
        try
        {
            setup = Vn2000SetupDialog.Show(drawingPath, Vn2000SetupMode.InsertImage);
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage($"\n[iSurvey] Không mở được hộp thoại: {ex.Message}");
            return false;
        }

        if (setup is null)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy.");
            return false;
        }

        if (Math.Abs(setup.CentralMeridian - setup.EnteredMeridian) > 1e-6)
        {
            doc.Editor.WriteMessage(
                $"\n[iSurvey] Múi 6°: kinh tuyến {setup.EnteredMeridian:0.###}° được đưa về {setup.CentralMeridian:0.#}°.");
        }

        return true;
    }

    private static int? PromptSatelliteMode(Editor ed)
    {
        var opts = new PromptKeywordOptions(
            "\n[iSurvey] Chon che do chen anh: [1. Xem luot (AutoRefresh) / 2. Ghep anh HD (Chon duong bao)] <1>:")
        {
            AllowNone = true,
            AppendKeywordsToMessage = false
        };
        opts.Keywords.Add("1");
        opts.Keywords.Add("2");
        opts.Keywords.Default = "1";

        var res = ed.GetKeywords(opts);
        if (res.Status == PromptStatus.Cancel)
            return null;
        if (res.Status == PromptStatus.None || res.StringResult == "1")
            return 1;
        if (res.StringResult == "2")
            return 2;

        return 1;
    }

    [CommandMethod("ISURVEY_UPDATE_GE")]
    public void UpdateGeCommand()
    {
        if (!AuthManager.EnsureAuthenticated()) return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null) return;

        if (!IsModelSpace(doc.Database))
        {
            doc.Editor.WriteMessage("\n[iSurvey] Hãy mở Model Space.");
            return;
        }

        try
        {
            if (!BasemapSession.IsActive)
            {
                var drawingPath = string.IsNullOrWhiteSpace(doc.Database.Filename) ? null : doc.Database.Filename;
                var saved = UserSettingsStore.Load(drawingPath);
                if (string.IsNullOrWhiteSpace(saved.ProvinceName))
                {
                    doc.Editor.WriteMessage("\n[iSurvey] Chưa có ảnh GE trong phiên này. Hãy chèn ảnh vệ tinh trước.");
                    return;
                }

                var basemap = string.IsNullOrWhiteSpace(saved.BasemapId) ? "google-satellite" : saved.BasemapId;
                BasemapSession.Activate(
                    saved.CentralMeridian,
                    basemap,
                    saved.AutoRefresh,
                    null,
                    saved.ZoneWidthDegrees);
            }

            _refresh.Refresh(doc, BasemapSession.CentralMeridian, BasemapSession.BasemapId);
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage($"\n[iSurvey] {ex.Message}");
            iSurvey.Auth.ActivityLogger.TrackError(ex);
        }
    }

    [CommandMethod("ISURVEY_DELETE_GE")]
    public void DeleteGeCommand()
    {
        if (!AuthManager.EnsureAuthenticated()) return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null) return;
        GeDeleteWorkflow.Run(doc);
    }

    private void RunMapWorkflow(MapInsertAction defaultAction)
    {
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null) return;

        var ed = doc.Editor;
        var db = doc.Database;

        if (!IsModelSpace(db))
        {
            ed.WriteMessage("\n[iSurvey] Hãy mở tab Model hoặc vào Model Space trong viewport (lệnh MSPACE).");
            return;
        }

        var drawingPath = string.IsNullOrWhiteSpace(db.Filename) ? null : db.Filename;

        MapInsertSettings? settings;
        try
        {
            settings = MapInsertDialog.Show(drawingPath, defaultAction);
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] Không mở được hộp thoại: {ex.Message}");
            return;
        }

        if (settings is null)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy.");
            return;
        }

        UserSettingsStore.Save(drawingPath, new MapUserSettings
        {
            ProvinceName = settings.ProvinceName,
            AreaLabel = settings.AreaLabel,
            CentralMeridian = settings.CentralMeridian,
            ZoneWidthDegrees = settings.ZoneWidthDegrees,
            BasemapId = settings.BasemapId,
            AutoRefresh = settings.AutoRefresh,
            UseBoundaryClip = settings.UseBoundaryClip
        });

        BoundarySelection? boundary = null;
        if (settings.UseBoundaryClip)
        {
            ed.WriteMessage("\n[iSurvey] Chọn đường bao (Polyline đóng / Region / Circle)...");
            boundary = RegionSelectionHelper.TrySelectBoundary(ed, db);
            if (boundary is null)
            {
                ed.WriteMessage("\n[iSurvey] Đã hủy — chưa chèn ảnh.");
                return;
            }
        }

        ApplyBasemap(doc, settings.CentralMeridian, settings.BasemapId, settings.AutoRefresh, boundary, settings.ZoneWidthDegrees);
    }

    private void ApplyBasemap(string basemapId)
    {
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null) return;

        if (!IsModelSpace(doc.Database))
        {
            doc.Editor.WriteMessage("\n[iSurvey] Hãy mở Model Space.");
            return;
        }

        var drawingPath = string.IsNullOrWhiteSpace(doc.Database.Filename) ? null : doc.Database.Filename;
        var saved = UserSettingsStore.Load(drawingPath);
        ApplyBasemap(doc, saved.CentralMeridian, basemapId, saved.AutoRefresh, null, saved.ZoneWidthDegrees);
    }

    private void ApplyBasemap(
        Document doc,
        double centralMeridian,
        string basemapId,
        bool autoRefresh,
        BoundarySelection? boundary,
        int zoneWidthDegrees = 3)
    {
        var ed = doc.Editor;
        var zone = CoordinateService.NormalizeZoneWidth(zoneWidthDegrees);
        var cm = zone == 6
            ? CoordinateService.SnapToTm6CentralMeridian(centralMeridian)
            : centralMeridian;
        ed.WriteMessage($"\n[iSurvey] Kinh tuyến {cm:0.##}° — TM-{zone} — {basemapId}");

        try
        {
            BasemapSession.Activate(
                cm,
                basemapId,
                autoRefresh,
                boundary?.ClipPolygonWcs,
                zone);
            AutoRefreshController.ResetViewSignature();
            _refresh.Refresh(doc, cm, basemapId);
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] {ex.Message}");
            iSurvey.Auth.ActivityLogger.TrackError(ex);
        }
    }

    private void ApplyBasemap(Document doc, double centralMeridian, string basemapId, bool autoRefresh)
        => ApplyBasemap(doc, centralMeridian, basemapId, autoRefresh, null, 3);

    internal static bool IsModelSpace(Database db)
    {
        try
        {
            var tileMode = Convert.ToInt16(AcadApp.GetSystemVariable("TILEMODE"));
            if (tileMode == 1)
                return true;

            var mspace = Convert.ToInt16(AcadApp.GetSystemVariable("MSPACE"));
            return mspace == 1;
        }
        catch
        {
            return db.TileMode;
        }
    }
}
