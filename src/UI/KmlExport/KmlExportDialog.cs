using System.Windows.Interop;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using iSurvey.Models;

namespace iSurvey.UI.KmlExport;

/// <summary>Mở hộp thoại xuất KML/KMZ gắn Owner AutoCAD.</summary>
public static class KmlExportDialog
{
    public static KmlExportSettings? Show(string? drawingPath)
    {
        var window = new KmlExportWindow(drawingPath);
        _ = new WindowInteropHelper(window)
        {
            Owner = AcadApp.MainWindow.Handle
        };

        return window.ShowDialog() == true ? window.Settings : null;
    }
}
