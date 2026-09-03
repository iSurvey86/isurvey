using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.Runtime;
using iSurvey.Core;
using iSurvey.Modules.Map;
using iSurvey.UI.KmlExport;

namespace iSurvey.Modules.Export;

/// <summary>Module xuất CAD → Google Earth (KML/KMZ).</summary>
public sealed class ExportModule : ISurveyModule
{
    private readonly KmlExportService _service = new();

    public string DisplayName => "Xuất GE";

    public void Initialize() { }

    public void Terminate() { }

    [CommandMethod("ISURVEY_EXPORT_KML")]
    public void ExportKmlCommand()
    {
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        var ed = doc.Editor;
        var db = doc.Database;

        if (!MapModule.IsModelSpace(db))
        {
            ed.WriteMessage("\n[iSurvey] Hãy mở Model Space trước khi xuất.");
            return;
        }

        var drawingPath = string.IsNullOrWhiteSpace(db.Filename) ? null : db.Filename;

        var settings = KmlExportDialog.Show(drawingPath);
        if (settings is null)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy xuất.");
            return;
        }

        try
        {
            ed.WriteMessage(
                $"\n[iSurvey] Đang xuất (kinh tuyến {settings.CentralMeridian:0.##}°, TM-{settings.ZoneWidthDegrees})...");
            var report = _service.Run(doc, settings);
            ed.WriteMessage($"\n[iSurvey] Đã lưu: {settings.OutputPath}");
            ed.WriteMessage($"\n[iSurvey] {report}");
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] Xuất thất bại: {ex.Message}");
        }
    }
}
