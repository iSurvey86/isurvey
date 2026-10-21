using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace iSurvey.Modules.Ai;

/// <summary>MCP client tối giản — JSON-RPC qua stdio (newline-delimited).</summary>
public sealed class McpStdioClient : IDisposable
{
    private readonly Process _proc;
    private readonly StreamWriter _stdin;
    private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonElement>> _pending = new();
    private readonly CancellationTokenSource _cts = new();
    private int _nextId = 1;
    private bool _disposed;

    public string ServerName { get; }
    public List<McpToolInfo> Tools { get; private set; } = [];

    private McpStdioClient(string serverName, Process proc, StreamWriter stdin)
    {
        ServerName = serverName;
        _proc = proc;
        _stdin = stdin;
        _ = Task.Run(ReadLoopAsync);
    }

    public static async Task<McpStdioClient> StartAsync(McpServerConfig cfg, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cfg.Command))
            throw new InvalidOperationException($"MCP '{cfg.Name}': thiếu Command.");

        var (fileName, arguments) = BuildProcessCommand(cfg);
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        EnsureNodeOnPath(psi);
        if (cfg.Env is not null)
        {
            foreach (var kv in cfg.Env)
                psi.EnvironmentVariables[kv.Key] = Expand(kv.Value);
        }

        Process proc;
        try
        {
            proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            if (!proc.Start())
                throw new InvalidOperationException($"MCP '{cfg.Name}': không start được process.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"MCP '{cfg.Name}': không chạy được '{fileName}' ({ex.Message}). " +
                "Cài Node.js hoặc đặt Command = đường dẫn đầy đủ tới npx.cmd / node.exe.",
                ex);
        }

        var stdin = new StreamWriter(proc.StandardInput.BaseStream, new UTF8Encoding(false)) { AutoFlush = true };
        var client = new McpStdioClient(cfg.Name, proc, stdin);

        _ = Task.Run(async () =>
        {
            try
            {
                while (!proc.HasExited)
                    await proc.StandardError.ReadLineAsync().ConfigureAwait(false);
            }
            catch { /* ignore */ }
        }, ct);

        await client.InitializeAsync(ct).ConfigureAwait(false);
        await client.RefreshToolsAsync(ct).ConfigureAwait(false);
        return client;
    }

    private static string QuoteArg(string a)
    {
        a = Expand(a);
        if (a.Length == 0) return "\"\"";
        if (a.IndexOfAny(new[] { ' ', '\t', '"' }) < 0) return a;
        return "\"" + a.Replace("\"", "\\\"") + "\"";
    }

    /// <summary>
    /// Civil 3D thường không có Node trong PATH; npx là .cmd nên phải bọc cmd.exe khi redirect stdio.
    /// </summary>
    private static (string FileName, string Arguments) BuildProcessCommand(McpServerConfig cfg)
    {
        var cmd = Expand(cfg.Command.Trim());
        var args = string.Join(" ", cfg.Args.Select(QuoteArg));
        cmd = ResolveExecutable(cmd);

        if (IsWindowsBatch(cmd))
        {
            // UseShellExecute=false không chạy .cmd trực tiếp → cmd /c
            var wrapped = QuoteArg(cmd) + (args.Length > 0 ? " " + args : "");
            return ("cmd.exe", "/c " + wrapped);
        }

        return (cmd, args);
    }

    private static bool IsWindowsBatch(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".cmd", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".bat", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveExecutable(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return command;

        // Đường dẫn có sẵn: chỉ nhận .exe/.cmd/.bat (tránh file "npx" kiểu Unix shell)
        if (Path.IsPathRooted(command) || command.Contains(Path.DirectorySeparatorChar)
            || command.Contains(Path.AltDirectorySeparatorChar))
        {
            var full = Expand(command);
            var win = PreferWindowsLauncher(full);
            if (win is not null)
                return win;
            if (File.Exists(full) && IsWindowsNativeLauncher(full))
                return Path.GetFullPath(full);
        }

        var name = Path.GetFileName(command);
        // Ưu tiên .cmd/.exe/.bat — KHÔNG lấy file không đuôi (Node cài "npx" = script bash)
        foreach (var dir in GetSearchDirs())
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                continue;
            foreach (var ext in new[] { ".cmd", ".exe", ".bat" })
            {
                var full = Path.Combine(dir, name + ext);
                if (File.Exists(full))
                    return full;
            }
            // "node" không có .cmd — chỉ nhận .exe thật
            var bare = Path.Combine(dir, name);
            if (File.Exists(bare) && IsWindowsNativeLauncher(bare))
                return bare;
        }

        return FindOnPath(name) ?? command;
    }

    /// <summary>Nếu path trỏ tới file không đuôi nhưng có sibling .cmd/.exe → dùng sibling.</summary>
    private static string? PreferWindowsLauncher(string path)
    {
        if (File.Exists(path) && IsWindowsNativeLauncher(path))
            return Path.GetFullPath(path);

        var dir = Path.GetDirectoryName(path);
        var name = Path.GetFileNameWithoutExtension(path);
        if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(name))
            return null;
        foreach (var ext in new[] { ".cmd", ".exe", ".bat" })
        {
            var sibling = Path.Combine(dir, name + ext);
            if (File.Exists(sibling))
                return sibling;
        }
        return null;
    }

    private static bool IsWindowsNativeLauncher(string path)
    {
        var ext = Path.GetExtension(path);
        if (ext.Equals(".exe", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".cmd", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".bat", StringComparison.OrdinalIgnoreCase))
            return true;
        // Extensionless: chỉ nhận PE (MZ). File "npx" của Node là script → bỏ.
        try
        {
            using var fs = File.OpenRead(path);
            return fs.ReadByte() == 'M' && fs.ReadByte() == 'Z';
        }
        catch
        {
            return false;
        }
    }

    private static List<string> GetSearchDirs()
    {
        var dirs = new List<string>
        {
            @"C:\Program Files\nodejs",
            @"C:\Program Files (x86)\nodejs",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "node"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm"),
        };

        void AddPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            foreach (var part in path.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var d = part.Trim().Trim('"');
                if (d.Length > 0) dirs.Add(d);
            }
        }

        AddPath(Environment.GetEnvironmentVariable("PATH"));
        // PATH user/machine (Civil 3D thường thiếu Node)
        try
        {
            AddPath(Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User));
            AddPath(Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine));
        }
        catch { /* ignore */ }

        return dirs;
    }

    private static string? FindOnPath(string name)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in path.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var d = dir.Trim().Trim('"');
            foreach (var ext in new[] { ".cmd", ".exe", ".bat" })
            {
                var full = Path.Combine(d, name + ext);
                if (File.Exists(full))
                    return full;
            }
        }
        return null;
    }

    private static void EnsureNodeOnPath(ProcessStartInfo psi)
    {
        const string nodeDir = @"C:\Program Files\nodejs";
        if (!Directory.Exists(nodeDir))
            return;
        var current = psi.EnvironmentVariables.ContainsKey("PATH")
            ? psi.EnvironmentVariables["PATH"]
            : Environment.GetEnvironmentVariable("PATH") ?? "";
        if (current.IndexOf(nodeDir, StringComparison.OrdinalIgnoreCase) >= 0)
            return;
        psi.EnvironmentVariables["PATH"] = nodeDir + ";" + current;
    }

    private async Task InitializeAsync(CancellationToken ct)
    {
        await RequestAsync("initialize", new
        {
            protocolVersion = "2024-11-05",
            capabilities = new { },
            clientInfo = new { name = "iSurvey", version = Auth.AuthManager.AppVersion.TrimStart('v') }
        }, ct).ConfigureAwait(false);

        await NotifyAsync("notifications/initialized", new { }).ConfigureAwait(false);
    }

    public async Task RefreshToolsAsync(CancellationToken ct)
    {
        var result = await RequestAsync("tools/list", new { }, ct).ConfigureAwait(false);
        Tools = [];
        if (result.TryGetProperty("tools", out var arr))
        {
            foreach (var t in arr.EnumerateArray())
            {
                var name = t.GetProperty("name").GetString() ?? "";
                var desc = t.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
                JsonElement schema = default;
                if (t.TryGetProperty("inputSchema", out var s))
                    schema = s.Clone();
                Tools.Add(new McpToolInfo(name, desc, schema));
            }
        }
    }

    public async Task<string> CallToolAsync(string toolName, JsonElement? arguments, CancellationToken ct)
    {
        object argsObj = new Dictionary<string, object?>();
        if (arguments is { ValueKind: JsonValueKind.Object })
            argsObj = JsonSerializer.Deserialize<object>(arguments.Value.GetRawText()) ?? argsObj;

        var result = await RequestAsync("tools/call", new
        {
            name = toolName,
            arguments = argsObj
        }, ct).ConfigureAwait(false);

        return result.GetRawText();
    }

    private async Task<JsonElement> RequestAsync(string method, object paramsObj, CancellationToken ct)
    {
        var id = Interlocked.Increment(ref _nextId);
        var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = tcs;

        var msg = new Dictionary<string, object?>
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = method,
            ["params"] = paramsObj
        };
        await _stdin.WriteLineAsync(JsonSerializer.Serialize(msg)).ConfigureAwait(false);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);
        using var reg = linked.Token.Register(() =>
        {
            if (_pending.TryRemove(id, out var p))
                p.TrySetException(new TimeoutException($"MCP {ServerName}/{method} timeout."));
        });

        return await tcs.Task.ConfigureAwait(false);
    }

    private Task NotifyAsync(string method, object paramsObj)
    {
        var msg = new Dictionary<string, object?>
        {
            ["jsonrpc"] = "2.0",
            ["method"] = method,
            ["params"] = paramsObj
        };
        return _stdin.WriteLineAsync(JsonSerializer.Serialize(msg));
    }

    private async Task ReadLoopAsync()
    {
        try
        {
            using var reader = new StreamReader(_proc.StandardOutput.BaseStream, Encoding.UTF8);
            while (!_cts.IsCancellationRequested && !_proc.HasExited)
            {
                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line is null)
                    break;
                line = line.Trim();
                if (line.Length == 0)
                    continue;

                JsonDocument doc;
                try { doc = JsonDocument.Parse(line); }
                catch { continue; }

                using (doc)
                {
                    var root = doc.RootElement;
                    if (!root.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.Number)
                        continue;
                    var id = idEl.GetInt32();
                    if (!_pending.TryRemove(id, out var tcs))
                        continue;
                    if (root.TryGetProperty("error", out var err))
                        tcs.TrySetException(new InvalidOperationException($"MCP error: {err.GetRawText()}"));
                    else if (root.TryGetProperty("result", out var result))
                        tcs.TrySetResult(result.Clone());
                    else
                        tcs.TrySetException(new InvalidOperationException("MCP: thiếu result."));
                }
            }
        }
        catch (Exception ex)
        {
            foreach (var kv in _pending)
                kv.Value.TrySetException(ex);
            _pending.Clear();
        }
    }

    private static string Expand(string s)
    {
        s = s.Replace("%USERPROFILE%", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            StringComparison.OrdinalIgnoreCase);
        return Environment.ExpandEnvironmentVariables(s);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        try { _stdin.Dispose(); } catch { /* ignore */ }
        try
        {
            if (!_proc.HasExited)
                _proc.Kill();
        }
        catch { /* ignore */ }
        try { _proc.Dispose(); } catch { /* ignore */ }
        _cts.Dispose();
    }
}

public sealed record McpToolInfo(string Name, string Description, JsonElement InputSchema);
