using Autodesk.AutoCAD.DatabaseServices;

namespace iSurvey.Modules.Map;

/// <summary>Hiển thị raster — ẩn khung tile để ảnh nền trông liền khối.</summary>
public static class RasterDisplayHelper
{
    private const string ImageVarsKey = "ACAD_IMAGE_VARS";

    /// <summary>Tắt khung RasterImage trong bản vẽ (tương đương IMAGEFRAME = 0).</summary>
    public static void HideImageFrames(Database db, Transaction tr)
    {
        var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
        RasterVariables rasterVars;

        if (nod.Contains(ImageVarsKey))
        {
            rasterVars = (RasterVariables)tr.GetObject(nod.GetAt(ImageVarsKey), OpenMode.ForWrite);
        }
        else
        {
            rasterVars = new RasterVariables();
            nod.UpgradeOpen();
            nod.SetAt(ImageVarsKey, rasterVars);
            tr.AddNewlyCreatedDBObject(rasterVars, true);
        }

        if (rasterVars.ImageFrame != FrameSetting.ImageFrameOff)
            rasterVars.ImageFrame = FrameSetting.ImageFrameOff;
    }
}
