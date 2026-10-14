using System.Windows.Interop;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Dgn;

/// <summary>Mở DgnExportWindow gắn Owner là cửa sổ AutoCAD.</summary>
public static class DgnExportDialog
{
    public static DgnExportOptions? ShowExport(string? drawingPath, int selectedCount)
    {
        var window = new DgnExportWindow(drawingPath, selectedCount);
        _ = new WindowInteropHelper(window) { Owner = AcadApp.MainWindow.Handle };
        return window.ShowDialog() == true ? window.Result : null;
    }
}
