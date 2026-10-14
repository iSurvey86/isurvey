using System.Text;
using Autodesk.AutoCAD.EditorInput;

namespace iSurvey.Dgn;

/// <summary>Ghi log đồng thời ra Command Line và file %LocalAppData%\iSurvey\logs\dgn_*.log.</summary>
internal sealed class DgnLog : IDisposable
{
    private readonly Editor _editor;
    private readonly string _tag;
    private StreamWriter? _file;

    public string? FilePath { get; }

    public DgnLog(Editor editor, string tag = "DGN", string filePrefix = "dgn")
    {
        _editor = editor;
        _tag = tag;
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "iSurvey", "logs");
            Directory.CreateDirectory(dir);
            FilePath = Path.Combine(dir, $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            _file = new StreamWriter(FilePath, false, new UTF8Encoding(false)) { AutoFlush = true };
        }
        catch
        {
            _file = null;
            FilePath = null;
        }
    }

    public void Info(string message)
    {
        try
        {
            // Ghi vào Command Line của bản vẽ đang hoạt động (DGNIMPORT có thể mở bản vẽ mới).
            var ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument?.Editor
                     ?? _editor;
            ed.WriteMessage($"\n[iSurvey][{_tag}] " + message);
        }
        catch
        {
            // không để log làm hỏng luồng nhập
        }

        try
        {
            _file?.WriteLine($"{DateTime.Now:HH:mm:ss} {message}");
        }
        catch
        {
            // bỏ qua
        }
    }

    public void Dispose()
    {
        _file?.Dispose();
        _file = null;
    }
}
