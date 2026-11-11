# Evidence `USER-BF-ISURVEY_FRAME_EDIT`

- **file:** `src/Modules/Frame/FrameService.FrameEdit.cs`
- **lines:** 17-53
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler EditFrameInfoCommand

```csharp
{
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        var id = FrameDoubleClick.TakePending();
        if (id.IsNull)
        {
            var opt = new PromptEntityOptions("\nChọn khung cần sửa thông tin: ")
            {
                AllowNone = false
            };
            opt.SetRejectMessage("\nHãy chọn block khung (KB_A0 … KB_A4).");
            opt.AddAllowedClass(typeof(BlockReference), exactMatch: false);
            var picked = doc.Editor.GetEntity(opt);
            if (picked.Status != PromptStatus.OK)
                return;
            id = picked.ObjectId;
        }

        try
        {
            EditFrameInfo(doc, id);
        }
        catch (System.Exception ex)
        {
            ActivityLogger.TrackError(ex, "ISURVEY_FRAME_EDIT");
            doc.Editor.WriteMessage($"\n[iSurvey] Sửa khung lỗi: {ex.Message}");
        }
        finally
        {
            FrameDoubleClick.Release();
        }
    }
```
