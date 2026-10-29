using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Auth;

/// <summary>
/// Tải zip Setup, giải nén, chạy helper chờ CAD thoát rồi Install-iSurvey.ps1.
/// Không cài đè bundle khi acad.exe còn mở (DLL bị khóa).
/// </summary>
public static class UpdateApplyService
{
    private static readonly HttpClient Http = new(new HttpClientHandler
    {
        UseProxy = true,
        DefaultProxyCredentials = CredentialCache.DefaultCredentials
    })
    { Timeout = TimeSpan.FromMinutes(10) };

    public sealed record ApplyResult(bool Ok, string Message, string? SetupDir = null, long Bytes = 0);

    /// <summary>Tiến trình tải/giải nén — <paramref name="Percent"/> null = không xác định.</summary>
    public sealed record UpdateProgress(string Title, string? Detail, double? Percent);

    /// <summary>Thư mục cập nhật cục bộ (zip + bản giải nén + script helper).</summary>
    public static string UpdatesRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "iSurvey", "updates");

    public static ApplyResult DownloadAndStage(UpdateCheckService.Manifest man, IProgress<UpdateProgress>? progress = null)
    {
        if (!Uri.TryCreate(man.Url?.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return new ApplyResult(false, "URL tải trong version.json không hợp lệ (chỉ http/https).");

        try
        {
            Directory.CreateDirectory(UpdatesRoot);
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var safeVer = SanitizeFileName(string.IsNullOrWhiteSpace(man.Latest) ? "update" : man.Latest);
            var zipPath = Path.Combine(UpdatesRoot, $"iSurvey-Setup-{safeVer}-{stamp}.zip");
            var extractDir = Path.Combine(UpdatesRoot, $"stage-{safeVer}-{stamp}");

            progress?.Report(new UpdateProgress("Đang tải gói cài đặt...", "Kết nối máy chủ...", 0));
            DownloadToFile(uri, zipPath, progress);

            var zipLen = File.Exists(zipPath) ? new FileInfo(zipPath).Length : 0;
            if (zipLen < 1024)
                return new ApplyResult(false, "File tải về quá nhỏ hoặc trống. Thử lại sau.", Bytes: zipLen);

            progress?.Report(new UpdateProgress("Đang giải nén...", null, null));
            if (Directory.Exists(extractDir))
                Directory.Delete(extractDir, recursive: true);
            Directory.CreateDirectory(extractDir);
            ExtractZip(zipPath, extractDir);

            var setupDir = ResolveSetupRoot(extractDir);
            if (setupDir is null)
                return new ApplyResult(false, "Gói Setup thiếu Install-iSurvey.ps1 hoặc iSurvey.bundle.", Bytes: zipLen);

            return new ApplyResult(true, "Đã tải và giải nén xong.", setupDir, zipLen);
        }
        catch (Exception ex)
        {
            Trace("download_fail " + ex.GetType().Name + ": " + ex.Message);
            return new ApplyResult(false, "Lỗi tải / giải nén: " + ex.Message);
        }
    }

    /// <summary>Ghi script helper và chạy ngoài tiến trình CAD. Helper gửi đóng cửa sổ (nút X) sau khi lệnh UPD kết thúc.</summary>
    public static ApplyResult LaunchHelper(string setupDir, string latest)
    {
        try
        {
            if (!Directory.Exists(setupDir))
                return new ApplyResult(false, "Không tìm thấy thư mục Setup đã giải nén.");

            Directory.CreateDirectory(UpdatesRoot);
            var helper = EnsureHelperScript();
            var pid = Process.GetCurrentProcess().Id;
            WriteRelaunchInfo();
            WriteLogContext(latest);
            var signal = Guid.NewGuid().ToString("N");
            var args =
                $"-NoProfile -ExecutionPolicy Bypass -STA -File \"{helper}\" -ParentPid {pid} -SetupDir \"{setupDir}\" -Signal {signal}";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = setupDir
            };
            var proc = Process.Start(psi);
            if (proc is null)
            {
                Trace("helper_start_fail process=null");
                return new ApplyResult(false, "Không khởi chạy được PowerShell trình cài.");
            }

            Trace($"helper_start pid={proc.Id} acadPid={pid} setup={setupDir}");
            Trace("close_hand_off " + signal);
            return new ApplyResult(true, "Đã chạy trình cài (PID " + proc.Id + ").");
        }
        catch (Exception ex)
        {
            Trace("helper_start_fail " + ex.GetType().Name + ": " + ex.Message);
            return new ApplyResult(false, "Không chạy được trình cài: " + ex.Message);
        }
    }

    private static void DownloadToFile(Uri uri, string zipPath, IProgress<UpdateProgress>? progress)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, uri);
        var config = AuthConfig.Load();
        // Bucket public; thêm apikey nếu Storage yêu cầu.
        req.Headers.TryAddWithoutValidation("apikey", config.ResolvedSupabaseKey);
        req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + config.ResolvedSupabaseKey);

        using var resp = Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"HTTP {(int)resp.StatusCode} khi tải gói cài đặt.");
        var total = resp.Content.Headers.ContentLength;
        using var stream = resp.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        using var fs = File.Create(zipPath);
        var buf = new byte[81920];
        long read = 0;
        int n;
        var lastPct = -1;
        while ((n = stream.Read(buf, 0, buf.Length)) > 0)
        {
            fs.Write(buf, 0, n);
            read += n;
            if (total is > 0)
            {
                var pct = (int)(100.0 * read / total.Value);
                if (pct == lastPct)
                    continue;
                lastPct = pct;
                progress?.Report(new UpdateProgress(
                    "Đang tải gói cài đặt...",
                    $"{FormatMb(read)} / {FormatMb(total.Value)}",
                    pct));
            }
            else
            {
                progress?.Report(new UpdateProgress("Đang tải gói cài đặt...", FormatMb(read), null));
            }
        }
    }

    private static string FormatMb(long bytes) =>
        (bytes / (1024.0 * 1024.0)).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " MB";

    /// <summary>File log cục bộ — đọc được cả khi CAD treo / thoát giữa chừng.</summary>
    public static string TracePath => Path.Combine(UpdatesRoot, "update.log");

    public static void Trace(string line)
    {
        try
        {
            Directory.CreateDirectory(UpdatesRoot);
            File.AppendAllText(TracePath,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
                + "  " + line + Environment.NewLine);
        }
        catch
        {
            // không chặn cập nhật
        }
    }

    /// <summary>Lần mở Civil sau: gửi nhật ký kết quả cài nếu helper chưa gửi được.</summary>
    public static void FlushPendingResult()
    {
        try
        {
            var path = Path.Combine(UpdatesRoot, "last-result.json");
            if (!File.Exists(path))
                return;
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("uploaded", out var up) && up.ValueKind == JsonValueKind.True)
                return;

            object? detail = JsonSerializer.Deserialize<object>(json);
            ActivityLogger.Enqueue("update_result", "UPD", detail);
            Trace("pending_result_enqueued");
            var sent = path + ".sent";
            if (File.Exists(sent))
                File.Delete(sent);
            File.Move(path, sent);
        }
        catch (Exception ex)
        {
            Trace("pending_result_fail " + ex.Message);
        }
    }

    /// <summary>Ghi đường dẫn + tham số AutoCAD/Civil để helper mở lại sau khi cài.</summary>
    private static void WriteRelaunchInfo()
    {
        try
        {
            var exe = Process.GetCurrentProcess().MainModule?.FileName ?? "";
            var cmd = Environment.CommandLine ?? "";
            var args = "";
            if (cmd.StartsWith("\"", StringComparison.Ordinal))
            {
                var end = cmd.IndexOf('"', 1);
                if (end > 0)
                    args = cmd[(end + 1)..].Trim();
            }
            else
            {
                var sp = cmd.IndexOf(' ');
                if (sp > 0)
                    args = cmd[(sp + 1)..].Trim();
            }

            var path = Path.Combine(UpdatesRoot, "relaunch.json");
            File.WriteAllText(path, JsonSerializer.Serialize(new { exe, args }));
            Trace("relaunch_written exe=" + exe);
        }
        catch (Exception ex)
        {
            Trace("relaunch_fail " + ex.Message);
        }
    }

    /// <summary>Thông tin để helper gửi nhật ký kết quả (CAD đã tắt, không còn ActivityLogger).</summary>
    private static void WriteLogContext(string latest)
    {
        try
        {
            var config = AuthConfig.Load();
            string? access = null;
            try
            {
                access = AuthManager.AcquireAccessToken();
            }
            catch
            {
                // offline / chưa đăng nhập
            }

            var ctx = new
            {
                url = config.HasSupabase
                    ? config.ResolvedSupabaseUrl.TrimEnd('/') + "/rest/v1/rpc/isurvey_log_event"
                    : "",
                apikey = config.HasSupabase ? config.ResolvedSupabaseKey : "",
                access = access ?? "",
                hwid = HardwareId.Current,
                machine = Environment.MachineName,
                current = AuthManager.AppVersion.TrimStart('v'),
                latest
            };
            File.WriteAllText(Path.Combine(UpdatesRoot, "log-context.json"), JsonSerializer.Serialize(ctx));
            Trace("log_context_written hasAccess=" + !string.IsNullOrEmpty(access));
        }
        catch (Exception ex)
        {
            Trace("log_context_fail " + ex.Message);
        }
    }

    private static void ExtractZip(string zipPath, string extractDir)
    {
        using var fs = File.OpenRead(zipPath);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Read);
        foreach (var entry in zip.Entries)
        {
            if (string.IsNullOrEmpty(entry.FullName) || entry.FullName.EndsWith("/", StringComparison.Ordinal)
                                                     || entry.FullName.EndsWith("\\", StringComparison.Ordinal))
                continue;

            // Chặn path traversal
            var dest = Path.GetFullPath(Path.Combine(extractDir, entry.FullName));
            if (!dest.StartsWith(Path.GetFullPath(extractDir) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(dest, Path.GetFullPath(extractDir), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Zip có đường dẫn không hợp lệ.");

            var dir = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            if (entry.Length == 0 && string.IsNullOrEmpty(entry.Name))
                continue;

            using var src = entry.Open();
            using var dst = File.Create(dest);
            src.CopyTo(dst);
        }
    }

    private static string? ResolveSetupRoot(string extractDir)
    {
        var directPs1 = Path.Combine(extractDir, "Install-iSurvey.ps1");
        var directBundle = Path.Combine(extractDir, "iSurvey.bundle");
        if (File.Exists(directPs1) && Directory.Exists(directBundle))
            return extractDir;

        // Compress-Archive đôi khi bọc thêm một thư mục gốc.
        foreach (var sub in Directory.GetDirectories(extractDir))
        {
            if (File.Exists(Path.Combine(sub, "Install-iSurvey.ps1"))
                && Directory.Exists(Path.Combine(sub, "iSurvey.bundle")))
                return sub;
        }

        return null;
    }

    private static string EnsureHelperScript()
    {
        var path = Path.Combine(UpdatesRoot, "Apply-iSurveyUpdate.ps1");
        var asm = typeof(UpdateApplyService).Assembly;
        using var src = asm.GetManifestResourceStream("iSurvey.Apply-iSurveyUpdate.ps1");
        if (src is null)
            throw new InvalidOperationException("Thiếu script cài đặt (Apply-iSurveyUpdate.ps1) trong DLL.");
        using var dst = File.Create(path);
        src.CopyTo(dst);
        return path;
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
