using System.Net.Http;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace iSurvey.Auth;

/// <summary>Kênh gửi OTP kiểu cũ (SMTP / webhook trên máy khách) — chỉ dùng khi KHÔNG cấu hình Supabase.</summary>
public interface IOtpTransport
{
    /// <exception cref="OtpNotConfiguredException">Chưa cấu hình SMTP/webhook.</exception>
    Task SendAsync(string email, string code, string subject, string body, CancellationToken ct);
}

public sealed class OtpNotConfiguredException : Exception
{
    public OtpNotConfiguredException() : base("Chua cau hinh SMTP/webhook") { }
}

public enum SendOutcome
{
    Delivered,
    NotConfigured,
    NetworkFailed,
    InvalidEmail,
    Cooldown,
    /// <summary>Máy chủ (Supabase) giới hạn tần suất gửi mail.</summary>
    RateLimited
}

/// <param name="Detail">Chi tiết kỹ thuật (ASCII) để in lên Command Line giúp Admin chẩn đoán.</param>
public sealed record SendResult(SendOutcome Outcome, int CooldownSeconds = 0, string? TestHint = null, string? Detail = null)
{
    /// <summary>OTP đã gửi thật. Nếu false (chưa cấu hình/mất mạng), người dùng nhận mã quản trị từ Admin.</summary>
    public bool Delivered => Outcome == SendOutcome.Delivered;
}

public enum VerifyOutcome
{
    Ok,
    /// <summary>Sai mã hoặc hết hạn (Supabase gộp hai trường hợp này).</summary>
    WrongCode,
    /// <summary>Mã local hết hạn (chỉ có ở chế độ không Supabase).</summary>
    Expired,
    LockedOut,
    NoPending,
    InvalidFormat,
    /// <summary>Admin đã khóa thiết bị/tài khoản (is_active = false).</summary>
    Blocked,
    /// <summary>Gmail này đã dùng đủ số thiết bị cho phép.</summary>
    DeviceLimit,
    /// <summary>Supabase giới hạn tần suất (HTTP 429) — không tính lượt thử.</summary>
    RateLimited,
    /// <summary>Mất mạng / máy chủ lỗi — không tính lượt thử.</summary>
    NetworkError,
    /// <summary>Máy chủ từ chối kích hoạt vì lý do khác (email/HWID không hợp lệ, phiên hỏng...).</summary>
    ServerRejected
}

/// <param name="JustLocked">true khi chính lần nhập sai này làm đủ <see cref="OtpAuthService.MaxAttempts"/> và kích hoạt khóa.</param>
/// <param name="Session">Phiên Supabase khi kích hoạt online thành công; null khi kích hoạt bằng mã Admin/Debug (offline).</param>
/// <param name="MaxDevices">Số thiết bị tối đa (khi DeviceLimit).</param>
public sealed record VerifyResult(
    VerifyOutcome Outcome,
    int AttemptsLeft = 0,
    int LockedMinutes = 0,
    bool JustLocked = false,
    SbSession? Session = null,
    int MaxDevices = 0,
    string? Detail = null)
{
    public bool Ok => Outcome == VerifyOutcome.Ok;
}

/// <summary>
/// OTP 6 số, tối đa 5 lần nhập sai (khóa 5 phút), gửi lại sau 60 giây, chỉ nhận Gmail.
/// <list type="bullet">
/// <item>Kênh chính: Supabase (OTP do server sinh/gửi; kích hoạt thiết bị qua RPC isurvey_activate).</item>
/// <item>Dự phòng offline: mã quản trị theo ngày = HMAC-SHA256(AdminSecret, email|yyyyMMdd) → 6 số;
/// bản Debug có thêm mã cố định <see cref="DebugMasterCode"/> (không tồn tại trong Release).</item>
/// <item>Chế độ cũ (không cấu hình Supabase): OTP sinh trên máy, gửi bằng SMTP/webhook cục bộ nếu có.</item>
/// </list>
/// </summary>
public sealed class OtpAuthService
{
    public const int CodeLength = 6;
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);
    public const int MaxAttempts = 5;
    public const string DebugMasterCode = "839205";

    private sealed class Pending
    {
        public string Email = "";
        public byte[] Salt = [];
        public byte[] Hash = [];
        public DateTime ExpiresAt;
    }

    private readonly Func<DateTime> _now;
    private readonly IOtpTransport _transport;
    private readonly ISupabaseAuth? _onlineOverride;
    private ISupabaseAuth? _onlineCached;
    private string _onlineCacheKey = "";

    private Pending? _pending;
    private DateTime _lastSentAt = DateTime.MinValue;
    private string _lastSentEmail = "";
    private int _failures;
    private DateTime _lockedUntil = DateTime.MinValue;

    public AuthConfig Config { get; set; }

    /// <summary>Gửi kèm khi đăng ký thiết bị (do AuthManager đặt).</summary>
    public string AppVersion { get; set; } = "";

    public string MachineName { get; set; } = Environment.MachineName;

    public OtpAuthService(AuthConfig config, Func<DateTime>? clock = null, IOtpTransport? transport = null,
        ISupabaseAuth? online = null)
    {
        Config = config;
        _now = clock ?? (() => DateTime.Now);
        _transport = transport ?? new DefaultTransport(() => Config);
        _onlineOverride = online;
    }

    /// <summary>Client Supabase đang hiệu lực (null nếu chưa cấu hình URL/khóa).</summary>
    private ISupabaseAuth? Online
    {
        get
        {
            if (_onlineOverride is not null)
                return _onlineOverride;
            if (!Config.HasSupabase)
                return null;

            var key = Config.ResolvedSupabaseUrl + "|" + Config.ResolvedSupabaseKey;
            if (_onlineCached is null || key != _onlineCacheKey)
            {
                _onlineCached = new SupabaseAuthClient(Config);
                _onlineCacheKey = key;
            }

            return _onlineCached;
        }
    }

    public bool OnlineConfigured => Online is not null;

    // ---------- Email ----------
    public static bool TryNormalizeGmail(string? input, out string email)
    {
        email = "";
        if (string.IsNullOrWhiteSpace(input))
            return false;

        try
        {
            var addr = new MailAddress(input.Trim());
            var host = addr.Host.ToLowerInvariant();
            if (host is not ("gmail.com" or "googlemail.com"))
                return false;
            if (!string.Equals(addr.Address, input.Trim(), StringComparison.OrdinalIgnoreCase))
                return false;

            email = addr.Address.ToLowerInvariant();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ---------- Mã quản trị theo ngày ----------
    /// <summary>6 số từ HMAC-SHA256(secret, "iSurvey-OTP|email|yyyyMMdd").</summary>
    public static string ComputeAdminCode(string secret, string email, DateTime date)
    {
        var msg = Encoding.UTF8.GetBytes($"iSurvey-OTP|{email.Trim().ToLowerInvariant()}|{date:yyyyMMdd}");
        var mac = Digest.HmacSha256(Encoding.UTF8.GetBytes(secret), msg);
        var n = (uint)((mac[0] << 24) | (mac[1] << 16) | (mac[2] << 8) | mac[3]);
        return (n % 1_000_000u).ToString("D6");
    }

    // ---------- Gửi ----------
    public async Task<SendResult> SendAsync(string emailInput, CancellationToken ct = default)
    {
        if (!TryNormalizeGmail(emailInput, out var email))
            return new SendResult(SendOutcome.InvalidEmail);

        var now = _now();
        if (_lastSentEmail == email && now - _lastSentAt < ResendCooldown)
        {
            var left = (int)Math.Ceiling((ResendCooldown - (now - _lastSentAt)).TotalSeconds);
            return new SendResult(SendOutcome.Cooldown, left);
        }

        _lastSentAt = now;
        _lastSentEmail = email;

        var online = Online;
        if (online is not null)
        {
            _pending = null;
            var r = await online.SendOtpAsync(email, ct).ConfigureAwait(true);
            return r.Outcome switch
            {
                SbOutcome.Ok => new SendResult(SendOutcome.Delivered),
                SbOutcome.RateLimited => new SendResult(SendOutcome.RateLimited, Detail: r.Describe()),
                SbOutcome.Rejected when r.ErrorCode == "email_address_invalid" =>
                    new SendResult(SendOutcome.InvalidEmail, Detail: r.Describe()),
                _ => new SendResult(SendOutcome.NetworkFailed, TestHint: BuildHint(email), Detail: r.Describe())
            };
        }

        var code = NextRandomInt(1_000_000).ToString("D6");
        var salt = NextRandomBytes(16);
        _pending = new Pending
        {
            Email = email,
            Salt = salt,
            Hash = HashCode(salt, code),
            ExpiresAt = now + CodeLifetime
        };

        var subject = $"[iSurvey] Ma kich hoat ban quyen (OTP): {code}";
        var body = $"Ma kich hoat iSurvey cua ban la: {code}\r\n" +
                   $"Ma co hieu luc {CodeLifetime.TotalMinutes:0} phut. Khong chia se ma nay cho nguoi khac.\r\n" +
                   $"Ma thiet bi: {HardwareId.Current}";

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(20));
            await _transport.SendAsync(email, code, subject, body, cts.Token).ConfigureAwait(true);
            return new SendResult(SendOutcome.Delivered);
        }
        catch (OtpNotConfiguredException)
        {
            return new SendResult(SendOutcome.NotConfigured, TestHint: BuildHint(email));
        }
        catch (Exception)
        {
            return new SendResult(SendOutcome.NetworkFailed, TestHint: BuildHint(email));
        }
    }

    /// <summary>Gợi ý mã để test: chỉ có ở bản Debug hoặc máy có AdminSecret. Bản Release máy thường → null.</summary>
    private string? BuildHint(string email)
    {
        if (Config.HasAdminSecret)
            return $"Ma goi y (may co AdminSecret): {ComputeAdminCode(Config.AdminSecret!, email, _now())}";
#if DEBUG
        return $"Ma goi y (Debug): {DebugMasterCode}";
#else
        return null;
#endif
    }

    // ---------- Kiểm tra: các mảnh dùng chung ----------
    /// <summary>Khóa / sai định dạng. Trả null nếu hợp lệ để đi tiếp.</summary>
    private VerifyResult? Precheck(string emailInput, string? codeInput, DateTime now, out string email)
    {
        email = "";
        if (now < _lockedUntil)
            return new VerifyResult(VerifyOutcome.LockedOut, 0, (int)Math.Ceiling((_lockedUntil - now).TotalMinutes));

        if (!TryNormalizeGmail(emailInput, out email) || codeInput is null || codeInput.Length != CodeLength
            || !codeInput.All(Ascii.IsDigit))
            return new VerifyResult(VerifyOutcome.InvalidFormat);

        return null;
    }

    private Pending? LocalPending(string email) => _pending is { } p && p.Email == email ? p : null;

    private bool AdminCodesAvailable
    {
        get
        {
#if DEBUG
            return true;
#else
            return Config.HasAdminSecret;
#endif
        }
    }

    /// <summary>Mã local: OTP sinh trên máy (chế độ cũ), mã admin theo ngày, mã Debug. Không cần mạng.</summary>
    private bool LocalMatch(string email, string code, DateTime now, Pending? pending)
    {
        var match = false;

        if (pending is not null && now <= pending.ExpiresAt)
            match |= FixedEquals(pending.Hash, HashCode(pending.Salt, code));

        if (Config.HasAdminSecret)
            match |= FixedEquals(Encoding.ASCII.GetBytes(ComputeAdminCode(Config.AdminSecret!, email, now)),
                Encoding.ASCII.GetBytes(code));
#if DEBUG
        match |= FixedEquals(Encoding.ASCII.GetBytes(DebugMasterCode), Encoding.ASCII.GetBytes(code));
#endif
        return match;
    }

    private VerifyResult Success(SbSession? session = null)
    {
        _pending = null;
        _failures = 0;
        return new VerifyResult(VerifyOutcome.Ok, Session: session);
    }

    /// <summary>Ghi nhận một lần nhập sai (tính vào giới hạn 5 lần).</summary>
    private VerifyResult RegisterFailure(DateTime now, Pending? pending, string? detail = null)
    {
        _failures++;
        if (_failures >= MaxAttempts)
        {
            _failures = 0;
            _pending = null;
            _lockedUntil = now + LockoutDuration;
            return new VerifyResult(VerifyOutcome.LockedOut, 0, (int)LockoutDuration.TotalMinutes, JustLocked: true,
                Detail: detail);
        }

        if (pending is not null && now > pending.ExpiresAt)
            return new VerifyResult(VerifyOutcome.Expired, MaxAttempts - _failures, Detail: detail);

        return new VerifyResult(VerifyOutcome.WrongCode, MaxAttempts - _failures, Detail: detail);
    }

    // ---------- Kiểm tra: chỉ local (không mạng) ----------
    public VerifyResult Verify(string emailInput, string? codeInput)
    {
        var now = _now();
        if (Precheck(emailInput, codeInput, now, out var email) is { } pre)
            return pre;

        var pending = LocalPending(email);
        if (pending is null && !AdminCodesAvailable)
            return new VerifyResult(VerifyOutcome.NoPending);

        return LocalMatch(email, codeInput!, now, pending) ? Success() : RegisterFailure(now, pending);
    }

    // ---------- Kiểm tra: đầy đủ (local trước, rồi Supabase) ----------
    public async Task<VerifyResult> VerifyAsync(string emailInput, string? codeInput, CancellationToken ct = default)
    {
        var now = _now();
        if (Precheck(emailInput, codeInput, now, out var email) is { } pre)
            return pre;

        var code = codeInput!;
        var pending = LocalPending(email);

        // Mã local (admin/Debug/OTP cục bộ) kiểm tra trước: không cần mạng, vẫn dùng được khi đi hiện trường.
        if (LocalMatch(email, code, now, pending))
            return Success();

        var online = Online;
        if (online is null)
        {
            if (pending is null && !AdminCodesAvailable)
                return new VerifyResult(VerifyOutcome.NoPending);
            return RegisterFailure(now, pending);
        }

        var v = await online.VerifyOtpAsync(email, code, ct).ConfigureAwait(true);
        switch (v.Result.Outcome)
        {
            case SbOutcome.Ok when v.Session is not null:
                return await ActivateDeviceAsync(online, v.Session, ct).ConfigureAwait(true);

            case SbOutcome.Rejected:
                // Supabase gộp "sai mã" và "hết hạn": tính một lượt thử.
                return RegisterFailure(now, null, v.Result.Describe());

            case SbOutcome.RateLimited:
                return new VerifyResult(VerifyOutcome.RateLimited, Detail: v.Result.Describe());

            default:
                return new VerifyResult(VerifyOutcome.NetworkError, Detail: v.Result.Describe());
        }
    }

    private async Task<VerifyResult> ActivateDeviceAsync(ISupabaseAuth online, SbSession session, CancellationToken ct)
    {
        // Mã OTP đã bị tiêu thụ ở bước verify: thử lại một lần nếu chỉ là lỗi mạng thoáng qua.
        SbDeviceResult act = default!;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            act = await online.ActivateAsync(session.AccessToken, HardwareId.Current, MachineName, AppVersion, ct)
                .ConfigureAwait(true);
            if (act.Result.Outcome is not (SbOutcome.Network or SbOutcome.ServerError))
                break;
        }

        var detail = act.Result.Describe();
        if (!act.Result.Ok)
        {
            return act.Result.Outcome == SbOutcome.RateLimited
                ? new VerifyResult(VerifyOutcome.RateLimited, Detail: detail)
                : act.Result.Outcome == SbOutcome.Rejected
                    ? new VerifyResult(VerifyOutcome.ServerRejected, Detail: detail)
                    : new VerifyResult(VerifyOutcome.NetworkError, Detail: detail);
        }

        return act.Status switch
        {
            DeviceStatus.Ok => Success(session),
            DeviceStatus.Blocked => new VerifyResult(VerifyOutcome.Blocked),
            DeviceStatus.DeviceLimit => new VerifyResult(VerifyOutcome.DeviceLimit, MaxDevices: act.MaxDevices),
            _ => new VerifyResult(VerifyOutcome.ServerRejected, Detail: "RPC status " + act.Status)
        };
    }

    private static byte[] HashCode(byte[] salt, string code)
    {
        var text = Encoding.ASCII.GetBytes(code);
        var buf = new byte[salt.Length + text.Length];
        Buffer.BlockCopy(salt, 0, buf, 0, salt.Length);
        Buffer.BlockCopy(text, 0, buf, salt.Length, text.Length);
        return Digest.Sha256(buf);
    }

    private static bool FixedEquals(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            return false;
        var diff = 0;
        for (var i = 0; i < a.Length; i++)
            diff |= a[i] ^ b[i];
        return diff == 0;
    }

    private static int NextRandomInt(int maxExclusive)
    {
        var buf = new byte[4];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(buf);
        return (int)(BitConverter.ToUInt32(buf, 0) % (uint)maxExclusive);
    }

    private static byte[] NextRandomBytes(int count)
    {
        var buf = new byte[count];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(buf);
        return buf;
    }

    // ---------- Kênh gửi cũ (chỉ khi không có Supabase) ----------
    private sealed class DefaultTransport(Func<AuthConfig> getConfig) : IOtpTransport
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

        public async Task SendAsync(string email, string code, string subject, string body, CancellationToken ct)
        {
            var cfg = getConfig();

            if (cfg.HasWebhook)
            {
                var json = JsonSerializer.Serialize(new { email, otp = code, subject, body, hwid = HardwareId.Current });
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var resp = await Http.PostAsync(cfg.WebhookUrl, content, ct).ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();
                return;
            }

            if (!cfg.HasSmtp)
                throw new OtpNotConfiguredException();

            using var smtp = new SmtpClient(cfg.SmtpHost, cfg.SmtpPort)
            {
                EnableSsl = cfg.SmtpEnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 20000
            };
            if (!string.IsNullOrWhiteSpace(cfg.SmtpUser))
                smtp.Credentials = new System.Net.NetworkCredential(cfg.SmtpUser, cfg.SmtpPassword);

            using var msg = new MailMessage
            {
                From = new MailAddress(cfg.FromAddress!, cfg.FromName ?? "iSurvey"),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            msg.To.Add(email);
            ct.ThrowIfCancellationRequested();
            await smtp.SendMailAsync(msg).ConfigureAwait(false);
        }
    }
}
