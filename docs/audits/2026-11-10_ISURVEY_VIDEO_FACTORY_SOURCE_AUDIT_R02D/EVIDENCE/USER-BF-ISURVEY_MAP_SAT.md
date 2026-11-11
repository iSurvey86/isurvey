# Evidence `USER-BF-ISURVEY_MAP_SAT`

- **file:** `src/Modules/Map/MapModule.cs`
- **lines:** 33-64
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler InsertSatelliteCommand

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

        if (!EnsureProvince(doc))
        {
            ActivityLogger.Result("ISURVEY_MAP_SAT", "đã hủy, chưa chọn múi chiếu");
            return;
        }

        var mode = PromptSatelliteMode(doc.Editor);
        if (mode is null)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy.");
            ActivityLogger.Result("ISURVEY_MAP_SAT", "đã hủy, chưa chọn chế độ");
            return;
        }

        if (mode == 2)
            HdMosaicWorkflow.Run(doc);
        else
            ApplyBasemap("google-satellite", "ISURVEY_MAP_SAT", "xem lướt");
    }
```
