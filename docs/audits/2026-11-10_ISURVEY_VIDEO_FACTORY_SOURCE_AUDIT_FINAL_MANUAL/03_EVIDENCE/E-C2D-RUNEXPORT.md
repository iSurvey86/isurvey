# E-C2D-RUNEXPORT — C2D -DGNEXPORT + poll File.Exists

- **file:** `src/Dgn/DgnExportService.cs`
- **lines:** 346-435
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** Command -DGNEXPORT; OnIdle File.Exists

```csharp
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
            job.Progress?.Report(0, 0, "Đang gọi -DGNEXPORT…");
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
        job.Progress?.Report(0, 0, "Đang chờ Civil 3D xuất file DGN…");
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
            if (job.Progress?.Cancelled == true)
            {
                try
                {
                    doc.SendStringToExecute("\u001b\u001b", true, false, false);
                }
                catch
                {
                    // bỏ qua
                }

                Complete(job, "đã hủy");
                return;
            }

            var path = job.Options.OutputPath;
            var exists = File.Exists(path);
            var size = exists ? new FileInfo(path).Length : -1;
            if (size != job.LastSize)
            {
                job.LastSize = size;
                job.LastChange = now;
            }

            if ((now - job.LastProgressUi).TotalSeconds >= 0.5)
            {
                job.LastProgressUi = now;
                var elapsed = (now - job.PollStarted).TotalSeconds;
                job.Progress?.Report(0, 0,
                    exists && size > 0
                        ? $"Đang ghi DGN… {size / 1024.0:0.#} KB ({elapsed:0}s)"
                        : $"Đang chờ xuất DGN… ({elapsed:0}s)");
            }

            var active = (Convert.ToInt32(AcadApp.GetSystemVariable("CMDACTIVE")) & 1) != 0;
            var quiet = (now - job.LastChange).TotalSeconds;
            var timedOut = (now - job.PollStarted).TotalSeconds > TimeoutSeconds;

            if (exists && size > 0 && !active && quiet >= QuietSecondsAfterFile)
```
