# Evidence `USER-BF-GOMOHINH`

- **file:** `src/Modules/Terrain/TerrainModule.cs`
- **lines:** 188-218
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler Detach

```csharp
{
        if (!AuthManager.EnsureAuthenticated("GOMOHINH"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null || !ModelSpace(doc.Editor))
            return;
        var model = TerrainService.Choose(doc.Editor, doc.Database);
        if (model is null)
            return;
        var kind = Kind(doc.Editor, "\nGỡ khỏi mô hình [Điểm/Đứt gãy/Đồng mức/Bao]");
        if (kind is null)
            return;
        if (TerrainService.CountKind(model, kind) == 0)
        {
            doc.Editor.WriteMessage("\n[iSurvey] \"" + model.Name + "\" chưa có " + TerrainService.KindLabel(kind, true) + " để gỡ.");
            return;
        }

        var removed = TerrainService.Detach(doc, model, kind);
        if (removed == 0)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Không gỡ được đối tượng nào. Hãy chọn đúng đối tượng đã gắn vào mô hình.");
            return;
        }

        doc.Editor.WriteMessage(
            "\n[iSurvey] Đã gỡ " + removed + " " + TerrainService.KindLabel(kind, true)
            + " khỏi \"" + model.Name + "\". Đối tượng trên bản vẽ vẫn giữ. Hãy xây dựng lại lưới.");
        SurveyPaletteManager.Reload();
    }
```
