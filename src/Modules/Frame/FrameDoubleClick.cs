using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Modules.Frame;

/// <summary>
/// Nhắp đúp vào block khung KB_A0…KB_A4 thì mở hộp sửa thông tin, thay cho trình sửa thuộc tính của AutoCAD.
/// </summary>
internal static class FrameDoubleClick
{
    private static bool _hooked;
    private static ObjectId _hit = ObjectId.Null;
    private static int _launchTries;
    private static short _savedDblClick = -1;
    private static System.Windows.Forms.Timer? _watch;
    private static TimerProc? _nativeProc;
    private static IntPtr _nativeTimer;

    /// <summary>0 = không chặn. 1 = đang mở hộp của mình. 2 = hộp đã đóng, vẫn chặn lệnh sửa chữ xếp sau đó.</summary>
    private static int _suppress;

    public static void Watch()
    {
        if (_hooked)
            return;
        _hooked = true;
        AcadApp.BeginDoubleClick += OnDoubleClick;
        var docs = AcadApp.DocumentManager;
        docs.DocumentCreated += OnDocumentCreated;
        foreach (Document doc in docs)
            Hook(doc);
    }

    public static void Stop()
    {
        if (!_hooked)
            return;
        _hooked = false;
        AcadApp.BeginDoubleClick -= OnDoubleClick;
        var docs = AcadApp.DocumentManager;
        docs.DocumentCreated -= OnDocumentCreated;
        foreach (Document doc in docs)
            doc.CommandWillStart -= OnCommand;
        AcadApp.Idle -= Launch;
        AcadApp.Idle -= EndSuppress;
        _hit = ObjectId.Null;
        _suppress = 0;
        RestoreDoubleClick();
        _watch?.Stop();
        KillNativeTimer();
    }

    private static void OnDocumentCreated(object sender, DocumentCollectionEventArgs e) => Hook(e.Document);

    private static void Hook(Document doc)
    {
        doc.CommandWillStart -= OnCommand;
        doc.CommandWillStart += OnCommand;
    }

    public static ObjectId TakePending()
    {
        var id = _hit;
        _hit = ObjectId.Null;
        _launchTries = 0;
        return id;
    }

    /// <summary>Gọi khi lệnh sửa khung kết thúc. Chặn thêm một nhịp để EATTEDIT xếp hàng không mở sau hộp thoại.</summary>
    public static void Release()
    {
        if (_suppress == 0)
        {
            RestoreDoubleClick();
            return;
        }

        _suppress = 0;
        RestoreDoubleClick();
        _watch?.Stop();
        KillNativeTimer();
    }

    private static void EndSuppress(object? sender, EventArgs e)
    {
        AcadApp.Idle -= EndSuppress;
        if (_suppress != 2)
            return;
        _suppress = 0;
        RestoreDoubleClick();
        _watch?.Stop();
        KillNativeTimer();
    }

    private static void OnDoubleClick(object sender, BeginDoubleClickEventArgs e)
    {
        if (!iSurvey.Auth.AuthManager.IsAuthenticated)
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;
        try
        {
            if (Convert.ToInt16(AcadApp.GetSystemVariable("BLOCKEDITOR")) != 0)
                return;
        }
        catch
        {
            // biến không có: vẫn thử nhận khung
        }

        if (!TryHit(doc, e.Location, out var id))
            return;
        _hit = id;
        _suppress = 1;
        _launchTries = 0;
        SuppressDoubleClick();
        ArmWatch();
        ArmNativeTimer();
        AcadApp.Idle -= Launch;
        AcadApp.Idle += Launch;
    }

    private static void OnCommand(object sender, CommandEventArgs e)
    {
        if (_suppress == 0 || !IsNativeEditor(e.GlobalCommandName))
            return;
        var doc = sender as Document ?? AcadApp.DocumentManager.MdiActiveDocument;
        doc?.SendStringToExecute("\x03", true, false, false);
        ArmNativeTimer();
        CloseNativeAttributeEditor();
        if (_suppress == 2)
        {
            _suppress = 0;
            AcadApp.Idle -= EndSuppress;
            RestoreDoubleClick();
            _watch?.Stop();
        KillNativeTimer();
        }
    }

    private static void ArmWatch()
    {
        if (_watch is null)
        {
            _watch = new System.Windows.Forms.Timer { Interval = 40 };
            _watch.Tick += (_, _) =>
            {
                if (_suppress == 0)
                {
                    _watch.Stop();
                    return;
                }

                CloseNativeAttributeEditor();
            };
        }

        _watch.Start();
    }

    private static void CloseNativeAttributeEditor()
    {
        var pid = (uint)Process.GetCurrentProcess().Id;
        EnumWindows((h, _) =>
        {
            GetWindowThreadProcessId(h, out var owner);
            if (owner != pid)
                return true;
            var sb = new StringBuilder(256);
            if (GetWindowText(h, sb, sb.Capacity) <= 0)
                return true;
            var title = sb.ToString();
            if (title.Contains("Attribute", StringComparison.OrdinalIgnoreCase))
            {
                PostMessage(h, 0x0100, (IntPtr)0x1B, IntPtr.Zero);
                PostMessage(h, 0x0101, (IntPtr)0x1B, IntPtr.Zero);
                PostMessage(h, 0x0010, IntPtr.Zero, IntPtr.Zero);
            }
            return true;
        }, IntPtr.Zero);
    }

    private static void ArmNativeTimer()
    {
        _nativeProc ??= NativeTick;
        if (_nativeTimer != IntPtr.Zero)
            return;
        _nativeTimer = SetTimer(IntPtr.Zero, IntPtr.Zero, 40, _nativeProc);
    }

    private static void NativeTick(IntPtr hwnd, uint msg, IntPtr idEvent, uint time)
    {
        if (_suppress == 0)
        {
            KillNativeTimer();
            return;
        }

        CloseNativeAttributeEditor();
    }

    private static void KillNativeTimer()
    {
        if (_nativeTimer == IntPtr.Zero)
            return;
        KillTimer(IntPtr.Zero, _nativeTimer);
        _nativeTimer = IntPtr.Zero;
    }

    private delegate void TimerProc(IntPtr hWnd, uint msg, IntPtr idEvent, uint time);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SetTimer(IntPtr hWnd, IntPtr nIDEvent, uint uElapse, TimerProc lpTimerFunc);

    [DllImport("user32.dll")]
    private static extern bool KillTimer(IntPtr hWnd, IntPtr uIDEvent);

    private static void SuppressDoubleClick()
    {
        if (_savedDblClick >= 0)
            return;
        try
        {
            var cur = Convert.ToInt16(AcadApp.GetSystemVariable("DBLCLKEDIT"));
            if (cur == 0)
                return;
            _savedDblClick = cur;
            AcadApp.SetSystemVariable("DBLCLKEDIT", 0);
        }
        catch
        {
            // giữ hành vi nhắp đúp sẵn có
        }
    }

    private static void RestoreDoubleClick()
    {
        if (_savedDblClick < 0)
            return;
        var value = _savedDblClick;
        _savedDblClick = -1;
        try
        {
            AcadApp.SetSystemVariable("DBLCLKEDIT", value);
        }
        catch
        {
            // không đặt lại được
        }
    }

    private static bool IsNativeEditor(string? name)
    {
        var cmd = (name ?? "").Trim().TrimStart('_', '.').ToUpperInvariant();
        return cmd is "EATTEDIT" or "ATTEDIT" or "-ATTEDIT" or "BEDIT" or "-BEDIT" or "REFEDIT"
            or "TEXTEDIT" or "DDEDIT" or "MTEDIT" or "QUICKPROPERTIES";
    }

    private static void Launch(object? sender, EventArgs e)
    {
        AcadApp.Idle -= Launch;
        if (_hit.IsNull)
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;
        int active;
        try
        {
            active = Convert.ToInt32(AcadApp.GetSystemVariable("CMDACTIVE"));
        }
        catch
        {
            active = 0;
        }

        if (active != 0)
        {
            if (++_launchTries > 8)
            {
                _hit = ObjectId.Null;
                _suppress = 0;
                RestoreDoubleClick();
                _watch?.Stop();
        KillNativeTimer();
                return;
            }

            AcadApp.Idle += Launch;
            return;
        }

        doc.SendStringToExecute("_.ISURVEY_FRAME_EDIT ", true, false, false);
    }

    private static bool TryHit(Document doc, Point3d wcs, out ObjectId frameId)
    {
        frameId = ObjectId.Null;
        var ed = doc.Editor;
        ObjectId[]? previous = null;
        try
        {
            var implied = ed.SelectImplied();
            if (implied.Status == PromptStatus.OK)
                previous = implied.Value.GetObjectIds();

            var view = ed.GetCurrentView();
            var screen = (Point2d)AcadApp.GetSystemVariable("SCREENSIZE");
            var pick = Convert.ToInt32(AcadApp.GetSystemVariable("PICKBOX"));
            if (pick < 3)
                pick = 3;
            var pixel = screen.Y > 1 ? view.Height / screen.Y : 1;
            var half = Math.Max(pick * pixel, 0.5);
            var p1 = new Point3d(wcs.X - half, wcs.Y - half, wcs.Z);
            var p2 = new Point3d(wcs.X + half, wcs.Y + half, wcs.Z);
            var res = ed.SelectCrossingWindow(p1, p2);
            if (res.Status != PromptStatus.OK)
                return false;

            using var tr = doc.Database.TransactionManager.StartTransaction();
            double bestArea = double.MaxValue;
            foreach (var entId in res.Value.GetObjectIds())
            {
                if (!TryFrame(tr, entId, out var br))
                    continue;
                Extents3d ext;
                try
                {
                    ext = br.GeometricExtents;
                }
                catch
                {
                    continue;
                }

                if (wcs.X < ext.MinPoint.X - half || wcs.X > ext.MaxPoint.X + half
                    || wcs.Y < ext.MinPoint.Y - half || wcs.Y > ext.MaxPoint.Y + half)
                    continue;
                var area = Math.Abs((ext.MaxPoint.X - ext.MinPoint.X) * (ext.MaxPoint.Y - ext.MinPoint.Y));
                if (area >= bestArea)
                    continue;
                bestArea = area;
                frameId = br.ObjectId;
            }

            tr.Commit();
            return !frameId.IsNull;
        }
        catch
        {
            return false;
        }
        finally
        {
            try
            {
                ed.SetImpliedSelection(previous ?? Array.Empty<ObjectId>());
            }
            catch
            {
                // không khôi phục được chọn cũ
            }
        }
    }

    private static bool TryFrame(Transaction tr, ObjectId id, out BlockReference frame)
    {
        frame = null!;
        var obj = tr.GetObject(id, OpenMode.ForRead);
        var br = obj as BlockReference;
        if (obj is AttributeReference ar)
            br = tr.GetObject(ar.OwnerId, OpenMode.ForRead) as BlockReference;
        if (br is null || br.IsErased)
            return false;
        if (!FrameService.IsFrameName(FrameService.BlockName(tr, br)))
            return false;
        frame = br;
        return true;
    }
}
