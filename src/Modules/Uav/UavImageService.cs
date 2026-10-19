using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using iSurvey.Modules.Map;
using AcadRegion = Autodesk.AutoCAD.DatabaseServices.Region;

namespace iSurvey.Modules.Uav;

/// <summary>
/// Cắt RasterImage UAV — All-in-One: Batch Polyline (Smart Filter) hoặc Auto-Clip nền trắng/đen.
/// </summary>
internal static class UavImageService
{
    private const double ArcStepRad = 5.0 * Math.PI / 180.0;
    private const double CloseTol = 1e-4;
    private const double MinAreaRatio = 0.05;
    private const string AutoBorderLayer = "ISURVEY_UAV_BORDER";
    private static readonly string[] BorderLayerHints = ["border", "vien", "khung"];

    /// <summary>Chọn ảnh → hỏi chế độ 1/2 → cắt hàng loạt.</summary>
    public static void ClipByBoundary(Editor ed, Database db)
    {
        var images = PromptSelectImages(ed, db);
        if (images is null)
            return;

        var mode = PromptClipMode(ed);
        if (mode is null)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy — chưa cắt ảnh.");
            return;
        }

        try
        {
            if (mode == 1)
                RunBatchPolylineClip(ed, db, images);
            else
                RunAutoClip(ed, db, images);
        }
        catch (System.Exception ex)
        {
            // Transaction chưa Commit → tự hoàn tác; báo rõ thay vì để AutoCAD hiện lỗi thô.
            ed.WriteMessage($"\n[iSurvey] Lỗi khi cắt ảnh, đã hoàn tác lần chạy này: {ex.Message}");
            iSurvey.Auth.ActivityLogger.TrackError(ex, "ISURVEY_UAV_CLIP");
        }
    }

    private static List<ObjectId>? PromptSelectImages(Editor ed, Database db)
    {
        ed.WriteMessage("\n[iSurvey] Quét chọn ảnh Raster (Window/Crossing)...");
        var filter = new SelectionFilter([new TypedValue((int)DxfCode.Start, "IMAGE")]);
        var selOpts = new PromptSelectionOptions
        {
            MessageForAdding = "\n[iSurvey] Chọn ảnh UAV (Enter khi xong): ",
            AllowDuplicates = false
        };

        var sel = ed.GetSelection(selOpts, filter);
        if (sel.Status != PromptStatus.OK || sel.Value is null || sel.Value.Count == 0)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy — chưa cắt ảnh.");
            return null;
        }

        var list = new List<ObjectId>();
        using (var tr = db.TransactionManager.StartTransaction())
        {
            foreach (SelectedObject so in sel.Value)
            {
                if (so is null) continue;
                if (tr.GetObject(so.ObjectId, OpenMode.ForRead) is RasterImage)
                    list.Add(so.ObjectId);
            }

            tr.Commit();
        }

        if (list.Count == 0)
        {
            ed.WriteMessage("\n[iSurvey] Không có ảnh Raster nào trong vùng chọn.");
            return null;
        }

        ed.WriteMessage($"\n[iSurvey] Đã chọn {list.Count} ảnh.");
        return list;
    }

    private static int? PromptClipMode(Editor ed)
    {
        var opts = new PromptIntegerOptions(
            "\n[iSurvey] Chọn phương pháp cắt: [1. Chọn Polyline làm biên / 2. Tự động cắt nền Trắng-Đen (Auto-Clip)] <1>: ")
        {
            AllowNone = true,
            AllowNegative = false,
            AllowZero = false,
            LowerLimit = 1,
            UpperLimit = 2,
            DefaultValue = 1,
            UseDefaultValue = true
        };

        var res = ed.GetInteger(opts);
        if (res.Status == PromptStatus.None)
            return 1;
        if (res.Status != PromptStatus.OK)
            return null;
        return res.Value;
    }

    private static void RunBatchPolylineClip(Editor ed, Database db, List<ObjectId> listImages)
    {
        ed.WriteMessage("\n[iSurvey] Quét chọn polyline biên (Window/Crossing)...");
        var filter = new SelectionFilter(
        [
            new TypedValue((int)DxfCode.Operator, "<OR"),
            new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
            new TypedValue((int)DxfCode.Start, "POLYLINE"),
            new TypedValue((int)DxfCode.Operator, "OR>")
        ]);
        var selOpts = new PromptSelectionOptions
        {
            MessageForAdding = "\n[iSurvey] Chọn biên (Enter khi xong): ",
            AllowDuplicates = false
        };

        var sel = ed.GetSelection(selOpts, filter);
        if (sel.Status != PromptStatus.OK || sel.Value is null || sel.Value.Count == 0)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy — chưa chọn biên.");
            return;
        }

        using var tr = db.TransactionManager.StartTransaction();
        var listPolylines = new List<ObjectId>();
        foreach (SelectedObject so in sel.Value)
        {
            if (so is null) continue;
            var ent = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Entity;
            if (ent is Polyline or Polyline2d or Polyline3d)
                listPolylines.Add(so.ObjectId);
        }

        var rawPolyCount = listPolylines.Count;
        listPolylines = ApplySmartFilter(tr, listImages, listPolylines, out var filterReport);
        ed.WriteMessage(
            $"\n[iSurvey] Biên: {rawPolyCount} polyline → còn {listPolylines.Count} sau Smart Filter" +
            (string.IsNullOrEmpty(filterReport) ? "." : $" ({filterReport})."));

        if (listPolylines.Count == 0)
        {
            ed.WriteMessage("\n[iSurvey] Không còn polyline biên hợp lệ.");
            return;
        }

        var imageBoxes = new Dictionary<ObjectId, Extents2d>();
        foreach (var id in listImages)
        {
            var raster = (RasterImage)tr.GetObject(id, OpenMode.ForRead);
            imageBoxes[id] = ToExtents2d(raster.GeometricExtents);
        }

        var polyBoxes = new Dictionary<ObjectId, Extents2d>();
        foreach (var id in listPolylines)
        {
            var ent = (Entity)tr.GetObject(id, OpenMode.ForRead);
            polyBoxes[id] = ToExtents2d(ent.GeometricExtents);
        }

        var usedPolys = new HashSet<ObjectId>();
        var ok = 0;
        var total = listImages.Count;
        var imagesOrdered = listImages.OrderBy(id => BoxArea(imageBoxes[id])).ToList();

        foreach (var imageId in imagesOrdered)
        {
            var imgBox = imageBoxes[imageId];
            ObjectId? bestPoly = null;
            var bestScore = -1.0;
            var bestContained = false;

            foreach (var polyId in listPolylines)
            {
                if (usedPolys.Contains(polyId))
                    continue;

                var pBox = polyBoxes[polyId];
                var overlap = OverlapArea(imgBox, pBox);
                if (overlap <= 0)
                    continue;

                var contained = Contains(imgBox, pBox);
                var score = contained ? 1e12 + overlap : overlap;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPoly = polyId;
                    bestContained = contained;
                }
            }

            if (bestPoly is null)
            {
                ed.WriteMessage("\n[iSurvey] Bỏ qua ảnh (không tìm thấy biên phù hợp).");
                continue;
            }

            var boundary = (Entity)tr.GetObject(bestPoly.Value, OpenMode.ForRead);
            List<Point2d> ring;
            try
            {
                ring = ExtractBoundaryRing(boundary, tr);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[iSurvey] Bỏ qua ảnh — không đọc biên: {ex.Message}");
                continue;
            }

            if (ring.Count < 3 || OverlapArea(imgBox, BoundsOf(ring)) <= 0)
            {
                ed.WriteMessage("\n[iSurvey] Bỏ qua ảnh — biên không hợp lệ / không giao khung.");
                continue;
            }

            // forceOpenOnLockedLayer: ảnh trên layer khóa vẫn cắt được, không làm hỏng cả lô.
            var raster = (RasterImage)tr.GetObject(imageId, OpenMode.ForWrite, false, true);
            if (!RasterClipService.TryApplyClipPolygon(raster, ring))
            {
                ed.WriteMessage("\n[iSurvey] Bỏ qua ảnh — SetClipBoundary thất bại.");
                continue;
            }

            SendImageToBack(tr, db, imageId);
            usedPolys.Add(bestPoly.Value);
            ok++;
            ed.WriteMessage($"\n[iSurvey] Đã cắt 1 ảnh ({ring.Count} đỉnh, {(bestContained ? "biên trong ảnh" : "overlap")}).");
        }

        tr.Commit();
        ed.WriteMessage($"\n[iSurvey] Batch Clip hoàn tất: Đã cắt thành công {ok}/{total} tấm ảnh UAV.");
    }

    private static void RunAutoClip(Editor ed, Database db, List<ObjectId> listImages)
    {
        ed.WriteMessage("\n[iSurvey] Auto-Clip: đọc file trên đĩa, downsample, dò nền trắng/đen...");
        var ok = 0;
        var total = listImages.Count;

        using var tr = db.TransactionManager.StartTransaction();
        EnsureLayer(db, tr, AutoBorderLayer);

        foreach (var imageId in listImages)
        {
            var raster = (RasterImage)tr.GetObject(imageId, OpenMode.ForRead);
            if (raster.ImageDefId.IsNull)
            {
                ed.WriteMessage("\n[iSurvey] Bỏ qua ảnh — không có ImageDef.");
                continue;
            }

            var def = (RasterImageDef)tr.GetObject(raster.ImageDefId, OpenMode.ForRead);
            var path = ResolveImagePath(def);
            if (string.IsNullOrWhiteSpace(path))
            {
                ed.WriteMessage("\n[iSurvey] Bỏ qua ảnh — không có ActiveFileName (kiểm tra đường dẫn External Reference).");
                continue;
            }

            ed.WriteMessage($"\n[iSurvey] Đang phân tích: {Path.GetFileName(path!)} ...");
            var drawingRing = UavAutoClipService.TryBuildClipRingInFullPixels(
                path!, out var detail, out _, out var fullH);
            if (drawingRing is null)
            {
                ed.WriteMessage($"\n[iSurvey] Bỏ qua ảnh — {detail}");
                continue;
            }

            var toModel = raster.PixelToModelTransform;
            var yUp = UavAutoClipService.DetectCadPixelYIncreasesUp(toModel, raster.Orientation.Yaxis, fullH);
            var cadPixels = UavAutoClipService.DrawingPixelsToCadPixels(drawingRing, fullH, yUp);

            // Clip trực tiếp bằng pixel CAD — không round-trip WCS.
            raster = (RasterImage)tr.GetObject(imageId, OpenMode.ForWrite, false, true);
            if (!RasterClipService.TryApplyClipPolygonPixels(raster, cadPixels))
            {
                ed.WriteMessage("\n[iSurvey] Bỏ qua ảnh — SetClipBoundary thất bại.");
                continue;
            }

            var wcsRing = new List<Point2d>(cadPixels.Count);
            foreach (var px in cadPixels)
            {
                var w = new Point3d(px.X, px.Y, 0).TransformBy(toModel);
                wcsRing.Add(new Point2d(w.X, w.Y));
            }

            DrawBorderPolyline(tr, db, wcsRing);
            SendImageToBack(tr, db, imageId);
            ok++;
            ed.WriteMessage(
                $"\n[iSurvey] Auto-Clip 1 ảnh OK ({detail}, Y{(yUp ? "↑" : "↓")}-CAD).");
        }

        tr.Commit();
        ed.WriteMessage($"\n[iSurvey] Auto-Clip hoàn tất cho {ok}/{total} tấm ảnh.");
    }

    private static string? ResolveImagePath(RasterImageDef def)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(def.ActiveFileName) && File.Exists(def.ActiveFileName))
                return def.ActiveFileName;
        }
        catch
        {
            // ActiveFileName có thể ném khi unload
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(def.SourceFileName) && File.Exists(def.SourceFileName))
                return def.SourceFileName;
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static void EnsureLayer(Database db, Transaction tr, string name)
    {
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (lt.Has(name))
            return;
        lt.UpgradeOpen();
        var layer = new LayerTableRecord { Name = name };
        lt.Add(layer);
        tr.AddNewlyCreatedDBObject(layer, true);
    }

    private static void DrawBorderPolyline(Transaction tr, Database db, IReadOnlyList<Point2d> wcsRing)
    {
        if (wcsRing.Count < 3)
            return;

        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
        var pl = new Polyline(wcsRing.Count);
        for (var i = 0; i < wcsRing.Count; i++)
            pl.AddVertexAt(i, wcsRing[i], 0, 0, 0);
        pl.Closed = true;
        pl.Layer = AutoBorderLayer;
        pl.ColorIndex = 3; // xanh lá — dễ thấy biên auto
        ms.AppendEntity(pl);
        tr.AddNewlyCreatedDBObject(pl, true);
    }

    /// <summary>Tắt / bật khung RasterImage trong bản vẽ (IMAGEFRAME).</summary>
    public static void ToggleImageFrames(Editor ed, Database db)
    {
        using var tr = db.TransactionManager.StartTransaction();
        var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
        RasterVariables vars;

        if (nod.Contains("ACAD_IMAGE_VARS"))
        {
            vars = (RasterVariables)tr.GetObject(nod.GetAt("ACAD_IMAGE_VARS"), OpenMode.ForWrite);
        }
        else
        {
            vars = new RasterVariables();
            nod.UpgradeOpen();
            nod.SetAt("ACAD_IMAGE_VARS", vars);
            tr.AddNewlyCreatedDBObject(vars, true);
        }

        var nowOff = vars.ImageFrame == FrameSetting.ImageFrameOff;
        vars.ImageFrame = nowOff ? FrameSetting.ImageFrameAbove : FrameSetting.ImageFrameOff;
        tr.Commit();

        ed.WriteMessage(nowOff
            ? "\n[iSurvey] Đã bật khung ảnh (IMAGEFRAME)."
            : "\n[iSurvey] Đã tắt khung ảnh (IMAGEFRAME).");
    }

    /// <summary>Đưa một RasterImage xuống dưới cùng DrawOrder.</summary>
    public static void SendImageToBack(Editor ed, Database db)
    {
        var opts = new PromptEntityOptions("\n[iSurvey] Chọn ảnh Raster cần đưa xuống dưới: ")
        {
            AllowNone = false
        };
        opts.SetRejectMessage("\n[iSurvey] Chỉ chọn RasterImage.");
        opts.AddAllowedClass(typeof(RasterImage), exactMatch: true);

        var pick = ed.GetEntity(opts);
        if (pick.Status != PromptStatus.OK)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy.");
            return;
        }

        using var tr = db.TransactionManager.StartTransaction();
        SendImageToBack(tr, db, pick.ObjectId);
        tr.Commit();
        ed.WriteMessage("\n[iSurvey] Đã đưa ảnh xuống dưới cùng.");
    }

    // ---------- Smart Filter ----------

    private static List<ObjectId> ApplySmartFilter(
        Transaction tr,
        List<ObjectId> images,
        List<ObjectId> polylines,
        out string report)
    {
        var minImageArea = double.MaxValue;
        foreach (var id in images)
        {
            var raster = (RasterImage)tr.GetObject(id, OpenMode.ForRead);
            var a = BoxArea(ToExtents2d(raster.GeometricExtents));
            if (a > 0 && a < minImageArea)
                minImageArea = a;
        }

        if (minImageArea == double.MaxValue || minImageArea <= 0)
            minImageArea = 1;

        var areaThreshold = minImageArea * MinAreaRatio;
        var kept = new List<ObjectId>();
        var dropOpen = 0;
        var dropArea = 0;
        var keepLayer = 0;

        foreach (var id in polylines)
        {
            var ent = (Entity)tr.GetObject(id, OpenMode.ForRead);
            if (!IsClosedPolyline(ent))
            {
                dropOpen++;
                continue;
            }

            if (IsBorderLayer(ent.Layer))
            {
                kept.Add(id);
                keepLayer++;
                continue;
            }

            var area = TryPolylineArea(ent);
            if (area < areaThreshold)
            {
                dropArea++;
                continue;
            }

            kept.Add(id);
        }

        report = $"hở={dropOpen}, nhỏ={dropArea}, layer biên={keepLayer}";
        return kept;
    }

    private static bool IsClosedPolyline(Entity ent) =>
        ent switch
        {
            Polyline pl => pl.Closed || NearlyClosed(pl),
            Polyline2d p2 => p2.Closed,
            Polyline3d p3 => p3.Closed,
            _ => false
        };

    /// <summary>LWPOLYLINE đôi khi không bật flag Closed nhưng điểm đầu≈cuối.</summary>
    private static bool NearlyClosed(Polyline pl)
    {
        if (pl.NumberOfVertices < 3)
            return false;
        var a = pl.GetPoint2dAt(0);
        var b = pl.GetPoint2dAt(pl.NumberOfVertices - 1);
        return a.GetDistanceTo(b) <= CloseTol;
    }

    private static bool IsBorderLayer(string layer)
    {
        if (string.IsNullOrWhiteSpace(layer))
            return false;
        var name = layer.ToLowerInvariant();
        foreach (var hint in BorderLayerHints)
        {
            if (name.Contains(hint))
                return true;
        }

        return false;
    }

    private static double TryPolylineArea(Entity ent)
    {
        try
        {
            switch (ent)
            {
                case Polyline pl when pl.Closed || NearlyClosed(pl):
                    return Math.Abs(pl.Area);
                case Polyline2d p2 when p2.Closed:
                    return Math.Abs(p2.Area);
            }
        }
        catch
        {
            // fall through — dùng bbox
        }

        var box = ToExtents2d(ent.GeometricExtents);
        return BoxArea(box);
    }

    // ---------- Geometry helpers ----------

    private readonly record struct Extents2d(double MinX, double MinY, double MaxX, double MaxY);

    private static Extents2d ToExtents2d(Extents3d e) =>
        new(e.MinPoint.X, e.MinPoint.Y, e.MaxPoint.X, e.MaxPoint.Y);

    private static Extents2d BoundsOf(IReadOnlyList<Point2d> pts)
    {
        var minX = pts[0].X;
        var minY = pts[0].Y;
        var maxX = minX;
        var maxY = minY;
        for (var i = 1; i < pts.Count; i++)
        {
            var p = pts[i];
            if (p.X < minX) minX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }

        return new Extents2d(minX, minY, maxX, maxY);
    }

    private static double BoxArea(Extents2d b)
    {
        var w = b.MaxX - b.MinX;
        var h = b.MaxY - b.MinY;
        return w > 0 && h > 0 ? w * h : 0;
    }

    private static bool Contains(Extents2d outer, Extents2d inner) =>
        inner.MinX >= outer.MinX - 1e-3 &&
        inner.MinY >= outer.MinY - 1e-3 &&
        inner.MaxX <= outer.MaxX + 1e-3 &&
        inner.MaxY <= outer.MaxY + 1e-3;

    private static double OverlapArea(Extents2d a, Extents2d b)
    {
        var minX = Math.Max(a.MinX, b.MinX);
        var minY = Math.Max(a.MinY, b.MinY);
        var maxX = Math.Min(a.MaxX, b.MaxX);
        var maxY = Math.Min(a.MaxY, b.MaxY);
        var w = maxX - minX;
        var h = maxY - minY;
        return w > 0 && h > 0 ? w * h : 0;
    }

    private static void SendImageToBack(Transaction tr, Database db, ObjectId imageId)
    {
        // Dùng block chứa ảnh (Model hoặc Layout), không mặc định Model Space.
        var image = (Entity)tr.GetObject(imageId, OpenMode.ForRead);
        var owner = (BlockTableRecord)tr.GetObject(image.BlockId, OpenMode.ForRead);
        if (owner.DrawOrderTableId.IsNull)
            return;

        var order = (DrawOrderTable)tr.GetObject(owner.DrawOrderTableId, OpenMode.ForWrite);
        order.MoveToBottom(new ObjectIdCollection { imageId });
    }

    private static List<Point2d> ExtractBoundaryRing(Entity entity, Transaction tr)
    {
        return entity switch
        {
            Polyline pl => TessellatePolyline(pl),
            Polyline2d pl2d => TessellatePolyline2d(pl2d, tr),
            Polyline3d pl3d => ExtractPolyline3d(pl3d, tr),
            Circle circle => ApproximateCircle(circle, 64),
            AcadRegion region => ExtractRegionOutline(region),
            _ => throw new InvalidOperationException($"Không hỗ trợ: {entity.GetType().Name}")
        };
    }

    private static List<Point2d> TessellatePolyline(Polyline pl)
    {
        var n = pl.NumberOfVertices;
        if (n < 2)
            return [];

        var pts = new List<Point2d>();
        var closed = pl.Closed || NearlyClosed(pl);
        var segCount = closed ? n : n - 1;
        // Nếu NearlyClosed, đỉnh cuối trùng đầu — tránh đoạn 0 chiều ở vòng cuối.
        if (NearlyClosed(pl) && !pl.Closed)
            segCount = n - 1;

        for (var i = 0; i < segCount; i++)
        {
            var a = pl.GetPoint2dAt(i);
            var next = (i + 1) % n;
            var b = pl.GetPoint2dAt(next);
            AppendSegment(pts, a, b, pl.GetBulgeAt(i), includeEnd: false);
        }

        if (!closed && n >= 2)
            AppendSegment(pts, pl.GetPoint2dAt(n - 1), pl.GetPoint2dAt(0), 0, includeEnd: false);

        return DedupClose(pts);
    }

    private static List<Point2d> TessellatePolyline2d(Polyline2d pl2d, Transaction tr)
    {
        var verts = new List<Vertex2d>();
        foreach (ObjectId id in pl2d)
        {
            if (tr.GetObject(id, OpenMode.ForRead) is Vertex2d v)
                verts.Add(v);
        }

        if (verts.Count < 2)
            return [];

        var pts = new List<Point2d>();
        var n = verts.Count;
        var segCount = pl2d.Closed ? n : n - 1;

        for (var i = 0; i < segCount; i++)
        {
            var a = new Point2d(verts[i].Position.X, verts[i].Position.Y);
            var next = verts[(i + 1) % n];
            var b = new Point2d(next.Position.X, next.Position.Y);
            AppendSegment(pts, a, b, verts[i].Bulge, includeEnd: false);
        }

        if (!pl2d.Closed && n >= 2)
        {
            var last = verts[n - 1];
            var first = verts[0];
            AppendSegment(pts,
                new Point2d(last.Position.X, last.Position.Y),
                new Point2d(first.Position.X, first.Position.Y),
                0, includeEnd: false);
        }

        return DedupClose(pts);
    }

    private static List<Point2d> ExtractPolyline3d(Polyline3d pl3d, Transaction tr)
    {
        var pts = new List<Point2d>();
        foreach (ObjectId id in pl3d)
        {
            if (tr.GetObject(id, OpenMode.ForRead) is PolylineVertex3d vtx)
                pts.Add(new Point2d(vtx.Position.X, vtx.Position.Y));
        }

        return DedupClose(pts);
    }

    private static List<Point2d> ExtractRegionOutline(AcadRegion region)
    {
        var exploded = new DBObjectCollection();
        region.Explode(exploded);

        Entity? best = null;
        double bestArea = 0;
        foreach (DBObject obj in exploded)
        {
            if (obj is not Entity ent)
                continue;
            try
            {
                var ext = ent.GeometricExtents;
                var area = (ext.MaxPoint.X - ext.MinPoint.X) * (ext.MaxPoint.Y - ext.MinPoint.Y);
                if (area > bestArea)
                {
                    bestArea = area;
                    best = ent;
                }
            }
            catch
            {
                // bỏ qua
            }
        }

        if (best is null)
            throw new InvalidOperationException("Không trích được biên Region.");

        return best switch
        {
            Polyline pl => TessellatePolyline(pl),
            Circle circle => ApproximateCircle(circle, 64),
            _ => throw new InvalidOperationException("Region explode không cho Polyline/Circle.")
        };
    }

    private static List<Point2d> ApproximateCircle(Circle circle, int segments)
    {
        var pts = new List<Point2d>(segments);
        var c = circle.Center;
        var r = circle.Radius;
        for (var i = 0; i < segments; i++)
        {
            var ang = 2.0 * Math.PI * i / segments;
            pts.Add(new Point2d(c.X + r * Math.Cos(ang), c.Y + r * Math.Sin(ang)));
        }

        return pts;
    }

    private static void AppendSegment(List<Point2d> pts, Point2d a, Point2d b, double bulge, bool includeEnd)
    {
        if (Math.Abs(bulge) < 1e-12)
        {
            pts.Add(a);
            if (includeEnd)
                pts.Add(b);
            return;
        }

        var chord = a.GetDistanceTo(b);
        if (chord < 1e-9)
        {
            pts.Add(a);
            return;
        }

        var angle = 4.0 * Math.Atan(bulge);
        var steps = Math.Max(2, (int)Math.Ceiling(Math.Abs(angle) / ArcStepRad));
        for (var k = 0; k < steps; k++)
        {
            var t = (double)k / steps;
            pts.Add(PointOnBulge(a, b, bulge, t));
        }

        if (includeEnd)
            pts.Add(b);
    }

    private static Point2d PointOnBulge(Point2d a, Point2d b, double bulge, double t)
    {
        var theta = 4.0 * Math.Atan(bulge);
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var chord = Math.Sqrt(dx * dx + dy * dy);
        if (chord < 1e-12)
            return a;

        var s = Math.Sin(theta / 2.0);
        if (Math.Abs(s) < 1e-12)
            return new Point2d(a.X + dx * t, a.Y + dy * t);

        var radius = chord / (2.0 * s);
        var mx = (a.X + b.X) / 2.0;
        var my = (a.Y + b.Y) / 2.0;
        var sign = bulge > 0 ? 1.0 : -1.0;
        var nx = -dy / chord;
        var ny = dx / chord;
        var d = Math.Abs(radius) * Math.Cos(theta / 2.0);
        var cx = mx + sign * nx * d;
        var cy = my + sign * ny * d;

        var a0 = Math.Atan2(a.Y - cy, a.X - cx);
        var ang = a0 + theta * t;
        return new Point2d(cx + Math.Abs(radius) * Math.Cos(ang), cy + Math.Abs(radius) * Math.Sin(ang));
    }

    private static List<Point2d> DedupClose(List<Point2d> pts)
    {
        if (pts.Count == 0)
            return pts;

        var cleaned = new List<Point2d>(pts.Count) { pts[0] };
        for (var i = 1; i < pts.Count; i++)
        {
            if (cleaned[^1].GetDistanceTo(pts[i]) > CloseTol)
                cleaned.Add(pts[i]);
        }

        if (cleaned.Count > 1 && cleaned[0].GetDistanceTo(cleaned[^1]) < CloseTol)
            cleaned.RemoveAt(cleaned.Count - 1);

        return cleaned;
    }
}
