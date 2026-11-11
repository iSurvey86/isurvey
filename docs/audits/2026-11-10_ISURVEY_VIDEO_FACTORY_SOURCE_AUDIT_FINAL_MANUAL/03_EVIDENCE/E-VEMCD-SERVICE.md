# E-VEMCD-SERVICE — EraseHandles + Draw strip loop

- **file:** `src/Modules/Profile/ProfileDrawService.cs`
- **lines:** 142-231
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** Xóa handle; sample; entity ModelSpace; Commit

```csharp
    public static void EraseHandles(Document doc, IReadOnlyList<string> handles)
    {
        if (handles is null || handles.Count == 0)
            return;
        using (doc.LockDocument())
        using (var tr = doc.TransactionManager.StartTransaction())
        {
            foreach (var text in handles)
            {
                if (string.IsNullOrWhiteSpace(text))
                    continue;
                try
                {
                    var handle = new Handle(Convert.ToInt64(text, 16));
                    if (!doc.Database.TryGetObjectId(handle, out var id) || id.IsNull)
                        continue;
                    if (tr.GetObject(id, OpenMode.ForWrite, true) is not Entity ent || ent.IsErased)
                        continue;
                    ent.Erase(true);
                }
                catch
                {
                    // bỏ handle hỏng
                }
            }

            tr.Commit();
        }
    }

    public static ProfileRecord Draw(
        Document doc,
        RouteRecord route,
        ProfileDrawOptions options,
        Point3d insert,
        string? existingId = null,
        IReadOnlyList<string>? eraseHandles = null)
    {
        if (eraseHandles is { Count: > 0 })
            EraseHandles(doc, eraseHandles);

        NormalizeHardCodedOptions(options);

        var terrain = ResolveTerrain(doc, route);
        var samples = ProfileSampleService.Collect(doc, route, options.SampleStepM, terrain);
        if (samples.Count < 2)
            throw new InvalidOperationException("Cần ít nhất hai điểm có cao độ Z trên tuyến.");

        var rows = options.Rows is { Count: > 0 } ? options.Rows : ProfileBandRow.CreateDefaultList();
        var bandH = ProfileBandRow.TotalHeight(rows);
        var segs = PlanSegments(samples, options.Datum, options.HorizontalScale, options.VerticalScale, bandH);
        if (segs.Count == 0)
            throw new InvalidOperationException("Không lập được đoạn mặt cắt.");

        var handles = new List<string>();
        var origin = insert;
        using (doc.LockDocument())
        using (var tr = doc.TransactionManager.StartTransaction())
        {
            var db = doc.Database;
            EnsureLayer(tr, db, iSurveyLayers.TdBang, 7);
            EnsureLayer(tr, db, iSurveyLayers.TdChu, 7);
            EnsureLayer(tr, db, iSurveyLayers.TdDat, 3);
            EnsureLayer(tr, db, iSurveyLayers.TdLuoi, 8);

            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var space = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
            var styleId = ProfileTextStyles.Ensure(tr, db);

            for (var i = 0; i < segs.Count; i++)
            {
                var seg = segs[i];
                var slice = samples.Where(s => s.Chainage >= seg.From - 1e-6 && s.Chainage <= seg.To + 1e-6).ToList();
                if (slice.Count < 2)
                    continue;
                DrawOneStrip(
                    tr, space, styleId,
                    slice, route,
                    options, rows, seg.Datum,
                    origin, handles);

                var phong = Exaggerate(options.HorizontalScale, options.VerticalScale);
                var zTop = slice.Max(s => s.Z);
                // Khoảng đứng = 0: band + đất (không cộng Cách đầu bảng — đó là khoảng ngang).
                var groundTop = bandH + Math.Max(0, (zTop - seg.Datum) * phong);
                origin = new Point3d(insert.X, origin.Y + groundTop + SegmentGap + options.RowGap, 0);
            }

            tr.Commit();
        }
```
