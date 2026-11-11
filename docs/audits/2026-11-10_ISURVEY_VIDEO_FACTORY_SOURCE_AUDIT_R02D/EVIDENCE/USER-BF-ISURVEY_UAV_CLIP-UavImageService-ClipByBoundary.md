# Evidence `USER-BF-ISURVEY_UAV_CLIP-UavImageService-ClipByBoundary`

- **file:** `src/Modules/Uav/UavImageService.cs`
- **lines:** 23-53
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** called from BF-ISURVEY_UAV_CLIP

```csharp
{
        var images = PromptSelectImages(ed, db);
        if (images is null)
        {
            ActivityLogger.Result("ISURVEY_UAV_CLIP", "đã hủy, chưa chọn ảnh");
            return;
        }

        var mode = PromptClipMode(ed);
        if (mode is null)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy — chưa cắt ảnh.");
            ActivityLogger.Result("ISURVEY_UAV_CLIP", "đã hủy, chưa chọn cách cắt");
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
```
