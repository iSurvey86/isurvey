# Evidence `USER-BF-THEMCOC`

- **file:** `src/Modules/Tuyen/RouteModule.cs`
- **lines:** 336-366
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler AddOne

```csharp
{
        if (!AuthManager.EnsureAuthenticated("THEMCOC"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null || !ModelSpace(doc.Editor))
            return;
        var route = RouteService.Choose(doc.Editor, doc.Database);
        if (route is null)
            return;

        var point = doc.Editor.GetPoint(new PromptPointOptions("\nChọn vị trí thêm cọc (gần tim tuyến): "));
        if (point.Status != PromptStatus.OK)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy thêm cọc.");
            return;
        }

        if (!RouteService.TryAddStationAtPoint(doc, route, point.Value, replaceNear: true, out var chainage, out var missingZ))
        {
            doc.Editor.WriteMessage("\n[iSurvey] Không thêm được cọc tại điểm đã chọn.");
            return;
        }

        var msg = "\n[iSurvey] Đã thêm cọc " + RouteService.FormatChainage(chainage)
            + " trên \"" + route.Name + "\".";
        if (missingZ > 0)
            msg += " Chưa lấy được Z từ mô hình.";
        doc.Editor.WriteMessage(msg);
        SurveyPaletteManager.Reload();
    }
```
