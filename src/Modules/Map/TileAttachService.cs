using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using iSurvey.Models;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Modules.Map;

/// <summary>Gắn từng tile 256×256 như RasterImage georeferenced.</summary>
public static class TileAttachService
{
    private const string ImageDictName = "ACAD_IMAGE_DICT";
    private const string DefPrefix = "ISURVEY_";

    public sealed class AttachBatchResult
    {
        public int Attached { get; init; }
        public int Failed { get; init; }
    }

    public static AttachBatchResult AttachTiles(
        Database db,
        string sourceId,
        IEnumerable<(int X, int Y, int Z, string ImagePath)> tiles,
        CoordinateService coordinates,
        double centralMeridian,
        IReadOnlyList<Point2d>? clipPolygonWcs = null)
    {
        var attached = 0;
        var failed = 0;

        using var docLock = AcadApp.DocumentManager.MdiActiveDocument?.LockDocument();

        using var tr = db.TransactionManager.StartTransaction();
        var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
        var imgDictId = nod.Contains(ImageDictName)
            ? nod.GetAt(ImageDictName)
            : RasterImageDef.CreateImageDictionary(db);
        var imgDict = (DBDictionary)tr.GetObject(imgDictId, OpenMode.ForWrite);

        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

        RasterDisplayHelper.HideImageFrames(db, tr);

        RasterImage.EnableReactors(true);

        foreach (var (x, y, z, imagePath) in tiles.OrderBy(t => t.Y).ThenBy(t => t.X))
        {
            if (!File.Exists(imagePath))
            {
                failed++;
                continue;
            }

            var defName = $"{DefPrefix}{sourceId}_Z{z}_X{x}_Y{y}";

            try
            {
                if (imgDict.Contains(defName))
                {
                    var existing = (RasterImageDef)tr.GetObject(imgDict.GetAt(defName), OpenMode.ForWrite);
                    existing.Erase();
                    imgDict.Remove(defName);
                }

                var nw = TileMath.TileTopLeft(x, y, z);
                var ne = TileMath.TileTopLeft(x + 1, y, z);
                var sw = TileMath.TileTopLeft(x, y + 1, z);

                var pNw = coordinates.ToVn2000(nw, centralMeridian);
                var pNe = coordinates.ToVn2000(ne, centralMeridian);
                var pSw = coordinates.ToVn2000(sw, centralMeridian);

                var origin = new Point3d(pSw.Easting, pSw.Northing, 0);
                var xAxis = new Vector3d(
                    pNe.Easting - pNw.Easting,
                    pNe.Northing - pNw.Northing,
                    0);
                var yAxis = new Vector3d(
                    pNw.Easting - pSw.Easting,
                    pNw.Northing - pSw.Northing,
                    0);

                if (xAxis.Length < 1e-9 || yAxis.Length < 1e-9)
                {
                    failed++;
                    continue;
                }

                var orientation = new CoordinateSystem3d(origin, xAxis, yAxis);
                if (clipPolygonWcs is { Count: >= 3 }
                    && !RasterClipService.TileIntersectsBoundary(orientation, clipPolygonWcs))
                {
                    continue;
                }

                var rasterDef = new RasterImageDef { SourceFileName = imagePath };
                rasterDef.Load();

                var defId = imgDict.SetAt(defName, rasterDef);
                tr.AddNewlyCreatedDBObject(rasterDef, true);

                var raster = new RasterImage
                {
                    ImageDefId = defId,
                    Orientation = orientation,
                    ShowImage = true,
                    ImageTransparency = false
                };

                var imageId = ms.AppendEntity(raster);
                tr.AddNewlyCreatedDBObject(raster, true);
                raster.AssociateRasterDef(rasterDef);

                // Chỉ clip theo đường bao chèn (tím). Exclusion dùng Wipeout riêng.
                if (clipPolygonWcs is { Count: >= 3 } || BasemapSession.HasBoundaryClip)
                {
                    var ok = clipPolygonWcs is { Count: >= 3 }
                        ? RasterClipService.TryApplyClip(raster, clipPolygonWcs)
                        : RasterClipService.TryApplySessionClip(raster);
                    if (!ok)
                    {
                        raster.Erase();
                        rasterDef.Erase();
                        imgDict.Remove(defName);
                        continue;
                    }
                }

                SendToBack(tr, ms, imageId);
                attached++;
            }
            catch
            {
                failed++;
            }
        }

        tr.Commit();
        return new AttachBatchResult { Attached = attached, Failed = failed };
    }

    private static void SendToBack(Transaction tr, BlockTableRecord btr, ObjectId entityId)
    {
        if (btr.DrawOrderTableId.IsNull)
            return;

        var dot = (DrawOrderTable)tr.GetObject(btr.DrawOrderTableId, OpenMode.ForWrite);
        dot.MoveToBottom(new ObjectIdCollection { entityId });
    }
}
