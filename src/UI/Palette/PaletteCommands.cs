using Autodesk.AutoCAD.ApplicationServices;
using iSurvey.Modules.Terrain;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.UI.Palette;

/// <summary>
/// Hành động từ bảng. Lệnh đã có thì gửi vào dòng lệnh.
/// Lệnh chưa làm chỉ báo, không mở Transaction.
/// </summary>
internal static class PaletteCommands
{
    public static RelayCommand Run(string command) => new(() => Send(command));

    public static RelayCommand Later(string message) => new(() => Tell(message));

    public static RelayCommand ForModel(string command, string? modelId, string? kind) => new(() =>
    {
        TerrainContext.ModelId = modelId;
        TerrainContext.AddKind = kind;
        Send(command);
    });

    private static void Send(string command)
    {
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;
        var text = command.Trim();
        if (!text.EndsWith(" ", StringComparison.Ordinal))
            text += " ";
        doc.SendStringToExecute(text, true, false, false);
    }

    private static void Tell(string message)
    {
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        doc?.Editor.WriteMessage("\n[iSurvey] " + message);
    }
}
