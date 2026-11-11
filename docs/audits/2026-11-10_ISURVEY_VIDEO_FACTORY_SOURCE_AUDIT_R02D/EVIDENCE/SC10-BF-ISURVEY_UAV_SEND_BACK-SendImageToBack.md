# Evidence `SC10-BF-ISURVEY_UAV_SEND_BACK-SendImageToBack`

- **file:** `src/Modules/Uav/UavImageService.cs`
- **lines:** 468-489
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** SOURCE_BEHAVIOR_VERIFIED_STATIC

```csharp
{
        var opts = new PromptEntityOptions("\niSurvey: Chọn ảnh Raster cần đưa xuống dưới: ")
        {
            AllowNone = false
        };
        opts.SetRejectMessage("\niSurvey: Chỉ chọn RasterImage.");
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
        ActivityLogger.Result("ISURVEY_UAV_SEND_BACK", "đã đưa 1 ảnh xuống dưới, bản vẽ " + ActivityLogger.FileName(db.Filename));
    }
```
