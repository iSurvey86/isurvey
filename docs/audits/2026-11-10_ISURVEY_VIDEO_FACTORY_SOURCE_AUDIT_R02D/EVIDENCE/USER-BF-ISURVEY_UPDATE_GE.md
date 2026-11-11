# Evidence `USER-BF-ISURVEY_UPDATE_GE`

- **file:** `src/Modules/Map/MapModule.cs`
- **lines:** 124-167
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler UpdateGeCommand

```csharp
{
        if (!AuthManager.EnsureAuthenticated(track: false)) return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null) return;

        if (!IsModelSpace(doc.Database))
        {
            doc.Editor.WriteMessage("\n[iSurvey] Hãy mở Model Space.");
            return;
        }

        try
        {
            if (!BasemapSession.IsActive)
            {
                var drawingPath = string.IsNullOrWhiteSpace(doc.Database.Filename) ? null : doc.Database.Filename;
                var saved = UserSettingsStore.Load(drawingPath);
                if (string.IsNullOrWhiteSpace(saved.ProvinceName))
                {
                    doc.Editor.WriteMessage("\n[iSurvey] Chưa có ảnh GE trong phiên này. Hãy chèn ảnh vệ tinh trước.");
                    return;
                }

                var basemap = string.IsNullOrWhiteSpace(saved.BasemapId) ? "google-satellite" : saved.BasemapId;
                BasemapSession.Activate(
                    saved.CentralMeridian,
                    basemap,
                    saved.AutoRefresh,
                    null,
                    saved.ZoneWidthDegrees);
            }

            var result = _refresh.Refresh(doc, BasemapSession.CentralMeridian, BasemapSession.BasemapId);
            ActivityLogger.Result("ISURVEY_UPDATE_GE",
                $"cập nhật khung nhìn, {BasemapSession.BasemapId}, zoom {result.Zoom}, {result.TileCount} tile, lỗi {result.FailedTileCount}, TM-{BasemapSession.ZoneWidthDegrees} kinh tuyến {BasemapSession.CentralMeridian:0.##}°, bản vẽ {ActivityLogger.FileName(doc.Database.Filename)}");
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage($"\n[iSurvey] {ex.Message}");
            iSurvey.Auth.ActivityLogger.TrackError(ex);
        }
    }
```
