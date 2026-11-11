# Evidence `USER-BF-ISURVEY_MAP_SAT-HdMosaicWorkflow-Run`

- **file:** `src/Modules/Map/HdMosaicWorkflow.cs`
- **lines:** 30-197
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** called from BF-ISURVEY_MAP_SAT

```csharp
{
        var ed = doc.Editor;
        var drawing = ActivityLogger.FileName(doc.Database.Filename);
        void Log(string note) => ActivityLogger.Result("ISURVEY_MAP_SAT", $"{note}, bản vẽ {drawing}");
        var db = doc.Database;

        BoundarySelection? boundary;
        try
        {
            ed.WriteMessage("\n[iSurvey] Chon duong bao anh HD (Polyline dong / Region / Circle)...");
            boundary = RegionSelectionHelper.TrySelectBoundary(ed, db);
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] {ex.Message}");
            Log("ghép HD, lỗi đường bao: " + ex.Message);
            return;
        }

        if (boundary is null)
        {
            ed.WriteMessage("\n[iSurvey] Da huy — chua ghep anh.");
            Log("ghép HD, đã hủy, chưa chọn đường bao");
            return;
        }

        var drawingPath = string.IsNullOrWhiteSpace(db.Filename) ? null : db.Filename;
        var saved = UserSettingsStore.Load(drawingPath);
        var zone = CoordinateService.NormalizeZoneWidth(saved.ZoneWidthDegrees);
        var cm = zone == 6
            ? CoordinateService.SnapToTm6CentralMeridian(saved.CentralMeridian)
            : saved.CentralMeridian;

        var coordinates = new CoordinateService();
        GeoBounds wgs;
        try
        {
            wgs = coordinates.PolygonToWgs84Bounds(boundary.ClipPolygonWcs, cm, zone);
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] Khong tinh duoc pham vi: {ex.Message}");
            Log("ghép HD, không tính được phạm vi: " + ex.Message);
            return;
        }

        var choices = new[]
        {
            Describe(wgs, 18),
            Describe(wgs, 19),
            Describe(wgs, 20)
        };

        foreach (var choice in choices)
            ed.WriteMessage("\n[iSurvey] " + choice.Line);

        if (!choices.Any(c => c.Allowed))
        {
            ed.WriteMessage("\n[iSurvey] Ca 3 muc zoom deu vuot gioi han an toan. Ve duong bao nho hon roi chay lai.");
            Log("ghép HD, cả 3 mức zoom vượt giới hạn");
            return;
        }

        var zoom = PromptZoom(ed, choices);
        if (zoom is null)
        {
            ed.WriteMessage("\n[iSurvey] Da huy — chua ghep anh.");
            Log("ghép HD, đã hủy, chưa chọn zoom");
            return;
        }

        var picked = choices.First(c => c.Zoom == zoom.Value);
        var tiles = TileMath.EnumerateTiles(wgs, picked.Zoom).ToList();
        ed.WriteMessage(
            $"\n[iSurvey] Kinh tuyen {cm.ToString("0.##", CultureInfo.InvariantCulture)}° — TM-{zone} — {SourceId} scale={Scale}");
        ed.WriteMessage(
            $"\n[iSurvey] HD zoom {picked.Zoom}, {tiles.Count} o, moi o {TilePixels}px. Dang tai...");

        IReadOnlyList<(int X, int Y, int Z, string Path)> ready;
        var ui = TaskProgressWindow.ShowNew(
            "iSurvey — Ghép ảnh HD",
            "Đang tải tile bản đồ",
            $"0 / {tiles.Count} ô…");
        try
        {
            using var downloader = new TileDownloader(SourceId);
            var progress = new Progress<(int Done, int Total)>(p =>
            {
                if (ui.Cancelled)
                    throw new OperationCanceledException();
                if (p.Done % 8 == 0 || p.Done == p.Total)
                {
                    ui.Report(p.Done, p.Total, $"Đã tải {p.Done} / {p.Total} ô…");
                    ed.WriteMessage($"\n[iSurvey] ... {p.Done}/{p.Total}");
                }
            });
            ready = downloader.EnsureTilesAsync(tiles, picked.Zoom, Scale, progress).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            ui.ForceClose();
            ed.WriteMessage("\n[iSurvey] Da huy tai tile.");
            Log("ghép HD, đã hủy");
            return;
        }
        catch (System.Exception ex)
        {
            ui.ForceClose();
            ed.WriteMessage
/* … truncated for pack size; full method in repo */

```
