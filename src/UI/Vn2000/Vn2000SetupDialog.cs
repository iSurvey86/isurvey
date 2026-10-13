using System.Windows.Interop;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using iSurvey.Models;

namespace iSurvey.UI.Vn2000;

/// <summary>Mở hộp thoại VN-2000, gắn cửa sổ AutoCAD làm owner.</summary>
public static class Vn2000SetupDialog
{
    public static Vn2000SetupResult? Show(string? drawingPath, Vn2000SetupMode mode)
    {
        var window = new Vn2000SetupWindow(drawingPath, mode);
        _ = new WindowInteropHelper(window) { Owner = AcadApp.MainWindow.Handle };
        return window.ShowDialog() == true ? window.Result : null;
    }
}
