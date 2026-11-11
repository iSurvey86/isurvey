# Evidence `USER-BF-DAODAUTUYEN`

- **file:** `src/Modules/Tuyen/RouteModule.cs`
- **lines:** 264-280
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler Reverse

```csharp
{
        if (!AuthManager.EnsureAuthenticated("DAODAUTUYEN"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null || !ModelSpace(doc.Editor))
            return;
        var route = RouteService.Choose(doc.Editor, doc.Database);
        if (route is null)
            return;
        var nowReversed = RouteService.Reverse(doc, route);
        doc.Editor.WriteMessage(
            "\n[iSurvey] Đã đảo đầu tuyến \"" + route.Name + "\". "
            + (nowReversed ? "Lý trình tăng ngược chiều vẽ polyline." : "Lý trình tăng theo chiều vẽ polyline.")
            + " Cọc cũ đã xóa — hãy phát sinh cọc lại. Sau khi vẽ MCD cũng đảo lại bằng lệnh này nếu cần.");
        SurveyPaletteManager.Reload();
    }
```
