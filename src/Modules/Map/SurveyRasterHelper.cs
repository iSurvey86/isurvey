using Autodesk.AutoCAD.DatabaseServices;

namespace iSurvey.Modules.Map;

/// <summary>Nhận diện raster/tile thuộc iSurvey.</summary>
internal static class SurveyRasterHelper
{
    private const string ImageDictName = "ACAD_IMAGE_DICT";
    public const string DefPrefix = "ISURVEY_";

    public static bool HasAnySurveyRaster(Database db)
    {
        using var tr = db.TransactionManager.StartTransaction();
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

        foreach (ObjectId id in ms)
        {
            if (id.IsErased)
                continue;

            if (tr.GetObject(id, OpenMode.ForRead) is RasterImage raster && IsSurveyRaster(tr, raster))
                return true;
        }

        return false;
    }

    public static bool IsSurveyRaster(Transaction tr, RasterImage raster)
    {
        try
        {
            if (raster.ImageDefId.IsNull)
                return false;

            var def = (RasterImageDef)tr.GetObject(raster.ImageDefId, OpenMode.ForRead);
            var source = def.SourceFileName ?? string.Empty;
            if (source.Contains("isurvey", StringComparison.OrdinalIgnoreCase))
                return true;

            if (source.Contains(@"\iSurvey\tiles\", StringComparison.OrdinalIgnoreCase))
                return true;

            if (source.Contains("google-satellite", StringComparison.OrdinalIgnoreCase))
                return true;

            var nod = (DBDictionary)tr.GetObject(
                raster.Database.NamedObjectsDictionaryId, OpenMode.ForRead);
            if (!nod.Contains(ImageDictName))
                return false;

            var imgDict = (DBDictionary)tr.GetObject(nod.GetAt(ImageDictName), OpenMode.ForRead);
            foreach (DBDictionaryEntry entry in imgDict)
            {
                if (entry.Value != raster.ImageDefId)
                    continue;

                return entry.Key.StartsWith(DefPrefix, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public static void RemoveOrphanDefs(Transaction tr, Database db, IEnumerable<ObjectId> candidateDefIds)
    {
        var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
        if (!nod.Contains(ImageDictName))
            return;

        var imgDict = (DBDictionary)tr.GetObject(nod.GetAt(ImageDictName), OpenMode.ForWrite);
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

        var stillUsed = new HashSet<ObjectId>();
        foreach (ObjectId id in ms)
        {
            if (id.IsErased)
                continue;

            if (tr.GetObject(id, OpenMode.ForRead) is RasterImage ri && !ri.ImageDefId.IsNull)
                stillUsed.Add(ri.ImageDefId);
        }

        foreach (var defId in candidateDefIds.Distinct())
        {
            if (defId.IsNull || stillUsed.Contains(defId))
                continue;

            string? keyToRemove = null;
            foreach (DBDictionaryEntry entry in imgDict)
            {
                if (entry.Value != defId)
                    continue;

                keyToRemove = entry.Key;
                break;
            }

            if (keyToRemove is null)
                continue;

            var def = (RasterImageDef)tr.GetObject(defId, OpenMode.ForWrite);
            def.Erase();
            imgDict.Remove(keyToRemove);
        }
    }
}
