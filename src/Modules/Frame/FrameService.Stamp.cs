using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace iSurvey.Modules.Frame;

public sealed partial class FrameService
{
    /// <summary>Ảnh mộc đỏ gắn tạm để in PDF. Gỡ trước khi kết thúc transaction thì bản vẽ không giữ dấu.</summary>
    internal readonly record struct PlotStamp(ObjectId ImageDef, List<ObjectId> Images);

    /// <summary>Đường kính vòng mực đỏ. Dấu tròn doanh nghiệp thông thường là 36 mm (Thông tư 05/2005/TT-BNV).</summary>
    private const double StampDiameterMm = 36;

    /// <summary>
    /// Đặt vòng mực đỏ 36 mm, tâm vòng trùng giao đường đứng và hàng TEN_1.
    /// Ảnh PNG có thể thừa lề trong suốt; căn theo vòng mực, không căn theo khung file.
    /// </summary>
    internal static PlotStamp PlacePlotStamps(
        Transaction tr, Database db, ObjectId spaceId, string path, IReadOnlyList<Extents2d> windows)
    {
        var images = new List<ObjectId>();
        if (windows.Count == 0 || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new PlotStamp(ObjectId.Null, images);

        if (!TryStampPixels(path, out var pixels))
            return new PlotStamp(ObjectId.Null, images);

        var space = (BlockTableRecord)tr.GetObject(spaceId, OpenMode.ForRead);
        var frames = new List<BlockReference>();
        foreach (ObjectId id in space)
        {
            if (!TryResolveFrame(tr, id, out var frame))
                continue;
            if (!TryExtents(frame, out var ext))
                continue;
            var mid = new Point3d(
                (ext.MinPoint.X + ext.MaxPoint.X) / 2,
                (ext.MinPoint.Y + ext.MaxPoint.Y) / 2,
                0);
            if (HitWindow(mid, windows))
                frames.Add(frame);
        }

        if (frames.Count == 0)
            return new PlotStamp(ObjectId.Null, images);

        var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
        var imgDictId = nod.Contains("ACAD_IMAGE_DICT")
            ? nod.GetAt("ACAD_IMAGE_DICT")
            : RasterImageDef.CreateImageDictionary(db);
        var imgDict = (DBDictionary)tr.GetObject(imgDictId, OpenMode.ForWrite);
        var defName = "ISURVEY_MOCDO_" + DateTime.Now.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
        if (imgDict.Contains(defName))
            defName += "_" + Guid.NewGuid().ToString("N").Substring(0, 4);

        var def = new RasterImageDef { SourceFileName = path };
        def.Load();
        imgDict.SetAt(defName, def);
        tr.AddNewlyCreatedDBObject(def, true);
        RasterImage.EnableReactors(true);

        if (!space.IsWriteEnabled)
            space.UpgradeOpen();

        foreach (var frame in frames)
        {
            if (!TryStampCenter(tr, frame, out var center))
                continue;
            var origin = new Point3d(
                center.X - pixels.Fx * pixels.WidthMm,
                center.Y - (1.0 - pixels.Fy) * pixels.HeightMm,
                center.Z);
            var raster = new RasterImage
            {
                ImageDefId = def.ObjectId,
                Orientation = new CoordinateSystem3d(origin, new Vector3d(pixels.WidthMm, 0, 0), new Vector3d(0, pixels.HeightMm, 0)),
                ShowImage = true,
                ImageTransparency = true,
                Layer = "0"
            };
            space.AppendEntity(raster);
            tr.AddNewlyCreatedDBObject(raster, true);
            raster.AssociateRasterDef(def);
            images.Add(raster.ObjectId);
        }

        if (images.Count == 0)
        {
            if (!def.IsWriteEnabled)
                def.UpgradeOpen();
            def.Erase();
            return new PlotStamp(ObjectId.Null, images);
        }

        return new PlotStamp(def.ObjectId, images);
    }

    internal static bool ErasePlotStamps(Transaction tr, PlotStamp stamp)
    {
        var ok = true;
        foreach (var id in stamp.Images)
            ok &= EraseIfLive(tr, id);
        ok &= EraseIfLive(tr, stamp.ImageDef);
        return ok;
    }

    /// <summary>Gỡ dấu còn sót nếu lần xóa trong transaction in không kịp.</summary>
    internal static void RemoveLeftoverStamps(Database db)
    {
        using var tr = db.TransactionManager.StartTransaction();
        var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
        if (!nod.Contains("ACAD_IMAGE_DICT"))
        {
            tr.Commit();
            return;
        }

        var imgDict = (DBDictionary)tr.GetObject(nod.GetAt("ACAD_IMAGE_DICT"), OpenMode.ForRead);
        var defIds = new List<ObjectId>();
        foreach (DBDictionaryEntry entry in imgDict)
        {
            if (entry.Key.StartsWith("ISURVEY_MOCDO_", StringComparison.OrdinalIgnoreCase))
                defIds.Add(entry.Value);
        }

        if (defIds.Count == 0)
        {
            tr.Commit();
            return;
        }

        var defSet = new HashSet<ObjectId>(defIds);
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var images = new List<ObjectId>();
        foreach (ObjectId btrId in bt)
        {
            var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
            if (!btr.IsLayout)
                continue;
            foreach (ObjectId id in btr)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is RasterImage ri && defSet.Contains(ri.ImageDefId))
                    images.Add(id);
            }
        }

        foreach (var id in images)
            EraseIfLive(tr, id);
        foreach (var id in defIds)
            EraseIfLive(tr, id);
        tr.Commit();
    }

    private static bool EraseIfLive(Transaction tr, ObjectId id)
    {
        if (id.IsNull || !id.IsValid)
            return true;
        try
        {
            var obj = tr.GetObject(id, OpenMode.ForWrite, false, true);
            if (!obj.IsErased)
                obj.Erase();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Khung file PNG và tâm vòng mực (tỉ lệ 0..1, gốc ảnh trên-trái).</summary>
    private readonly record struct StampPixels(double WidthMm, double HeightMm, double Fx, double Fy);

    /// <summary>Đo vòng mực để vòng đỏ rộng 36 mm và tâm vòng trùng điểm chèn.</summary>
    private static bool TryStampPixels(string path, out StampPixels pixels)
    {
        pixels = default;
        try
        {
            using var bmp = new System.Drawing.Bitmap(path);
            var minX = bmp.Width;
            var minY = bmp.Height;
            var maxX = 0;
            var maxY = 0;
            var found = false;
            for (var y = 0; y < bmp.Height; y += 2)
            {
                for (var x = 0; x < bmp.Width; x += 2)
                {
                    var c = bmp.GetPixel(x, y);
                    if (c.A < 30)
                        continue;
                    if (c.R > 245 && c.G > 245 && c.B > 245)
                        continue;
                    found = true;
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }

            if (!found || maxX <= minX || maxY <= minY)
                return false;

            var inkW = maxX - minX;
            var inkH = maxY - minY;
            double widthMm;
            double heightMm;
            if (inkW >= inkH)
            {
                widthMm = StampDiameterMm * bmp.Width / inkW;
                heightMm = widthMm * bmp.Height / bmp.Width;
            }
            else
            {
                heightMm = StampDiameterMm * bmp.Height / inkH;
                widthMm = heightMm * bmp.Width / bmp.Height;
            }

            pixels = new StampPixels(
                widthMm,
                heightMm,
                (minX + maxX) / 2.0 / bmp.Width,
                (minY + maxY) / 2.0 / bmp.Height);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tâm dấu là giao của đường đứng (cạnh ô ký) với tâm hàng polyline chứa TEN_1.
    /// Không lấy tâm ô trống bên phải, vì dấu sẽ lệch khỏi nét đứng.
    /// </summary>
    private static bool TryStampCenter(Transaction tr, BlockReference frame, out Point3d center)
    {
        center = Point3d.Origin;
        AttributeReference? name = null;
        foreach (ObjectId aid in frame.AttributeCollection)
        {
            if (tr.GetObject(aid, OpenMode.ForRead) is not AttributeReference ar)
                continue;
            if (!ar.Tag.Equals("TEN_1", StringComparison.OrdinalIgnoreCase))
                continue;
            name = ar;
            break;
        }

        if (name is null)
            return false;

        var paper = name.AlignmentPoint;
        if (paper.DistanceTo(Point3d.Origin) < 1e-6)
            paper = name.Position;
        var local = paper.TransformBy(frame.BlockTransform.Inverse());
        var def = (BlockTableRecord)tr.GetObject(frame.BlockTableRecord, OpenMode.ForRead);
        if (!TryNameRow(tr, def, local, out var row, out var splitX))
        {
            center = paper;
            return true;
        }

        var y = (row.MinPoint.Y + row.MaxPoint.Y) / 2.0;
        center = new Point3d(splitX, y, 0).TransformBy(frame.BlockTransform);
        return true;
    }

    private static bool TryNameRow(Transaction tr, BlockTableRecord def, Point3d local, out Extents3d row, out double splitX)
    {
        row = new Extents3d();
        splitX = local.X;
        Extents3d? best = null;
        var bestArea = double.MaxValue;
        var dividers = new List<(double X, double Y0, double Y1)>();
        foreach (ObjectId id in def)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead);
            switch (ent)
            {
                case Polyline pl when pl.Closed:
                {
                    Extents3d ext;
                    try
                    {
                        ext = pl.GeometricExtents;
                    }
                    catch
                    {
                        continue;
                    }

                    if (local.X < ext.MinPoint.X - 0.5 || local.X > ext.MaxPoint.X + 0.5
                        || local.Y < ext.MinPoint.Y - 0.5 || local.Y > ext.MaxPoint.Y + 0.5)
                        continue;
                    var area = (ext.MaxPoint.X - ext.MinPoint.X) * (ext.MaxPoint.Y - ext.MinPoint.Y);
                    if (area < 4 || area >= bestArea)
                        continue;
                    bestArea = area;
                    best = ext;
                    break;
                }
                case Line ln when Math.Abs(ln.StartPoint.X - ln.EndPoint.X) <= 0.2:
                {
                    var y0 = Math.Min(ln.StartPoint.Y, ln.EndPoint.Y);
                    var y1 = Math.Max(ln.StartPoint.Y, ln.EndPoint.Y);
                    if (y1 - y0 < 4)
                        continue;
                    dividers.Add(((ln.StartPoint.X + ln.EndPoint.X) / 2.0, y0, y1));
                    break;
                }
                case Polyline open when !open.Closed:
                {
                    Extents3d ext;
                    try
                    {
                        ext = open.GeometricExtents;
                    }
                    catch
                    {
                        continue;
                    }

                    if (ext.MaxPoint.X - ext.MinPoint.X > 0.2 || ext.MaxPoint.Y - ext.MinPoint.Y < 4)
                        continue;
                    dividers.Add(((ext.MinPoint.X + ext.MaxPoint.X) / 2.0, ext.MinPoint.Y, ext.MaxPoint.Y));
                    break;
                }
            }
        }

        if (best is null)
            return false;
        row = best.Value;
        double? split = null;
        foreach (var d in dividers)
        {
            if (d.X < local.X + 1.0 || d.X > row.MaxPoint.X - 0.5)
                continue;
            var overlap = Math.Min(d.Y1, row.MaxPoint.Y) - Math.Max(d.Y0, row.MinPoint.Y);
            if (overlap < (row.MaxPoint.Y - row.MinPoint.Y) * 0.5)
                continue;
            if (split is null || d.X < split)
                split = d.X;
        }

        if (split is null)
            return false;
        splitX = split.Value;
        return true;
    }

    private static bool HitWindow(Point3d p, IReadOnlyList<Extents2d> windows)
    {
        foreach (var w in windows)
        {
            if (p.X >= w.MinPoint.X - 1 && p.X <= w.MaxPoint.X + 1
                && p.Y >= w.MinPoint.Y - 1 && p.Y <= w.MaxPoint.Y + 1)
                return true;
        }

        return false;
    }
}
