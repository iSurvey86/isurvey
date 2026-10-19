using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace iSurvey.Auth;

/// <summary>
/// Gửi nhật ký hoạt động lên Supabase (RPC isurvey_log_event) — nền, không chặn CAD.
/// Chỉ gửi khi đã kích hoạt Online (có refresh token).
/// </summary>
public static class ActivityLogger
{
    private static readonly ConcurrentQueue<Entry> Queue = new();
    private static int _pumping;
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };

    private sealed record Entry(string EventType, string? Command, string? DetailJson);

    /// <summary>Xếp hàng ghi log (nền). Không ném exception.</summary>
    public static void Enqueue(string eventType, string? command = null, object? detail = null)
    {
        try
        {
            string? json = null;
            if (detail is not null)
                json = JsonSerializer.Serialize(detail);

            Queue.Enqueue(new Entry(eventType, command, json));
            if (Interlocked.CompareExchange(ref _pumping, 1, 0) == 0)
                ThreadPool.QueueUserWorkItem(_ => Pump());
        }
        catch
        {
            // nuốt mọi lỗi — log không được làm hỏng lệnh
        }
    }

    public static void TrackCommand([CallerMemberName] string? command = null) =>
        Enqueue("command", command);

    /// <summary>Heartbeat khi giấy phép Online còn hiệu lực (sau recheck 24h / đăng nhập).</summary>
    public static void TrackHeartbeat(string? reason = null) =>
        Enqueue("heartbeat", detail: reason is null ? null : new { reason });

    /// <summary>Ghi lỗi (không ném). Dùng trong catch các lệnh quan trọng.</summary>
    public static void TrackError(Exception ex, [CallerMemberName] string? command = null) =>
        Enqueue("error", command, new { message = ex.Message, type = ex.GetType().Name });

    private static void Pump()
    {
        try
        {
            while (Queue.TryDequeue(out var entry))
                SendOne(entry);
        }
        finally
        {
            Interlocked.Exchange(ref _pumping, 0);
            if (!Queue.IsEmpty && Interlocked.CompareExchange(ref _pumping, 1, 0) == 0)
                ThreadPool.QueueUserWorkItem(_ => Pump());
        }
    }

    private static void SendOne(Entry entry)
    {
        var license = AuthManager.License;
        if (license is null || license.Mode != LicenseMode.Online || string.IsNullOrWhiteSpace(license.RefreshToken))
            return;

        var config = AuthConfig.Load();
        if (!config.HasSupabase)
            return;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var client = new SupabaseAuthClient(config);
            var refresh = client.RefreshAsync(license.RefreshToken!, cts.Token).GetAwaiter().GetResult();
            if (!refresh.Result.Ok || refresh.Session is null)
                return;

            var access = refresh.Session.AccessToken;
            var payload = new Dictionary<string, object?>
            {
                ["p_event_type"] = entry.EventType,
                ["p_command"] = entry.Command,
                ["p_detail"] = string.IsNullOrEmpty(entry.DetailJson)
                    ? null
                    : JsonSerializer.Deserialize<JsonElement>(entry.DetailJson!),
                ["p_app_version"] = AuthManager.AppVersion.TrimStart('v'),
                ["p_hwid"] = HardwareId.Current,
                ["p_machine_name"] = Environment.MachineName
            };

            var url = config.ResolvedSupabaseUrl.TrimEnd('/') + "/rest/v1/rpc/isurvey_log_event";
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
            req.Headers.TryAddWithoutValidation("apikey", config.ResolvedSupabaseKey);
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var _ = Http.SendAsync(req, cts.Token).GetAwaiter().GetResult();
        }
        catch
        {
            // bỏ qua
        }
    }
}
