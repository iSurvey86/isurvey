using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.Runtime;
using iSurvey.Auth;
using iSurvey.Core;
using iSurvey.Models;
using iSurvey.Modules.Map;
using iSurvey.UI.Vn2000;
using WinFormsDialog = System.Windows.Forms.OpenFileDialog;

namespace iSurvey.Modules.Import;

/// <summary>Nhập KML/KMZ vào Model Space theo VN-2000.</summary>
public sealed class KmlImportModule : ISurveyModule
{
    public string DisplayName => "Nhập KML";

    public void Initialize() { }

    public void Terminate() { }

    [CommandMethod("ISURVEY_IMPORT_KML")]
    public void ImportKmlCommand()
    {
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        var ed = doc.Editor;
        if (!MapModule.IsModelSpace(doc.Database))
        {
            ed.WriteMessage("\n[iSurvey] Hãy mở Model Space trước khi nhập.");
            return;
        }

        var dialog = new WinFormsDialog
        {
            Title = "Chọn file KML/KMZ",
            Filter = "Google Earth (*.kml;*.kmz)|*.kml;*.kmz|KML (*.kml)|*.kml|KMZ (*.kmz)|*.kmz",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(new AcadWindow(AcadApp.MainWindow.Handle)) != System.Windows.Forms.DialogResult.OK)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy.");
            return;
        }

        var drawingPath = string.IsNullOrWhiteSpace(doc.Database.Filename) ? null : doc.Database.Filename;
        Vn2000SetupResult? setup;
        try
        {
            setup = Vn2000SetupDialog.Show(drawingPath, Vn2000SetupMode.Import);
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] Không mở được hộp thoại: {ex.Message}");
            return;
        }

        if (setup is null)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy nhập.");
            return;
        }

        NoteMeridian(ed, setup);

        string? temp = null;
        try
        {
            var kmlPath = KmlPackage.Materialize(dialog.FileName, out temp);
            var read = KmlReader.Read(kmlPath);
            if (read.Shapes.Count == 0)
            {
                ed.WriteMessage("\n[iSurvey] File không có Point, LineString hoặc Polygon để vẽ.");
                NoteSkipped(ed, read.InnerRings, read.TracksSkipped);
                return;
            }

            var coordinates = new CoordinateService();
            var report = KmlDrawService.Draw(
                doc.Database,
                read.Shapes,
                coordinates,
                setup.CentralMeridian,
                setup.ZoneWidthDegrees,
                setup.InsertPoints,
                setup.LabelNames,
                setup.TextHeight,
                setup.SimplifyVertices,
                setup.SimplifyTolerance);
            report.InnerRings = read.InnerRings;
            report.TracksSkipped = read.TracksSkipped;

            if (report.Points > 0)
                report.PointStyleAdjusted = EnsurePointVisible();

            ed.Regen();
            ed.WriteMessage(
                $"\n[iSurvey] Đã nhập vào layer {KmlDrawService.LayerName}: {report.Points} điểm, {report.Texts} chữ, {report.Lines} đường, {report.Polygons} vùng.");
            ActivityLogger.Result("ISURVEY_IMPORT_KML",
                $"file {ActivityLogger.FileName(dialog.FileName)}, {report.Points} điểm, {report.Texts} chữ, {report.Lines} đường, {report.Polygons} vùng, TM-{setup.ZoneWidthDegrees} kinh tuyến {setup.CentralMeridian:0.##}°");
            NoteSkipped(ed, report.InnerRings, report.TracksSkipped);
            if (report.Simplified)
            {
                ed.WriteMessage(
                    $"\n[iSurvey] Da loc bot dinh thua: Giam tu {report.OriginalVertices} xuong con {report.SimplifiedVertices} dinh.");
            }
            if (report.PointStyleAdjusted)
                ed.WriteMessage("\n[iSurvey] PDMODE đang bằng 0 (điểm không hiện). Đã đặt PDMODE = 3.");
            KmlDrawService.FitImported(ed, report);
        }
        catch (IOException ex)
        {
            ed.WriteMessage($"\n[iSurvey] Không đọc được file (đang mở ở chương trình khác?): {ex.Message}");
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] Nhập thất bại: {ex.Message}");
            iSurvey.Auth.ActivityLogger.TrackError(ex);
        }
        finally
        {
            KmlPackage.Cleanup(temp);
        }
    }

    private static void NoteMeridian(Autodesk.AutoCAD.EditorInput.Editor ed, Vn2000SetupResult setup)
    {
        if (Math.Abs(setup.CentralMeridian - setup.EnteredMeridian) > 1e-6)
        {
            ed.WriteMessage(
                $"\n[iSurvey] Múi 6°: kinh tuyến {setup.EnteredMeridian:0.###}° được đưa về {setup.CentralMeridian:0.#}°.");
        }
    }

    private static void NoteSkipped(Autodesk.AutoCAD.EditorInput.Editor ed, int holes, int tracks)
    {
        if (holes > 0)
            ed.WriteMessage($"\n[iSurvey] Bỏ qua {holes} vòng trong (innerBoundaryIs).");
        if (tracks > 0)
            ed.WriteMessage($"\n[iSurvey] Bỏ qua {tracks} gx:Track.");
    }

    private static bool EnsurePointVisible()
    {
        try
        {
            var mode = Convert.ToInt16(AcadApp.GetSystemVariable("PDMODE"));
            if (mode != 0)
                return false;
            AcadApp.SetSystemVariable("PDMODE", 3);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private sealed class AcadWindow : System.Windows.Forms.IWin32Window
    {
        public IntPtr Handle { get; }
        public AcadWindow(IntPtr handle) => Handle = handle;
    }
}
