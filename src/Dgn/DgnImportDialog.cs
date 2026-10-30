using System.Windows.Interop;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Dgn;

/// <summary>Mở DgnImportWindow gắn Owner là cửa sổ AutoCAD.</summary>
public static class DgnImportDialog
{
    public static DgnImportOptions? ShowImport(string? drawingPath)
    {
        var window = new DgnImportWindow(drawingPath);
        _ = new WindowInteropHelper(window) { Owner = AcadApp.MainWindow.Handle };
        return window.ShowDialog() == true ? window.Result : null;
    }
}
