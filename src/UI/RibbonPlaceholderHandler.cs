using System.Windows.Input;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using iSurvey.Auth;

namespace iSurvey.UI;

/// <summary>Nút Ribbon chưa có lệnh thật — chỉ in thông báo lên Command Line (ASCII). Vẫn yêu cầu đăng nhập.</summary>
public sealed class RibbonPlaceholderHandler : ICommand
{
    private readonly string _commandName;

    public RibbonPlaceholderHandler(string commandName)
    {
        _commandName = commandName;
    }

#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        if (!AuthManager.EnsureAuthenticated())
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        doc?.Editor.WriteMessage($"\n[iSurvey] Tinh nang {_commandName} dang duoc cap nhat...");
    }
}
