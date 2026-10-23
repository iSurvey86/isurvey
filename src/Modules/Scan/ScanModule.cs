using System.Diagnostics;
using System.Text;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using iSurvey.Auth;
using iSurvey.Core;
using Win32SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace iSurvey.Modules.Scan;

/// <summary>
/// Module quét cấu trúc bản vẽ — lệnh AUTOSCAN xuất báo cáo .txt rồi mở bằng notepad.
/// Thông báo Command Line dùng tiếng Việt không dấu (ASCII).
/// </summary>
public sealed class ScanModule : ISurveyModule
{
    public string DisplayName => "Quet cau truc";

    public void Initialize() { }

    public void Terminate() { }

    [CommandMethod("AUTOSCAN")]
    public void AutoScanCommand()
    {
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        var ed = doc.Editor;
        var db = doc.Database;

        var pso = new PromptSelectionOptions
        {
            MessageForAdding = "\niSurvey: Quét chọn vùng cần phân tích (gõ ALL để chọn hết): "
        };
        var ps = ed.GetSelection(pso);
        if (ps.Status != PromptStatus.OK)
        {
            ed.WriteMessage("\n[iSurvey] Da huy AUTOSCAN.");
            return;
        }

        var dialog = new Win32SaveFileDialog
        {
            Title = "Luu bao cao AutoScan",
            Filter = "Text (*.txt)|*.txt",
            DefaultExt = ".txt",
            AddExtension = true,
            FileName = "CAD_AutoScan_Report.txt"
        };
        if (!string.IsNullOrWhiteSpace(db.Filename) && Path.IsPathRooted(db.Filename))
            dialog.InitialDirectory = Path.GetDirectoryName(db.Filename);

        if (dialog.ShowDialog() != true)
        {
            ed.WriteMessage("\n[iSurvey] Da huy AUTOSCAN.");
            return;
        }

        try
        {
            string text;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var name = string.IsNullOrWhiteSpace(db.Filename) ? "(chua luu)" : db.Filename;
                text = new ScanReport().Build(db, tr, ps.Value.GetObjectIds(), name);
                tr.Commit();
            }

            File.WriteAllText(dialog.FileName, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            ed.WriteMessage($"\n[iSurvey] Da luu bao cao: {dialog.FileName}");
            ActivityLogger.Result("AUTOSCAN",
                $"báo cáo {ActivityLogger.FileName(dialog.FileName)}, {ps.Value.Count} đối tượng, bản vẽ {ActivityLogger.FileName(db.Filename)}");

            Process.Start(new ProcessStartInfo("notepad.exe", $"\"{dialog.FileName}\"") { UseShellExecute = true });
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] AUTOSCAN that bai: {ex.Message}");
            iSurvey.Auth.ActivityLogger.TrackError(ex);
        }
    }
}
