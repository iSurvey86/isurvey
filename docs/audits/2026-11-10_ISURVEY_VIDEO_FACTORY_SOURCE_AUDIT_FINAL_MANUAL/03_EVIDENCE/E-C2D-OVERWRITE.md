# E-C2D-OVERWRITE — C2D file overwrite + Start path

- **file:** `src/Dgn/DgnExportService.cs`
- **lines:** 106-148
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** File.Exists/Delete trước xuất

```csharp
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
            Orig = orig, Options = options, Log = log, Report = report, Seed = seed, Done = done, Progress = progress
        };

        // Xuất trực tiếp bản vẽ hiện hành chỉ khi: toàn bộ + không dọn rác. Còn lại dùng bản sao tạm.
        var direct = options.Scope == DgnExportScope.All && !options.Purge;
        IsRunning = true;
        progress?.Report(0, 0, "Đang chuẩn bị xuất — " + Path.GetFileName(options.OutputPath));

        if (direct)
        {
            log.Info("Xuất trực tiếp bản vẽ hiện hành (không dọn rác).");
            report.Entities = CountModelSpace(orig.Database);
            RunExport(job, orig);
            return;
```
