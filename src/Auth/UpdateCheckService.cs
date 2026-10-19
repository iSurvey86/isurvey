using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace iSurvey.Auth;

/// <summary>Đọc version.json công khai (Supabase Storage) và so với AppVersion.</summary>
public static class UpdateCheckService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(12) };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public sealed class Manifest
    {
        [JsonPropertyName("latest")] public string Latest { get; set; } = "";
        [JsonPropertyName("url")] public string Url { get; set; } = "";
        [JsonPropertyName("released_at")] public string? ReleasedAt { get; set; }
        [JsonPropertyName("notes")] public string? Notes { get; set; }
        [JsonPropertyName("min_version")] public string? MinVersion { get; set; }
    }

    public sealed record CheckResult(
        bool Ok,
        string Message,
        Manifest? Manifest = null,
        bool UpdateAvailable = false,
        bool ForceUpdate = false);

    public static string DefaultManifestUrl(AuthConfig config) =>
        config.ResolvedSupabaseUrl.TrimEnd('/') + "/storage/v1/object/public/isurvey-releases/version.json";

    public static CheckResult Check(AuthConfig? config = null)
    {
        config ??= AuthConfig.Load();
        var url = string.IsNullOrWhiteSpace(config.UpdateManifestUrl)
            ? DefaultManifestUrl(config)
            : config.UpdateManifestUrl!.Trim();

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.TryAddWithoutValidation("apikey", config.ResolvedSupabaseKey);
            req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + config.ResolvedSupabaseKey);
            var resp = Http.SendAsync(req).GetAwaiter().GetResult();
            var body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!resp.IsSuccessStatusCode)
            {
                return new CheckResult(false,
                    $"Không đọc được thông tin bản mới (HTTP {(int)resp.StatusCode}). Kiểm tra bucket isurvey-releases.");
            }

            // PowerShell 5.1 ghi UTF-8 có BOM — bỏ BOM nếu còn sót.
            var man = JsonSerializer.Deserialize<Manifest>(body.TrimStart('\uFEFF'), JsonOptions);
            if (man is null || string.IsNullOrWhiteSpace(man.Latest))
                return new CheckResult(false, "File version.json không hợp lệ.");

            var local = Normalize(AuthManager.AppVersion);
            var remote = Normalize(man.Latest);
            var newer = CompareSemVer(local, remote) < 0;
            var force = !string.IsNullOrWhiteSpace(man.MinVersion)
                        && CompareSemVer(local, Normalize(man.MinVersion!)) < 0;

            if (!newer)
            {
                return new CheckResult(true,
                    $"Bạn đang dùng phiên bản mới nhất ({AuthManager.AppVersion}).",
                    man, UpdateAvailable: false);
            }

            return new CheckResult(true,
                $"Có bản mới {man.Latest} (đang dùng {AuthManager.AppVersion}).",
                man, UpdateAvailable: true, ForceUpdate: force);
        }
        catch (Exception ex)
        {
            return new CheckResult(false, "Lỗi mạng khi kiểm tra cập nhật: " + ex.Message);
        }
    }

    /// <summary>Mở trình duyệt tới URL tải (zip Setup).</summary>
    public static void OpenDownload(Manifest man)
    {
        // Chỉ mở link http/https (không để manifest mở file cục bộ / chương trình).
        if (!Uri.TryCreate(man.Url?.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return;
        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch
        {
            // ignore
        }
    }

    private static string Normalize(string v)
    {
        v = v.Trim();
        if (v.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            v = v[1..];
        var m = Regex.Match(v, @"^\d+(\.\d+){0,3}");
        return m.Success ? m.Value : v;
    }

    /// <summary>&lt;0 local cũ hơn remote; 0 bằng; &gt;0 local mới hơn.</summary>
    public static int CompareSemVer(string a, string b)
    {
        var pa = a.Split('.').Select(ParsePart).ToArray();
        var pb = b.Split('.').Select(ParsePart).ToArray();
        var n = Math.Max(pa.Length, pb.Length);
        for (var i = 0; i < n; i++)
        {
            var x = i < pa.Length ? pa[i] : 0;
            var y = i < pb.Length ? pb[i] : 0;
            if (x != y)
                return x.CompareTo(y);
        }

        return 0;
    }

    private static int ParsePart(string s) => int.TryParse(s, out var n) ? n : 0;
}
