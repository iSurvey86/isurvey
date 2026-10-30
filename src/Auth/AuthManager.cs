using System.Reflection;
using System.Runtime.CompilerServices;
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

    /// <summary>Hỏi server xem admin có cắt phiên / khóa máy không. Không cập nhật mốc "lần thấy".</summary>
    private static readonly TimeSpan SessionWatchInterval = TimeSpan.FromSeconds(15);

    private static readonly LicenseStorage Storage = new();
    private static LicenseInfo? _license;
    private static OtpAuthService? _otp;
    private static Dispatcher? _ui;
    private static bool _expiredNotice;
    private static int _rechecking;
    private static int _watching;
    private static int _watchStarted;
    private static System.Threading.Timer? _sessionWatch;

    // ---------- Token Supabase dùng chung (nhật ký + kiểm tra định kỳ) ----------
    // Supabase đổi refresh token mỗi lần dùng → mọi nơi gọi RefreshAsync phải đi qua TokenGate
    // và lưu token mới NGAY vào license.dat, nếu không lần dùng sau sẽ gặp token đã bị thu hồi.
    internal static readonly SemaphoreSlim TokenGate = new(1, 1);
    private static readonly object LicenseLock = new();
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(50);
    private static readonly TimeSpan RefreshBackoff = TimeSpan.FromMinutes(5);
    private static string? _accessToken;
    private static string? _accessOwner;
    private static DateTimeOffset _accessExpiry;
    private static DateTimeOffset _noRefreshUntil;

    /// <summary>Phát khi trạng thái đổi (đăng nhập/đăng xuất/mất hiệu lực) — Ribbon dùng để mở/khóa nút.</summary>
    public static event Action? StateChanged;

    public static bool IsAuthenticated => _license is not null;

    public static LicenseInfo? License => _license;

    /// <summary>Email đang kích hoạt (null nếu chưa đăng nhập).</summary>
    public static string? CurrentUserEmail
    {
        get
        {
            lock (LicenseLock)
                return _license?.Email;
        }
    }

    /// <summary>
    /// Email được xem panel/lệnh thử nghiệm (Admin-Only). So khớp không phân biệt hoa thường.
    /// Chỉ để ẩn UI / chặn command — không thay thế kiểm soát phía server.
    /// </summary>
    private static readonly HashSet<string> AdminEmails = new(StringComparer.OrdinalIgnoreCase)
    {
        "minhphuong.npsc@gmail.com",
    };

    /// <summary>True nếu email giấy phép hiện tại nằm trong danh sách Admin.</summary>
    public static bool IsAdmin()
    {
        var email = CurrentUserEmail;
        return !string.IsNullOrWhiteSpace(email) && AdminEmails.Contains(email.Trim());
    }

    /// <summary>
    /// Đảm bảo đã đăng nhập và là Admin. Dùng đầu các lệnh thử nghiệm (sau EnsureAuthenticated hoặc thay thế).
    /// </summary>
    public static bool EnsureAdmin([CallerMemberName] string? commandName = null)
    {
        if (!EnsureAuthenticated(commandName))
            return false;
        if (IsAdmin())
            return true;

        Print("\n[iSurvey] Lenh nay chi danh cho Admin hoac dang trong qua trinh thu nghiem.");
        return false;
    }

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
        LicenseInfo? before;
        LicenseInfo? loaded;
        lock (LicenseLock)
        {
            loaded = Storage.Load();
            _expiredNotice = false;
            if (loaded is not null && loaded.IsExpired(DateTimeOffset.Now))
            {
                _expiredNotice = true;
                loaded = null;
            }

            before = _license;
            _license = loaded;
        }

        // Chỉ đổi refresh token (nền) thì không cần dựng lại Ribbon.
        if (!Equals(before is null ? null : before with { RefreshToken = null },
                loaded is null ? null : loaded with { RefreshToken = null }))
            RaiseChanged();
    }

    /// <summary>Đầu mỗi lệnh tính năng. Chưa đăng nhập → nhắc và mở LoginWindow; thành công → true để lệnh chạy tiếp.</summary>
    public static bool EnsureAuthenticated([CallerMemberName] string? commandName = null, bool track = true)
    {
        Refresh();
        if (IsAuthenticated)
        {
            if (track && !string.IsNullOrWhiteSpace(commandName))
                ActivityLogger.Enqueue("command", commandName);
            return true;
        }

        Print(_expiredNotice
            ? "\n[iSurvey] Giay phep da qua 7 ngay chua xac minh truc tuyen. Vui long dang nhap lai de kich hoat!"
            : "\n[iSurvey] Vui long dang nhap Gmail de kich hoat ban quyen su dung!");
        var ok = ShowLogin();
        if (track && ok && !string.IsNullOrWhiteSpace(commandName))
            ActivityLogger.Enqueue("command", commandName);
        return ok;
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
        ClearAccessToken();
        RaiseChanged();
        ActivityLogger.Enqueue("login", detail: new { email, mode = _license.Mode.ToString() });
        if (_license.Mode == LicenseMode.Online)
            ActivityLogger.TrackHeartbeat("login");
    }

    public static void SignOut()
    {
        var old = _license;
        Storage.Delete();
        _license = null;
        RaiseChanged();

        // "Đổi máy": ghi nhật ký đăng xuất + trả lại suất thiết bị trên server
        // (nền, không chặn; lỗi thì Admin giải phóng thủ công).
        if (old is { Mode: LicenseMode.Online, RefreshToken: not null })
            _ = Task.Run(() => ReleaseDeviceAsync(old));
    }

    /// <summary>Lưu refresh token mới (an toàn đa luồng) — gọi ngay sau mỗi lần RefreshAsync thành công.</summary>
    internal static void PersistRefreshToken(string newToken, string ownerEmail)
    {
        if (string.IsNullOrWhiteSpace(newToken))
            return;

        lock (LicenseLock)
        {
            var cur = _license;
            if (cur is null || cur.Mode != LicenseMode.Online
                || !string.Equals(cur.Email, ownerEmail, StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                _license = Storage.Save(cur with { RefreshToken = newToken });
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[iSurvey] Luu refresh token: " + ex.Message);
            }
        }
    }

    /// <summary>Server từ chối access token (401): quên token đang cache để lần sau refresh lại.</summary>
    internal static void InvalidateAccessToken()
    {
        _accessToken = null;
    }

    private static void ClearAccessToken()
    {
        _accessToken = null;
        _accessOwner = null;
        _noRefreshUntil = default;
    }

    /// <summary>
    /// Access token Online hiện hành (nền, chặn luồng gọi). Dùng lại token đang còn hạn; hết hạn thì refresh
    /// một lần và lưu refresh token mới. Lỗi refresh → nghỉ 5 phút, không thử lại dồn dập. Không ném exception.
    /// </summary>
    internal static string? AcquireAccessToken(CancellationToken ct = default)
    {
        try
        {
            TokenGate.Wait(ct);
        }
        catch (OperationCanceledException)
        {
            return null;
        }

        try
        {
            var lic = _license;
            return lic is { Mode: LicenseMode.Online } && !string.IsNullOrWhiteSpace(lic.RefreshToken)
                ? AcquireAccessTokenLocked(lic, persist: true, ct)
                : null;
        }
        finally
        {
            TokenGate.Release();
        }
    }

    /// <summary>Cần đang giữ TokenGate. persist=false khi giấy phép sắp bị xóa (đăng xuất).</summary>
    private static string? AcquireAccessTokenLocked(LicenseInfo lic, bool persist, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        if (_accessToken is not null && _accessOwner == lic.Email && now < _accessExpiry)
            return _accessToken;

        if (now < _noRefreshUntil)
            return null;

        try
        {
            var config = AuthConfig.Load();
            if (!config.HasSupabase)
                return null;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(8));
            var refreshed = new SupabaseAuthClient(config).RefreshAsync(lic.RefreshToken!, cts.Token)
                .GetAwaiter().GetResult();
            if (refreshed.Session is null)
            {
                // Token hỏng/hết hạn hoặc mất mạng: không dồn dập; kiểm tra định kỳ vẫn tự xử lý (giữ kích hoạt).
                _noRefreshUntil = now + RefreshBackoff;
                return null;
            }

            if (persist)
                PersistRefreshToken(refreshed.Session.RefreshToken, lic.Email);

            _accessToken = refreshed.Session.AccessToken;
            _accessOwner = lic.Email;
            _accessExpiry = now + AccessTokenLifetime;
            return _accessToken;
        }
        catch
        {
            _noRefreshUntil = now + RefreshBackoff;
            return null;
        }
    }

    // ---------- Kiểm tra định kỳ (nền) ----------
    /// <summary>
    /// Gọi sau khi dựng Ribbon. Chạy hoàn toàn nền: lỗi/timeout mạng → âm thầm giữ kích hoạt (đến hết hạn 7 ngày).
    /// Chỉ khi server trả rõ ràng thiết bị bị khóa (is_active=false) hoặc không còn đăng ký mới khóa lại trên UI thread.
    /// </summary>
    public static void StartBackgroundRecheck()
    {
        StartSessionWatch();

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

        var gateTaken = false;
        try
        {
            // Cùng cổng với ActivityLogger: không ai dùng refresh token cũ trong lúc người kia đang đổi token.
            await TokenGate.WaitAsync().ConfigureAwait(false);
            gateTaken = true;

            // Luôn dùng giấy phép HIỆN HÀNH (token có thể đã được ActivityLogger đổi sau khi snapshot).
            var current = _license;
            if (current is null || current.Email != lic.Email || current.HardwareId != lic.HardwareId)
                return;
            lic = current;

            var outcome = await LicenseRechecker.RunAsync(lic, client, now, RecheckTimeout).ConfigureAwait(false);

            // Lưu token mới ngay (trong cổng), trước khi nhả cho ActivityLogger.
            if (!string.IsNullOrEmpty(outcome.NewRefreshToken))
                PersistRefreshToken(outcome.NewRefreshToken!, lic.Email);

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
            if (gateTaken)
                TokenGate.Release();
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
            lock (LicenseLock)
            {
                Storage.Delete();
                _license = null;
            }

            ClearAccessToken();
            RaiseChanged();
            if (outcome.Action == RecheckAction.Logout)
            {
                Print("\n[iSurvey] Admin da dang xuat phien tren may nay. Hay dang nhap lai.");
                try
                {
                    UI.Account.DarkDialog.ShowInfo(
                        "ĐĂNG XUẤT",
                        "Admin đã đăng xuất phiên trên máy này",
                        note: "Máy không bị khóa. Nhập lại Gmail và mã OTP để dùng tiếp.");
                    ShowLogin();
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("[iSurvey] Hop thoai dang xuat: " + ex.Message);
                }
            }
            else
            {
                Print("\n[iSurvey] Giay phep tren may nay da bi Admin thu hoi hoac khong con hieu luc. Cac tinh nang da bi khoa.");
            }

            return;
        }

        try
        {
            lock (LicenseLock)
            {
                // Token mới nhất nằm trong _license (đã lưu trong cổng) — không để bản kiểm tra cũ ghi đè.
                var latestToken = _license?.RefreshToken;
                if (!string.IsNullOrEmpty(latestToken))
                    updated = updated with { RefreshToken = latestToken };
                _license = Storage.Save(updated);
            }

            if (outcome.Action == RecheckAction.Verified)
                ActivityLogger.TrackHeartbeat("recheck");
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[iSurvey] Luu giay phep: " + ex.Message);
        }
    }

    /// <summary>
    /// Trong lúc Civil mở: khoảng 15 giây một lần hỏi cờ đăng xuất / khóa.
    /// Chạy cả khi lúc mở Civil chưa đăng nhập, để phiên đăng nhập sau đó vẫn bị cắt được.
    /// </summary>
    private static void StartSessionWatch()
    {
        if (Interlocked.Exchange(ref _watchStarted, 1) == 1)
            return;

        _sessionWatch = new System.Threading.Timer(
            _ => { _ = WatchSessionAsync(); },
            null,
            TimeSpan.FromSeconds(5),
            SessionWatchInterval);
    }

    private static async Task WatchSessionAsync()
    {
        if (Interlocked.Exchange(ref _watching, 1) == 1)
            return;

        try
        {
            var lic = _license;
            if (lic is not { Mode: LicenseMode.Online } || string.IsNullOrEmpty(lic.RefreshToken))
                return;

            var config = AuthConfig.Load();
            if (!config.HasSupabase)
                return;

            var access = AcquireAccessToken();
            if (access is null)
                return;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var check = await new SupabaseAuthClient(config)
                .CheckAsync(access, lic.HardwareId, cts.Token, touch: false)
                .ConfigureAwait(false);
            if (!check.Result.Ok)
                return;

            var action = check.Status switch
            {
                DeviceStatus.Logout => RecheckAction.Logout,
                DeviceStatus.Blocked or DeviceStatus.NotRegistered => RecheckAction.Revoke,
                _ => (RecheckAction?)null
            };
            if (action is null)
                return;

            var current = _license;
            if (current is null || current.Email != lic.Email || current.HardwareId != lic.HardwareId)
                return;

            if (action == RecheckAction.Logout)
            {
                try
                {
                    ActivityLogger.SendNow(access, "remote_logout", null,
                        new { email = lic.Email }, lic.HardwareId, CancellationToken.None);
                }
                catch
                {
                    // Nhật ký không chặn việc cắt phiên.
                }
            }

            PostToUi(() => ApplyRecheck(lic, new RecheckOutcome(action.Value), DateTimeOffset.Now));
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[iSurvey] Session watch: " + ex.Message);
        }
        finally
        {
            Interlocked.Exchange(ref _watching, 0);
        }
    }

    private static async Task ReleaseDeviceAsync(LicenseInfo lic)
    {
        try
        {
            var cfg = AuthConfig.Load();
            if (!cfg.HasSupabase)
                return;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            var client = new SupabaseAuthClient(cfg);

            string? access;
            await TokenGate.WaitAsync(cts.Token).ConfigureAwait(false);
            try
            {
                // Giấy phép đã bị xóa khỏi máy → không lưu refresh token mới (persist=false).
                access = AcquireAccessTokenLocked(lic, persist: false, cts.Token);
            }
            finally
            {
                TokenGate.Release();
            }

            if (access is null)
                return;

            ActivityLogger.SendNow(access, "logout", null, new { email = lic.Email }, lic.HardwareId, cts.Token);
            await client.ReleaseAsync(access, lic.HardwareId, cts.Token).ConfigureAwait(false);
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
