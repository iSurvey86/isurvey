using System.Globalization;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices.Filters;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using iSurvey.Auth;
using iSurvey.Modules.Map;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcadColor = Autodesk.AutoCAD.Colors.Color;
using ColorMethod = Autodesk.AutoCAD.Colors.ColorMethod;

namespace iSurvey.Modules.Tuyen;

/// <summary>
/// Duỗi thẳng tuyến: RasterImage (clip pixel) + CAD (anonymous block + SpatialFilter).
/// Mỗi đoạn xoay cho song song trục X rồi xếp nối tiếp. Tọa độ trong file .txt là tọa độ gốc.
/// Một transaction + undo mark = một lần UNDO.
/// </summary>
public sealed class StripMapService
{
    private const string OutLayer = "ISURVEY_STRIP";
    private const string GocLayer = "ISURVEY_GOC_TUYEN";
    private const double BulgeTol = 1e-8;
    [CommandMethod("ISURVEY_STRIP_MAP")]
    public void StripMapCommand()
    {
        if (!AuthManager.EnsureAuthenticated())
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        var db = doc.Database;
        var ed = doc.Editor;

        try
        {
            Run(doc, db, ed);
        }
        catch (System.Exception ex)
        {
            ActivityLogger.TrackError(ex, "ISURVEY_STRIP_MAP");
            ed.WriteMessage($"\n[iSurvey] Duỗi tuyến lỗi: {ex.Message}");
        }
    }

    private static void Run(Document doc, Database db, Editor ed)
    {
        var sel = ed.GetSelection(new PromptSelectionOptions
        {
            MessageForAdding = "\n[iSurvey] Chọn đối tượng cần duỗi (ảnh, nét, hatch, block…): "
        });
        if (sel.Status != PromptStatus.OK || sel.Value is null || sel.Value.Count == 0)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy — chưa chọn đối tượng.");
            return;
        }

        var peo = new PromptEntityOptions("\n[iSurvey] Chọn tim tuyến (click vị trí nào trên tuyến cũng được): ")
        {
            AllowNone = false
        };
        peo.SetRejectMessage("\n[iSurvey] Chỉ chấp nhận Polyline / Polyline2d / Polyline3d.");
        peo.AddAllowedClass(typeof(Polyline), true);
        peo.AddAllowedClass(typeof(Polyline2d), true);
        peo.AddAllowedClass(typeof(Polyline3d), true);
        var per = ed.GetEntity(peo);
        if (per.Status != PromptStatus.OK)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy.");
            return;
        }

        List<Point3d> vertices;
        using (var trRead = db.TransactionManager.StartTransaction())
        {
            var ent = (Entity)trRead.GetObject(per.ObjectId, OpenMode.ForRead);
            if (HasBulge(ent, trRead))
            {
                ed.WriteMessage(
                    "\n[iSurvey] Tim tuyến còn cung (bulge). PEDIT → Decurve (chuyển thành đoạn thẳng) rồi chạy lại.");
                return;
            }

            vertices = ReadVertices(ent, trRead, out var closed);
            Normalize(vertices);
            if (closed)
            {
                ed.WriteMessage("\n[iSurvey] Tuyến khép kín chưa hỗ trợ. Hãy mở polyline (không Closed).");
                return;
            }

            if (vertices.Count < 2)
            {
                ed.WriteMessage("\n[iSurvey] Cần ít nhất 2 đỉnh khác nhau.");
                return;
            }

            trRead.Commit();
        }

        // Bắt buộc: người dùng chỉ rõ ĐIỂM ĐẦU. Đầu mút gần điểm chọn hơn = DD; đầu kia = DC.
        var ppStart = new PromptPointOptions(
            "\n[iSurvey] Chọn ĐIỂM ĐẦU tuyến (click gần đầu mút bắt đầu — điểm này là DD): ")
        {
            AllowNone = false
        };
        var prStart = ed.GetPoint(ppStart);
        if (prStart.Status != PromptStatus.OK)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy — chưa chọn điểm đầu.");
            return;
        }

        var startPick = prStart.Value.TransformBy(ed.CurrentUserCoordinateSystem);
        var dFirst = Dist2d(startPick, vertices[0]);
        var dLast = Dist2d(startPick, vertices[^1]);
        if (Math.Abs(dFirst - dLast) < 1e-6)
        {
            ed.WriteMessage("\n[iSurvey] Điểm đầu không xác định (cách đều hai đầu mút). Chọn lại gần một đầu hơn.");
            return;
        }

        var reversed = dLast < dFirst;
        if (reversed)
            vertices.Reverse();

        ed.WriteMessage(
            $"\n[iSurvey] Điểm đầu = DD tại ({vertices[0].X:0.###}, {vertices[0].Y:0.###}); " +
            $"điểm cuối = DC tại ({vertices[^1].X:0.###}, {vertices[^1].Y:0.###})" +
            (reversed ? " (đã đảo chiều đỉnh)." : "."));

        var units = db.Insunits;
        ed.WriteMessage($"\n[iSurvey] INSUNITS = {units}. Hành lang tính theo đơn vị bản vẽ.");
        if (units is not (UnitsValue.Meters or UnitsValue.Undefined))
            ed.WriteMessage("\n[iSurvey] Cảnh báo: bản vẽ không phải mét — số 100 là đơn vị vẽ, không tự đổi ra mét.");

        var pDist = new PromptDistanceOptions($"\n[iSurvey] Hành lang mỗi bên ({units}) <100>: ")
        {
            AllowNegative = false,
            AllowZero = false,
            AllowNone = true,
            DefaultValue = 100,
            UseDefaultValue = true
        };
        var prDist = ed.GetDistance(pDist);
        if (prDist.Status != PromptStatus.OK)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy.");
            return;
        }

        var halfWidth = prDist.Value;

        var pso = new PromptStringOptions("\n[iSurvey] Tiền tố điểm góc <G>: ")
        {
            AllowSpaces = false,
            UseDefaultValue = true,
            DefaultValue = "G"
        };
        var prStr = ed.GetString(pso);
        if (prStr.Status != PromptStatus.OK)
            return;
        var prefix = string.IsNullOrEmpty(prStr.StringResult) ? "G" : prStr.StringResult;

        var pio = new PromptIntegerOptions("\n[iSurvey] Số bắt đầu cho góc đầu tiên sau DD <1>: ")
        {
            AllowNone = true,
            AllowZero = true,
            AllowNegative = false,
            DefaultValue = 1,
            UseDefaultValue = true
        };
        var pir = ed.GetInteger(pio);
        if (pir.Status == PromptStatus.Cancel)
            return;
        var startIndex = pir.Status == PromptStatus.OK ? pir.Value : 1;

        var ppo = new PromptPointOptions("\n[iSurvey] Điểm chèn dải tuyến: ")
        {
            AllowNone = false
        };
        var ppr = ed.GetPoint(ppo);
        if (ppr.Status != PromptStatus.OK)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy.");
            return;
        }

        var insert0 = ppr.Value.TransformBy(ed.CurrentUserCoordinateSystem);

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Lưu bảng góc tuyến",
            Filter = "Text (*.txt)|*.txt",
            DefaultExt = "txt",
            FileName = "BangGocTuyen.txt",
            AddExtension = true,
            OverwritePrompt = true
        };
        if (dlg.ShowDialog() != true || string.IsNullOrWhiteSpace(dlg.FileName))
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy lưu file.");
            return;
        }

        var txtPath = dlg.FileName;
        var report = new StringBuilder();
        var imageCount = 0;
        var blockCount = 0;
        var labelCount = 0;

        object? undoDoc = null;
        try
        {
            undoDoc = doc.GetAcadDocument();
            undoDoc?.GetType().InvokeMember(
                "StartUndoMark",
                System.Reflection.BindingFlags.InvokeMethod,
                null,
                undoDoc,
                null);
        }
        catch
        {
            undoDoc = null;
        }

        try
        {
            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                var layerId = EnsureLayer(tr, db);
                Partition(tr, sel.Value.GetObjectIds(), per.ObjectId, out var rasterIds, out var cadIds);
                if (!cadIds.Contains(per.ObjectId))
                    cadIds.Add(per.ObjectId);

                ObjectId? blockId = null;
                if (cadIds.Count > 0)
                    blockId = BuildAnonymousBlock(db, tr, cadIds);

                var markers = CollectGocMarkers(tr, space, db);
                var names = AssignNames(tr, vertices, markers.Texts, prefix, startIndex, Math.Max(halfWidth, 150));
                var circles = AssignNearest(vertices, markers.Circles, Math.Max(halfWidth, 150));
                var hatches = AssignNearest(vertices, markers.Hatches, Math.Max(halfWidth, 150));

                var segCount = vertices.Count - 1;
                var inserts = new Point3d[segCount];
                var cursor = insert0;
                for (var i = 0; i < segCount; i++)
                {
                    inserts[i] = cursor;
                    var len = Dist2d(vertices[i], vertices[i + 1]);
                    cursor = new Point3d(cursor.X + len, cursor.Y, insert0.Z);
                }

                var imageOut = new ObjectIdCollection();
                for (var i = 0; i < segCount; i++)
                {
                    var clip = BuildClip(vertices, i, halfWidth);
                    if (clip is null)
                    {
                        ed.WriteMessage($"\n[iSurvey] Bỏ đoạn {i + 1}: đa giác cắt suy biến.");
                        continue;
                    }

                    var angle = Angle(vertices[i], vertices[i + 1]);
                    var origin = vertices[i];
                    var dest = inserts[i];

                    foreach (var rid in rasterIds)
                    {
                        var src = tr.GetObject(rid, OpenMode.ForRead) as RasterImage;
                        if (src is null || src is Wipeout)
                            continue;
                        if (!ExtentsOverlap(src, clip))
                            continue;
                        var placed = PlaceImage(tr, space, layerId, src, clip, origin, angle, dest);
                        if (placed is null)
                            continue;
                        imageOut.Add(placed.Value);
                        imageCount++;
                    }

                    if (blockId is not null)
                    {
                        var brId = PlaceBlockClip(tr, space, layerId, blockId.Value, clip, origin, angle, dest);
                        if (brId is not null)
                            blockCount++;
                    }
                }

                for (var i = 0; i < vertices.Count; i++)
                {
                    var owner = i == 0 ? 0 : i - 1;
                    var pack = new ObjectIdCollection();
                    if (names.Ids[i] is ObjectId tid)
                        pack.Add(tid);
                    if (circles[i] is ObjectId cid)
                        pack.Add(cid);
                    if (hatches[i] is ObjectId hid)
                        pack.Add(hid);
                    if (pack.Count == 0)
                        continue;
                    labelCount += TransformClones(
                        db, tr, space, pack,
                        vertices[owner],
                        Angle(vertices[owner], vertices[owner + 1]),
                        inserts[owner],
                        names.Labels[i]);
                }

                if (imageOut.Count > 0 && !space.DrawOrderTableId.IsNull)
                {
                    var order = (DrawOrderTable)tr.GetObject(space.DrawOrderTableId, OpenMode.ForWrite);
                    order.MoveToBottom(imageOut);
                }

                report.Append(BuildReport(vertices, names.Labels));
                tr.Commit();
            }
        }
        finally
        {
            try
            {
                undoDoc?.GetType().InvokeMember(
                    "EndUndoMark",
                    System.Reflection.BindingFlags.InvokeMethod,
                    null,
                    undoDoc,
                    null);
            }
            catch { /* ignore */ }
        }

        File.WriteAllText(txtPath, report.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(txtPath)
            {
                UseShellExecute = true
            });
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] Không mở được file: {ex.Message}");
        }

        ActivityLogger.Enqueue("strip_map", "ISURVEY_STRIP_MAP", new
        {
            segments = vertices.Count - 1,
            images = imageCount,
            blocks = blockCount,
            labels = labelCount
        });

        ed.WriteMessage(
            $"\n[iSurvey] Duỗi tuyến xong: {vertices.Count - 1} đoạn, {imageCount} ảnh, {blockCount} block cắt, {labelCount} nhãn. " +
            $"Layer {OutLayer}. File: {txtPath}. Một lần UNDO để hoàn tác.");
    }

    private static void Partition(
        Transaction tr, ObjectId[] ids, ObjectId centerline,
        out List<ObjectId> rasters, out List<ObjectId> cad)
    {
        rasters = [];
        cad = [];
        var seen = new HashSet<ObjectId>();
        foreach (var id in ids)
        {
            if (!seen.Add(id) || id == centerline)
                continue;
            if (tr.GetObject(id, OpenMode.ForRead) is not Entity ent)
                continue;
            if (ent.Layer.Equals(GocLayer, StringComparison.OrdinalIgnoreCase))
                continue;
            if (ent is RasterImage and not Wipeout)
                rasters.Add(id);
            else if (IsStripCad(ent))
                cad.Add(id);
        }
    }

    private static bool IsStripCad(Entity e) =>
        e is Line or Polyline or Polyline2d or Polyline3d
            or Circle or Arc or Ellipse or Spline
            or DBText or MText or DBPoint
            or Hatch or Solid or Face
            or BlockReference or Leader or MLeader
            or Dimension or Autodesk.AutoCAD.DatabaseServices.Region or Wipeout;

    private static ObjectId BuildAnonymousBlock(Database db, Transaction tr, List<ObjectId> cadIds)
    {
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
        var btr = new BlockTableRecord { Name = "*U" };
        var blockId = bt.Add(btr);
        tr.AddNewlyCreatedDBObject(btr, true);
        var mapping = new IdMapping();
        db.DeepCloneObjects(new ObjectIdCollection(cadIds.ToArray()), blockId, mapping, false);
        return blockId;
    }

    private static ObjectId? PlaceImage(
        Transaction tr, BlockTableRecord space, ObjectId layerId,
        RasterImage src, IReadOnlyList<Point2d> clip, Point3d pivot, double angle, Point3d dest)
    {
        if (src.ImageDefId.IsNull)
            return null;

        var def = (RasterImageDef)tr.GetObject(src.ImageDefId, OpenMode.ForWrite);
        var clone = new RasterImage
        {
            ImageDefId = src.ImageDefId,
            Orientation = src.Orientation,
            ShowImage = true,
            ImageTransparency = src.ImageTransparency,
            LayerId = layerId
        };
        var id = space.AppendEntity(clone);
        tr.AddNewlyCreatedDBObject(clone, true);
        clone.Brightness = src.Brightness;
        clone.Contrast = src.Contrast;
        clone.Fade = src.Fade;
        RasterImage.EnableReactors(true);
        clone.AssociateRasterDef(def);

        if (!RasterClipService.TryApplyClipPolygon(clone, clip))
        {
            clone.Erase();
            return null;
        }

        ApplyStraighten(clone, pivot, angle, dest);
        return id;
    }

    private static ObjectId? PlaceBlockClip(
        Transaction tr, BlockTableRecord space, ObjectId layerId,
        ObjectId blockId, IReadOnlyList<Point2d> clip, Point3d pivot, double angle, Point3d dest)
    {
        var br = new BlockReference(Point3d.Origin, blockId)
        {
            ScaleFactors = new Scale3d(1),
            LayerId = layerId
        };
        var id = space.AppendEntity(br);
        tr.AddNewlyCreatedDBObject(br, true);
        AttachSpatialFilter(tr, br, clip);
        ApplyStraighten(br, pivot, angle, dest);
        return id;
    }

    /// <summary>Xoay −α quanh P1 (đoạn nằm ngang) rồi dời P1 tới điểm chèn. Giống LISP tdm2.</summary>
    private static void ApplyStraighten(Entity ent, Point3d pivot, double angle, Point3d dest)
    {
        ent.TransformBy(Matrix3d.Rotation(-angle, Vector3d.ZAxis, pivot));
        ent.TransformBy(Matrix3d.Displacement(dest - pivot));
    }

    /// <summary>Clip trong hệ tọa độ block (nội dung còn ở WCS, insert = gốc).</summary>
    private static void AttachSpatialFilter(Transaction tr, BlockReference br, IReadOnlyList<Point2d> clip)
    {
        var pts = new Point2dCollection();
        foreach (var p in clip)
            pts.Add(p);

        var filter = new SpatialFilter
        {
            Definition = new SpatialFilterDefinition(pts, Vector3d.ZAxis, 0, 0, 0, true)
        };

        if (br.ExtensionDictionary.IsNull)
            br.CreateExtensionDictionary();

        var ext = (DBDictionary)tr.GetObject(br.ExtensionDictionary, OpenMode.ForWrite);
        DBDictionary filterDict;
        if (ext.Contains("ACAD_FILTER"))
        {
            filterDict = (DBDictionary)tr.GetObject(ext.GetAt("ACAD_FILTER"), OpenMode.ForWrite);
        }
        else
        {
            filterDict = new DBDictionary();
            ext.SetAt("ACAD_FILTER", filterDict);
            tr.AddNewlyCreatedDBObject(filterDict, true);
        }

        if (filterDict.Contains("SPATIAL"))
            filterDict.Remove("SPATIAL");
        filterDict.SetAt("SPATIAL", filter);
        tr.AddNewlyCreatedDBObject(filter, true);
    }

    private static int TransformClones(
        Database db, Transaction tr, BlockTableRecord space, ObjectIdCollection sources,
        Point3d pivot, double angle, Point3d dest, string? labelText = null)
    {
        var mapping = new IdMapping();
        db.DeepCloneObjects(sources, space.ObjectId, mapping, false);
        var n = 0;
        foreach (IdPair pair in mapping)
        {
            if (!pair.IsCloned || !pair.IsPrimary)
                continue;
            if (tr.GetObject(pair.Value, OpenMode.ForWrite) is not Entity ent)
                continue;
            ApplyStraighten(ent, pivot, angle, dest);
            if (ent is DBText txt)
            {
                if (!string.IsNullOrEmpty(labelText))
                    txt.TextString = labelText;
                txt.Rotation = 0.0;
            }
            else if (ent is MText mt)
            {
                if (!string.IsNullOrEmpty(labelText))
                    mt.Contents = labelText;
                mt.Rotation = 0.0;
            }

            n++;
        }

        return n;
    }

    private static List<Point2d>? BuildClip(IReadOnlyList<Point3d> v, int seg, double width)
    {
        var a = v[seg];
        var b = v[seg + 1];
        if (Dist2d(a, b) < 1e-8)
            return null;

        // Hình chữ nhật vuông góc tại hai đỉnh: sau khi duỗi, các đoạn khít nhau (không khe, không răng cưa),
        // và mỗi đoạn được dời cứng (không biến dạng) nên số đo trong từng đoạn chính xác.
        var d = Unit(a, b);
        var n = new Vector2d(-d.Y, d.X);
        var rect = new[]
        {
            To2d(a) + n * width,
            To2d(b) + n * width,
            To2d(b) - n * width,
            To2d(a) - n * width
        };
        ForceCcw(rect);
        return Shoelace(rect) > 1e-6 ? rect.ToList() : null;
    }

    private static void ForceCcw(Point2d[] pts)
    {
        if (Shoelace(pts) < 0)
            Array.Reverse(pts);
    }

    private static double Shoelace(Point2d[] p)
    {
        double a = 0;
        for (var i = 0; i < p.Length; i++)
        {
            var j = (i + 1) % p.Length;
            a += p[i].X * p[j].Y - p[j].X * p[i].Y;
        }

        return a * 0.5;
    }

    private static bool ExtentsOverlap(RasterImage img, IReadOnlyList<Point2d> clip)
    {
        Extents3d ext;
        try { ext = img.GeometricExtents; }
        catch { return true; }

        double minX = clip[0].X, maxX = clip[0].X, minY = clip[0].Y, maxY = clip[0].Y;
        foreach (var p in clip)
        {
            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.Y > maxY) maxY = p.Y;
        }

        return ext.MaxPoint.X >= minX && ext.MinPoint.X <= maxX
            && ext.MaxPoint.Y >= minY && ext.MinPoint.Y <= maxY;
    }

    private sealed class MarkerSets
    {
        public List<(ObjectId Id, Point3d Pos)> Texts { get; } = [];
        public List<(ObjectId Id, Point3d Pos)> Circles { get; } = [];
        public List<(ObjectId Id, Point3d Pos)> Hatches { get; } = [];
    }

    private static MarkerSets CollectGocMarkers(Transaction tr, BlockTableRecord space, Database db)
    {
        var sets = new MarkerSets();
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (!lt.Has(GocLayer))
            return sets;

        foreach (ObjectId id in space)
        {
            if (tr.GetObject(id, OpenMode.ForRead) is not Entity ent)
                continue;
            if (!ent.Layer.Equals(GocLayer, StringComparison.OrdinalIgnoreCase))
                continue;

            switch (ent)
            {
                case DBText txt:
                    var anchored = txt.HorizontalMode != TextHorizontalMode.TextLeft
                                   || txt.VerticalMode != TextVerticalMode.TextBase;
                    sets.Texts.Add((id, anchored ? txt.AlignmentPoint : txt.Position));
                    break;
                case Circle c:
                    sets.Circles.Add((id, c.Center));
                    break;
                case Hatch h:
                    try
                    {
                        var e = h.GeometricExtents;
                        var mid = new Point3d(
                            (e.MinPoint.X + e.MaxPoint.X) * 0.5,
                            (e.MinPoint.Y + e.MaxPoint.Y) * 0.5,
                            0);
                        sets.Hatches.Add((id, mid));
                    }
                    catch { /* extents rỗng */ }
                    break;
            }
        }

        return sets;
    }

    private static (string[] Labels, ObjectId?[] Ids) AssignNames(
        Transaction tr,
        IReadOnlyList<Point3d> vertices,
        List<(ObjectId Id, Point3d Pos)> texts,
        string prefix, int start, double radius)
    {
        var ids = AssignNearest(vertices, texts, radius);
        var labels = new string[vertices.Count];
        var last = vertices.Count - 1;
        for (var i = 0; i < vertices.Count; i++)
        {
            // DD = điểm đầu, DC = điểm cuối, giữa là {prefix}{start}, {prefix}{start+1}, …
            labels[i] = i == 0 ? "DD"
                : i == last ? "DC"
                : $"{prefix}{start + i - 1}";
        }

        return (labels, ids);
    }

    private static ObjectId?[] AssignNearest(
        IReadOnlyList<Point3d> vertices,
        List<(ObjectId Id, Point3d Pos)> items,
        double radius)
    {
        var result = new ObjectId?[vertices.Count];
        var pairs = new List<(double D, int Index, ObjectId Id)>();
        for (var i = 0; i < vertices.Count; i++)
        {
            foreach (var item in items)
            {
                var d = Dist2d(vertices[i], item.Pos);
                if (d <= radius)
                    pairs.Add((d, i, item.Id));
            }
        }

        pairs.Sort((a, b) => a.D.CompareTo(b.D));
        var usedV = new HashSet<int>();
        var usedId = new HashSet<ObjectId>();
        foreach (var p in pairs)
        {
            if (usedV.Contains(p.Index) || usedId.Contains(p.Id))
                continue;
            usedV.Add(p.Index);
            usedId.Add(p.Id);
            result[p.Index] = p.Id;
        }

        return result;
    }

    private static string BuildReport(IReadOnlyList<Point3d> v, string[] names)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Tên Điểm,X,Y,Khoảng Cách Cạnh Trước,Góc Rẽ (T/P Độ Phút Giây)");
        var inv = CultureInfo.InvariantCulture;
        for (var i = 0; i < v.Count; i++)
        {
            var dist = i == 0 ? "" : Dist2d(v[i - 1], v[i]).ToString("0.00", inv);
            var turn = "";
            var dms = "";
            if (i > 0 && i < v.Count - 1)
            {
                var vin = Unit(v[i - 1], v[i]);
                var vout = Unit(v[i], v[i + 1]);
                var cross = vin.X * vout.Y - vin.Y * vout.X;
                var dot = vin.X * vout.X + vin.Y * vout.Y;
                var deg = Math.Atan2(Math.Abs(cross), dot) * 180.0 / Math.PI;
                if (deg >= 1.0 / 60.0)
                {
                    turn = cross > 0 ? "T" : "P";
                    dms = ToDms(deg);
                }
            }

            sb.Append(names[i]).Append(',')
                .Append(v[i].X.ToString("0.00", inv)).Append(',')
                .Append(v[i].Y.ToString("0.00", inv)).Append(',')
                .Append(dist).Append(',')
                .Append(turn.Length > 0 ? turn + " " + dms : "")
                .AppendLine();
        }

        return sb.ToString();
    }

    private static string ToDms(double degrees)
    {
        if (degrees < 0)
            degrees = 0;
        var d = (int)Math.Floor(degrees);
        var minFull = (degrees - d) * 60.0;
        var m = (int)Math.Floor(minFull + 1e-9);
        var s = (int)Math.Round((minFull - m) * 60.0, MidpointRounding.AwayFromZero);
        if (s >= 60)
        {
            s = 0;
            m++;
        }

        if (m >= 60)
        {
            m = 0;
            d++;
        }

        return $"{d:00} {m:00} {s:00}";
    }

    private static ObjectId EnsureLayer(Transaction tr, Database db)
    {
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (lt.Has(OutLayer))
            return lt[OutLayer];

        lt.UpgradeOpen();
        var ltr = new LayerTableRecord
        {
            Name = OutLayer,
            Color = AcadColor.FromColorIndex(ColorMethod.ByAci, 4)
        };
        var id = lt.Add(ltr);
        tr.AddNewlyCreatedDBObject(ltr, true);
        return id;
    }

    private static bool HasBulge(Entity ent, Transaction tr)
    {
        switch (ent)
        {
            case Polyline pl:
                for (var i = 0; i < pl.NumberOfVertices; i++)
                {
                    if (Math.Abs(pl.GetBulgeAt(i)) > BulgeTol)
                        return true;
                }

                return false;
            case Polyline2d pl2:
                foreach (ObjectId vxId in pl2)
                {
                    if (tr.GetObject(vxId, OpenMode.ForRead) is Vertex2d vx && Math.Abs(vx.Bulge) > BulgeTol)
                        return true;
                }

                return false;
            default:
                return false;
        }
    }

    private static List<Point3d> ReadVertices(Entity ent, Transaction tr, out bool closed)
    {
        var pts = new List<Point3d>();
        closed = ent switch
        {
            Polyline p => p.Closed,
            Polyline2d p2 => p2.Closed,
            Polyline3d p3 => p3.Closed,
            _ => false
        };

        switch (ent)
        {
            case Polyline pl:
                for (var i = 0; i < pl.NumberOfVertices; i++)
                    pts.Add(pl.GetPoint3dAt(i));
                break;
            case Polyline2d pl2:
                foreach (ObjectId vxId in pl2)
                {
                    if (tr.GetObject(vxId, OpenMode.ForRead) is Vertex2d vx)
                        pts.Add(vx.Position);
                }

                break;
            case Polyline3d pl3:
                foreach (ObjectId vxId in pl3)
                {
                    if (tr.GetObject(vxId, OpenMode.ForRead) is PolylineVertex3d vx)
                        pts.Add(vx.Position);
                }

                break;
        }

        return pts;
    }

    private static void Normalize(List<Point3d> pts)
    {
        for (var i = pts.Count - 1; i > 0; i--)
        {
            if (Dist2d(pts[i], pts[i - 1]) < 1e-8)
                pts.RemoveAt(i);
        }

        if (pts.Count > 2 && Dist2d(pts[0], pts[^1]) < 1e-8)
            pts.RemoveAt(pts.Count - 1);
    }

    private static double Dist2d(Point3d a, Point3d b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static double Angle(Point3d a, Point3d b) => Math.Atan2(b.Y - a.Y, b.X - a.X);

    private static Vector2d Unit(Point3d a, Point3d b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-12)
            return new Vector2d(1, 0);
        return new Vector2d(dx / len, dy / len);
    }

    private static Point2d To2d(Point3d p) => new(p.X, p.Y);
}
