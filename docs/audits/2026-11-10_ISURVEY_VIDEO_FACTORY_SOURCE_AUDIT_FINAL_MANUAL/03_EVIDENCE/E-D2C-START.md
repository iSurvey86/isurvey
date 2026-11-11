# E-D2C-START — D2C Start → DGNIMPORT async

- **file:** `src/Dgn/DgnImportService.cs`
- **lines:** 108-165
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** Gửi -DGNIMPORT rồi trả về; hậu xử lý Idle

```csharp
    /// Bắt đầu nhập: gửi -DGNIMPORT rồi TRẢ VỀ NGAY. Civil 3D hoàn tất việc nhập sau khi lệnh kết thúc
    /// (log thực tế: CMDACTIVE=1 và 0 đối tượng ngay sau Editor.Command), nên hậu xử lý được hoãn tới
    /// khi Handseed ngừng thay đổi và không còn lệnh đang chạy (xem <see cref="OnIdle"/>).
    /// </summary>
    public static void Start(
        Document doc, DgnImportOptions options, Action<DgnImportReport> done, TaskProgressWindow? progress = null)
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
            LastSeed = seed,
            Progress = progress
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

            ReportProgress(job, 0, 0, "Đang gửi lệnh DGNIMPORT — " + Path.GetFileName(options.FilePath));
            RunImportCommand(ed, options, log);
        }
```
