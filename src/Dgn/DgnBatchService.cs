using System.Diagnostics;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace iSurvey.Dgn;

internal enum DgnBatchStatus
{
    Ok,
    Skipped,
    Failed
}

/// <summary>Kết quả của một file DGN trong Batch.</summary>
internal sealed class DgnBatchFileResult
{
    public required string Source { get; init; }

    /// <summary>File .dwg đã lưu (chế độ không ghép); null khi ghép vào một bản vẽ mới.</summary>
    public string? Output { get; set; }

    public DgnBatchStatus Status { get; set; }

    public string? Message { get; set; }

    public int Entities { get; set; }

    public int TextsConverted { get; set; }

    public int LayersRenamed { get; set; }

    public TimeSpan Elapsed { get; set; }

    public string? LogPath { get; set; }
}

/// <summary>
/// Batch DGN ➔ CAD. Mỗi file được nhập bằng đúng đường đi của D2C thường (DGNIMPORTMODE=2, tọa độ chuẩn,
/// hậu xử lý theo Handseed) vào một bản vẽ trống do ta tự tạo từ template mặc định:
/// <list type="bullet">
/// <item>Ghép: tất cả file vào MỘT bản vẽ mới, để chưa lưu.</item>
/// <item>Không ghép: mỗi file một bản vẽ mới ➔ lưu .dwg cạnh file .dgn ➔ đóng ➔ file kế tiếp (luôn chỉ một tab phụ).</item>
/// </list>
/// Tạo/đóng bản vẽ chạy ở ngữ cảnh ứng dụng (Application.Idle); gọi lệnh DGNIMPORT chạy ở ngữ cảnh lệnh.
/// </summary>
internal static class DgnBatchService
{
    private sealed class Batch
    {
        public required Document Orig { get; init; }

        public required DgnImportOptions Options { get; init; }

        public required Queue<string> Files { get; init; }

        public required int Total { get; init; }

        public required Action<IReadOnlyList<DgnBatchFileResult>, string?> Done { get; init; }

        public List<DgnBatchFileResult> Results { get; } = [];

        public int Index { get; set; }

        /// <summary>Bản vẽ đang nhận file hiện tại (ghép: dùng chung; không ghép: mỗi file một bản).</summary>
        public Document? Target { get; set; }

        public bool Combine => Options.BatchCombine;
    }

    public static bool IsRunning { get; private set; }

    /// <summary>Bắt đầu Batch (gọi từ lệnh). Tạo bản vẽ và nhập đều chạy ở các sự kiện Idle kế tiếp.</summary>
    /// <param name="done">(kết quả, đường dẫn bản vẽ ghép chưa lưu / null).</param>
    public static void Start(
        Document orig, DgnImportOptions options, Action<IReadOnlyList<DgnBatchFileResult>, string?> done)
    {
        if (IsRunning)
        {
            orig.Editor.WriteMessage("\n[iSurvey][DGN] Một Batch khác đang chạy — hãy đợi nó xong.");
            return;
        }

        var files = options.BatchFiles
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (files.Count == 0)
        {
            orig.Editor.WriteMessage("\n[iSurvey][DGN] Batch: không có file nào.");
            return;
        }

        IsRunning = true;
        var batch = new Batch
        {
            Orig = orig,
            Options = options,
            Files = new Queue<string>(files),
            Total = files.Count,
            Done = done
        };

        orig.Editor.WriteMessage(options.BatchCombine
            ? $"\n[iSurvey][DGN] BATCH GHÉP: {files.Count} file ➔ MỘT bản vẽ mới (tuần tự, đừng thao tác trong lúc chạy)."
            : $"\n[iSurvey][DGN] BATCH TÁCH: {files.Count} file ➔ mỗi file một .dwg cùng thư mục (tuần tự, đừng thao tác trong lúc chạy).");

        RunLater(batch, () => Step(batch));
    }

    /// <summary>Ngữ cảnh ứng dụng: lấy file kế tiếp, chuẩn bị bản vẽ đích rồi ra lệnh nhập.</summary>
    private static void Step(Batch b)
    {
        while (true)
        {
            if (b.Files.Count == 0)
            {
                Finish(b);
                return;
            }

            var file = b.Files.Dequeue();
            b.Index++;
            var prefix = $"[{b.Index}/{b.Total}] {Path.GetFileName(file)}";

            if (!File.Exists(file))
            {
                AddResult(b, new DgnBatchFileResult
                {
                    Source = file, Status = DgnBatchStatus.Failed, Message = "không tìm thấy file"
                }, prefix);
                continue;
            }

            string? output = null;
            if (!b.Combine)
            {
                output = ResolveOutput(file, b.Options.BatchExisting);
                if (output is null)
                {
                    AddResult(b, new DgnBatchFileResult
                    {
                        Source = file, Status = DgnBatchStatus.Skipped,
                        Message = "đã có .dwg cùng tên (bỏ qua theo tùy chọn)"
                    }, prefix);
                    continue;
                }
            }

            // Bản vẽ đích: ghép ➔ tạo một lần; không ghép ➔ tạo mới cho từng file.
            var created = false;
            if (b.Target is null)
            {
                try
                {
                    b.Target = CreateBlankDocument();
                    created = true;
                }
                catch (System.Exception ex)
                {
                    AddResult(b, new DgnBatchFileResult
                    {
                        Source = file, Status = DgnBatchStatus.Failed,
                        Message = "không tạo được bản vẽ trống: " + ex.Message
                    }, prefix);
                    continue;
                }
            }

            WriteLine(b.Combine
                ? $"\n[iSurvey][DGN] {prefix} ➔ ghép vào bản vẽ mới ..."
                : $"\n[iSurvey][DGN] {prefix} ➔ {Path.GetFileName(output)} ...", b);

            var target = b.Target;
            var watch = Stopwatch.StartNew();

            void Dispatch()
            {
                _ = AcadApp.DocumentManager.ExecuteInCommandContextAsync(
                    _ =>
                    {
                        try
                        {
                            var options = b.Options.Clone();
                            options.FilePath = file;
                            options.ImportMode = 2; // nhập thẳng vào bản vẽ đích (tọa độ chuẩn)
                            options.BatchFiles = [];
                            DgnImportService.Start(target, options,
                                report => OnFileDone(b, file, output, prefix, watch, report));
                        }
                        catch (System.Exception ex)
                        {
                            OnFileDone(b, file, output, prefix, watch, new DgnImportReport { Error = ex.Message });
                        }

                        return System.Threading.Tasks.Task.CompletedTask;
                    },
                    null);
            }

            // Bản vẽ vừa tạo kích hoạt bất đồng bộ: chờ nó thật sự là bản vẽ hiện hành rồi mới ra lệnh
            // (gọi sớm gây eNotApplicable).
            if (created)
                WaitUntilActive(b, target, Dispatch);
            else
                Dispatch();

            return; // tiếp tục khi file này xong (OnFileDone)
        }
    }

    /// <summary>Ngữ cảnh lệnh của bản vẽ đích, khi hậu xử lý xong.</summary>
    private static void OnFileDone(
        Batch b, string source, string? output, string prefix, Stopwatch watch, DgnImportReport report)
    {
        var result = new DgnBatchFileResult
        {
            Source = source,
            Output = output,
            Entities = report.ImportedEntities,
            TextsConverted = report.TextsConverted,
            LayersRenamed = report.LayersRenamed,
            LogPath = report.LogPath
        };

        var target = b.Target;
        if (report.Error is not null)
        {
            result.Status = DgnBatchStatus.Failed;
            result.Message = report.Error;
        }
        else
        {
            result.Status = DgnBatchStatus.Ok;
            if (!b.Combine && output is not null && target is not null)
            {
                try
                {
                    using (target.LockDocument())
                    {
                        target.Database.SaveAs(output, DwgVersion.Current);
                    }
                }
                catch (System.Exception ex)
                {
                    result.Status = DgnBatchStatus.Failed;
                    result.Message = "không lưu được DWG: " + ex.Message;
                }
            }
        }

        result.Elapsed = watch.Elapsed;
        AddResult(b, result, prefix);

        if (b.Combine)
        {
            RunLater(b, () => Step(b));
            return;
        }

        // Không ghép: đóng bản vẽ vừa lưu (ngữ cảnh ứng dụng) rồi mới sang file kế tiếp.
        RunLater(b, () => CloseTargetAndContinue(b));
    }

    private static void CloseTargetAndContinue(Batch b)
    {
        var target = b.Target;
        b.Target = null;
        if (target is not null)
        {
            try
            {
                target.CloseAndDiscard();
            }
            catch (System.Exception ex)
            {
                WriteLine($"\n[iSurvey][DGN] Không đóng được bản vẽ tạm: {ex.Message}", b);
            }
        }

        RunLater(b, () => Step(b));
    }

    /// <summary>Tạo bản vẽ trống từ template mặc định (QNEWTEMPLATE) và kích hoạt nó. Chạy ở ngữ cảnh ứng dụng.</summary>
    private static Document CreateBlankDocument()
    {
        var template = string.Empty;
        try
        {
            template = AcadApp.GetSystemVariable("QNEWTEMPLATE") as string ?? string.Empty;
        }
        catch
        {
            // dùng template mặc định của Add("")
        }

        if (!File.Exists(template))
            template = string.Empty;

        var doc = AcadApp.DocumentManager.Add(template);
        AcadApp.DocumentManager.MdiActiveDocument = doc;
        return doc;
    }

    private static void AddResult(Batch b, DgnBatchFileResult result, string prefix)
    {
        b.Results.Add(result);
        var text = result.Status switch
        {
            DgnBatchStatus.Ok =>
                $"OK — {result.Entities} đối tượng, {result.TextsConverted} chữ đổi font, {result.Elapsed.TotalSeconds:0.#}s",
            DgnBatchStatus.Skipped => "BỎ QUA — " + result.Message,
            _ => "LỖI — " + result.Message
        };
        WriteLine($"\n[iSurvey][DGN] {prefix}: {text}", b);
    }

    private static void WriteLine(string text, Batch b)
    {
        // Ghi thông báo không bao giờ được làm hỏng Batch (Editor của bản vẽ không phải hiện hành có thể ném lỗi).
        try
        {
            var ed = AcadApp.DocumentManager.MdiActiveDocument?.Editor ?? b.Orig.Editor;
            ed.WriteMessage(text);
        }
        catch
        {
            try
            {
                b.Orig.Editor.WriteMessage(text);
            }
            catch
            {
                // bỏ qua
            }
        }
    }

    private static void Finish(Batch b)
    {
        IsRunning = false;
        var combinedName = b.Combine ? b.Target?.Name : null;

        // Ghép: để người dùng thấy bản vẽ ghép (đang là bản vẽ hiện hành). Không ghép: quay lại bản vẽ gốc.
        try
        {
            var show = b.Combine && b.Target is not null ? b.Target : b.Orig;
            if (!ReferenceEquals(AcadApp.DocumentManager.MdiActiveDocument, show))
                AcadApp.DocumentManager.MdiActiveDocument = show;
        }
        catch
        {
            // bản vẽ có thể đã đóng
        }

        b.Done(b.Results, combinedName);
    }

    /// <summary>Đường dẫn .dwg đích theo tùy chọn; null = bỏ qua file này.</summary>
    internal static string? ResolveOutput(string dgnPath, DgnBatchExisting policy)
    {
        var dir = Path.GetDirectoryName(dgnPath) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(dgnPath);
        var target = Path.Combine(dir, name + ".dwg");

        if (!File.Exists(target) || policy == DgnBatchExisting.Overwrite)
            return target;

        if (policy == DgnBatchExisting.Skip)
            return null;

        for (var i = 1; i < 10_000; i++)
        {
            var candidate = Path.Combine(dir, $"{name}_{i}.dwg");
            if (!File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    /// <summary>Chờ (theo từng nhịp Idle, tối đa ~20 giây) tới khi <paramref name="doc"/> là bản vẽ hiện hành và rảnh lệnh.</summary>
    private static void WaitUntilActive(Batch b, Document doc, Action then)
    {
        var started = DateTime.UtcNow;
        var activationRequested = false;
        var settledTicks = 0;

        EventHandler? handler = null;
        handler = (_, _) =>
        {
            try
            {
                var active = ReferenceEquals(AcadApp.DocumentManager.MdiActiveDocument, doc);
                if (!active)
                {
                    settledTicks = 0;
                    if (!activationRequested)
                    {
                        activationRequested = true;
                        AcadApp.DocumentManager.MdiActiveDocument = doc;
                    }
                }
                else
                {
                    settledTicks++;
                }

                // Đã là bản vẽ hiện hành và ổn định thêm vài nhịp Idle ➔ an toàn để ra lệnh.
                if (active && settledTicks >= 3)
                {
                    AcadApp.Idle -= handler;
                    then();
                    return;
                }

                if ((DateTime.UtcNow - started).TotalSeconds > 20)
                {
                    AcadApp.Idle -= handler;
                    throw new InvalidOperationException("bản vẽ mới không được kích hoạt sau 20 giây");
                }
            }
            catch (System.Exception ex)
            {
                AcadApp.Idle -= handler;
                Abort(b, ex);
            }
        };
        AcadApp.Idle += handler;
    }

    /// <summary>Chạy <paramref name="action"/> một lần ở sự kiện Idle kế tiếp (ngữ cảnh ứng dụng).</summary>
    private static void RunLater(Batch b, Action action)
    {
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            AcadApp.Idle -= handler;
            try
            {
                action();
            }
            catch (System.Exception ex)
            {
                Abort(b, ex);
            }
        };
        AcadApp.Idle += handler;
    }

    /// <summary>Lỗi nội bộ: ghi log đầy đủ, dọn bản vẽ tạm (nếu không ghép) và kết thúc Batch kèm báo cáo.</summary>
    private static void Abort(Batch b, System.Exception ex)
    {
        string? logPath = null;
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "iSurvey", "logs");
            Directory.CreateDirectory(dir);
            logPath = Path.Combine(dir, $"dgn_batch_error_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            File.WriteAllText(logPath, ex.ToString());
        }
        catch
        {
            // chỉ để chẩn đoán
        }

        try
        {
            var ed = AcadApp.DocumentManager.MdiActiveDocument?.Editor ?? b.Orig.Editor;
            ed.WriteMessage($"\n[iSurvey][DGN] LỖI nội bộ Batch: {ex.GetType().Name}: {ex.Message}");
            if (logPath is not null)
                ed.WriteMessage($"\n[iSurvey][DGN] Chi tiết lỗi: {logPath}");
        }
        catch
        {
            // bỏ qua
        }

        if (!b.Combine && b.Target is { } target)
        {
            b.Target = null;
            try
            {
                target.CloseAndDiscard();
            }
            catch
            {
                // để người dùng tự đóng
            }
        }

        Finish(b);
    }
}
