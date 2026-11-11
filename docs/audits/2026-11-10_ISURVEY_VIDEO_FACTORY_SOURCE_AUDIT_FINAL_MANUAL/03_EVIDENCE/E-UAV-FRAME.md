# E-UAV-FRAME — FRAME_TOGGLE

- **file:** `src/Modules/Uav/UavImageService.cs`
- **lines:** 439-465
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** IMAGEFRAME vars

```csharp
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
        ActivityLogger.Result("ISURVEY_UAV_FRAME_TOGGLE", nowOff ? "đã bật khung ảnh" : "đã tắt khung ảnh");
    }
```
