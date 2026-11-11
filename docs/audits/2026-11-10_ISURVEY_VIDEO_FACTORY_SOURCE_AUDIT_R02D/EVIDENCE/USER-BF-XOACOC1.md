# Evidence `USER-BF-XOACOC1`

- **file:** `src/Modules/Tuyen/RouteModule.cs`
- **lines:** 303-334
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler RemoveOne

```csharp
{
        if (!AuthManager.EnsureAuthenticated("XOACOC1"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null || !ModelSpace(doc.Editor))
            return;

        var options = new PromptEntityOptions("\nChọn gạch cọc hoặc chữ cọc cần xóa: ")
        {
            AllowNone = false
        };
        options.SetRejectMessage("\nChỉ chọn Line, Text hoặc MText của cọc.");
        options.AddAllowedClass(typeof(Line), exactMatch: false);
        options.AddAllowedClass(typeof(DBText), exactMatch: false);
        options.AddAllowedClass(typeof(MText), exactMatch: false);
        var picked = doc.Editor.GetEntity(options);
        if (picked.Status != PromptStatus.OK)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy xóa cọc.");
            return;
        }

        if (!RouteService.RemoveStationByEntity(doc, picked.ObjectId, out var routeName, out var stationName))
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đối tượng không phải cọc iSurvey đã lưu.");
            return;
        }

        doc.Editor.WriteMessage("\n[iSurvey] Đã xóa cọc " + stationName + " trên \"" + routeName + "\".");
        SurveyPaletteManager.Reload();
    }
```
