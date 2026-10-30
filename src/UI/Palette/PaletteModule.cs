using Autodesk.AutoCAD.Runtime;
using iSurvey.Auth;

namespace iSurvey.UI.Palette;

/// <summary>Mở hoặc đóng bảng dữ liệu neo bên trái.</summary>
public sealed class PaletteModule
{
    [CommandMethod("ISURVEY_PALETTE")]
    public void ShowCommand()
    {
        if (!AuthManager.EnsureAuthenticated("ISURVEY_PALETTE"))
            return;
        SurveyPaletteManager.Toggle();
    }
}
