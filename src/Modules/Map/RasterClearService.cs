using Autodesk.AutoCAD.DatabaseServices;

namespace iSurvey.Modules.Map;

/// <summary>Xóa toàn bộ tile iSurvey trước khi refresh / khi Xóa GE.</summary>
public static class RasterClearService
{
    private const string ImageDictName = "ACAD_IMAGE_DICT";
    /// <summary>Layer wipeout cũ (phiên bản thử nghiệm) — dọn khi xóa GE.</summary>
    private const string LegacyExclusionLayer = "ISURVEY_EXCL";

    public static int ClearAll(Database db)
    {
        var erased = 0;

        using var tr = db.TransactionManager.StartTransaction();

        DBDictionary? imgDict = null;
        var defKeys = new List<string>();
        var defIds = new HashSet<ObjectId>();

        var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
        if (nod.Contains(ImageDictName))
        {
            imgDict = (DBDictionary)tr.GetObject(nod.GetAt(ImageDictName), OpenMode.ForWrite);
            foreach (DBDictionaryEntry entry in imgDict)
            {
                if (!entry.Key.StartsWith(SurveyRasterHelper.DefPrefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                defKeys.Add(entry.Key);
                defIds.Add(entry.Value);
            }
        }

        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

        // Dọn wipeout thử nghiệm còn sót
        foreach (ObjectId id in ms)
        {
            if (tr.GetObject(id, OpenMode.ForRead) is not Wipeout wipe)
                continue;
            if (!string.Equals(wipe.Layer, LegacyExclusionLayer, StringComparison.OrdinalIgnoreCase))
                continue;
            ((Entity)tr.GetObject(id, OpenMode.ForWrite)).Erase();
        }

        if (defIds.Count == 0)
        {
            tr.Commit();
            return 0;
        }

        var rasterIds = new List<ObjectId>();
        foreach (ObjectId id in ms)
        {
            if (tr.GetObject(id, OpenMode.ForRead) is not RasterImage raster)
                continue;

            if (defIds.Contains(raster.ImageDefId))
                rasterIds.Add(id);
        }

        foreach (var id in rasterIds)
        {
            var raster = (RasterImage)tr.GetObject(id, OpenMode.ForWrite);
            raster.Erase();
            erased++;
        }

        if (imgDict is not null)
        {
            foreach (var key in defKeys)
            {
                if (!imgDict.Contains(key))
                    continue;

                var def = (RasterImageDef)tr.GetObject(imgDict.GetAt(key), OpenMode.ForWrite);
                def.Erase();
                imgDict.Remove(key);
            }
        }

        tr.Commit();
        return erased;
    }
}
