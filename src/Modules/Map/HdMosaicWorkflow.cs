using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using iSurvey.Auth;
using iSurvey.Core;
using iSurvey.Models;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Modules.Map;

/// <summary>
/// Ghép tile Google Satellite scale=2 thành một RasterImage, clip theo đường bao.
/// Ảnh này đứng yên: AutoRefresh của chế độ xem lướt không xóa và không thay nó.
/// </summary>
internal static class HdMosaicWorkflow
{
    public const int MaxTiles = 400;
    public const int MaxEdgePixels = 32768;
    public const int TilePixels = 512;
    public const int Scale = 2;
    public const string SourceId = "google-satellite";
    private const long MaxBitmapBytes = 600L * 1024 * 1024;

    public static void Run(Document doc)
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
        try
        {
            using var downloader = new TileDownloader(SourceId);
            var progress = new Progress<(int Done, int Total)>(p =>
            {
                if (p.Done % 24 == 0 || p.Done == p.Total)
                    ed.WriteMessage($"\n[iSurvey] ... {p.Done}/{p.Total}");
            });
            ready = downloader.EnsureTilesAsync(tiles, picked.Zoom, Scale, progress).GetAwaiter().GetResult();
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] Loi tai tile: {ex.Message}");
            Log("ghép HD, lỗi tải tile: " + ex.Message);
            return;
        }

        if (ready.Count == 0)
        {
            ed.WriteMessage("\n[iSurvey] Khong tai duoc tile nao — kiem tra mang.");
            Log("ghép HD, không tải được tile");
            return;
        }

        string mosaicPath;
        try
        {
            ed.WriteMessage("\n[iSurvey] Dang ghep anh...");
            mosaicPath = Stitch(ready, picked);
        }
        catch (OutOfMemoryException)
        {
            ed.WriteMessage("\n[iSurvey] Khong du bo nho de ghep anh. Chon zoom thap hon hoac duong bao nho hon.");
            Log($"ghép HD, hết bộ nhớ, zoom {picked.Zoom}, {tiles.Count} ô");
            return;
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] Loi ghep anh: {ex.Message}");
            Log("ghép HD, lỗi ghép: " + ex.Message);
            return;
        }

        try
        {
            Insert(db, coordinates, mosaicPath, picked, cm, zone, boundary.ClipPolygonWcs);
            ed.Regen();
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] Loi chen anh: {ex.Message}");
            Log($"ghép HD, lỗi chèn file {ActivityLogger.FileName(mosaicPath)}: {ex.Message}");
            return;
        }

        var missing = tiles.Count - ready.Count;
        var missNote = missing > 0 ? $" Thieu {missing}/{tiles.Count} o (vung den)." : "";
        ed.WriteMessage(
            $"\n[iSurvey] Da gan 1 anh HD zoom {picked.Zoom}, {TilePixels}px/o, {ready.Count} o, clip duong bao.{missNote} AutoRefresh khong doi anh nay.");
        Log($"ghép ảnh HD, file {ActivityLogger.FileName(mosaicPath)}, zoom {picked.Zoom}, {ready.Count}/{tiles.Count} ô, TM-{zone} kinh tuyến {cm:0.##}°");
    }

    private static int? PromptZoom(Editor ed, IReadOnlyList<ZoomChoice> choices)
    {
        var fallback = choices.FirstOrDefault(c => c.Zoom == 19 && c.Allowed)
                       ?? choices.First(c => c.Allowed);
        var fallbackKey = fallback.Zoom.ToString(CultureInfo.InvariantCulture);

        while (true)
        {
            var opts = new PromptKeywordOptions("\niSurvey: Chọn mức zoom")
            {
                AllowNone = true,
                AppendKeywordsToMessage = true
            };
            opts.Keywords.Add("18", "18", "Zoom 18");
            opts.Keywords.Add("19", "19", "Zoom 19");
            opts.Keywords.Add("20", "20", "Zoom 20");
            opts.Keywords.Default = fallbackKey;

            var res = ed.GetKeywords(opts);
            if (res.Status == PromptStatus.Cancel)
                return null;

            var key = res.Status == PromptStatus.None ? fallbackKey : res.StringResult;
            if (!int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var zoom))
                continue;

            var choice = choices.FirstOrDefault(c => c.Zoom == zoom);
            if (choice is null)
                continue;

            if (!choice.Allowed)
            {
                ed.WriteMessage("\n[iSurvey] " + choice.BlockReason);
                continue;
            }

            return zoom;
        }
    }

    private static ZoomChoice Describe(GeoBounds bounds, int zoom)
    {
        var (xMin, xMax, yMin, yMax) = TileMath.TileRangeForBounds(bounds, zoom);
        var cols = xMax - xMin + 1;
        var rows = yMax - yMin + 1;
        var tiles = cols * rows;
        var widthPx = (long)cols * TilePixels;
        var heightPx = (long)rows * TilePixels;
        var note = zoom switch
        {
            19 => " - Khuyen dung",
            20 => " - Rat cham",
            _ => ""
        };

        string? block = null;
        if (tiles > MaxTiles)
        {
            block = $"Zoom {zoom} co ~{tiles} o, vuot gioi han {MaxTiles}. Chon zoom thap hon hoac ve duong bao nho lai.";
        }
        else if (widthPx > MaxEdgePixels || heightPx > MaxEdgePixels)
        {
            var edge = Math.Max(widthPx, heightPx);
            block = $"Zoom {zoom} rong {edge} px, vuot {MaxEdgePixels} px mot canh. Chon zoom thap hon hoac ve duong bao hep lai.";
        }
        else if (widthPx * heightPx * 3 > MaxBitmapBytes)
        {
            block = $"Zoom {zoom} can qua nhieu bo nho. Chon zoom thap hon hoac ve duong bao nho lai.";
        }

        var extra = block is null ? "" : " — vuot gioi han";
        return new ZoomChoice
        {
            Zoom = zoom,
            Tiles = tiles,
            Cols = cols,
            Rows = rows,
            XMin = xMin,
            YMin = yMin,
            XMax = xMax,
            YMax = yMax,
            Allowed = block is null,
            BlockReason = block ?? "",
            Line = $"Zoom {zoom} (Du kien: ~{tiles} o{note}){extra}"
        };
    }

    private static string Stitch(IReadOnlyList<(int X, int Y, int Z, string Path)> ready, ZoomChoice choice)
    {
        var width = choice.Cols * TilePixels;
        var height = choice.Rows * TilePixels;
        var byCell = new Dictionary<(int X, int Y), string>(ready.Count);
        foreach (var tile in ready)
            byCell[(tile.X, tile.Y)] = tile.Path;

        Bitmap? bitmap = null;
        try
        {
            bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.PixelOffsetMode = PixelOffsetMode.None;
                graphics.SmoothingMode = SmoothingMode.None;
                graphics.Clear(Color.Black);

                for (var row = 0; row < choice.Rows; row++)
                {
                    for (var col = 0; col < choice.Cols; col++)
                    {
                        var x = choice.XMin + col;
                        var y = choice.YMin + row;
                        if (!byCell.TryGetValue((x, y), out var path))
                            continue;

                        DrawTile(graphics, path, col * TilePixels, row * TilePixels);
                    }
                }
            }

            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "iSurvey",
                "mosaic");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, "hd_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".jpg");
            SaveJpeg(bitmap, file);
            return file;
        }
        finally
        {
            bitmap?.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    private static void DrawTile(Graphics graphics, string path, int destX, int destY)
    {
        var bytes = File.ReadAllBytes(path);
        using var stream = new MemoryStream(bytes, writable: false);
        using var image = System.Drawing.Image.FromStream(stream, false, false);
        var dest = new Rectangle(destX, destY, TilePixels, TilePixels);
        if (image.Width == TilePixels && image.Height == TilePixels)
            graphics.DrawImageUnscaled(image, destX, destY);
        else
            graphics.DrawImage(image, dest, new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
    }

    private static void SaveJpeg(Bitmap bitmap, string path)
    {
        var encoder = ImageCodecInfo.GetImageEncoders()
            .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
        if (encoder is null)
        {
            bitmap.Save(path, ImageFormat.Jpeg);
            return;
        }

        using var parameters = new EncoderParameters(1);
        parameters.Param[0] = new EncoderParameter(Encoder.Quality, 93L);
        bitmap.Save(path, encoder, parameters);
    }

    private static void Insert(
        Database db,
        CoordinateService coordinates,
        string imagePath,
        ZoomChoice choice,
        double centralMeridian,
        int zone,
        IReadOnlyList<Point2d> clipPolygonWcs)
    {
        var nw = coordinates.ToVn2000(TileMath.TileTopLeft(choice.XMin, choice.YMin, choice.Zoom), centralMeridian, zone);
        var sw = coordinates.ToVn2000(TileMath.TileTopLeft(choice.XMin, choice.YMax + 1, choice.Zoom), centralMeridian, zone);
        var se = coordinates.ToVn2000(TileMath.TileTopLeft(choice.XMax + 1, choice.YMax + 1, choice.Zoom), centralMeridian, zone);

        var origin = new Point3d(sw.Easting, sw.Northing, 0);
        var xAxis = new Vector3d(se.Easting - sw.Easting, se.Northing - sw.Northing, 0);
        var yAxis = new Vector3d(nw.Easting - sw.Easting, nw.Northing - sw.Northing, 0);
        if (xAxis.Length < 1e-6 || yAxis.Length < 1e-6)
            throw new InvalidOperationException("Pham vi anh HD qua nho de gan toa do.");

        using var docLock = AcadApp.DocumentManager.MdiActiveDocument?.LockDocument();
        using var tr = db.TransactionManager.StartTransaction();

        RasterDisplayHelper.HideImageFrames(db, tr);

        var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
        var imgDictId = nod.Contains("ACAD_IMAGE_DICT")
            ? nod.GetAt("ACAD_IMAGE_DICT")
            : RasterImageDef.CreateImageDictionary(db);
        var imgDict = (DBDictionary)tr.GetObject(imgDictId, OpenMode.ForWrite);
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

        var defName = SurveyRasterHelper.HdDefPrefix
                      + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        if (imgDict.Contains(defName))
            defName += "_" + Guid.NewGuid().ToString("N").Substring(0, 4);

        var rasterDef = new RasterImageDef { SourceFileName = imagePath };
        rasterDef.Load();
        imgDict.SetAt(defName, rasterDef);
        tr.AddNewlyCreatedDBObject(rasterDef, true);

        var raster = new RasterImage
        {
            ImageDefId = rasterDef.ObjectId,
            Orientation = new CoordinateSystem3d(origin, xAxis, yAxis),
            ShowImage = true,
            ImageTransparency = false
        };

        var imageId = ms.AppendEntity(raster);
        tr.AddNewlyCreatedDBObject(raster, true);
        raster.AssociateRasterDef(rasterDef);

        if (!RasterClipService.TryApplyClip(raster, clipPolygonWcs))
            throw new InvalidOperationException("Khong clip duoc anh HD theo duong bao.");

        if (!ms.DrawOrderTableId.IsNull)
        {
            var order = (DrawOrderTable)tr.GetObject(ms.DrawOrderTableId, OpenMode.ForWrite);
            order.MoveToBottom(new ObjectIdCollection { imageId });
        }

        tr.Commit();
    }

    private sealed class ZoomChoice
    {
        public int Zoom { get; init; }
        public int Tiles { get; init; }
        public int Cols { get; init; }
        public int Rows { get; init; }
        public int XMin { get; init; }
        public int YMin { get; init; }
        public int XMax { get; init; }
        public int YMax { get; init; }
        public bool Allowed { get; init; }
        public string BlockReason { get; init; } = "";
        public string Line { get; init; } = "";
    }
}
