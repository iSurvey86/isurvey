using System.Windows.Input;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.UI;

/// <summary>Gửi lệnh AutoCAD khi bấm nút trên Ribbon.</summary>
public sealed class RibbonCommandHandler : ICommand
{
    private readonly string _commandLine;

    public RibbonCommandHandler(string commandLine)
    {
        _commandLine = commandLine;
    }

#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null) return;

        doc.SendStringToExecute(_commandLine, true, false, false);
    }
}
