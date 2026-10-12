using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace iSurvey.Auth;

public enum SbOutcome
{
    Ok,
    /// <summary>Server từ chối rõ ràng (HTTP 4xx, ví dụ mã sai/hết hạn).</summary>
    Rejected,
    /// <summary>HTTP 429 hoặc lỗi *_rate_limit — KHÔNG tính vào lượt thử.</summary>
    RateLimited,
    /// <summary>HTTP 5xx hoặc phản hồi không đọc được.</summary>
    ServerError,
    /// <summary>Mất mạng / timeout / lỗi TLS.</summary>
    Network
}

public sealed record SbResult(SbOutcome Outcome, int HttpStatus = 0, string? ErrorCode = null, string? Message = null)
{
    public bool Ok => Outcome == SbOutcome.Ok;

    public string Describe() =>
        Ok ? "ok" : $"{Outcome} (HTTP {HttpStatus}{(ErrorCode is null ? "" : ", " + ErrorCode)}){(Message is null ? "" : ": " + Message)}";
}

public sealed record SbSession(string UserId, string Email, string AccessToken, string RefreshToken);

public sealed record SbVerifyResult(SbResult Result, SbSession? Session);

public enum DeviceStatus
{
    Ok,
    Blocked,
    DeviceLimit,
    NotRegistered,
    InvalidEmail,
    Unauthenticated,
    Released,
    Unknown
}

public sealed record SbDeviceResult(SbResult Result, DeviceStatus Status, int DeviceCount = 0, int MaxDevices = 0);

/// <summary>Giao diện Supabase Auth + RPC giấy phép — tách ra để test offline bằng đối tượng giả.</summary>
public interface ISupabaseAuth
{
    Task<SbResult> SendOtpAsync(string email, CancellationToken ct);
    Task<SbVerifyResult> VerifyOtpAsync(string email, string token, CancellationToken ct);
    Task<SbVerifyResult> RefreshAsync(string refreshToken, CancellationToken ct);
    Task<SbDeviceResult> ActivateAsync(string accessToken, string hwid, string machineName, string appVersion, CancellationToken ct);
    Task<SbDeviceResult> CheckAsync(string accessToken, string hwid, CancellationToken ct);
    Task<SbDeviceResult> ReleaseAsync(string accessToken, string hwid, CancellationToken ct);
}

/// <summary>
/// Gọi REST thuần của Supabase bằng HttpClient + System.Text.Json (KHÔNG dùng gói supabase-csharp để tránh
/// kéo thêm DLL gây FileLoadException trong AutoCAD/Civil 3D). Không bao giờ ném exception ra ngoài.
/// </summary>
public sealed class SupabaseAuthClient : ISupabaseAuth
{
    private static readonly HttpClient SharedHttp = CreateShared();

    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _key;

    /// <summary>Trần thời gian cho mỗi lời gọi (người gọi có thể đặt ngắn hơn qua CancellationToken).</summary>
    public TimeSpan MaxCallDuration { get; set; } = TimeSpan.FromSeconds(20);

    public SupabaseAuthClient(string baseUrl, string key, HttpMessageHandler? handler = null)
    {
        _baseUrl = baseUrl.Trim().TrimEnd('/');
        _key = key.Trim();
        _http = handler is null
            ? SharedHttp
            : new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
    }

    public SupabaseAuthClient(AuthConfig config)
        : this(config.ResolvedSupabaseUrl, config.ResolvedSupabaseKey)
    {
    }

    private static HttpClient CreateShared()
    {
        // Dùng proxy hệ thống (mạng công ty) kèm thông tin đăng nhập Windows hiện tại.
        var handler = new HttpClientHandler
        {
            UseProxy = true,
            DefaultProxyCredentials = CredentialCache.DefaultCredentials
        };
        return new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
    }

    // ---------- Auth ----------
    public async Task<SbResult> SendOtpAsync(string email, CancellationToken ct)
    {
        var (result, _) = await SendAsync(HttpMethod.Post, "/auth/v1/otp",
            new { email, create_user = true }, bearer: null, ct).ConfigureAwait(false);
        return result;
    }

    public async Task<SbVerifyResult> VerifyOtpAsync(string email, string token, CancellationToken ct)
    {
        var (result, json) = await SendAsync(HttpMethod.Post, "/auth/v1/verify",
            new { type = "email", email, token }, bearer: null, ct).ConfigureAwait(false);
        return ToSession(result, json);
    }

    public async Task<SbVerifyResult> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var (result, json) = await SendAsync(HttpMethod.Post, "/auth/v1/token?grant_type=refresh_token",
            new { refresh_token = refreshToken }, bearer: null, ct).ConfigureAwait(false);
        return ToSession(result, json);
    }

    private static SbVerifyResult ToSession(SbResult result, JsonElement? json)
    {
        if (!result.Ok)
            return new SbVerifyResult(result, null);

        try
        {
            var root = json!.Value;
            var user = root.GetProperty("user");
            var session = new SbSession(
                user.GetProperty("id").GetString()!,
                user.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "",
                root.GetProperty("access_token").GetString()!,
                root.GetProperty("refresh_token").GetString()!);
            return new SbVerifyResult(result, session);
        }
        catch
        {
            return new SbVerifyResult(
                new SbResult(SbOutcome.ServerError, result.HttpStatus, "bad_response", "Phan hoi khong co phien dang nhap"), null);
        }
    }

    // ---------- RPC giấy phép (PostgREST) ----------
    public Task<SbDeviceResult> ActivateAsync(string accessToken, string hwid, string machineName, string appVersion,
        CancellationToken ct) =>
        RpcAsync("isurvey_activate",
            new { p_hwid = hwid, p_machine_name = machineName, p_app_version = appVersion }, accessToken, ct);

    public Task<SbDeviceResult> CheckAsync(string accessToken, string hwid, CancellationToken ct) =>
        RpcAsync("isurvey_check", new { p_hwid = hwid }, accessToken, ct);

    public Task<SbDeviceResult> ReleaseAsync(string accessToken, string hwid, CancellationToken ct) =>
        RpcAsync("isurvey_release_device", new { p_hwid = hwid }, accessToken, ct);

    private async Task<SbDeviceResult> RpcAsync(string function, object body, string accessToken, CancellationToken ct)
    {
        var (result, json) = await SendAsync(HttpMethod.Post, "/rest/v1/rpc/" + function, body, accessToken, ct)
            .ConfigureAwait(false);
        if (!result.Ok)
            return new SbDeviceResult(result, DeviceStatus.Unknown);

        try
        {
            var root = json!.Value;
            var status = root.GetProperty("status").GetString();
            var count = root.TryGetProperty("device_count", out var c) && c.TryGetInt32(out var ci) ? ci : 0;
            var max = root.TryGetProperty("max_devices", out var m) && m.TryGetInt32(out var mi) ? mi : 0;
            var mapped = status switch
            {
                "ok" => DeviceStatus.Ok,
                "blocked" => DeviceStatus.Blocked,
                "device_limit" => DeviceStatus.DeviceLimit,
                "not_registered" => DeviceStatus.NotRegistered,
                "invalid_email" => DeviceStatus.InvalidEmail,
                "unauthenticated" => DeviceStatus.Unauthenticated,
                "released" => DeviceStatus.Released,
                _ => DeviceStatus.Unknown
            };
            return new SbDeviceResult(result, mapped, count, max);
        }
        catch
        {
            return new SbDeviceResult(
                new SbResult(SbOutcome.ServerError, result.HttpStatus, "bad_response", "Phan hoi RPC khong hop le"),
                DeviceStatus.Unknown);
        }
    }

    // ---------- HTTP ----------
    private async Task<(SbResult Result, JsonElement? Json)> SendAsync(HttpMethod method, string path, object? body,
        string? bearer, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(method, _baseUrl + path);
            req.Headers.TryAddWithoutValidation("apikey", _key);
            if (!string.IsNullOrEmpty(bearer))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            if (body is not null)
                req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(MaxCallDuration);

            using var resp = await _http.SendAsync(req, cts.Token).ConfigureAwait(false);
            var text = await resp.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
            var json = TryParse(text);
            var status = (int)resp.StatusCode;

            if (status is >= 200 and < 300)
                return (new SbResult(SbOutcome.Ok, status), json);

            var code = Str(json, "error_code") ?? Str(json, "code") ?? Str(json, "error");
            var message = Str(json, "msg") ?? Str(json, "message") ?? Str(json, "error_description");
            var outcome =
                status == 429 || (code?.Contains("rate_limit", StringComparison.OrdinalIgnoreCase) ?? false)
                    ? SbOutcome.RateLimited
                    : status >= 500 ? SbOutcome.ServerError : SbOutcome.Rejected;
            return (new SbResult(outcome, status, code, message), json);
        }
        catch (Exception ex) when (ex is OperationCanceledException or HttpRequestException or IOException)
        {
            return (new SbResult(SbOutcome.Network, 0, null, ex.Message), null);
        }
        catch (Exception ex)
        {
            return (new SbResult(SbOutcome.Network, 0, "exception", ex.GetType().Name + ": " + ex.Message), null);
        }
    }

    private static JsonElement? TryParse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(text);
            return doc.RootElement.Clone();
        }
        catch
        {
            return null;
        }
    }

    private static string? Str(JsonElement? json, string name)
    {
        if (json is { ValueKind: JsonValueKind.Object } root && root.TryGetProperty(name, out var p)
            && p.ValueKind == JsonValueKind.String)
            return p.GetString();
        return null;
    }
}
