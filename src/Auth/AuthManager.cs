using System.Reflection;
using System.Windows.Threading;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Auth;

/// <summary>
/// Trạng thái đăng nhập/kích hoạt toàn add-in. Initialize() chỉ đọc file (không mạng, không dialog).
/// Kiểm tra lại giấy phép qua mạng (mỗi 24 giờ) chạy NỀN sau khi Ribbon nạp, không chặn Initialize().
/// Thông báo Command Line dùng tiếng Việt không dấu (ASCII).
/// </summary>
public static class AuthManager
{
    /// <summary>Chờ Ribbon nạp xong rồi mới bắt đầu kiểm tra nền.</summary>
    private static readonly TimeSpan RecheckStartDelay = TimeSpan.FromSeconds(3);

    /// <summary>Tổng thời gian tối đa cho cả chuỗi refresh + check (timeout ngắn, không làm phiền người dùng).</summary>
    private static readonly TimeSpan RecheckTimeout = TimeSpan.FromSeconds(5);

    private static readonly LicenseStorage Storage = new();
    private static LicenseInfo? _license;
    private static OtpAuthService? _otp;
    private static Dispatcher? _ui;
    private static bool _expiredNotice;
    private static int _rechecking;

    /// <summary>Phát khi trạng thái đổi (đăng nhập/đăng xuất/mất hiệu lực) — Ribbon dùng để mở/khóa nút.</summary>
    public static event Action? StateChanged;

    public static bool IsAuthenticated => _license is not null;

    public static LicenseInfo? License => _license;

    /// <summary>Phiên bản đọc từ assembly (đặt trong csproj &lt;Version&gt;), dạng "v1.2.0".</summary>
    public static string AppVersion { get; } = ReadVersion();

    public static OtpAuthService Otp
    {
        get
        {
            _otp ??= new OtpAuthService(AuthConfig.Load());
            _otp.AppVersion = AppVersion.TrimStart('v');
            return _otp;
        }
    }

    /// <summary>Gọi trong Main.Initialize() (luồng chính): chỉ đọc license.dat, không mạng, không dialog.</summary>
    public static void Initialize()
    {
        _ui = Dispatcher.CurrentDispatcher;
        Refresh();
    }

    /// <summary>Đọc lại license.dat; giấy phép quá hạn xác minh (7 ngày) coi như chưa đăng nhập.</summary>
    public static void Refresh()
    {
        var loaded = Storage.Load();
        _expiredNotice = false;
        if (loaded is not null && loaded.IsExpired(DateTimeOffset.Now))
        {
            _expiredNotice = true;
            loaded = null;
        }

        var before = _license;
        _license = loaded;
        if (!Equals(before, loaded))
            RaiseChanged();
    }

    /// <summary>Đầu mỗi lệnh tính năng. Chưa đăng nhập → nhắc và mở LoginWindow; thành công → true để lệnh chạy tiếp.</summary>
    public static bool EnsureAuthenticated()
    {
        Refresh();
        if (IsAuthenticated)
            return true;

        Print(_expiredNotice
            ? "\n[iSurvey] Giay phep da qua 7 ngay chua xac minh truc tuyen. Vui long dang nhap lai de kich hoat!"
            : "\n[iSurvey] Vui long dang nhap Gmail de kich hoat ban quyen su dung!");
        return ShowLogin();
    }

    /// <summary>Mở cửa sổ đăng nhập (modal). Trả true nếu đã kích hoạt.</summary>
    public static bool ShowLogin()
    {
        try
        {
            Otp.Config = AuthConfig.Load();
            var window = new UI.Account.LoginWindow();
            AcadApp.ShowModalWindow(window);
        }
        catch (System.Exception ex)
        {
            Print($"\n[iSurvey] Khong mo duoc cua so dang nhap: {ex.Message}");
        }

        return IsAuthenticated;
    }

    /// <summary>Lưu giấy phép sau khi xác thực thành công. Có phiên Supabase → Online; ngược lại (mã Admin/Debug) → Offline.</summary>
    public static void Activate(string email, VerifyResult result)
    {
        var session = result.Session;
        _license = session is null
            ? Storage.Save(email, LicenseMode.Offline)
            : Storage.Save(email, LicenseMode.Online, session.UserId, session.RefreshToken);
        _expiredNotice = false;
        RaiseChanged();
    }

    public static void SignOut()
    {
        var old = _license;
        Storage.Delete();
        _license = null;
        RaiseChanged();

        // "Đổi máy": trả lại suất thiết bị trên server (nền, không chặn; lỗi thì Admin giải phóng thủ công).
        if (old is { Mode: LicenseMode.Online, RefreshToken: not null })
            _ = Task.Run(() => ReleaseDeviceAsync(old));
    }

    // ---------- Kiểm tra định kỳ (nền) ----------
    /// <summary>
    /// Gọi sau khi dựng Ribbon. Chạy hoàn toàn nền: lỗi/timeout mạng → âm thầm giữ kích hoạt (đến hết hạn 7 ngày).
    /// Chỉ khi server trả rõ ràng thiết bị bị khóa (is_active=false) hoặc không còn đăng ký mới khóa lại trên UI thread.
    /// </summary>
    public static void StartBackgroundRecheck()
    {
        var snapshot = _license;
        if (snapshot is null || snapshot.Mode != LicenseMode.Online || string.IsNullOrEmpty(snapshot.RefreshToken))
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(RecheckStartDelay).ConfigureAwait(false);

                var config = AuthConfig.Load();
                if (!config.HasSupabase)
                    return;

                await RecheckAsync(snapshot, new SupabaseAuthClient(config), DateTimeOffset.Now).ConfigureAwait(false);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[iSurvey] Recheck: " + ex.Message);
            }
        });
    }

    /// <summary>Chạy nền (luồng thread pool): gọi mạng, rồi chuyển kết quả sang UI thread. Không ném exception.</summary>
    internal static async Task RecheckAsync(LicenseInfo lic, ISupabaseAuth client, DateTimeOffset now)
    {
        if (Interlocked.Exchange(ref _rechecking, 1) == 1)
            return;

        try
        {
            var outcome = await LicenseRechecker.RunAsync(lic, client, now, RecheckTimeout).ConfigureAwait(false);
            if (outcome.Action is RecheckAction.Skip or RecheckAction.NoChange)
                return;

            PostToUi(() => ApplyRecheck(lic, outcome, DateTimeOffset.Now));
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[iSurvey] Recheck: " + ex.Message);
        }
        finally
        {
            Interlocked.Exchange(ref _rechecking, 0);
        }
    }

    /// <summary>Chạy trên UI thread: áp kết quả kiểm tra lên trạng thái và Ribbon.</summary>
    private static void ApplyRecheck(LicenseInfo checkedLicense, RecheckOutcome outcome, DateTimeOffset now)
    {
        // Người dùng đã đăng xuất / đổi tài khoản trong lúc kiểm tra → bỏ qua.
        if (_license is null || _license.Email != checkedLicense.Email || _license.HardwareId != checkedLicense.HardwareId)
            return;

        var updated = LicenseRechecker.Apply(_license, outcome, now);
        if (updated is null)
        {
            Storage.Delete();
            _license = null;
            RaiseChanged();
            Print("\n[iSurvey] Giay phep tren may nay da bi Admin thu hoi hoac khong con hieu luc. Cac tinh nang da bi khoa.");
            return;
        }

        try
        {
            _license = Storage.Save(updated);
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[iSurvey] Luu giay phep: " + ex.Message);
        }
    }

    private static async Task ReleaseDeviceAsync(LicenseInfo lic)
    {
        try
        {
            var cfg = AuthConfig.Load();
            if (!cfg.HasSupabase)
                return;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var client = new SupabaseAuthClient(cfg);
            var refreshed = await client.RefreshAsync(lic.RefreshToken!, cts.Token).ConfigureAwait(false);
            if (refreshed.Session is null)
                return;

            await client.ReleaseAsync(refreshed.Session.AccessToken, lic.HardwareId, cts.Token).ConfigureAwait(false);
        }
        catch
        {
            // Best-effort
        }
    }

    // ---------- Tiện ích ----------
    private static void PostToUi(Action action)
    {
        if (_ui is null)
        {
            action();
            return;
        }

        _ui.BeginInvoke(action);
    }

    public static void Print(string message)
    {
        System.Diagnostics.Debug.WriteLine(message.TrimStart('\n'));
        try
        {
            AcadApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(message);
        }
        catch
        {
            // Không có document — bỏ qua
        }
    }

    private static void RaiseChanged()
    {
        try
        {
            StateChanged?.Invoke();
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[iSurvey] StateChanged: " + ex.Message);
        }
    }

    private static string ReadVersion()
    {
        var asm = typeof(AuthManager).Assembly;
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info))
        {
            var plus = info.IndexOf('+');
            return "v" + (plus >= 0 ? info[..plus] : info);
        }

        var v = asm.GetName().Version;
        return v is null ? "v?" : $"v{v.Major}.{v.Minor}.{v.Build}";
    }
}
