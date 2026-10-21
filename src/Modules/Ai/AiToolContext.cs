using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

namespace iSurvey.Modules.Ai;

/// <summary>Ngữ cảnh CAD khi chạy tool (luồng UI AutoCAD).</summary>
public sealed class AiToolContext
{
    public required Document Doc { get; init; }
    public Editor Ed => Doc.Editor;
    public Func<string, bool>? ConfirmDestructive { get; init; }
}
