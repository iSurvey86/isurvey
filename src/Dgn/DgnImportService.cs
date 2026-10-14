using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace iSurvey.Dgn;

/// <summary>Kết quả một lần nhập DGN.</summary>
internal sealed class DgnImportReport
{
    public string? Error { get; set; }

    public int ImportedEntities { get; set; }

    public int ExplodedCells { get; set; }

    public int ExplodeFailures { get; set; }

    public int BlocksPurged { get; set; }

    public int LayersRenamed { get; set; }

    public int LayersMerged { get; set; }

    public int TextsConverted { get; set; }

    public int LinetypesPurged { get; set; }

    public int LayersPurged { get; set; }

    public Extents3d? Extents { get; set; }

    /// <summary>Bản vẽ mới do DGNIMPORT tạo (khi DGNIMPORTMODE=0, dùng cho Batch); null nếu nhập vào bản vẽ hiện hành.</summary>
    public Document? NewDocument { get; set; }

    public List<string> Warnings { get; } = [];

    public string? LogPath { get; set; }

    public bool Succeeded => Error is null && ImportedEntities > 0;
}

/// <summary>
/// Nhập DGN (V7/V8) bằng engine có sẵn của AutoCAD/Civil 3D: gọi lệnh -DGNIMPORT với FILEDIA=0,
/// lọc đối tượng mới theo Database.Handseed, bọc UNDO BEGIN/END, rồi hậu xử lý
/// (Explode cell, đổi tên layer, TCVN3 ➔ Unicode, dọn rác, zoom).
/// </summary>
internal static class DgnImportService
{
    private const string ArialStyleName = "ISURVEY_ARIAL";
    private const int MaxExplodePasses = 6;
    private const int MaxExplodeEntities = 500_000;

    private static readonly System.Text.RegularExpressions.Regex V8LayerPrefixRegex =
        new(@"^.+\$\d+\$(.+)$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private const double QuietSecondsAfterChange = 2.0;
    private const double QuietSecondsWhenForcedIdle = 10.0;
    private const double QuietSecondsNoObjects = 8.0;
    private const double TimeoutSeconds = 300.0;

    /// <summary>Một lần nhập đang chạy: DGNIMPORT hoàn tất BẤT ĐỒNG BỘ sau khi lệnh của ta đã trả về.</summary>
    private sealed class Job
    {
        /// <summary>Bản vẽ đích. Đổi sang bản vẽ MỚI nếu DGNIMPORT tạo Drawing mới.</summary>
        public required Document Doc { get; set; }

        /// <summary>Các bản vẽ đã mở trước khi nhập (để nhận ra bản vẽ do DGNIMPORT tạo).</summary>
        public HashSet<Document> KnownDocs { get; } = [];

        public bool TargetIsNewDoc { get; set; }

        public required DgnImportOptions Options { get; init; }

        public required DgnLog Log { get; init; }

        public required DgnImportReport Report { get; init; }

        public required SysVarScope Vars { get; init; }

        public required Action<DgnImportReport> Done { get; init; }

        public long Seed { get; set; }

        public long LastSeed { get; set; }

        /// <summary>Handseed của bản vẽ mới ngay khi phát hiện (để biết khi nào DGNIMPORT bắt đầu ghi đối tượng).</summary>
        public long BaselineSeed { get; set; }

        public DateTime Started { get; } = DateTime.UtcNow;

        public DateTime LastChange { get; set; } = DateTime.UtcNow;

        public bool UndoOpen { get; set; }

        public bool Finishing { get; set; }

        public EventHandler? IdleHandler { get; set; }
    }

    /// <summary>
    /// Bắt đầu nhập: gửi -DGNIMPORT rồi TRẢ VỀ NGAY. Civil 3D hoàn tất việc nhập sau khi lệnh kết thúc
    /// (log thực tế: CMDACTIVE=1 và 0 đối tượng ngay sau Editor.Command), nên hậu xử lý được hoãn tới
    /// khi Handseed ngừng thay đổi và không còn lệnh đang chạy (xem <see cref="OnIdle"/>).
    /// </summary>
    public static void Start(Document doc, DgnImportOptions options, Action<DgnImportReport> done)
    {
        var report = new DgnImportReport();
        var db = doc.Database;
        var ed = doc.Editor;

        var log = new DgnLog(ed);
        report.LogPath = log.FilePath;

        log.Info($"Bắt đầu nhập: {options.FilePath}");
        log.Info($"Tùy chọn: đơn vị={(options.UseSubUnits ? "Sub" : "Master")}, layer={options.LayerMode}, " +
                 $"font={options.FixFonts}, explode={options.ExplodeCells}, dọn rác={options.PurgeJunk}, zoom={options.ZoomToImport}");

        if (!File.Exists(options.FilePath))
        {
            report.Error = "Không tìm thấy file DGN.";
            log.Info(report.Error);
            log.Dispose();
            done(report);
            return;
        }

        var seed = db.Handseed.Value;
        log.Info($"Handseed trước khi nhập = {seed:X}");

        var job = new Job
        {
            Doc = doc,
            Options = options,
            Log = log,
            Report = report,
            Vars = new SysVarScope(log),
            Done = done,
            Seed = seed,
            LastSeed = seed
        };

        try
        {
            job.Vars.Set("FILEDIA", 0);
            job.Vars.Set("CMDDIA", 0);
            job.Vars.Set("CMDECHO", 1);
            // DGNIMPORTMODE=2: nhập thẳng vào bản vẽ hiện hành (mặc định 0 tạo bản vẽ MỚI — log thực tế).
            if (options.ImportMode >= 0)
                job.Vars.Set("DGNIMPORTMODE", options.ImportMode);

            foreach (Document d in AcadApp.DocumentManager)
                job.KnownDocs.Add(d);

            RunImportCommand(ed, options, log);
        }
        catch (System.Exception ex)
        {
            report.Error = ex.Message;
            log.Info("LỖI khi gửi lệnh: " + ex);
            FinishJob(job);
            return;
        }

        log.Info("Đang chờ Civil 3D nhập xong DGN (hậu xử lý tự chạy khi hoàn tất)...");
        job.LastSeed = db.Handseed.Value;
        job.LastChange = DateTime.UtcNow;
        job.IdleHandler = (_, _) => OnIdle(job);
        AcadApp.Idle += job.IdleHandler;
    }

    private static bool IsCommandActive()
    {
        try
        {
            return (Convert.ToInt32(AcadApp.GetSystemVariable("CMDACTIVE")) & 1) != 0;
        }
        catch
        {
            return false;
        }
    }

    private static void OnIdle(Job job)
    {
        if (job.Finishing)
            return;

        try
        {
            var now = DateTime.UtcNow;

            // DGNIMPORT có thể tạo bản vẽ mới (Drawing2...) thay vì nhập vào bản vẽ hiện hành.
            if (!job.TargetIsNewDoc)
            {
                foreach (Document d in AcadApp.DocumentManager)
                {
                    if (job.KnownDocs.Contains(d))
                        continue;

                    job.Log.Info($"DGNIMPORT đã tạo bản vẽ MỚI '{System.IO.Path.GetFileName(d.Name)}' — " +
                                 "hậu xử lý sẽ chạy trên bản vẽ đó (không nhập vào bản vẽ cũ).");
                    job.KnownDocs.Add(d);
                    job.Doc = d;
                    job.TargetIsNewDoc = true;
                    job.Seed = 0;
                    job.BaselineSeed = d.Database.Handseed.Value;
                    job.LastSeed = job.BaselineSeed;
                    job.LastChange = now;
                    break;
                }
            }

            var seedNow = job.Doc.Database.Handseed.Value;
            if (seedNow != job.LastSeed)
            {
                job.LastSeed = seedNow;
                job.LastChange = now;
            }

            var active = IsCommandActive();
            var quiet = (now - job.LastChange).TotalSeconds;
            // Bản vẽ mới: Seed=0 (lấy mọi đối tượng) nhưng "đã có thay đổi" chỉ tính khi Handseed vượt mốc lúc tạo.
            var changed = seedNow > (job.TargetIsNewDoc ? job.BaselineSeed : job.Seed);
            var timeout = (now - job.Started).TotalSeconds > TimeoutSeconds;

            var ready =
                (changed && quiet >= QuietSecondsAfterChange && (!active || quiet >= QuietSecondsWhenForcedIdle))
                || (!changed && !active && quiet >= QuietSecondsNoObjects);

            if (!ready && !timeout)
                return;

            // Hậu xử lý cần lệnh trong bản vẽ đang hoạt động.
            if (!timeout && !ReferenceEquals(AcadApp.DocumentManager.MdiActiveDocument, job.Doc))
                return;

            job.Finishing = true;
            AcadApp.Idle -= job.IdleHandler;

            _ = AcadApp.DocumentManager.ExecuteInCommandContextAsync(
                _ =>
                {
                    Complete(job);
                    return System.Threading.Tasks.Task.CompletedTask;
                },
                null);
        }
        catch (System.Exception ex)
        {
            job.Finishing = true;
            AcadApp.Idle -= job.IdleHandler;
            job.Report.Error = ex.Message;
            job.Log.Info("LỖI khi theo dõi import: " + ex);
            FinishJob(job);
        }
    }

    /// <summary>Chạy trong ngữ cảnh lệnh: thu thập đối tượng mới, hậu xử lý, zoom, đóng UNDO.</summary>
    private static void Complete(Job job)
    {
        var report = job.Report;
        var log = job.Log;
        var db = job.Doc.Database;
        var ed = job.Doc.Editor;
        var options = job.Options;

        try
        {
            using (job.Doc.LockDocument())
            {
                var imported = CollectNewModelSpace(db, job.Seed);
                report.ImportedEntities = imported.Count;
                log.Info($"DGNIMPORT sinh ra {imported.Count} đối tượng mới trong Model Space " +
                         $"của '{System.IO.Path.GetFileName(job.Doc.Name)}' " +
                         $"(Handseed {job.Seed:X} ➔ {db.Handseed.Value:X}" +
                         (job.TargetIsNewDoc ? ", bản vẽ mới" : "") + ").");

                if (imported.Count == 0)
                {
                    report.Error = "DGNIMPORT không tạo đối tượng nào. Xem prompt trên Command Line " +
                                   "(trình tự prompt có thể khác) và gửi lại log.";
                }
                else
                {
                    // Một nhóm UNDO cho toàn bộ hậu xử lý (bản thân DGNIMPORT là một bước UNDO riêng).
                    try
                    {
                        ed.Command("_.UNDO", "_BEGIN");
                        job.UndoOpen = true;
                    }
                    catch (System.Exception ex)
                    {
                        log.Info("Không mở được nhóm UNDO: " + ex.Message);
                    }

                    PostProcess(db, job.Seed, job.TargetIsNewDoc, options, report, log);
                }
            }

            if (report.Error is null && options.ZoomToImport)
                ZoomExtents(ed, log);
        }
        catch (System.Exception ex)
        {
            report.Error = ex.Message;
            log.Info("LỖI: " + ex);
        }
        finally
        {
            FinishJob(job);
        }
    }

    /// <summary>Đóng UNDO group, khôi phục biến hệ thống, đóng log, báo kết quả.</summary>
    private static void FinishJob(Job job)
    {
        if (job.UndoOpen)
        {
            try
            {
                job.Doc.Editor.Command("_.UNDO", "_END");
            }
            catch (System.Exception ex)
            {
                job.Log.Info("Không đóng được UNDO group: " + ex.Message);
            }

            job.UndoOpen = false;
        }

        job.Vars.Dispose();
        if (job.TargetIsNewDoc)
            job.Report.NewDocument = job.Doc;
        try
        {
            job.Done(job.Report);
        }
        finally
        {
            job.Log.Dispose();
        }
    }

    private static void RunImportCommand(Editor ed, DgnImportOptions options, DgnLog log)
    {
        var units = options.UseSubUnits ? "_Sub" : "_Master";
        var args = new List<object> { options.Command };
        foreach (var template in options.CommandArgs)
        {
            args.Add(template
                .Replace("{path}", options.FilePath)
                .Replace("{units}", units)
                .Replace("{mapping}", options.MappingSetup));
        }

        log.Info("Gọi: " + string.Join(" | ", args.Select(a => a is string { Length: 0 } ? "<Enter>" : a.ToString())));
        ed.Command(args.ToArray());

        try
        {
            log.Info("CMDACTIVE sau import = " + AcadApp.GetSystemVariable("CMDACTIVE"));
        }
        catch
        {
            // chỉ để đối chiếu
        }
    }

    // ---------------------------------------------------------------- hậu xử lý

    /// <summary>
    /// <paramref name="newDoc"/>=true: bản vẽ do DGNIMPORT tạo từ template (không biết mốc Handseed riêng cho
    /// layer/block), nên chỉ xử lý layer đang được đối tượng dùng, và không purge block/linetype/layer của template.
    /// </summary>
    private static void PostProcess(
        Database db, long seed, bool newDoc, DgnImportOptions options, DgnImportReport report, DgnLog log)
    {
        if (options.ExplodeCells)
        {
            ExplodeCells(db, seed, report, log);
            if (!newDoc)
            {
                report.BlocksPurged = PurgeNewBlocks(db, seed);
                if (report.BlocksPurged > 0)
                    log.Info($"Đã xóa {report.BlocksPurged} block định nghĩa thừa sau Explode.");
            }
        }

        using (var tr = db.TransactionManager.StartTransaction())
        {
            var msIds = CollectNewModelSpace(db, seed);
            var blockEntityIds = newDoc ? [] : CollectNewBlockEntities(db, seed, tr);
            var usedLayers = newDoc ? CollectUsedLayerNames(msIds, tr) : null;
            LogNewLayers(db, seed, usedLayers, tr, log);

            // Keep cũng chạy: bỏ tiền tố "<file> - <model>$0$" để các mảnh dùng chung layer "Level N".
            var merge = ApplyLayerMode(db, seed, usedLayers, options, tr, report, log);

            var all = new List<ObjectId>(msIds.Count + blockEntityIds.Count);
            all.AddRange(msIds);
            all.AddRange(blockEntityIds);

            if (merge.Count > 0)
                MergeLayers(all, merge, tr);

            if (options.FixFonts)
                report.TextsConverted = FixTexts(db, all, tr, log);

            report.Extents = ComputeExtents(msIds, tr);
            tr.Commit();
        }

        if (options.PurgeJunk && !newDoc)
        {
            report.LinetypesPurged = PurgeNewSymbols(db, seed, db.LinetypeTableId);
            report.LayersPurged = PurgeNewSymbols(db, seed, db.LayerTableId);
            log.Info($"Dọn rác: {report.LinetypesPurged} linetype, {report.LayersPurged} layer không dùng.");
        }

        CheckExtents(report);
    }

    // ---- Explode

    private static void ExplodeCells(Database db, long seed, DgnImportReport report, DgnLog log)
    {
        var failed = new HashSet<ObjectId>();
        var total = 0;

        for (var pass = 1; pass <= MaxExplodePasses; pass++)
        {
            var refs = CollectNewModelSpace(db, seed)
                .Where(id => id.ObjectClass.DxfName == "INSERT" && !failed.Contains(id))
                .ToList();
            if (refs.Count == 0)
                break;

            var done = 0;
            using var tr = db.TransactionManager.StartTransaction();
            var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);

            foreach (var id in refs)
            {
                if (total >= MaxExplodeEntities)
                {
                    report.Warnings.Add($"Dừng Explode ở mức {MaxExplodeEntities} đối tượng (giới hạn an toàn).");
                    break;
                }

                try
                {
                    var br = (BlockReference)tr.GetObject(id, OpenMode.ForWrite);
                    var parts = new DBObjectCollection();
                    br.Explode(parts);
                    foreach (DBObject part in parts)
                    {
                        if (part is Entity entity)
                        {
                            ms.AppendEntity(entity);
                            tr.AddNewlyCreatedDBObject(entity, true);
                            total++;
                        }
                        else
                        {
                            part.Dispose();
                        }
                    }

                    br.Erase();
                    done++;
                }
                catch (System.Exception ex)
                {
                    failed.Add(id);
                    report.ExplodeFailures++;
                    if (report.ExplodeFailures <= 3)
                        log.Info($"Không Explode được một cell: {ex.Message}");
                }
            }

            tr.Commit();
            report.ExplodedCells += done;
            log.Info($"Explode lượt {pass}: {done}/{refs.Count} cell.");

            if (done == 0 || total >= MaxExplodeEntities)
                break;
        }
    }

    // ---- Layer

    /// <summary>Tên các layer đang có đối tượng nằm trên (dùng cho bản vẽ mới, không có mốc Handseed riêng).</summary>
    private static HashSet<string> CollectUsedLayerNames(List<ObjectId> ids, Transaction tr)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in ids)
        {
            try
            {
                if (tr.GetObject(id, OpenMode.ForRead) is Entity ent)
                    names.Add(ent.Layer);
            }
            catch
            {
                // bỏ qua
            }
        }

        return names;
    }

    /// <summary>Layer do DGN sinh ra: theo Handseed (bản vẽ hiện hành) hoặc theo tên đang dùng (bản vẽ mới).</summary>
    private static bool IsImportedLayer(ObjectId id, string name, long seed, HashSet<string>? usedNames) =>
        usedNames is null
            ? id.Handle.Value >= seed
            : usedNames.Contains(name) && name != "0" && !name.Equals("Defpoints", StringComparison.OrdinalIgnoreCase);

    private static void LogNewLayers(Database db, long seed, HashSet<string>? usedNames, Transaction tr, DgnLog log)
    {
        var names = new List<string>();
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        foreach (ObjectId id in lt)
        {
            var name = ((LayerTableRecord)tr.GetObject(id, OpenMode.ForRead)).Name;
            if (IsImportedLayer(id, name, seed, usedNames))
                names.Add(name);
        }

        log.Info(names.Count == 0
            ? "DGNIMPORT không tạo layer mới (đối tượng nằm trên layer có sẵn)."
            : $"Layer mới ({names.Count}): " + string.Join(", ", names.Take(40)) + (names.Count > 40 ? ", ..." : ""));
    }

    /// <summary>Đổi tên layer mới tại chỗ; layer trùng đích được ghi vào bảng gộp (merge).</summary>
    private static Dictionary<string, string> ApplyLayerMode(
        Database db, long seed, HashSet<string>? usedNames, DgnImportOptions options, Transaction tr,
        DgnImportReport report, DgnLog log)
    {
        var merge = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var map = options.LayerMode == DgnLayerMode.Map ? DgnLayerMap.Load() : null;
        if (map is not null)
            log.Info($"Bảng ánh xạ: {map.Levels.Count} Level ({map.SourcePath ?? "không đọc được"}).");

        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        var newLayers = new List<(ObjectId Id, string Name)>();
        foreach (ObjectId id in lt)
        {
            var layerName = ((LayerTableRecord)tr.GetObject(id, OpenMode.ForRead)).Name;
            if (IsImportedLayer(id, layerName, seed, usedNames))
                newLayers.Add((id, layerName));
        }

        var unmapped = new List<string>();
        foreach (var (id, name) in newLayers)
        {
            if (name == "0" || name.Equals("Defpoints", StringComparison.OrdinalIgnoreCase))
                continue;

            string? target = null;
            if (options.LayerMode == DgnLayerMode.Prefix)
            {
                if (!name.StartsWith(options.LayerPrefix, StringComparison.OrdinalIgnoreCase))
                    target = options.LayerPrefix + name;
            }
            else if (map is not null)
            {
                if (DgnLayerMap.TryExtractLevel(name, out var level) && map.Levels.TryGetValue(level, out var layer))
                    target = layer;
                else
                    unmapped.Add(name);
            }
            else
            {
                // Keep: "DC1 - Model$0$Level 10" ➔ "Level 10" (V7 chỉ có số "46" thì giữ nguyên).
                var m = V8LayerPrefixRegex.Match(name);
                if (m.Success)
                    target = m.Groups[1].Value;
            }

            if (target is null || target.Equals(name, StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                SymbolUtilityServices.ValidateSymbolName(target, false);
            }
            catch
            {
                log.Info($"Tên layer đích không hợp lệ, bỏ qua: '{target}'.");
                continue;
            }

            if (!lt.Has(target))
            {
                var rec = (LayerTableRecord)tr.GetObject(id, OpenMode.ForWrite);
                rec.Name = target;
                report.LayersRenamed++;
            }
            else
            {
                merge[name] = target;
                report.LayersMerged++;
            }
        }

        log.Info($"Layer: đổi tên {report.LayersRenamed}, gộp vào layer có sẵn {report.LayersMerged}.");
        if (unmapped.Count > 0)
            log.Info("Layer không có trong bảng ánh xạ (giữ nguyên): " + string.Join(", ", unmapped.Take(30)));

        return merge;
    }

    private static void MergeLayers(List<ObjectId> ids, Dictionary<string, string> merge, Transaction tr)
    {
        foreach (var id in ids)
        {
            try
            {
                var ent = (Entity)tr.GetObject(id, OpenMode.ForRead);
                if (!merge.TryGetValue(ent.Layer, out var target))
                    continue;

                ent.UpgradeOpen();
                ent.Layer = target;
            }
            catch
            {
                // entity không mở được ➔ bỏ qua
            }
        }
    }

    // ---- Font TCVN3

    private static int FixTexts(Database db, List<ObjectId> ids, Transaction tr, DgnLog log)
    {
        var texts = new List<(ObjectId Id, string Text, bool IsMText, ObjectId StyleId)>();
        foreach (var id in ids)
        {
            var cls = id.ObjectClass.DxfName;
            if (cls != "TEXT" && cls != "MTEXT")
                continue;

            try
            {
                var obj = tr.GetObject(id, OpenMode.ForRead);
                if (obj is DBText t)
                    texts.Add((id, t.TextString, false, t.TextStyleId));
                else if (obj is MText m)
                    texts.Add((id, m.Contents, true, m.TextStyleId));
            }
            catch
            {
                // bỏ qua
            }
        }

        if (texts.Count == 0)
        {
            log.Info("Font: không có DBText/MText mới.");
            return 0;
        }

        // DGN V8: DGNIMPORT tạo style "…Styledgn000" cho font không có (TCVN3 .Vn*) và style "…StyleArial"
        // cho text Unicode thật. Chỉ chuỗi ở style dgnNNN mới cần chuyển (sau khi giải mã "N/64", ∅, ±).
        var dgnStyleCache = new Dictionary<ObjectId, bool>();
        var v8Mode = texts.Any(t => IsDgnFallbackStyle(t.StyleId, dgnStyleCache, tr));
        if (v8Mode)
            return FixTextsV8(db, texts, dgnStyleCache, tr, log);

        var strong = 0;
        foreach (var item in texts)
        {
            if (Tcvn3Converter.Classify(item.Text) == Tcvn3Converter.Kind.Strong)
                strong++;
        }

        // Bằng chứng thứ hai: text style dùng font TCVN3 (.VnTime, .VnArial, ...).
        var fontNames = new Dictionary<ObjectId, string>();
        var vnFontTexts = 0;
        foreach (var item in texts)
        {
            if (Tcvn3Converter.Classify(item.Text) == Tcvn3Converter.Kind.None)
                continue;
            if (Tcvn3Converter.IsTcvn3Font(StyleFontName(item.StyleId, fontNames, tr)))
                vnFontTexts++;
        }

        var detected = strong > 0 || vnFontTexts > 0;
        log.Info($"Font: {texts.Count} chuỗi, {strong} chuỗi có dấu hiệu TCVN3 rõ ràng, " +
                 $"{vnFontTexts} chuỗi dùng font .Vn* ➔ " +
                 (detected ? "áp dụng chuyển mã." : "KHÔNG chuyển (không thấy TCVN3)."));

        // Ghi mẫu (kèm font) để đối chiếu nếu kết quả không như mong đợi.
        foreach (var sample in texts.Where(t => Tcvn3Converter.Classify(t.Text) != Tcvn3Converter.Kind.None).Take(6))
        {
            log.Info($"  mẫu: '{sample.Text}' [font={StyleFontName(sample.StyleId, fontNames, tr)}] ➔ " +
                     $"'{Tcvn3Converter.ToUnicode(sample.Text, false)}'");
        }

        foreach (var sample in texts.Where(t => Tcvn3Converter.Classify(t.Text) == Tcvn3Converter.Kind.None
                                                && t.Text.Any(c => c > 0x7F)).Take(3))
            log.Info($"  chuỗi không đổi (có ký tự lạ): '{sample.Text}' [font={StyleFontName(sample.StyleId, fontNames, tr)}]");

        if (!detected)
            return 0;

        var styleId = EnsureArialStyle(db, tr);
        var upperCache = new Dictionary<ObjectId, bool>();
        var converted = 0;

        foreach (var item in texts)
        {
            if (Tcvn3Converter.Classify(item.Text) == Tcvn3Converter.Kind.None)
                continue;

            try
            {
                var ent = (Entity)tr.GetObject(item.Id, OpenMode.ForWrite);
                if (ent is DBText t)
                {
                    var upper = IsUpperStyle(t.TextStyleId, upperCache, tr) || Tcvn3Converter.LooksAllCaps(item.Text);
                    t.TextString = Tcvn3Converter.ToUnicode(item.Text, upper);
                    t.TextStyleId = styleId;
                    if (t.Justify != AttachmentPoint.BaseLeft)
                    {
                        try
                        {
                            t.AdjustAlignment(db);
                        }
                        catch
                        {
                            // giữ nguyên điểm gióng nếu không điều chỉnh được
                        }
                    }
                }
                else if (ent is MText m)
                {
                    var upper = IsUpperStyle(m.TextStyleId, upperCache, tr) || Tcvn3Converter.LooksAllCaps(item.Text);
                    m.Contents = Tcvn3Converter.ToUnicode(item.Text, upper);
                    m.TextStyleId = styleId;
                }

                converted++;
            }
            catch (System.Exception ex)
            {
                log.Info("Không đổi được một chuỗi: " + ex.Message);
            }
        }

        log.Info($"Đã chuyển TCVN3 ➔ Unicode (Arial) cho {converted} chuỗi.");
        return converted;
    }

    private static bool IsDgnFallbackStyle(ObjectId styleId, Dictionary<ObjectId, bool> cache, Transaction tr)
    {
        if (cache.TryGetValue(styleId, out var cached))
            return cached;

        var result = false;
        try
        {
            var rec = (TextStyleTableRecord)tr.GetObject(styleId, OpenMode.ForRead);
            result = System.Text.RegularExpressions.Regex.IsMatch(rec.Name, @"dgn\d+$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        catch
        {
            // false
        }

        cache[styleId] = result;
        return result;
    }

    private static int FixTextsV8(Database db, List<(ObjectId Id, string Text, bool IsMText, ObjectId StyleId)> texts,
        Dictionary<ObjectId, bool> dgnStyleCache, Transaction tr, DgnLog log)
    {
        var candidates = texts
            .Where(t => IsDgnFallbackStyle(t.StyleId, dgnStyleCache, tr)
                        && (Tcvn3Converter.HasV8Artifacts(t.Text) || t.Text.Any(c => c > 0x7F)))
            .ToList();

        log.Info($"Font (DGN V8): {texts.Count} chuỗi, {candidates.Count} chuỗi ở style dgnNNN (font TCVN3 bị mất tên) ➔ " +
                 (candidates.Count > 0 ? "áp dụng giải mã N/64 + chuyển TCVN3." : "không có gì cần chuyển."));
        foreach (var sample in candidates.Take(6))
        {
            var decoded = Tcvn3Converter.ToUnicode(Tcvn3Converter.DecodeV8Artifacts(sample.Text), false);
            log.Info($"  mẫu: '{sample.Text}' ➔ '{decoded}'");
        }

        if (candidates.Count == 0)
            return 0;

        var styleId = EnsureArialStyle(db, tr);
        var converted = 0;
        foreach (var item in candidates)
        {
            try
            {
                var raw = Tcvn3Converter.DecodeV8Artifacts(item.Text);
                var upper = Tcvn3Converter.LooksAllCaps(raw);
                var result = Tcvn3Converter.ToUnicode(raw, upper);
                var ent = (Entity)tr.GetObject(item.Id, OpenMode.ForWrite);
                if (ent is DBText t)
                {
                    t.TextString = result;
                    t.TextStyleId = styleId;
                    if (t.Justify != AttachmentPoint.BaseLeft)
                    {
                        try
                        {
                            t.AdjustAlignment(db);
                        }
                        catch
                        {
                            // giữ nguyên điểm gióng
                        }
                    }
                }
                else if (ent is MText m)
                {
                    m.Contents = result;
                    m.TextStyleId = styleId;
                }

                converted++;
            }
            catch (System.Exception ex)
            {
                log.Info("Không đổi được một chuỗi: " + ex.Message);
            }
        }

        log.Info($"Đã chuyển TCVN3 ➔ Unicode (Arial) cho {converted} chuỗi (V8).");
        return converted;
    }

    private static string StyleFontName(ObjectId styleId, Dictionary<ObjectId, string> cache, Transaction tr)
    {
        if (cache.TryGetValue(styleId, out var cached))
            return cached;

        var name = "?";
        try
        {
            var rec = (TextStyleTableRecord)tr.GetObject(styleId, OpenMode.ForRead);
            var face = rec.Font.TypeFace;
            if (!string.IsNullOrWhiteSpace(face))
                name = face;
            else
            {
                var file = Path.GetFileNameWithoutExtension(rec.FileName);
                name = string.IsNullOrWhiteSpace(file) ? rec.Name : file;
            }
        }
        catch
        {
            // giữ "?"
        }

        cache[styleId] = name;
        return name;
    }

    private static bool IsUpperStyle(ObjectId styleId, Dictionary<ObjectId, bool> cache, Transaction tr)
    {
        if (cache.TryGetValue(styleId, out var cached))
            return cached;

        var upper = false;
        try
        {
            var rec = (TextStyleTableRecord)tr.GetObject(styleId, OpenMode.ForRead);
            upper = Tcvn3Converter.IsUpperFont(rec.Name)
                    || Tcvn3Converter.IsUpperFont(rec.Font.TypeFace)
                    || Tcvn3Converter.IsUpperFont(Path.GetFileNameWithoutExtension(rec.FileName));
        }
        catch
        {
            // mặc định chữ thường
        }

        cache[styleId] = upper;
        return upper;
    }

    private static ObjectId EnsureArialStyle(Database db, Transaction tr)
    {
        var table = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
        if (table.Has(ArialStyleName))
            return table[ArialStyleName];

        table.UpgradeOpen();
        var rec = new TextStyleTableRecord
        {
            Name = ArialStyleName,
            Font = new Autodesk.AutoCAD.GraphicsInterface.FontDescriptor("Arial", false, false, 0, 0)
        };
        var id = table.Add(rec);
        tr.AddNewlyCreatedDBObject(rec, true);
        return id;
    }

    // ---- Purge

    private static int PurgeNewBlocks(Database db, long seed)
    {
        var removed = 0;
        for (var round = 0; round < 5; round++)
        {
            using var tr = db.TransactionManager.StartTransaction();
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var candidates = new ObjectIdCollection();
            foreach (ObjectId id in bt)
            {
                if (id.Handle.Value < seed)
                    continue;

                var btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
                if (btr.IsLayout || btr.IsAnonymous || btr.IsFromExternalReference || btr.IsFromOverlayReference)
                    continue;

                candidates.Add(id);
            }

            if (candidates.Count == 0)
                break;

            db.Purge(candidates);
            if (candidates.Count == 0)
                break;

            foreach (ObjectId id in candidates)
                tr.GetObject(id, OpenMode.ForWrite).Erase();

            removed += candidates.Count;
            tr.Commit();
        }

        return removed;
    }

    private static int PurgeNewSymbols(Database db, long seed, ObjectId tableId)
    {
        using var tr = db.TransactionManager.StartTransaction();
        var table = (SymbolTable)tr.GetObject(tableId, OpenMode.ForRead);
        var candidates = new ObjectIdCollection();
        foreach (ObjectId id in table)
        {
            if (id.Handle.Value >= seed)
                candidates.Add(id);
        }

        if (candidates.Count == 0)
            return 0;

        db.Purge(candidates);
        foreach (ObjectId id in candidates)
            tr.GetObject(id, OpenMode.ForWrite).Erase();

        tr.Commit();
        return candidates.Count;
    }

    // ---- Thu thập đối tượng mới (theo Handseed)

    private static List<ObjectId> CollectNewModelSpace(Database db, long seed)
    {
        var ids = new List<ObjectId>();
        using var tr = db.TransactionManager.StartTransaction();
        var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
        foreach (ObjectId id in ms)
        {
            if (id.Handle.Value >= seed)
                ids.Add(id);
        }

        tr.Commit();
        return ids;
    }

    private static List<ObjectId> CollectNewBlockEntities(Database db, long seed, Transaction tr)
    {
        var ids = new List<ObjectId>();
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        foreach (ObjectId id in bt)
        {
            if (id.Handle.Value < seed)
                continue;

            var btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
            if (btr.IsLayout || btr.IsAnonymous || btr.IsFromExternalReference || btr.IsFromOverlayReference)
                continue;

            foreach (ObjectId entId in btr)
                ids.Add(entId);
        }

        return ids;
    }

    // ---- Extents, cảnh báo, zoom

    private static Extents3d? ComputeExtents(List<ObjectId> ids, Transaction tr)
    {
        Extents3d? result = null;
        foreach (var id in ids)
        {
            try
            {
                if (tr.GetObject(id, OpenMode.ForRead) is not Entity ent || ent.IsErased)
                    continue;

                var b = ent.Bounds;
                if (b is not { } e)
                    continue;

                if (result is null)
                    result = e;
                else
                {
                    var u = result.Value;
                    u.AddExtents(e);
                    result = u;
                }
            }
            catch
            {
                // entity không có extents
            }
        }

        return result;
    }

    /// <summary>Cảnh báo khi vùng nhập không giống toạ độ VN-2000 (lệch gốc/đơn vị Master–Sub).</summary>
    private static void CheckExtents(DgnImportReport report)
    {
        if (report.Extents is not { } e)
        {
            report.Warnings.Add("Không tính được vùng bao (extents) của đối tượng vừa nhập.");
            return;
        }

        var inVn2000 = Within(e.MinPoint.X, e.MaxPoint.X) && Within(e.MinPoint.Y, e.MaxPoint.Y);
        if (!inVn2000)
        {
            report.Warnings.Add(
                $"Vùng nhập X {e.MinPoint.X:0.##}..{e.MaxPoint.X:0.##}, Y {e.MinPoint.Y:0.##}..{e.MaxPoint.Y:0.##} " +
                "không nằm trong dải toạ độ VN-2000 thường gặp (1e5–3e6). Hãy thử đổi đơn vị Master/Sub hoặc kiểm tra gốc toàn cục của DGN.");
        }

        static bool Within(double min, double max) => min >= 1e5 && max <= 3e6;
    }

    /// <summary>Zoom toàn bộ bản vẽ (Extents) để thấy trọn bản đồ vừa nhập.</summary>
    private static void ZoomExtents(Editor ed, DgnLog log)
    {
        try
        {
            ed.Command("_.ZOOM", "_E");
        }
        catch (System.Exception ex)
        {
            log.Info("Không zoom được: " + ex.Message);
        }
    }
    // ---- Biến hệ thống

    internal sealed class SysVarScope(DgnLog log) : IDisposable
    {
        private readonly List<(string Name, object Value)> _saved = [];

        public void Set(string name, object value)
        {
            try
            {
                var old = AcadApp.GetSystemVariable(name);
                _saved.Add((name, old));
                AcadApp.SetSystemVariable(name, value);
                log.Info($"Biến {name}: {old} ➔ {value} (sẽ khôi phục).");
            }
            catch (System.Exception ex)
            {
                log.Info($"Không đặt được biến {name}: {ex.Message}");
            }
        }

        public void Dispose()
        {
            for (var i = _saved.Count - 1; i >= 0; i--)
            {
                try
                {
                    AcadApp.SetSystemVariable(_saved[i].Name, _saved[i].Value);
                }
                catch
                {
                    // bỏ qua
                }
            }

            _saved.Clear();
        }
    }
}
