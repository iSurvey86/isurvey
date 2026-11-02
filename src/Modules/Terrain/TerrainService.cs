using System.Globalization;
using Autodesk.AutoCAD.DatabaseServices;
using iSurvey.UI.Terrain;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AcadColor = Autodesk.AutoCAD.Colors.Color;
using ColorMethod = Autodesk.AutoCAD.Colors.ColorMethod;

namespace iSurvey.Modules.Terrain;

/// <summary>Thêm thành phần, dựng lưới và tra cao độ.</summary>
public static class TerrainService
{
    public const string AppName = "ISURVEY_DTM";

    public static TerrainRecord? Choose(Editor editor, Database db)
    {
        var models = TerrainStore.Load(db);
        if (models.Count == 0)
        {
            editor.WriteMessage("\n[iSurvey] Chưa có mô hình. Hãy tạo mô hình trước.");
            return null;
        }

        var pending = TerrainContext.TakeModel();
        if (!string.IsNullOrWhiteSpace(pending))
        {
            foreach (var model in models)
            {
                if (string.Equals(model.Id, pending, StringComparison.OrdinalIgnoreCase))
                    return model;
            }
        }

        if (models.Count == 1)
            return models[0];

        editor.WriteMessage("\n[iSurvey] Các mô hình trong bản vẽ:");
        for (var i = 0; i < models.Count; i++)
            editor.WriteMessage("\n  " + (i + 1) + ". " + models[i].Name);
        var options = new PromptIntegerOptions("\nSố thứ tự mô hình")
        {
            LowerLimit = 1,
            UpperLimit = models.Count
        };
        var picked = editor.GetInteger(options);
        if (picked.Status != PromptStatus.OK)
            return null;
        return models[picked.Value - 1];
    }

    public static int AddPoints(
        Autodesk.AutoCAD.ApplicationServices.Document doc,
        TerrainRecord model,
        bool points,
        bool blocks,
        bool blockElevFromLabels,
        bool labels,
        bool chains)
    {
        var names = new List<string>();
        if (points || blockElevFromLabels)
            names.Add("POINT");
        if (blocks || blockElevFromLabels)
            names.Add("INSERT");
        if (labels)
        {
            names.Add("TEXT");
            names.Add("MTEXT");
        }

        // Cho phép quét cả chữ khi chỉ bật «Điểm/Block + chữ cạnh» — chữ không được lưu thành đỉnh.
        if (blockElevFromLabels && !labels)
        {
            names.Add("TEXT");
            names.Add("MTEXT");
        }

        if (chains)
        {
            names.Add("LINE");
            names.Add("LWPOLYLINE");
            names.Add("POLYLINE");
        }

        if (names.Count == 0)
            return 0;
        model.Points ??= new List<string>();
        if (blockElevFromLabels)
            model.BlockElevFromLabels = true;
        var editor = doc.Editor;
        var selection = editor.GetSelection(
            new PromptSelectionOptions { MessageForAdding = "\nChọn đối tượng đưa vào mô hình: " },
            new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, string.Join(",", names)) }));
        if (selection.Status != PromptStatus.OK || selection.Value is null)
            return 0;

        var added = 0;
        var skippedLabels = 0;
        var blockElevHints = 0;
        using (doc.LockDocument())
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            List<ElevLabel>? elevPool = null;
            if (blockElevFromLabels)
                elevPool = LoadElevLabels(tr, doc.Database);

            foreach (SelectedObject item in selection.Value)
            {
                if (item is null)
                    continue;
                if (tr.GetObject(item.ObjectId, OpenMode.ForRead) is not Entity entity || entity.IsErased)
                    continue;

                // Chế độ block+chữ: chỉ lưu block; chữ chỉ để gợi ý Z lúc dựng.
                if (blockElevFromLabels && !labels && entity is DBText or MText)
                    continue;

                if (entity is DBText or MText)
                {
                    if (!TryLabelVertex(entity, out _))
                    {
                        skippedLabels++;
                        continue;
                    }
                }

                if (blockElevFromLabels && elevPool is not null)
                {
                    double hx = 0, hy = 0;
                    var pairable = false;
                    if (entity is BlockReference block)
                    {
                        hx = block.Position.X;
                        hy = block.Position.Y;
                        pairable = true;
                    }
                    else if (entity is DBPoint dbPoint)
                    {
                        hx = dbPoint.Position.X;
                        hy = dbPoint.Position.Y;
                        pairable = true;
                    }

                    if (pairable && TryTakeNearbyElev(elevPool, hx, hy, out _))
                        blockElevHints++;
                }

                var handle = entity.Handle.ToString();
                if (model.Points.Exists(itemHandle => string.Equals(itemHandle, handle, StringComparison.OrdinalIgnoreCase)))
                    continue;
                model.Points.Add(handle);
                added++;
            }

            tr.Commit();
        }

        if (skippedLabels > 0)
            editor.WriteMessage("\n[iSurvey] Bỏ qua " + skippedLabels + " chữ không đọc được thành số.");
        if (blockElevFromLabels && added > 0)
        {
            editor.WriteMessage(
                "\n[iSurvey] Điểm/Block + chữ cạnh: đã gắn tạm Z cho " + blockElevHints
                + " đối tượng (ghép lại khi xây dựng lưới; không sửa CAD).");
        }

        if (added > 0)
            TerrainStore.Save(doc.Database, model);
        return added;
    }

    public static int Add(Autodesk.AutoCAD.ApplicationServices.Document doc, TerrainRecord model, string kind)
    {
        var editor = doc.Editor;
        model.Breaklines ??= new List<string>();
        var dxf = "LWPOLYLINE,POLYLINE";
        var message = kind switch
        {
            "contour" => "\nChọn đường đồng mức: ",
            "break" => "\nChọn đường đứt gãy: ",
            "boundary" => "\nChọn đường bao kín: ",
            _ => "\nChọn đường: "
        };
        var selection = editor.GetSelection(
            new PromptSelectionOptions { MessageForAdding = message },
            new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, dxf) }));
        if (selection.Status != PromptStatus.OK || selection.Value is null)
            return 0;

        var added = 0;
        using (doc.LockDocument())
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            foreach (SelectedObject item in selection.Value)
            {
                if (item is null)
                    continue;
                if (tr.GetObject(item.ObjectId, OpenMode.ForRead) is not Entity entity || entity.IsErased)
                    continue;
                var handle = entity.Handle.ToString();
                if (kind == "boundary")
                {
                    if (!IsClosed(tr, entity))
                    {
                        editor.WriteMessage("\n[iSurvey] Bỏ qua đường chưa kín.");
                        continue;
                    }

                    model.RememberBoundary(handle);
                    added++;
                }
                else
                {
                    var list = kind switch
                    {
                        "contour" => model.Contours,
                        "break" => model.Breaklines,
                        _ => model.Points
                    };
                    if (list.Exists(itemHandle => string.Equals(itemHandle, handle, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    list.Add(handle);
                    added++;
                }
            }

            tr.Commit();
        }

        if (added > 0)
            TerrainStore.Save(doc.Database, model);
        return added;
    }

    /// <summary>
    /// Gỡ toàn bộ một loại khỏi mô hình. Không xóa đối tượng CAD.
    /// Có lưới thì xóa lưới vì dữ liệu đã lệch.
    /// </summary>
    public static int ClearKind(Autodesk.AutoCAD.ApplicationServices.Document doc, TerrainRecord model, string kind)
    {
        var removed = CountKind(model, kind);
        if (removed == 0)
            return 0;
        ClearList(model, kind);
        DropMesh(doc, model);
        return removed;
    }

    /// <summary>
    /// Chọn trên bản vẽ rồi gỡ handle khỏi mô hình. Không xóa đối tượng CAD.
    /// </summary>
    public static int Detach(Autodesk.AutoCAD.ApplicationServices.Document doc, TerrainRecord model, string kind)
    {
        var editor = doc.Editor;
        var filter = kind switch
        {
            "point" => "POINT,INSERT,TEXT,MTEXT,LINE,LWPOLYLINE,POLYLINE",
            _ => "LWPOLYLINE,POLYLINE"
        };
        var message = kind switch
        {
            "contour" => "\nChọn đường đồng mức cần gỡ khỏi mô hình: ",
            "break" => "\nChọn đường đứt gãy cần gỡ khỏi mô hình: ",
            "boundary" => "\nChọn đường bao cần gỡ khỏi mô hình: ",
            _ => "\nChọn đối tượng điểm cần gỡ khỏi mô hình: "
        };
        var selection = editor.GetSelection(
            new PromptSelectionOptions { MessageForAdding = message },
            new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, filter) }));
        if (selection.Status != PromptStatus.OK || selection.Value is null)
            return 0;

        var removed = 0;
        using (doc.LockDocument())
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            foreach (SelectedObject item in selection.Value)
            {
                if (item is null)
                    continue;
                if (tr.GetObject(item.ObjectId, OpenMode.ForRead) is not Entity entity || entity.IsErased)
                    continue;
                if (DropHandle(model, kind, entity.Handle.ToString()))
                    removed++;
            }

            tr.Commit();
        }

        if (removed > 0)
            DropMesh(doc, model);
        return removed;
    }

    /// <summary>Xóa lưới Face và Mesh đã lưu. Danh sách điểm/đứt gãy giữ nguyên.</summary>
    public static void DropMesh(Autodesk.AutoCAD.ApplicationServices.Document doc, TerrainRecord model) =>
        InvalidateMesh(doc, model);

    public static int CountKind(TerrainRecord model, string kind) =>
        kind switch
        {
            "contour" => model.Contours?.Count ?? 0,
            "break" => model.Breaklines?.Count ?? 0,
            "boundary" => model.BoundaryCount,
            _ => model.Points?.Count ?? 0
        };

    public static string KindLabel(string kind, bool plural) =>
        kind switch
        {
            "contour" => plural ? "đường đồng mức" : "đường đồng mức",
            "break" => plural ? "đường đứt gãy" : "đường đứt gãy",
            "boundary" => plural ? "đường bao" : "đường bao",
            _ => plural ? "điểm" : "điểm"
        };

    public static int Build(Autodesk.AutoCAD.ApplicationServices.Document doc, TerrainRecord model)
    {
        var points = new List<TinVertex>();
        var constraints = new List<TinSegment>();
        var boundaries = new List<List<TinVertex>>();
        var missing = 0;
        var pruned = false;
        model.Breaklines ??= new List<string>();
        model.Contours ??= new List<string>();
        model.Points ??= new List<string>();
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            missing += PruneList(tr, doc.Database, model.Points, out var pointsChanged);
            missing += PruneList(tr, doc.Database, model.Breaklines, out var breakChanged);
            missing += PruneList(tr, doc.Database, model.Contours, out var contourChanged);
            var boundaryHandles = model.BoundaryHandles();
            missing += PruneList(tr, doc.Database, boundaryHandles, out var boundaryChanged);
            if (boundaryChanged)
            {
                model.Boundaries = boundaryHandles;
                model.Boundary = null;
            }

            pruned = pointsChanged || breakChanged || contourChanged || boundaryChanged;
            List<ElevLabel>? elevPool = null;
            var unmatchedBlockElev = 0;
            if (model.BlockElevFromLabels)
                elevPool = LoadElevLabels(tr, doc.Database);
            foreach (var handle in model.Points)
                Collect(tr, doc.Database, handle, points, null, elevPool, ref unmatchedBlockElev);
            foreach (var handle in model.Breaklines)
                Collect(tr, doc.Database, handle, points, constraints, null, ref unmatchedBlockElev);
            foreach (var handle in model.Contours)
                Collect(tr, doc.Database, handle, points, constraints, null, ref unmatchedBlockElev);
            if (model.BlockElevFromLabels && unmatchedBlockElev > 0)
            {
                doc.Editor.WriteMessage(
                    "\n[iSurvey] " + unmatchedBlockElev
                    + " điểm/block không tìm thấy chữ cao độ gần; dùng Z sẵn có.");
            }
            var boundaryHandlesNow = model.BoundaryHandles();
            foreach (var boundaryHandle in boundaryHandlesNow)
            {
                var ring = new List<TinVertex>();
                var unused = 0;
                var lost = Collect(tr, doc.Database, boundaryHandle, ring, null, null, ref unused);
                if (lost == 0 && ring.Count >= 3)
                    boundaries.Add(ring);
            }

            tr.Commit();
            if (boundaryHandlesNow.Count > 0 && boundaries.Count == 0)
            {
                doc.Editor.WriteMessage(
                    "\n[iSurvey] Có " + boundaryHandlesNow.Count
                    + " đường bao trong mô hình nhưng không đọc được đa giác kín. Lưới sẽ là bao lồi.");
            }
        }

        if (pruned)
            TerrainStore.Save(doc.Database, model);
        if (missing > 0)
            doc.Editor.WriteMessage("\n[iSurvey] Đã bỏ " + missing + " đối tượng không còn trong bản vẽ khỏi mô hình.");
        if (points.Count < 3)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Cần ít nhất 3 điểm để xây dựng lưới.");
            return 0;
        }

        if (boundaries.Count > 0)
            doc.Editor.WriteMessage("\n[iSurvey] Chỉ dựng điểm nằm trong " + boundaries.Count + " đường bao.");

        var window = BuildProgressWindow.ShowNew();
        try
        {
            return BuildMesh(doc, model, points, constraints, boundaries, window);
        }
        finally
        {
            window.ForceClose();
        }
    }

    private static int BuildMesh(
        Autodesk.AutoCAD.ApplicationServices.Document doc,
        TerrainRecord model,
        List<TinVertex> points,
        List<TinSegment> constraints,
        List<List<TinVertex>> boundaries,
        BuildProgressWindow window)
    {
        List<TinTriangle> triangles;
        int unforced;
        try
        {
            window.Report(0, points.Count, "Đang dựng " + points.Count.ToString("N0") + " điểm...");
            triangles = TinBuilder.Build(
                points,
                constraints,
                boundaries.Count == 0 ? null : boundaries,
                out unforced,
                (done, total) =>
                {
                    window.Report(done, total, "Đang dựng tam giác " + done.ToString("N0") + " / " + total.ToString("N0"));
                    if (window.Cancelled)
                        throw new OperationCanceledException();
                });
            if (window.Cancelled)
                throw new OperationCanceledException();
        }
        catch (OperationCanceledException)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy xây dựng lưới.");
            return 0;
        }

        if (boundaries.Count == 0)
            doc.Editor.WriteMessage("\n[iSurvey] Chưa có đường bao. Lưới giữ bao lồi, kể cả tam giác dài giữa các cụm điểm.");
        if (unforced > 0)
            doc.Editor.WriteMessage("\n[iSurvey] " + unforced + " cạnh đứt gãy hoặc đồng mức chưa ép được vào lưới.");
        using (doc.LockDocument())
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            EnsureApp(tr, doc.Database);
            EnsureLayer(tr, doc.Database, model.Layer);
            EraseFaces(tr, doc, model.Id);
            var space = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);
            var drawn = 0;
            foreach (var tri in triangles)
            {
                drawn++;
                if ((drawn & 127) == 0)
                {
                    window.Report(drawn, triangles.Count, "Đang vẽ tam giác " + drawn.ToString("N0") + " / " + triangles.Count.ToString("N0"));
                    if (window.Cancelled)
                    {
                        doc.Editor.WriteMessage("\n[iSurvey] Đã hủy xây dựng lưới.");
                        return 0;
                    }
                }

                var face = new Face(
                    new Point3d(tri.A.X, tri.A.Y, tri.A.Z),
                    new Point3d(tri.B.X, tri.B.Y, tri.B.Z),
                    new Point3d(tri.C.X, tri.C.Y, tri.C.Z),
                    new Point3d(tri.C.X, tri.C.Y, tri.C.Z),
                    true, true, true, true);
                face.SetDatabaseDefaults();
                face.Layer = model.Layer;
                face.Color = AcadColor.FromColorIndex(ColorMethod.ByLayer, 256);
                face.Visible = model.MeshVisible;
                space.AppendEntity(face);
                tr.AddNewlyCreatedDBObject(face, true);
                face.XData = new ResultBuffer(
                    new TypedValue((int)DxfCode.ExtendedDataRegAppName, AppName),
                    new TypedValue((int)DxfCode.ExtendedDataAsciiString, Clip(model.Id)));
            }

            tr.Commit();
        }

        model.SetTriangles(triangles);
        TerrainStore.Save(doc.Database, model);
        doc.Editor.Regen();
        return triangles.Count;
    }

    public static bool Toggle(Autodesk.AutoCAD.ApplicationServices.Document doc, TerrainRecord model)
    {
        var show = !model.MeshVisible;
        var changed = 0;
        using (doc.LockDocument())
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            var space = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForRead);
            foreach (ObjectId id in space)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is not Face face || face.IsErased)
                    continue;
                if (!Owned(face, model.Id))
                    continue;
                face.UpgradeOpen();
                face.Visible = show;
                changed++;
            }

            tr.Commit();
        }

        if (changed == 0)
            return false;
        model.MeshVisible = show;
        TerrainStore.Save(doc.Database, model);
        doc.Editor.Regen();
        return true;
    }

    public static bool TryQuery(TerrainRecord model, double x, double y, out double z)
    {
        var triangles = model.Triangles();
        if (triangles.Count == 0)
        {
            z = 0;
            return false;
        }

        return TinBuilder.TryElevation(triangles, x, y, out z);
    }

    /// <summary>
    /// Tra Z: ưu tiên Face trên bản vẽ (đúng với lưới đang hiện), rồi mới tới Mesh đã lưu.
    /// Nếu Mesh trống mà Face có dữ liệu thì ghi lại Mesh.
    /// </summary>
    public static bool TryQuery(
        Autodesk.AutoCAD.ApplicationServices.Document doc,
        TerrainRecord model,
        double x,
        double y,
        out double z)
    {
        z = 0;
        if (doc is null)
            return TryQuery(model, x, y, out z);

        var faces = new List<TinTriangle>();
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            var space = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForRead);
            foreach (ObjectId id in space)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is not Face face || face.IsErased)
                    continue;
                if (!Owned(face, model.Id))
                    continue;
                var a = face.GetVertexAt(0);
                var b = face.GetVertexAt(1);
                var c = face.GetVertexAt(2);
                faces.Add(new TinTriangle(
                    new TinVertex(a.X, a.Y, a.Z),
                    new TinVertex(b.X, b.Y, b.Z),
                    new TinVertex(c.X, c.Y, c.Z)));
            }

            tr.Commit();
        }

        if (faces.Count > 0 && TinBuilder.TryElevation(faces, x, y, out z))
        {
            if (model.TriangleCount == 0)
            {
                model.SetTriangles(faces);
                TerrainStore.Save(doc.Database, model);
            }

            return true;
        }

        return TryQuery(model, x, y, out z);
    }

    /// <summary>Chữ tạm trên bản vẽ khi tra cao độ. Xóa nhãn cũ nếu có.</summary>
    public static ObjectId ShowElevationMark(
        Autodesk.AutoCAD.ApplicationServices.Document doc,
        TerrainRecord model,
        double x,
        double y,
        double z,
        ObjectId previous)
    {
        EraseElevationMark(doc, previous);
        using (doc.LockDocument())
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            EnsureLayer(tr, doc.Database, model.Layer);
            var space = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);
            var label = new DBText();
            label.SetDatabaseDefaults();
            label.Layer = model.Layer;
            label.Position = new Point3d(x, y, z);
            label.Height = Math.Max(EstimateTextHeight(doc), 0.5);
            label.TextString = "Z=" + z.ToString("0.000", CultureInfo.CurrentCulture);
            label.Color = AcadColor.FromColorIndex(ColorMethod.ByAci, 1);
            space.AppendEntity(label);
            tr.AddNewlyCreatedDBObject(label, true);
            tr.Commit();
            doc.Editor.Regen();
            return label.ObjectId;
        }
    }

    public static void EraseElevationMark(Autodesk.AutoCAD.ApplicationServices.Document doc, ObjectId id)
    {
        if (id.IsNull || !id.IsValid || id.IsErased)
            return;
        try
        {
            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                if (tr.GetObject(id, OpenMode.ForWrite, true) is Entity entity && !entity.IsErased)
                    entity.Erase();
                tr.Commit();
            }
        }
        catch (Autodesk.AutoCAD.Runtime.Exception)
        {
        }
    }

    private static double EstimateTextHeight(Autodesk.AutoCAD.ApplicationServices.Document doc)
    {
        try
        {
            var view = doc.Editor.GetCurrentView();
            return Math.Max(view.Height / 40.0, 0.2);
        }
        catch (System.Exception)
        {
            return 1.0;
        }
    }

    private sealed class ElevLabel
    {
        public double X;
        public double Y;
        public double Z;
        public double Height;
        public bool Used;
    }

    private static List<ElevLabel> LoadElevLabels(Transaction tr, Database db)
    {
        var list = new List<ElevLabel>();
        var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead);
        foreach (ObjectId id in space)
        {
            if (tr.GetObject(id, OpenMode.ForRead) is not Entity entity || entity.IsErased)
                continue;
            if (!TryLabelVertex(entity, out var vertex))
                continue;
            var height = 1.0;
            if (entity is DBText text)
                height = text.Height > 1e-9 ? text.Height : 1.0;
            else if (entity is MText mtext)
                height = mtext.TextHeight > 1e-9 ? mtext.TextHeight : 1.0;
            list.Add(new ElevLabel
            {
                X = vertex.X,
                Y = vertex.Y,
                Z = vertex.Z,
                Height = height
            });
        }

        return list;
    }

    private static bool TryTakeNearbyElev(List<ElevLabel> labels, double x, double y, out double z)
    {
        z = 0;
        var best = -1;
        var bestDist = double.MaxValue;
        for (var i = 0; i < labels.Count; i++)
        {
            var label = labels[i];
            if (label.Used)
                continue;
            var dx = label.X - x;
            var dy = label.Y - y;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            var radius = Math.Max(2.0, label.Height * 10.0);
            if (dist > radius)
                continue;
            if (dist >= bestDist)
                continue;
            bestDist = dist;
            best = i;
        }

        if (best < 0)
            return false;
        labels[best].Used = true;
        z = labels[best].Z;
        return true;
    }

    private static int Collect(
        Transaction tr,
        Database db,
        string handle,
        List<TinVertex> target,
        List<TinSegment>? links,
        List<ElevLabel>? elevPool,
        ref int unmatchedBlockElev)
    {
        Entity? entity;
        try
        {
            entity = OpenHandle(tr, db, handle);
        }
        catch (Autodesk.AutoCAD.Runtime.Exception)
        {
            return 1;
        }

        if (entity is null)
            return 1;
        if (entity is DBPoint point)
        {
            var z = point.Position.Z;
            if (elevPool is not null)
            {
                if (TryTakeNearbyElev(elevPool, point.Position.X, point.Position.Y, out var elev))
                    z = elev;
                else
                    unmatchedBlockElev++;
            }

            target.Add(new TinVertex(point.Position.X, point.Position.Y, z));
            return 0;
        }

        if (entity is BlockReference block)
        {
            var z = block.Position.Z;
            if (elevPool is not null)
            {
                if (TryTakeNearbyElev(elevPool, block.Position.X, block.Position.Y, out var elev))
                    z = elev;
                else
                    unmatchedBlockElev++;
            }

            target.Add(new TinVertex(block.Position.X, block.Position.Y, z));
            return 0;
        }

        if (entity is DBText or MText)
        {
            if (TryLabelVertex(entity, out var label))
                target.Add(label);
            return 0;
        }

        if (entity is Line line)
        {
            var start = new TinVertex(line.StartPoint.X, line.StartPoint.Y, line.StartPoint.Z);
            var end = new TinVertex(line.EndPoint.X, line.EndPoint.Y, line.EndPoint.Z);
            target.Add(start);
            target.Add(end);
            if (links is not null)
            {
                var dx = start.X - end.X;
                var dy = start.Y - end.Y;
                if (dx * dx + dy * dy > 1e-8)
                    links.Add(new TinSegment(start, end));
            }

            return 0;
        }

        var vertices = Vertices(tr, entity);
        if (vertices.Count == 0)
            return 1;
        foreach (var vertex in vertices)
            target.Add(vertex);
        if (links is null || vertices.Count < 2)
            return 0;
        for (var i = 0; i < vertices.Count - 1; i++)
        {
            var a = vertices[i];
            var b = vertices[i + 1];
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            if (dx * dx + dy * dy <= 1e-8)
                continue;
            links.Add(new TinSegment(a, b));
        }

        return 0;
    }

    private static List<TinVertex> Vertices(Transaction tr, Entity entity)
    {
        var list = new List<TinVertex>();
        if (entity is Polyline polyline)
        {
            for (var i = 0; i < polyline.NumberOfVertices; i++)
            {
                var point = polyline.GetPoint3dAt(i);
                var z = Math.Abs(point.Z) < 1e-9 ? polyline.Elevation : point.Z;
                list.Add(new TinVertex(point.X, point.Y, z));
            }

            if (polyline.Closed && list.Count > 1)
                list.Add(list[0]);
            return list;
        }

        if (entity is Polyline3d poly3)
        {
            foreach (ObjectId id in poly3)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is PolylineVertex3d vertex)
                    list.Add(new TinVertex(vertex.Position.X, vertex.Position.Y, vertex.Position.Z));
            }

            if (poly3.Closed && list.Count > 1)
                list.Add(list[0]);
            return list;
        }

        if (entity is Polyline2d poly2)
        {
            foreach (ObjectId id in poly2)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is not Vertex2d vertex)
                    continue;
                var z = Math.Abs(vertex.Position.Z) < 1e-9 ? poly2.Elevation : vertex.Position.Z;
                list.Add(new TinVertex(vertex.Position.X, vertex.Position.Y, z));
            }

            if (poly2.Closed && list.Count > 1)
                list.Add(list[0]);
        }

        return list;
    }

    private static bool TryLabelVertex(Entity entity, out TinVertex vertex)
    {
        string raw;
        Point3d anchor;
        if (entity is DBText text)
        {
            raw = text.TextString ?? "";
            anchor = text.HorizontalMode == TextHorizontalMode.TextLeft
                && text.VerticalMode == TextVerticalMode.TextBase
                ? text.Position
                : text.AlignmentPoint;
        }
        else if (entity is MText mtext)
        {
            raw = mtext.Text ?? "";
            anchor = mtext.Location;
        }
        else
        {
            vertex = default;
            return false;
        }

        if (!TryElevation(raw, out var elevation))
        {
            vertex = default;
            return false;
        }

        vertex = new TinVertex(anchor.X, anchor.Y, elevation);
        return true;
    }

    private static bool TryElevation(string text, out double value)
    {
        var label = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (label.Length == 0)
        {
            value = 0;
            return false;
        }

        if (double.TryParse(label, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            return true;
        return double.TryParse(label.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static bool IsClosed(Transaction tr, Entity entity)
    {
        if (entity is Polyline polyline)
        {
            if (polyline.NumberOfVertices < 2)
                return false;
            return polyline.Closed || Near(polyline.GetPoint3dAt(0), polyline.GetPoint3dAt(polyline.NumberOfVertices - 1));
        }
        if (entity is Polyline3d or Polyline2d)
        {
            var vertices = Vertices(tr, entity);
            return vertices.Count >= 4 && NearVertex(vertices[0], vertices[vertices.Count - 1]);
        }

        return false;
    }

    private static bool Near(Point3d a, Point3d b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy < 1e-6;
    }

    private static bool NearVertex(TinVertex a, TinVertex b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy < 1e-6;
    }

    private static Entity? OpenHandle(Transaction tr, Database db, string handle)
    {
        if (!long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            return null;
        var id = db.GetObjectId(false, new Handle(value), 0);
        if (!id.IsValid || id.IsErased)
            return null;
        return tr.GetObject(id, OpenMode.ForRead) as Entity;
    }

    private static void ClearList(TerrainRecord model, string kind)
    {
        switch (kind)
        {
            case "contour":
                model.Contours = new List<string>();
                break;
            case "break":
                model.Breaklines = new List<string>();
                break;
            case "boundary":
                model.Boundaries = new List<string>();
                model.Boundary = null;
                break;
            default:
                model.Points = new List<string>();
                break;
        }
    }

    private static bool DropHandle(TerrainRecord model, string kind, string handle)
    {
        if (kind == "boundary")
        {
            var changed = false;
            if (!string.IsNullOrWhiteSpace(model.Boundary)
                && string.Equals(model.Boundary, handle, StringComparison.OrdinalIgnoreCase))
            {
                model.Boundary = null;
                changed = true;
            }

            model.Boundaries ??= new List<string>();
            var removed = model.Boundaries.RemoveAll(item => string.Equals(item, handle, StringComparison.OrdinalIgnoreCase));
            return changed || removed > 0;
        }

        var list = kind switch
        {
            "contour" => model.Contours ??= new List<string>(),
            "break" => model.Breaklines ??= new List<string>(),
            _ => model.Points ??= new List<string>()
        };
        return list.RemoveAll(item => string.Equals(item, handle, StringComparison.OrdinalIgnoreCase)) > 0;
    }

    private static void InvalidateMesh(Autodesk.AutoCAD.ApplicationServices.Document doc, TerrainRecord model)
    {
        var hadMesh = model.TriangleCount > 0;
        model.Mesh = new List<double>();
        using (doc.LockDocument())
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            EraseFaces(tr, doc, model.Id);
            tr.Commit();
        }

        TerrainStore.Save(doc.Database, model);
        if (hadMesh)
            doc.Editor.Regen();
    }

    private static int PruneList(Transaction tr, Database db, List<string> handles, out bool changed)
    {
        changed = false;
        var missing = 0;
        for (var i = handles.Count - 1; i >= 0; i--)
        {
            var handle = handles[i];
            if (string.IsNullOrWhiteSpace(handle))
            {
                handles.RemoveAt(i);
                changed = true;
                missing++;
                continue;
            }

            Entity? entity;
            try
            {
                entity = OpenHandle(tr, db, handle);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception)
            {
                entity = null;
            }

            if (entity is not null)
                continue;
            handles.RemoveAt(i);
            changed = true;
            missing++;
        }

        return missing;
    }

    private static void EraseFaces(Transaction tr, Autodesk.AutoCAD.ApplicationServices.Document doc, string modelId)
    {
        var space = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForRead);
        var doomed = new List<ObjectId>();
        foreach (ObjectId id in space)
        {
            if (tr.GetObject(id, OpenMode.ForRead) is Face face && Owned(face, modelId))
                doomed.Add(id);
        }

        foreach (var id in doomed)
        {
            var face = (Face)tr.GetObject(id, OpenMode.ForWrite);
            face.Erase();
        }
    }

    private static bool Owned(Face face, string modelId)
    {
        var data = face.GetXDataForApplication(AppName);
        if (data is null)
            return false;
        using (data)
        {
            foreach (TypedValue value in data)
            {
                if (value.TypeCode == (int)DxfCode.ExtendedDataAsciiString
                    && value.Value is string text
                    && string.Equals(text, modelId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    private static void EnsureLayer(Transaction tr, Database db, string name)
    {
        var layers = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (layers.Has(name))
            return;
        layers.UpgradeOpen();
        var record = new LayerTableRecord
        {
            Name = name,
            Color = AcadColor.FromColorIndex(ColorMethod.ByAci, 8)
        };
        layers.Add(record);
        tr.AddNewlyCreatedDBObject(record, true);
    }

    private static void EnsureApp(Transaction tr, Database db)
    {
        var apps = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
        if (apps.Has(AppName))
            return;
        apps.UpgradeOpen();
        var record = new RegAppTableRecord { Name = AppName };
        apps.Add(record);
        tr.AddNewlyCreatedDBObject(record, true);
    }

    private static string Clip(string text) => text.Length <= 255 ? text : text.Substring(0, 255);
}
