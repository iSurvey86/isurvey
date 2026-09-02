using System.Windows.Interop;

using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

using iSurvey.Models;



namespace iSurvey.UI.MapInsert;



/// <summary>Mở hộp thoại WPF gắn cửa sổ AutoCAD làm Owner.</summary>

public static class MapInsertDialog

{

    public static MapInsertSettings? Show(string? drawingPath, MapInsertAction defaultAction = MapInsertAction.Insert)

    {

        var window = new MapInsertWindow(drawingPath, defaultAction);

        var helper = new WindowInteropHelper(window)

        {

            Owner = AcadApp.MainWindow.Handle

        };



        return window.ShowDialog() == true ? window.Settings : null;

    }

}


