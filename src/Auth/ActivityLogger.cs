using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace iSurvey.Auth;

/// <summary>
/// Gửi nhật ký hoạt động lên Supabase (RPC isurvey_log_event) — nền, không chặn CAD.
/// Chỉ gửi khi đã kích hoạt Online (có refresh token). Access token do <see cref="AuthManager"/> cấp
/// (dùng lại ~50 phút, lưu refresh token mới sau mỗi lần đổi) nên không làm hỏng giấy phép.
/// </summary>
public static class ActivityLogger
{
    private static readonly ConcurrentQueue<Entry> Queue = new();
    private static int _pumping;

    private static readonly HttpClient Http = new(new HttpClientHandler
    {
        UseProxy = true,
        DefaultProxyCredentials = CredentialCache.DefaultCredentials
    })
    { Timeout = TimeSpan.FromSeconds(10) };

    private sealed record Entry(string EventType, string? Command, string? DetailJson);

    /// <summary>Xếp hàng ghi log (nền). Không ném exception.</summary>
    public static void Enqueue(string eventType, string? command = null, object? detail = null)
    {
        try
        {
            string? json = null;
            if (detail is not null)
                json = JsonSerializer.Serialize(detail);

            Queue.Enqueue(new Entry(eventType, ActivityPhrases.Command(command), json));
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

    /// <summary>
    /// Một dòng kết quả, hiện sau tên lệnh trên trang quản trị.
    /// Gọi khi lệnh đã xong (biết file, số tile, số đối tượng), không gọi lúc vừa mở lệnh.
    /// </summary>
    public static void Result(string command, string note) =>
        Enqueue("command", command, new { note = Trim(note, 500) });

    public static string FileName(string? path) =>
        string.IsNullOrWhiteSpace(path) ? "bản vẽ chưa lưu" : Path.GetFileName(path);

    public static string FileList(IEnumerable<string?> paths, int max = 6)
    {
        var names = paths
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => Path.GetFileName(p!))
            .ToList();
        if (names.Count == 0)
            return "";
        if (names.Count <= max)
            return string.Join(", ", names);
        return string.Join(", ", names.Take(max)) + $" và {names.Count - max} file nữa";
    }

    /// <summary>Heartbeat khi giấy phép Online còn hiệu lực (sau recheck 24h / đăng nhập).</summary>
    public static void TrackHeartbeat(string? reason = null) =>
        Enqueue("heartbeat", detail: reason is null ? null : new { reason });

    /// <summary>Ghi lỗi (không ném). Dùng trong catch các lệnh quan trọng.</summary>
    public static void TrackError(Exception ex, [CallerMemberName] string? command = null) =>
        Enqueue("error", command, new { message = Trim(ex.Message, 300), type = ex.GetType().Name });

    /// <summary>Gửi ngay trên luồng hiện tại bằng access token cho trước (dùng khi đăng xuất / trước khi thoát CAD). Không ném.</summary>
    internal static void SendNow(string accessToken, string eventType, string? command, object? detail,
        string? hwid, CancellationToken ct)
    {
        try
        {
            Post(accessToken, eventType, ActivityPhrases.Command(command), detail is null ? null : JsonSerializer.Serialize(detail), hwid, ct);
        }
        catch
        {
            // bỏ qua
        }
    }

    /// <summary>Gửi hết hàng đợi trên luồng hiện tại (trước khi thoát CAD để không mất log).</summary>
    internal static void FlushNow()
    {
        try
        {
            while (Queue.TryDequeue(out var entry))
                SendOne(entry);
        }
        catch
        {
            // không chặn thoát
        }
    }

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
        try
        {
            var access = AuthManager.AcquireAccessToken();
            if (access is null)
                return;

            Post(access, entry.EventType, entry.Command, entry.DetailJson, null, CancellationToken.None);
        }
        catch
        {
            // bỏ qua
        }
    }

    private static void Post(string accessToken, string eventType, string? command, string? detailJson,
        string? hwid, CancellationToken ct)
    {
        var config = AuthConfig.Load();
        if (!config.HasSupabase)
            return;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(8));

        var payload = new Dictionary<string, object?>
        {
            ["p_event_type"] = eventType,
            ["p_command"] = command,
            ["p_detail"] = string.IsNullOrEmpty(detailJson)
                ? null
                : JsonSerializer.Deserialize<JsonElement>(detailJson!),
            ["p_app_version"] = AuthManager.AppVersion.TrimStart('v'),
            ["p_hwid"] = hwid ?? HardwareId.Current,
            ["p_machine_name"] = Environment.MachineName
        };

        var url = config.ResolvedSupabaseUrl.TrimEnd('/') + "/rest/v1/rpc/isurvey_log_event";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        req.Headers.TryAddWithoutValidation("apikey", config.ResolvedSupabaseKey);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var resp = Http.SendAsync(req, cts.Token).GetAwaiter().GetResult();

        // Token bị từ chối → bỏ cache để lần sau refresh lại.
        if (resp.StatusCode == HttpStatusCode.Unauthorized)
            AuthManager.InvalidateAccessToken();
    }

    private static string Trim(string s, int max) => s.Length <= max ? s : s[..max];
}
