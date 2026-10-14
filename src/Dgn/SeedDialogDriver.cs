using System.Runtime.InteropServices;
using System.Text;

namespace iSurvey.Dgn;

/// <summary>
/// -DGNEXPORT hỏi seed bằng prompt keyword: chỉ nhận tên seed đã đăng ký hoặc "~" để mở hộp thoại
/// "Select Seed File" (hộp thoại modal, KHÔNG nhận đường dẫn gõ từ Command Line, kể cả FILEDIA=0).
/// Lớp này chạy một luồng nền, thấy hộp thoại đó là điền sẵn đường dẫn seed và bấm Open — nên người dùng
/// không phải tìm seed thủ công. Nhận diện theo cấu trúc control (ComboBox 13006 + nút IDOK), không phụ thuộc
/// ngôn ngữ của tiêu đề.
/// </summary>
internal sealed class SeedDialogDriver : IDisposable
{
    private const int FileNameComboId = 13006;
    private const int WmSetText = 0x000C;
    private const int BmClick = 0x00F5;
    private const int IdOk = 1;

    private readonly string _seedPath;
    private readonly uint _pid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
    private readonly Thread _thread;
    private volatile bool _stop;

    /// <summary>Số lần đã điền và bấm Open vào hộp thoại chọn seed.</summary>
    public int Driven { get; private set; }

    public SeedDialogDriver(string seedPath)
    {
        _seedPath = seedPath;
        _thread = new Thread(Loop) { IsBackground = true, Name = "iSurvey.SeedDialogDriver" };
        _thread.Start();
    }

    public void Dispose() => _stop = true;

    private void Loop()
    {
        while (!_stop)
        {
            try
            {
                foreach (var h in TopLevelDialogs())
                {
                    var combo = GetDlgItem(h, FileNameComboId);
                    var ok = GetDlgItem(h, IdOk);
                    if (combo == IntPtr.Zero || ok == IntPtr.Zero)
                        continue;

                    var edit = FindWindowEx(combo, IntPtr.Zero, "Edit", null);
                    if (edit == IntPtr.Zero)
                        continue;

                    SendMessage(edit, WmSetText, IntPtr.Zero, _seedPath);
                    PostMessage(ok, BmClick, IntPtr.Zero, IntPtr.Zero);
                    Driven++;
                    Thread.Sleep(800); // đợi hộp thoại đóng, tránh bấm lặp
                }
            }
            catch
            {
                // luồng nền không được làm hỏng Civil 3D
            }

            Thread.Sleep(120);
        }
    }

    private List<IntPtr> TopLevelDialogs()
    {
        var list = new List<IntPtr>();
        EnumWindows((h, _) =>
        {
            GetWindowThreadProcessId(h, out var pid);
            if (pid == _pid && IsWindowVisible(h))
            {
                var cls = new StringBuilder(32);
                GetClassName(h, cls, cls.Capacity);
                if (cls.ToString() == "#32770")
                    list.Add(h);
            }

            return true;
        }, IntPtr.Zero);
        return list;
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder text, int maxCount);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDlgItem(IntPtr hDlg, int id);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string? title);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
