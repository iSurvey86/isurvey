# Evidence `USER-BF-ISURVEY_NUMBER_VERTICES`

- **file:** `src/Modules/Tuyen/VertexNumberingService.cs`
- **lines:** 38-238
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler NumberVerticesCommand

```csharp
{
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        var db = doc.Database;
        var ed = doc.Editor;

        var peo = new PromptEntityOptions("\niSurvey: Chọn Polyline tim tuyến: ")
        {
            AllowNone = false
        };
        peo.SetRejectMessage("\niSurvey: Chỉ chấp nhận Polyline.");
        peo.AddAllowedClass(typeof(Polyline), true);
        peo.AddAllowedClass(typeof(Polyline2d), true);
        peo.AddAllowedClass(typeof(Polyline3d), true);

        var per = ed.GetEntity(peo);
        if (per.Status != PromptStatus.OK)
            return;

        var pso = new PromptStringOptions("\niSurvey: Nhập tiền tố (Prefix) <G>: ")
        {
            AllowSpaces = false,
            UseDefaultValue = true,
            DefaultValue = "G"
        };
        var prStr = ed.GetString(pso);
        if (prStr.Status != PromptStatus.OK)
            return;
        var prefix = string.IsNullOrEmpty(prStr.StringResult) ? "G" : prStr.StringResult;

        var pio = new PromptIntegerOptions("\niSurvey: Nhập chỉ số bắt đầu <0>: ")
        {
            AllowNone = true,
            DefaultValue = 0,
            UseDefaultValue = true
        };
        var pir = ed.GetInteger(pio);
        if (pir.Status == PromptStatus.Cancel)
            return;
        var startIndex = pir.Status == PromptStatus.OK ? pir.Value : 0;

        // Tỷ lệ in bản đồ địa hình — mặc định 1:1000.
        var pScale = new PromptIntegerOptions("\niSurvey: Tỷ lệ bản đồ 1:<1000>: ")
        {
            AllowNone = true,
            AllowNegative = false,
            AllowZero = false,
            LowerLimit = 100,
            UpperLimit = 100000,
            DefaultValue = 1000,
            UseDefaultValue = true
        };
        var prScale = ed.GetInteger(pScale);
        if (prScale.Status == PromptStatus.Cancel)
            return;
        var mapScale = prScale.Status == PromptStatus.OK ? prScale.Value : 1000;

        // mm giấy → mét model: H_m = H_mm * M / 1000
        var textHeight = PaperMmToModel(TextHeightMm, mapScale);
        var dotRadius = PaperMmToModel(DotDiameterMm * 0.5, mapScale);
        var baseOffset = dotRadius + PaperMmToModel(TextGapMm, mapScale);
        var clearOfTim = PaperMmToModel(ClearOfTimMm, mapScale);
        var pushStep = Math.Max(textHeight * PushStepFactor, clearOfTim * 0.5);
        // Không để chữ cách đỉnh quá xa (kể cả khi đẩy né tim).
        var maxOffset = baseOffset + textHeight * 1.8;

        ed.WriteMessage(
            $"\n[iSurvey] Tỷ lệ 1:{mapScale} — chữ {TextHeightMm:0.#} mm ({textHeight:0.###} m), " +
            $"chấm Ø {DotDiameterMm:0.#} mm ({dotRadius * 2:0.###} m).");

        using var tr = db.TransactionManager.StartTransaction();
        var layerId = EnsureRedLayer(tr, db);

        var ent = (Entity)tr.GetObject(per.ObjectId, OpenMode.ForRead);
        var vertices = GetVertices(ent, tr, out var closed);
        NormalizeVertices(vertices, ref closed);
        if (vertices.Count < (closed ? 3 : 2))
        {
            ed.WriteMessage("\n[iSurvey] Polyline cần ít nhất 2 đỉnh khác nhau (3 nếu khép kín).");
            return;
        }

        var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
        var plane = new Plane(Point3d.Origin, Vector3d.ZAxis);
        var pushed = 0;

        for (var i = 0; i < vertices.Count; i++)
        {
            var v_i = vertices[i];
            var vDir = ComputeOutsideBisector(vertices, i, closed);
            vDir = new Vector3d(vDir.X, vDir.Y, 0);
            if (vDir.Length < 1e-12)
                vDir = Vector3d.XAxis;
            else
                vDir = vDir.GetNormal();

            // Chấm tròn đặc
            var dot = new Circle
            {
                Center = v_i,
                Radius = dotRadius,
                LayerId = layerId,
      
/* … truncated for pack size; full method in repo */

```
