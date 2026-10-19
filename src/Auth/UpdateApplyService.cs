using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Auth;

/// <summary>
/// Tải zip Setup, giải nén, chạy helper chờ CAD thoát rồi Install-iSurvey.ps1.
/// Không cài đè bundle khi acad.exe còn mở (DLL bị khóa).
/// </summary>
public static class UpdateApplyService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(10) };

    public sealed record ApplyResult(bool Ok, string Message, string? SetupDir = null);

    /// <summary>Thư mục cập nhật cục bộ (zip + bản giải nén + script helper).</summary>
    public static string UpdatesRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "iSurvey", "updates");

    public static ApplyResult DownloadAndStage(UpdateCheckService.Manifest man, IProgress<string>? progress = null)
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

            progress?.Report("Đang tải bản cập nhật...");
            DownloadToFile(uri, zipPath);

            if (!File.Exists(zipPath) || new FileInfo(zipPath).Length < 1024)
                return new ApplyResult(false, "File tải về quá nhỏ hoặc trống. Thử lại sau.");

            progress?.Report("Đang giải nén...");
            if (Directory.Exists(extractDir))
                Directory.Delete(extractDir, recursive: true);
            Directory.CreateDirectory(extractDir);
            ExtractZip(zipPath, extractDir);

            var setupDir = ResolveSetupRoot(extractDir);
            if (setupDir is null)
                return new ApplyResult(false, "Gói Setup thiếu Install-iSurvey.ps1 hoặc iSurvey.bundle.");

            return new ApplyResult(true, "Đã tải và giải nén xong.", setupDir);
        }
        catch (Exception ex)
        {
            return new ApplyResult(false, "Lỗi tải / giải nén: " + ex.Message);
        }
    }

    /// <summary>
    /// Ghi script helper (nếu cần), chạy ngoài tiến trình CAD, rồi thoát AutoCAD.
    /// Helper đợi PID hiện tại kết thúc rồi mới gọi Install-iSurvey.ps1.
    /// </summary>
    public static ApplyResult LaunchInstallerAndQuit(string setupDir)
    {
        try
        {
            if (!Directory.Exists(setupDir))
                return new ApplyResult(false, "Không tìm thấy thư mục Setup đã giải nén.");

            var helper = EnsureHelperScript();
            var pid = Process.GetCurrentProcess().Id;
            var args =
                $"-NoProfile -ExecutionPolicy Bypass -File \"{helper}\" -ParentPid {pid} -SetupDir \"{setupDir}\"";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = args,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal,
                WorkingDirectory = setupDir
            };
            Process.Start(psi);

            // Thoát CAD — DLL được nhả khóa; helper đang chờ PID này.
            try
            {
                AcadApp.Quit();
            }
            catch (Exception ex)
            {
                return new ApplyResult(false,
                    "Đã khởi chạy trình cài nhưng không thoát được AutoCAD: " + ex.Message
                    + "\nHãy thoát AutoCAD thủ công để hoàn tất cài đặt.");
            }

            return new ApplyResult(true, "Đang thoát AutoCAD để cài đặt...");
        }
        catch (Exception ex)
        {
            return new ApplyResult(false, "Không chạy được trình cài: " + ex.Message);
        }
    }

    private static void DownloadToFile(Uri uri, string zipPath)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, uri);
        var config = AuthConfig.Load();
        // Bucket public; thêm apikey nếu Storage yêu cầu.
        req.Headers.TryAddWithoutValidation("apikey", config.ResolvedSupabaseKey);
        req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + config.ResolvedSupabaseKey);

        using var resp = Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
        resp.EnsureSuccessStatusCode();
        using var stream = resp.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        using var fs = File.Create(zipPath);
        stream.CopyTo(fs);
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
        // Ưu tiên bản trong repo/deploy cạnh plugin; fallback ghi từ mã nguồn.
        var fromBundle = Path.Combine(
            Path.GetDirectoryName(typeof(UpdateApplyService).Assembly.Location) ?? "",
            "..", "..", "..", "Apply-iSurveyUpdate.ps1");
        try
        {
            fromBundle = Path.GetFullPath(fromBundle);
            if (File.Exists(fromBundle))
            {
                var copy = Path.Combine(UpdatesRoot, "Apply-iSurveyUpdate.ps1");
                File.Copy(fromBundle, copy, overwrite: true);
                return copy;
            }
        }
        catch
        {
            // ghi embedded bên dưới
        }

        var path = Path.Combine(UpdatesRoot, "Apply-iSurveyUpdate.ps1");
        File.WriteAllText(path, EmbeddedHelperScript, System.Text.Encoding.UTF8);
        return path;
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    private const string EmbeddedHelperScript =
        """
        param(
            [Parameter(Mandatory = $true)][int]$ParentPid,
            [Parameter(Mandatory = $true)][string]$SetupDir
        )
        $ErrorActionPreference = "Stop"
        function Show-Msg([string]$Text, [string]$Title) {
            try {
                Add-Type -AssemblyName System.Windows.Forms -ErrorAction SilentlyContinue
                [System.Windows.Forms.MessageBox]::Show($Text, $Title) | Out-Null
            } catch { Write-Host $Text }
        }
        $SetupDir = [System.IO.Path]::GetFullPath($SetupDir)
        $installPs1 = Join-Path $SetupDir "Install-iSurvey.ps1"
        $bundle = Join-Path $SetupDir "iSurvey.bundle"
        if (-not (Test-Path $installPs1) -or -not (Test-Path $bundle)) {
            Show-Msg "Thu muc Setup khong hop le.`n$SetupDir" "iSurvey Cap nhat"
            exit 1
        }
        $deadline = (Get-Date).AddMinutes(30)
        while ($true) {
            if (-not (Get-Process -Id $ParentPid -ErrorAction SilentlyContinue)) { break }
            if ((Get-Date) -gt $deadline) {
                Show-Msg "Het thoi gian cho. Dong AutoCAD roi chay Install-iSurvey.bat.`n$SetupDir" "iSurvey Cap nhat"
                exit 2
            }
            Start-Sleep -Seconds 1
        }
        Start-Sleep -Seconds 2
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $installPs1
        if ($LASTEXITCODE -ne 0 -and $null -ne $LASTEXITCODE) {
            Show-Msg "Cai dat that bai (ma $LASTEXITCODE).`n$SetupDir" "iSurvey Cap nhat"
            exit $LASTEXITCODE
        }
        Show-Msg "Cap nhat iSurvey xong.`nHay mo lai AutoCAD / Civil 3D." "iSurvey Cap nhat"
        exit 0
        """;
}
