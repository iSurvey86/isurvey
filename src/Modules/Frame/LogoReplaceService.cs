using System.Globalization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using iSurvey.Modules.Map;

namespace iSurvey.Modules.Frame;

/// <summary>
/// THAYLOGO: thay logo trong khung tên bằng một file ảnh.
/// Logo EVN gốc (block KB_LOGO_EVN) không bị xóa: được cất sang layer tắt KB_LOGO_CDT_GOC / KB_LOGO_TK_GOC để khôi phục.
/// Ảnh mới là RasterImage đặt trong định nghĩa khối khung (KB_A0…KB_A4), vừa khít ô logo cũ, cùng layer với logo
/// (KB_LOGO_CDT / KB_LOGO_TK) nên ANLOGO1 / ANLOGO2 vẫn ẩn hiện được.
/// </summary>
internal static class LogoReplaceService
{
    private const string DefPrefix = "ISURVEY_LOGO_";

    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff", ".gif"];

    internal const string ImageFilter =
        "Ảnh logo (*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.gif";

    internal sealed record PreparedImage(string Path, int Width, int Height, string Note);

    internal sealed record ReplaceResult(int Frames, int Logos, string Message);

    /// <summary>Kiểm tra ảnh, đọc kích thước, đặt bản sao ổn định (cạnh bản vẽ đã lưu, hoặc %AppData%\iSurvey\logos).</summary>
    public static PreparedImage PrepareImage(Database db, string source, bool owner)
    {
        source = source.Trim().Trim('"');
        if (!File.Exists(source))
            throw new InvalidOperationException("Không tìm thấy file ảnh: " + source);

        var ext = Path.GetExtension(source).ToLowerInvariant();
        if (Array.IndexOf(ImageExtensions, ext) < 0)
            throw new InvalidOperationException("Định dạng ảnh chưa hỗ trợ (" + ext + "). Dùng PNG, JPG, BMP, TIF hoặc GIF.");

        int w, h;
        try
        {
            using var img = System.Drawing.Image.FromFile(source);
            w = img.Width;
            h = img.Height;
        }
        catch (System.Exception ex)
        {
            throw new InvalidOperationException("Không đọc được ảnh: " + ex.Message);
        }

        if (w < 4 || h < 4)
            throw new InvalidOperationException("Ảnh quá nhỏ để làm logo.");

        string? drawingDir = null;
        try
        {
            if (!string.IsNullOrEmpty(db.Filename) && File.Exists(db.Filename))
                drawingDir = Path.GetDirectoryName(db.Filename);
        }
        catch
        {
            // bản vẽ chưa lưu
        }

        var srcDir = Path.GetDirectoryName(Path.GetFullPath(source));
        if (drawingDir is not null && string.Equals(srcDir, drawingDir, StringComparison.OrdinalIgnoreCase))
            return new PreparedImage(Path.GetFullPath(source), w, h, "dùng file cạnh bản vẽ");

        var stamp = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var name = $"iSurvey_logo_{(owner ? "cdt" : "tk")}_{stamp}{ext}";

        var targets = new List<string>();
        if (drawingDir is not null)
            targets.Add(drawingDir);
        targets.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "iSurvey", "logos"));

        foreach (var dir in targets)
        {
            try
            {
                Directory.CreateDirectory(dir);
                var dest = Path.Combine(dir, name);
                File.Copy(source, dest, overwrite: false);
                var where = dir == drawingDir ? "cạnh bản vẽ" : "trong %AppData%\\iSurvey\\logos";
                return new PreparedImage(dest, w, h, "đã chép ảnh " + where);
            }
            catch
            {
                // thử thư mục kế tiếp
            }
        }

        return new PreparedImage(Path.GetFullPath(source), w, h, "dùng nguyên file gốc (không chép được)");
    }

    /// <summary>Layer của logo ứng với bên chọn (kể cả khi logo gốc đang được cất): true = chủ đầu tư.</summary>
    public static bool? SideOfLayer(string layer)
    {
        if (layer.Equals(TitleBlockFit.LogoOwnerLayer, StringComparison.OrdinalIgnoreCase)
            || layer.Equals(TitleBlockFit.LogoOwnerOrigLayer, StringComparison.OrdinalIgnoreCase))
            return true;
        if (layer.Equals(TitleBlockFit.LogoDesignLayer, StringComparison.OrdinalIgnoreCase)
            || layer.Equals(TitleBlockFit.LogoDesignOrigLayer, StringComparison.OrdinalIgnoreCase))
            return false;
        return null;
    }

    /// <summary>Thay logo bên <paramref name="owner"/> bằng ảnh. Gọi sau khi đã mở transaction và khóa tài liệu.</summary>
    public static ReplaceResult Replace(Transaction tr, Database db, bool owner, PreparedImage image)
    {
        var split = TitleBlockFit.SeparateLogos(tr, db);
        if (split.Count == 0)
            return new ReplaceResult(0, 0, "Bản vẽ này không có logo EVN trong khung tên (chèn khung bằng KHUNG trước).");

        // Thay lần hai: trả logo gốc về trước để lấy lại đúng ô logo, rồi mới đặt ảnh mới.
        Restore(tr, db, owner);

        var sideLayer = owner ? TitleBlockFit.LogoOwnerLayer : TitleBlockFit.LogoDesignLayer;
        var origLayer = owner ? TitleBlockFit.LogoOwnerOrigLayer : TitleBlockFit.LogoDesignOrigLayer;
        TitleBlockFit.EnsureLogoLayer(tr, db, origLayer);
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        var orig = (LayerTableRecord)tr.GetObject(lt[origLayer], OpenMode.ForWrite);
        orig.IsOff = true;
        orig.IsPlottable = false;
        var side = (LayerTableRecord)tr.GetObject(lt[sideLayer], OpenMode.ForWrite);
        side.IsOff = false;
        side.IsFrozen = false;

        // Gom ô logo (hộp bao) của từng khung trước, rồi mới sửa — không sửa khi đang duyệt block.
        var targets = new List<(ObjectId BlockId, ObjectId LogoId, Extents3d Box)>();
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        foreach (ObjectId btrId in bt)
        {
            var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
            if (btr.Name.Equals(TitleBlockFit.LogoBlock, StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (ObjectId id in btr)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is BlockReference br
                    && TitleBlockFit.IsLogo(tr, br)
                    && br.Layer.Equals(sideLayer, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        targets.Add((btrId, id, br.GeometricExtents));
                    }
                    catch
                    {
                        // logo không có hình học: bỏ qua
                    }
                }
            }
        }

        if (targets.Count == 0)
            return new ReplaceResult(0, 0, "Không tìm thấy ô logo để thay.");

        RasterDisplayHelper.HideImageFrames(db, tr);

        var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
        var imgDictId = nod.Contains("ACAD_IMAGE_DICT")
            ? nod.GetAt("ACAD_IMAGE_DICT")
            : RasterImageDef.CreateImageDictionary(db);
        var imgDict = (DBDictionary)tr.GetObject(imgDictId, OpenMode.ForWrite);

        var defName = DefPrefix + (owner ? "CDT_" : "TK_")
                      + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        if (imgDict.Contains(defName))
            defName += "_" + Guid.NewGuid().ToString("N").Substring(0, 4);

        var def = new RasterImageDef { SourceFileName = image.Path };
        def.Load();
        imgDict.SetAt(defName, def);
        tr.AddNewlyCreatedDBObject(def, true);

        RasterImage.EnableReactors(true);

        var frames = new HashSet<ObjectId>();
        foreach (var t in targets)
        {
            var box = t.Box;
            var bw = box.MaxPoint.X - box.MinPoint.X;
            var bh = box.MaxPoint.Y - box.MinPoint.Y;
            if (bw < 1e-9 || bh < 1e-9)
                continue;

            // Vừa khít ô logo cũ, giữ tỷ lệ ảnh, căn giữa.
            var k = Math.Min(bw / image.Width, bh / image.Height);
            var iw = image.Width * k;
            var ih = image.Height * k;
            var cx = (box.MinPoint.X + box.MaxPoint.X) / 2.0;
            var cy = (box.MinPoint.Y + box.MaxPoint.Y) / 2.0;
            var origin = new Point3d(cx - iw / 2.0, cy - ih / 2.0, box.MinPoint.Z);

            var logo = (BlockReference)tr.GetObject(t.LogoId, OpenMode.ForWrite);
            logo.Layer = origLayer;

            var owner2 = (BlockTableRecord)tr.GetObject(t.BlockId, OpenMode.ForWrite);
            var raster = new RasterImage
            {
                ImageDefId = def.ObjectId,
                Orientation = new CoordinateSystem3d(origin, new Vector3d(iw, 0, 0), new Vector3d(0, ih, 0)),
                ShowImage = true,
                ImageTransparency = true,
                Layer = sideLayer
            };

            owner2.AppendEntity(raster);
            tr.AddNewlyCreatedDBObject(raster, true);
            raster.AssociateRasterDef(def);
            frames.Add(t.BlockId);
        }

        var label = owner ? "logo chủ đầu tư" : "logo đơn vị thiết kế";
        return new ReplaceResult(frames.Count, targets.Count,
            $"Đã thay {label} bằng {Path.GetFileName(image.Path)} ({targets.Count} chỗ trong {frames.Count} khung; {image.Note})");
    }

    /// <summary>Gỡ ảnh logo, trả logo EVN gốc về. owner: true / false = một bên. Trả về số chỗ đã khôi phục.</summary>
    public static int Restore(Transaction tr, Database db, bool owner)
    {
        var sideLayer = owner ? TitleBlockFit.LogoOwnerLayer : TitleBlockFit.LogoDesignLayer;
        var origLayer = owner ? TitleBlockFit.LogoOwnerOrigLayer : TitleBlockFit.LogoDesignOrigLayer;
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

        var toErase = new List<ObjectId>();
        var toRestore = new List<ObjectId>();
        foreach (ObjectId btrId in bt)
        {
            var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
            foreach (ObjectId id in btr)
            {
                var obj = tr.GetObject(id, OpenMode.ForRead);
                if (obj is RasterImage ri && ri.Layer.Equals(sideLayer, StringComparison.OrdinalIgnoreCase))
                    toErase.Add(id);
                else if (obj is BlockReference br && TitleBlockFit.IsLogo(tr, br)
                         && br.Layer.Equals(origLayer, StringComparison.OrdinalIgnoreCase))
                    toRestore.Add(id);
            }
        }

        foreach (var id in toErase)
            tr.GetObject(id, OpenMode.ForWrite).Erase();

        if (toRestore.Count > 0)
        {
            TitleBlockFit.EnsureLogoLayer(tr, db, sideLayer);
            foreach (var id in toRestore)
                ((BlockReference)tr.GetObject(id, OpenMode.ForWrite)).Layer = sideLayer;

            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            var side = (LayerTableRecord)tr.GetObject(lt[sideLayer], OpenMode.ForWrite);
            side.IsOff = false;
            side.IsFrozen = false;
        }

        if (toErase.Count > 0)
            EraseUnusedDefs(tr, db, owner);

        return toRestore.Count;
    }

    /// <summary>Xóa định nghĩa ảnh logo cũ của bên này (không còn ảnh nào dùng). Lỗi thì bỏ qua, PURGE dọn sau.</summary>
    private static void EraseUnusedDefs(Transaction tr, Database db, bool owner)
    {
        try
        {
            var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
            if (!nod.Contains("ACAD_IMAGE_DICT"))
                return;

            var dict = (DBDictionary)tr.GetObject(nod.GetAt("ACAD_IMAGE_DICT"), OpenMode.ForWrite);
            var prefix = DefPrefix + (owner ? "CDT_" : "TK_");
            var names = new List<string>();
            foreach (var entry in dict)
            {
                if (entry.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    names.Add(entry.Key);
            }

            foreach (var name in names)
            {
                try
                {
                    var def = (RasterImageDef)tr.GetObject(dict.GetAt(name), OpenMode.ForWrite);
                    def.Erase();
                    dict.Remove(name);
                }
                catch
                {
                    // còn được tham chiếu: để PURGE
                }
            }
        }
        catch
        {
            // không quan trọng
        }
    }

    /// <summary>Ảnh đã chuẩn, không chép thêm bản mới.</summary>
    public static PreparedImage PreparedFromFile(string path)
    {
        using var img = System.Drawing.Image.FromFile(path);
        return new PreparedImage(path, img.Width, img.Height, "logo đã chèn trong bản vẽ");
    }

    /// <summary>Còn khung nào bên này vẫn là logo EVN, chưa có ảnh thay.</summary>
    public static bool HasUnreplacedLogo(Database db, bool owner)
    {
        using var tr = db.TransactionManager.StartTransaction();
        var sideLayer = owner ? TitleBlockFit.LogoOwnerLayer : TitleBlockFit.LogoDesignLayer;
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        foreach (ObjectId btrId in bt)
        {
            var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
            if (btr.IsLayout || btr.Name.Equals(TitleBlockFit.LogoBlock, StringComparison.OrdinalIgnoreCase))
                continue;
            var unreplaced = false;
            var hasRaster = false;
            foreach (ObjectId id in btr)
            {
                switch (tr.GetObject(id, OpenMode.ForRead))
                {
                    case BlockReference br when TitleBlockFit.IsLogo(tr, br) && !TitleBlockFit.IsStoredOriginalLayer(br.Layer):
                        if (TitleBlockFit.SideOfLogo(tr, br) == owner)
                            unreplaced = true;
                        break;
                    case RasterImage ri when ri.Layer.Equals(sideLayer, StringComparison.OrdinalIgnoreCase):
                        hasRaster = true;
                        break;
                }
            }

            if (unreplaced && !hasRaster)
                return true;
        }

        return false;
    }

    /// <summary>Đường dẫn ảnh logo đang gắn trong khung, nếu file còn trên đĩa.</summary>
    public static string? AppliedImagePath(Database db, bool owner)
    {
        using var tr = db.TransactionManager.StartTransaction();
        var sideLayer = owner ? TitleBlockFit.LogoOwnerLayer : TitleBlockFit.LogoDesignLayer;
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        foreach (ObjectId btrId in bt)
        {
            var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
            if (btr.IsLayout)
                continue;
            foreach (ObjectId id in btr)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is not RasterImage ri)
                    continue;
                if (!ri.Layer.Equals(sideLayer, StringComparison.OrdinalIgnoreCase) || ri.ImageDefId.IsNull)
                    continue;
                if (tr.GetObject(ri.ImageDefId, OpenMode.ForRead) is not RasterImageDef def)
                    continue;
                var path = def.SourceFileName;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    return path;
            }
        }

        return null;
    }
}
