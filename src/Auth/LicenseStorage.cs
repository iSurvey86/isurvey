using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace iSurvey.Auth;

public enum LicenseMode
{
    /// <summary>Kích hoạt qua Supabase (có refresh token, kiểm tra lại định kỳ được).</summary>
    Online,
    /// <summary>Kích hoạt bằng mã Admin/Debug (không internet) — không kiểm tra lại được, hết hạn sau 7 ngày.</summary>
    Offline
}

/// <summary>Thông tin giấy phép đã kích hoạt.</summary>
public sealed record LicenseInfo(
    string Email,
    string HardwareId,
    DateTimeOffset ActivatedAt,
    LicenseMode Mode = LicenseMode.Offline,
    DateTimeOffset? LastVerifiedAt = null,
    string? UserId = null,
    string? RefreshToken = null)
{
    /// <summary>Thời gian tối đa được dùng mà chưa xác minh lại (cả online lẫn offline).</summary>
    public static readonly TimeSpan OfflineGrace = TimeSpan.FromDays(7);

    /// <summary>Chu kỳ kiểm tra lại qua mạng (chạy nền khi mở Civil).</summary>
    public static readonly TimeSpan RecheckInterval = TimeSpan.FromHours(24);

    /// <summary>Lần xác minh gần nhất (mặc định = lúc kích hoạt).</summary>
    public DateTimeOffset VerifiedAt => LastVerifiedAt ?? ActivatedAt;

    public DateTimeOffset ExpiresAt => VerifiedAt + OfflineGrace;

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public bool NeedsRecheck(DateTimeOffset now) => now - VerifiedAt >= RecheckInterval;
}

/// <summary>
/// %AppData%\iSurvey\license.dat — JSON được mã hóa DPAPI (CurrentUser) kèm entropy từ MachineGuid,
/// có HMAC toàn vẹn. File copy sang máy/tài khoản khác không giải mã được, hoặc HWID/chữ ký không khớp → bị từ chối.
/// V2 thêm Mode, VerifiedAt, UserId, RefreshToken. File V1 cũ vẫn đọc được (coi là Offline).
/// </summary>
public sealed class LicenseStorage
{
    private sealed class Payload
    {
        public int V { get; set; } = 2;
        public string Email { get; set; } = "";
        public string HardwareId { get; set; } = "";
        public string ActivatedAt { get; set; } = "";
        public string? Mode { get; set; }
        public string? VerifiedAt { get; set; }
        public string? UserId { get; set; }
        public string? RefreshToken { get; set; }
        public string SignatureHash { get; set; } = "";
    }

    private readonly string _path;
    private readonly string _machineGuid;

    public LicenseStorage(string? path = null, string? machineGuid = null)
    {
        _path = path ?? DefaultPath;
        _machineGuid = machineGuid ?? Auth.HardwareId.MachineGuid;
    }

    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "iSurvey", "license.dat");

    public string FilePath => _path;

    private string CurrentHwid => Auth.HardwareId.FromMachineGuid(_machineGuid);

    private byte[] Entropy => Digest.Sha256(Encoding.UTF8.GetBytes("iSurvey|license-entropy|" + _machineGuid));

    private byte[] HmacKey => Digest.Sha256(Encoding.UTF8.GetBytes("iSurvey|license-hmac|" + _machineGuid));

    private string Sign(Payload p)
    {
        var text = p.V >= 2
            ? $"{p.V}|{p.Email}|{p.HardwareId}|{p.ActivatedAt}|{p.Mode}|{p.VerifiedAt}|{p.UserId}|{p.RefreshToken}"
            : $"{p.V}|{p.Email}|{p.HardwareId}|{p.ActivatedAt}";
        return Digest.Hex(Digest.HmacSha256(HmacKey, Encoding.UTF8.GetBytes(text)));
    }

    /// <summary>Kích hoạt mới (hoặc ghi đè). HWID luôn là của máy hiện tại.</summary>
    public LicenseInfo Save(string email, LicenseMode mode = LicenseMode.Offline, string? userId = null,
        string? refreshToken = null, DateTimeOffset? now = null)
    {
        var at = now ?? DateTimeOffset.Now;
        return Save(new LicenseInfo(email.Trim().ToLowerInvariant(), CurrentHwid, at, mode, at, userId, refreshToken));
    }

    /// <summary>Ghi nguyên thông tin (dùng khi cập nhật LastVerifiedAt / RefreshToken sau kiểm tra định kỳ).</summary>
    public LicenseInfo Save(LicenseInfo info)
    {
        var hwid = CurrentHwid;
        var payload = new Payload
        {
            V = 2,
            Email = info.Email.Trim().ToLowerInvariant(),
            HardwareId = hwid,
            ActivatedAt = info.ActivatedAt.ToString("o"),
            Mode = info.Mode.ToString(),
            VerifiedAt = info.VerifiedAt.ToString("o"),
            UserId = info.UserId,
            RefreshToken = info.RefreshToken
        };
        payload.SignatureHash = Sign(payload);

        var cipher = Dpapi.Protect(JsonSerializer.SerializeToUtf8Bytes(payload), Entropy);

        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var tmp = _path + ".tmp";
        File.WriteAllBytes(tmp, cipher);
        if (File.Exists(_path))
            File.Delete(_path);
        File.Move(tmp, _path);

        return info with
        {
            Email = payload.Email,
            HardwareId = hwid,
            ActivatedAt = DateTimeOffset.Parse(payload.ActivatedAt),
            LastVerifiedAt = DateTimeOffset.Parse(payload.VerifiedAt)
        };
    }

    /// <summary>Đọc và xác thực. Trả null nếu thiếu file, giải mã lỗi, HWID khác máy, hoặc chữ ký sai.</summary>
    public LicenseInfo? Load(out string? failReason)
    {
        failReason = null;
        try
        {
            if (!File.Exists(_path))
            {
                failReason = "missing";
                return null;
            }

            var plain = Dpapi.Unprotect(File.ReadAllBytes(_path), Entropy);
            var p = JsonSerializer.Deserialize<Payload>(plain);
            if (p is null || string.IsNullOrWhiteSpace(p.Email))
            {
                failReason = "corrupt";
                return null;
            }

            if (!string.Equals(p.HardwareId, CurrentHwid, StringComparison.Ordinal))
            {
                failReason = "other-machine";
                return null;
            }

            var expected = Encoding.UTF8.GetBytes(Sign(p));
            var actual = Encoding.UTF8.GetBytes(p.SignatureHash ?? "");
            if (!Digest.FixedEquals(expected, actual))
            {
                failReason = "bad-signature";
                return null;
            }

            if (!DateTimeOffset.TryParse(p.ActivatedAt, out var activatedAt))
            {
                failReason = "corrupt";
                return null;
            }

            var mode = LicenseMode.Offline;
            DateTimeOffset? verified = null;
            if (p.V >= 2)
            {
                if (!Enum.TryParse(p.Mode, out mode))
                    mode = LicenseMode.Offline;
                if (DateTimeOffset.TryParse(p.VerifiedAt, out var v))
                    verified = v;
            }

            return new LicenseInfo(p.Email, p.HardwareId, activatedAt, mode, verified ?? activatedAt, p.UserId, p.RefreshToken);
        }
        catch (Exception ex)
        {
            failReason = "unreadable: " + ex.GetType().Name;
            return null;
        }
    }

    public LicenseInfo? Load() => Load(out _);

    public void Delete()
    {
        try
        {
            if (File.Exists(_path))
                File.Delete(_path);
        }
        catch
        {
            // File bị khóa — lần sau Load vẫn xác thực lại
        }
    }
}
