using System.Diagnostics;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Dgn;

/// <summary>
/// Xuất bản vẽ CAD ra MicroStation DGN (V7/V8) bằng engine có sẵn của Civil 3D: lệnh -DGNEXPORT.
/// <para>
/// Trình tự prompt (đã xác minh trên Civil 3D 2026): định dạng [V7/V8] ➔ tên file ➔ đơn vị chuyển đổi [Master/Sub]
/// ➔ mapping setup ➔ seed. Prompt seed chỉ nhận keyword hoặc "~" (mở hộp thoại modal) — hộp thoại này được
/// <see cref="SeedDialogDriver"/> tự điền, nên người dùng không phải chọn seed.
/// </para>
/// <para>
/// -DGNEXPORT luôn xuất bản vẽ HIỆN HÀNH, nên khi cần xuất "đối tượng đang chọn" hoặc dọn rác, dịch vụ tạo một
/// bản sao tạm (Wblock các đối tượng ➔ DWG tạm ➔ mở ➔ xuất ➔ đóng bỏ). Bản vẽ gốc không bị thay đổi.
/// </para>
/// </summary>
internal static class DgnExportService
{
    private const double TimeoutSeconds = 300;
    private const double SeedStallSeconds = 90;
    private const double QuietSecondsAfterFile = 1.5;
    private const double QuietSecondsNoFile = 4;

    private sealed class Job
    {
        public required Document Orig { get; init; }

        public required DgnExportOptions Options { get; init; }

        public required DgnLog Log { get; init; }

        public required DgnExportReport Report { get; init; }

        public required DgnSeedLocator.SeedInfo Seed { get; init; }

        public required Action<DgnExportReport> Done { get; init; }

        public Stopwatch Watch { get; } = Stopwatch.StartNew();

        public string? ScratchPath { get; set; }

        public Document? Scratch { get; set; }

        public DgnImportService.SysVarScope? Vars { get; set; }

        public SeedDialogDriver? Driver { get; set; }

        public DateTime PollStarted { get; set; }

        public long LastSize { get; set; } = -1;

        public DateTime LastChange { get; set; }

        public EventHandler? IdleHandler { get; set; }

        public bool Finishing { get; set; }
    }

    public static bool IsRunning { get; private set; }

    /// <summary>Bắt đầu xuất. Gọi từ lệnh (ngữ cảnh lệnh của <paramref name="orig"/>); báo cáo trả về qua <paramref name="done"/>.</summary>
    public static void Start(Document orig, DgnExportOptions options, Action<DgnExportReport> done)
    {
        var report = new DgnExportReport { OutputPath = options.OutputPath };
        var log = new DgnLog(orig.Editor, "C2D", "c2d");
        report.LogPath = log.FilePath;

        if (IsRunning)
        {
            Fail(log, report, done, "Một lần xuất DGN khác đang chạy — hãy đợi nó xong.");
            return;
        }

        log.Info($"Bắt đầu xuất DGN {options.VersionText}: {options.OutputPath}");
        log.Info($"Tùy chọn: phạm vi={options.Scope}, dọn rác={options.Purge}, ghi đè={options.Overwrite}, " +
                 $"đơn vị={(options.UseSubUnits ? "Sub" : "Master")}");

        // Bản vẽ có dữ liệu 3D (Z ≠ 0, polyline 3D, solid...) bắt buộc dùng seed 3D; seed 2D bị từ chối
        // ("Incompatible seed file. A 3D seed file is required") và prompt seed treo chờ.
        var is3D = Detect3D(orig.Database, options, out var why3D);
        log.Info(is3D ? $"Dữ liệu 3D: {why3D} ➔ dùng seed 3D." : "Dữ liệu phẳng (Z = 0) ➔ dùng seed 2D.");

        var seed = DgnSeedLocator.Resolve(options.Version, options.SeedOverride, is3D);
        if (seed is null)
        {
            Fail(log, report, done,
                $"Không tìm thấy file Seed {(is3D ? "3D" : "2D")} cho {options.VersionText}. Đặt " +
                $"VN2000_Seed{(is3D ? "3D" : "2D")}_{options.VersionText}.dgn vào thư mục Contents\\Seeds của bundle iSurvey.");
            return;
        }

        report.SeedPath = seed.Path;
        report.SeedSource = seed.Source;
        log.Info($"Seed: {seed.Path} ({seed.Source})");

        try
        {
            var folder = Path.GetDirectoryName(Path.GetFullPath(options.OutputPath));
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                Fail(log, report, done, "Thư mục đích không tồn tại.");
                return;
            }

            if (File.Exists(options.OutputPath))
            {
                if (!options.Overwrite)
                {
                    Fail(log, report, done, "File DGN đích đã tồn tại (đang tắt tùy chọn ghi đè).");
                    return;
                }

                File.Delete(options.OutputPath);
                log.Info("Đã xóa file DGN cũ để ghi đè.");
            }
        }
        catch (System.Exception ex)
        {
            Fail(log, report, done, "Không ghi đè được file đích (đang mở trong MicroStation?): " + ex.Message);
            return;
        }

        var job = new Job
        {
            Orig = orig, Options = options, Log = log, Report = report, Seed = seed, Done = done
        };

        // Xuất trực tiếp bản vẽ hiện hành chỉ khi: toàn bộ + không dọn rác. Còn lại dùng bản sao tạm.
        var direct = options.Scope == DgnExportScope.All && !options.Purge;
        IsRunning = true;

        if (direct)
        {
            log.Info("Xuất trực tiếp bản vẽ hiện hành (không dọn rác).");
            report.Entities = CountModelSpace(orig.Database);
            RunExport(job, orig);
            return;
        }

        try
        {
            job.ScratchPath = BuildScratchCopy(job);
            report.UsedScratchCopy = true;
        }
        catch (System.Exception ex)
        {
            log.Info("LỖI tạo bản sao tạm: " + ex);
            Finish(job, "Không tạo được bản sao tạm để xuất: " + ex.Message);
            return;
        }

        RunLater(job, () => OpenScratch(job));
    }

    // ---------------------------------------------------------------- phát hiện 3D

    /// <summary>
    /// true nếu phạm vi xuất có đối tượng 3D: kiểu 3D thuần (Solid3d, Polyline3d, Face, mesh, surface...) hoặc
    /// vùng bao có Z ≠ 0. Dừng ở đối tượng 3D đầu tiên nên rẻ với bản vẽ lớn có Z.
    /// </summary>
    private static bool Detect3D(Database db, DgnExportOptions options, out string reason)
    {
        reason = string.Empty;
        try
        {
            using var tr = db.TransactionManager.StartOpenCloseTransaction();
            IEnumerable<ObjectId> ids;
            if (options.Scope == DgnExportScope.Selected)
            {
                ids = options.SelectedIds;
            }
            else
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                ids = ms.Cast<ObjectId>();
            }

            foreach (var id in ids)
            {
                if (id.IsNull || id.IsErased)
                    continue;

                if (tr.GetObject(id, OpenMode.ForRead, false, true) is not Entity ent)
                    continue;

                if (ent is Solid3d or Polyline3d or Face or PolyFaceMesh or PolygonMesh or SubDMesh
                    or Autodesk.AutoCAD.DatabaseServices.Surface)
                {
                    reason = $"đối tượng {ent.GetType().Name}";
                    return true;
                }

                try
                {
                    var e = ent.GeometricExtents;
                    if (Math.Abs(e.MinPoint.Z) > 1e-6 || Math.Abs(e.MaxPoint.Z) > 1e-6)
                    {
                        reason = $"{ent.GetType().Name} có Z = {e.MinPoint.Z:0.###}";
                        return true;
                    }
                }
                catch
                {
                    // đối tượng không có vùng bao (xref, proxy...) — bỏ qua
                }
            }
        }
        catch
        {
            // không xác định được ➔ coi là 2D
        }

        return false;
    }

    // ---------------------------------------------------------------- bản sao tạm

    private static string BuildScratchCopy(Job job)
    {
        var db = job.Orig.Database;
        var ids = new ObjectIdCollection();

        using (job.Orig.LockDocument())
        {
            if (job.Options.Scope == DgnExportScope.Selected)
            {
                foreach (var id in job.Options.SelectedIds)
                {
                    if (!id.IsNull && !id.IsErased && id.Database == db)
                        ids.Add(id);
                }
            }
            else
            {
                using var tr = db.TransactionManager.StartOpenCloseTransaction();
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    if (!id.IsErased)
                        ids.Add(id);
                }
            }

            if (ids.Count == 0)
                throw new InvalidOperationException("không có đối tượng nào để xuất.");

            job.Report.Entities = ids.Count;
            job.Log.Info($"Bản sao tạm: {ids.Count} đối tượng " +
                         (job.Options.Scope == DgnExportScope.Selected ? "đang chọn" : "Model Space") +
                         (job.Options.Purge ? " (chỉ giữ layer/linetype/style/block đang dùng)." : "."));

            var dir = Path.Combine(Path.GetTempPath(), "iSurvey_c2d");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"c2d_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.dwg");

            using var copy = new Database(true, true);
            db.Wblock(copy, ids, Point3d.Origin, DuplicateRecordCloning.Replace);

            // Giữ cấu hình đơn vị của bản vẽ gốc (ảnh hưởng quy đổi khi xuất).
            TryCopy(() => copy.Insunits = db.Insunits);
            TryCopy(() => copy.Lunits = db.Lunits);
            TryCopy(() => copy.Luprec = db.Luprec);
            TryCopy(() => copy.Measurement = db.Measurement);
            TryCopy(() => copy.Ltscale = db.Ltscale);

            copy.SaveAs(path, DwgVersion.Current);
            job.Log.Info("Đã lưu bản sao tạm: " + path);
            return path;
        }
    }

    private static void TryCopy(Action action)
    {
        try
        {
            action();
        }
        catch
        {
            // biến không áp dụng được — bỏ qua
        }
    }

    private static int CountModelSpace(Database db)
    {
        try
        {
            using var tr = db.TransactionManager.StartOpenCloseTransaction();
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            var n = 0;
            foreach (ObjectId _ in ms)
                n++;
            return n;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>Ngữ cảnh ứng dụng: mở bản sao tạm, chờ nó thành bản vẽ hiện hành rồi mới ra lệnh.</summary>
    private static void OpenScratch(Job job)
    {
        job.Scratch = AcadApp.DocumentManager.Open(job.ScratchPath!, false);
        job.Log.Info("Đã mở bản sao tạm.");

        WaitUntilActive(job, job.Scratch, () =>
        {
            _ = AcadApp.DocumentManager.ExecuteInCommandContextAsync(
                _ =>
                {
                    try
                    {
                        RunExport(job, job.Scratch!);
                    }
                    catch (System.Exception ex)
                    {
                        job.Log.Info("LỖI: " + ex);
                        Finish(job, ex.Message);
                    }

                    return System.Threading.Tasks.Task.CompletedTask;
                },
                null);
        });
    }

    // ---------------------------------------------------------------- xuất

    /// <summary>Ngữ cảnh lệnh của <paramref name="doc"/>: đặt biến, gửi -DGNEXPORT, rồi theo dõi tới khi file xuất hiện.</summary>
    private static void RunExport(Job job, Document doc)
    {
        var log = job.Log;
        var options = job.Options;

        try
        {
            job.Vars = new DgnImportService.SysVarScope(log);
            job.Vars.Set("FILEDIA", 0);
            job.Vars.Set("CMDDIA", 0);
            job.Vars.Set("CMDECHO", 1);

            job.Driver = new SeedDialogDriver(job.Seed.Path);

            var args = new List<object>
            {
                "_.-DGNEXPORT",
                options.VersionText,
                options.OutputPath,
                options.UseSubUnits ? "_Sub" : "_Master",
                options.MappingSetup,
                "~" // mở hộp thoại Select Seed File — SeedDialogDriver tự điền
            };

            log.Info("Gọi: " + string.Join(" | ", args.Select(a => a is string { Length: 0 } ? "<Enter>" : a.ToString())));
            doc.Editor.Command(args.ToArray());
            log.Info($"CMDACTIVE sau lệnh = {SafeSysVar("CMDACTIVE")}; hộp thoại seed đã điền {job.Driver.Driven} lần.");
        }
        catch (System.Exception ex)
        {
            log.Info("LỖI khi gửi lệnh: " + ex);
            Finish(job, ex.Message);
            return;
        }

        job.PollStarted = DateTime.UtcNow;
        job.LastChange = DateTime.UtcNow;
        job.IdleHandler = (_, _) => OnIdle(job, doc);
        AcadApp.Idle += job.IdleHandler;
    }

    private static void OnIdle(Job job, Document doc)
    {
        if (job.Finishing)
            return;

        try
        {
            var now = DateTime.UtcNow;
            var path = job.Options.OutputPath;
            var exists = File.Exists(path);
            var size = exists ? new FileInfo(path).Length : -1;
            if (size != job.LastSize)
            {
                job.LastSize = size;
                job.LastChange = now;
            }

            var active = (Convert.ToInt32(AcadApp.GetSystemVariable("CMDACTIVE")) & 1) != 0;
            var quiet = (now - job.LastChange).TotalSeconds;
            var timedOut = (now - job.PollStarted).TotalSeconds > TimeoutSeconds;

            if (exists && size > 0 && !active && quiet >= QuietSecondsAfterFile)
                Complete(job, null);
            else if (!exists && !active && quiet >= QuietSecondsNoFile)
                Complete(job, "Lệnh -DGNEXPORT kết thúc nhưng không tạo file DGN. Xem log để biết prompt cuối: " +
                              SafeSysVar("LASTPROMPT"));
            else if (!exists && active && (now - job.PollStarted).TotalSeconds > SeedStallSeconds
                     && SafeSysVar("LASTPROMPT").Contains("seed", StringComparison.OrdinalIgnoreCase))
            {
                // Seed bị -DGNEXPORT từ chối thì prompt seed hiện lại và chờ mãi (không file, lệnh vẫn chạy).
                try
                {
                    doc.SendStringToExecute("\u001b\u001b", true, false, false);
                }
                catch
                {
                    // bỏ qua
                }

                Complete(job, "Kẹt ở bước chọn seed: -DGNEXPORT không nhận seed " +
                              $"'{Path.GetFileName(job.Seed.Path)}' (xem dòng 'Incompatible seed file' trên Command Line).");
            }
            else if (timedOut)
            {
                try
                {
                    doc.SendStringToExecute("\u001b\u001b", true, false, false);
                }
                catch
                {
                    // bỏ qua
                }

                Complete(job, $"Quá thời gian {TimeoutSeconds:0}s. Prompt cuối: {SafeSysVar("LASTPROMPT")}");
            }
        }
        catch (System.Exception ex)
        {
            job.Log.Info("LỖI khi theo dõi xuất: " + ex);
            Complete(job, ex.Message);
        }
    }

    private static void Complete(Job job, string? error)
    {
        if (job.Finishing)
            return;

        job.Finishing = true;
        if (job.IdleHandler is not null)
            AcadApp.Idle -= job.IdleHandler;

        if (error is null)
        {
            try
            {
                job.Report.OutputBytes = new FileInfo(job.Options.OutputPath).Length;
            }
            catch
            {
                // chỉ để báo cáo
            }
        }

        Finish(job, error);
    }

    /// <summary>Dọn dẹp (biến, luồng điền hộp thoại, bản sao tạm) rồi báo kết quả.</summary>
    private static void Finish(Job job, string? error)
    {
        job.Finishing = true;
        job.Report.Error = error;
        job.Report.Elapsed = job.Watch.Elapsed;

        try
        {
            job.Driver?.Dispose();
        }
        catch
        {
            // bỏ qua
        }

        try
        {
            job.Vars?.Dispose();
        }
        catch
        {
            // bỏ qua
        }

        if (error is not null)
            job.Log.Info("KHÔNG hoàn tất: " + error);

        if (job.Scratch is null && job.ScratchPath is null)
        {
            ReportDone(job);
            return;
        }

        // Đóng bản sao tạm (ngữ cảnh ứng dụng), quay lại bản vẽ gốc, xóa file tạm.
        RunLater(job, () =>
        {
            try
            {
                if (job.Scratch is not null)
                    job.Scratch.CloseAndDiscard();
            }
            catch (System.Exception ex)
            {
                job.Log.Info("Không đóng được bản sao tạm: " + ex.Message);
            }

            try
            {
                if (!ReferenceEquals(AcadApp.DocumentManager.MdiActiveDocument, job.Orig))
                    AcadApp.DocumentManager.MdiActiveDocument = job.Orig;
            }
            catch
            {
                // bản vẽ gốc có thể đã đóng
            }

            DeleteScratchFiles(job);
            ReportDone(job);
        });
    }

    private static void DeleteScratchFiles(Job job)
    {
        if (job.ScratchPath is null)
            return;

        foreach (var ext in new[] { ".dwg", ".bak", ".dwl", ".dwl2", ".sv$" })
        {
            try
            {
                var file = Path.ChangeExtension(job.ScratchPath, ext);
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch
            {
                // file tạm — không quan trọng
            }
        }
    }

    private static void ReportDone(Job job)
    {
        IsRunning = false;
        try
        {
            job.Done(job.Report);
        }
        finally
        {
            job.Log.Dispose();
        }
    }

    private static void Fail(DgnLog log, DgnExportReport report, Action<DgnExportReport> done, string message)
    {
        report.Error = message;
        log.Info("KHÔNG hoàn tất: " + message);
        try
        {
            done(report);
        }
        finally
        {
            log.Dispose();
        }
    }

    // ---------------------------------------------------------------- tiện ích

    private static string SafeSysVar(string name)
    {
        try
        {
            return Convert.ToString(AcadApp.GetSystemVariable(name)) ?? string.Empty;
        }
        catch
        {
            return "?";
        }
    }

    /// <summary>Chờ (theo nhịp Idle, tối đa ~20 giây) tới khi <paramref name="doc"/> là bản vẽ hiện hành và ổn định.</summary>
    private static void WaitUntilActive(Job job, Document doc, Action then)
    {
        var started = DateTime.UtcNow;
        var requested = false;
        var settled = 0;

        EventHandler? handler = null;
        handler = (_, _) =>
        {
            try
            {
                var active = ReferenceEquals(AcadApp.DocumentManager.MdiActiveDocument, doc);
                if (!active)
                {
                    settled = 0;
                    if (!requested)
                    {
                        requested = true;
                        AcadApp.DocumentManager.MdiActiveDocument = doc;
                    }
                }
                else
                {
                    settled++;
                }

                if (active && settled >= 3)
                {
                    AcadApp.Idle -= handler;
                    then();
                    return;
                }

                if ((DateTime.UtcNow - started).TotalSeconds > 20)
                {
                    AcadApp.Idle -= handler;
                    throw new InvalidOperationException("bản sao tạm không được kích hoạt sau 20 giây");
                }
            }
            catch (System.Exception ex)
            {
                AcadApp.Idle -= handler;
                job.Log.Info("LỖI: " + ex);
                Finish(job, ex.Message);
            }
        };
        AcadApp.Idle += handler;
    }

    /// <summary>Chạy <paramref name="action"/> một lần ở nhịp Idle kế tiếp (ngữ cảnh ứng dụng).</summary>
    private static void RunLater(Job job, Action action)
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
                job.Log.Info("LỖI: " + ex);
                if (!job.Finishing)
                    Finish(job, ex.Message);
                else
                    ReportDone(job);
            }
        };
        AcadApp.Idle += handler;
    }
}
