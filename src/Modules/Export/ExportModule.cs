using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.Runtime;
using iSurvey.Auth;
using iSurvey.Core;
using iSurvey.Modules.Map;
using iSurvey.Models;
using iSurvey.UI.Vn2000;

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
        if (!AuthManager.EnsureAuthenticated())
            return;

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

        Vn2000SetupResult? setup;
        try
        {
            setup = Vn2000SetupDialog.Show(drawingPath, Vn2000SetupMode.Export);
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] Không mở được hộp thoại: {ex.Message}");
            return;
        }

        if (setup is null)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy xuất.");
            return;
        }

        if (Math.Abs(setup.CentralMeridian - setup.EnteredMeridian) > 1e-6)
        {
            ed.WriteMessage(
                $"\n[iSurvey] Múi 6°: kinh tuyến {setup.EnteredMeridian:0.###}° được đưa về {setup.CentralMeridian:0.#}°.");
        }

        var settings = new KmlExportSettings
        {
            CentralMeridian = setup.CentralMeridian,
            ZoneWidthDegrees = setup.ZoneWidthDegrees,
            SelectionOnly = setup.SelectionOnly,
            UseKmz = setup.UseKmz,
            GroupByLayer = setup.GroupByLayer,
            UseElevationZ = setup.UseElevationZ,
            OpenAfterExport = setup.OpenAfterExport
        };

        KmlExportService.KmlCollected collected;
        try
        {
            collected = _service.Collect(doc, settings);
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] Xuất thất bại: {ex.Message}");
            iSurvey.Auth.ActivityLogger.TrackError(ex);
            return;
        }

        if (collected.Cancelled)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy xuất.");
            return;
        }

        if (collected.Features.Count == 0)
        {
            ed.WriteMessage($"\n[iSurvey] Không có hình học nào xuất được ({collected.Report}).");
            return;
        }

        var path = AskOutputPath(setup.UseKmz);
        if (path is null)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy xuất.");
            return;
        }

        settings.OutputPath = path;

        try
        {
            ed.WriteMessage(
                $"\n[iSurvey] Đang xuất (kinh tuyến {settings.CentralMeridian:0.##}°, TM-{settings.ZoneWidthDegrees})...");
            _service.Write(db, settings, collected.Features);
            ed.WriteMessage($"\n[iSurvey] Đã lưu: {settings.OutputPath}");
            ed.WriteMessage($"\n[iSurvey] {collected.Report}");
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] Xuất thất bại: {ex.Message}");
            iSurvey.Auth.ActivityLogger.TrackError(ex);
        }
    }

    private static string? AskOutputPath(bool useKmz)
    {
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        var dialog = new System.Windows.Forms.SaveFileDialog
        {
            Title = "Lưu file Google Earth",
            Filter = useKmz ? "Google Earth KMZ (*.kmz)|*.kmz" : "Google Earth KML (*.kml)|*.kml",
            DefaultExt = useKmz ? "kmz" : "kml",
            AddExtension = true,
            FileName = useKmz ? $"isurvey_export_{stamp}.kmz" : $"isurvey_export_{stamp}.kml"
        };

        var owner = new AcadWindow(AcadApp.MainWindow.Handle);
        return dialog.ShowDialog(owner) == System.Windows.Forms.DialogResult.OK ? dialog.FileName : null;
    }

    private sealed class AcadWindow : System.Windows.Forms.IWin32Window
    {
        public IntPtr Handle { get; }
        public AcadWindow(IntPtr handle) => Handle = handle;
    }
}
