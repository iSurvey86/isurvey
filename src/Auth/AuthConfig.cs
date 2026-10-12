using System.Text.Json;

namespace iSurvey.Auth;

/// <summary>
/// Cấu hình Auth đọc từ %AppData%\iSurvey\auth.config.json (KHÔNG commit, KHÔNG nhúng vào DLL).
/// Thiếu file hoặc lỗi JSON → dùng mặc định rỗng (chế độ không SMTP).
/// </summary>
public sealed class AuthConfig
{
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUser { get; set; }
    public string? SmtpPassword { get; set; }
    public bool SmtpEnableSsl { get; set; } = true;
    public string? FromAddress { get; set; }
    public string? FromName { get; set; } = "iSurvey";

    /// <summary>Webhook (máy chủ trung gian) nhận {email, otp, subject, hwid} để gửi mail — ưu tiên hơn SMTP.</summary>
    public string? WebhookUrl { get; set; }

    /// <summary>Khóa bí mật sinh mã quản trị theo ngày (HMAC-SHA256). Chỉ khai báo trên máy Admin / máy được phép offline.</summary>
    public string? AdminSecret { get; set; }

    // ---------- Supabase (kênh chính, zero-config cho người dùng) ----------
    /// <summary>Điền sau khi tạo project Supabase (xem docs/Huong_dan_Supabase.md). Chỉ dùng publishable/anon key — KHÔNG dùng service_role.</summary>
    public const string DefaultSupabaseUrl = "https://lmraewcashlnfdawrvmg.supabase.co";
    public const string DefaultSupabaseKey = "sb_publishable_jxvwYq9YQhGNfuAgSA7mLg_y9ZAVmLj";

    /// <summary>Ghi đè hằng mặc định (auth.config.json). Để trống = dùng hằng mặc định.</summary>
    public string? SupabaseUrl { get; set; }
    public string? SupabaseKey { get; set; }

    public string ResolvedSupabaseUrl =>
        (string.IsNullOrWhiteSpace(SupabaseUrl) ? DefaultSupabaseUrl : SupabaseUrl).Trim().TrimEnd('/');

    public string ResolvedSupabaseKey =>
        (string.IsNullOrWhiteSpace(SupabaseKey) ? DefaultSupabaseKey : SupabaseKey).Trim();

    public bool HasSupabase =>
        ResolvedSupabaseKey.Length > 0
        && Uri.TryCreate(ResolvedSupabaseUrl, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback));

    public bool HasWebhook => !string.IsNullOrWhiteSpace(WebhookUrl);
    public bool HasSmtp => !string.IsNullOrWhiteSpace(SmtpHost) && !string.IsNullOrWhiteSpace(FromAddress);
    public bool HasAdminSecret => !string.IsNullOrWhiteSpace(AdminSecret);

    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "iSurvey", "auth.config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static AuthConfig Load(string? path = null)
    {
        try
        {
            path ??= DefaultPath;
            if (!File.Exists(path))
                return new AuthConfig();

            return JsonSerializer.Deserialize<AuthConfig>(File.ReadAllText(path), JsonOptions) ?? new AuthConfig();
        }
        catch
        {
            return new AuthConfig();
        }
    }
}
