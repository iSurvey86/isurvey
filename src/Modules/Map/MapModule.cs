using Autodesk.AutoCAD.ApplicationServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using iSurvey.Core;
using iSurvey.Models;
using iSurvey.UI.MapInsert;

namespace iSurvey.Modules.Map;

/// <summary>Module bản đồ vệ tinh Google — lệnh ISURVEY_MAP / ISURVEY_DELETE_GE.</summary>
public sealed class MapModule : ISurveyModule
{
    private readonly BasemapRefreshService _refresh = new();

    public string DisplayName => "Bản đồ";

    public void Initialize() => AutoRefreshController.Subscribe();

    public void Terminate() => AutoRefreshController.Unsubscribe();

    [CommandMethod("ISURVEY_MAP")]
    public void InsertMapCommand() => RunMapWorkflow(MapInsertAction.Insert);

    [CommandMethod("ISURVEY_MAP_SAT")]
    public void InsertSatelliteCommand() => ApplyBasemap("google-satellite");

    [CommandMethod("ISURVEY_DELETE_GE")]
    public void DeleteGeCommand()
    {
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

        ApplyBasemap(doc, settings.CentralMeridian, settings.BasemapId, settings.AutoRefresh, boundary);
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
        ApplyBasemap(doc, saved.CentralMeridian, basemapId, saved.AutoRefresh, null);
    }

    private void ApplyBasemap(
        Document doc,
        double centralMeridian,
        string basemapId,
        bool autoRefresh,
        BoundarySelection? boundary)
    {
        var ed = doc.Editor;
        ed.WriteMessage($"\n[iSurvey] Kinh tuyến {centralMeridian:0.##}° — {basemapId}");

        try
        {
            BasemapSession.Activate(
                centralMeridian,
                basemapId,
                autoRefresh,
                boundary?.ClipPolygonWcs);
            AutoRefreshController.ResetViewSignature();
            _refresh.Refresh(doc, centralMeridian, basemapId);
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] {ex.Message}");
        }
    }

    private void ApplyBasemap(Document doc, double centralMeridian, string basemapId, bool autoRefresh)
        => ApplyBasemap(doc, centralMeridian, basemapId, autoRefresh, null);

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
