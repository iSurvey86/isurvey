# E-TRACAOD-QUERY — TRACAOD Query loop

- **file:** `src/Modules/Terrain/TerrainModule.cs`
- **lines:** 303-344
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** ShowElevationMark loop; EraseElevationMark end

```csharp
    private static void Query()
    {
        if (!AuthManager.EnsureAuthenticated("TRACAOD"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null || !ModelSpace(doc.Editor))
            return;
        var model = TerrainService.Choose(doc.Editor, doc.Database);
        if (model is null)
            return;
        if (model.TriangleCount == 0)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Mô hình chưa có lưới. Hãy xây dựng trước.");
            return;
        }

        doc.Editor.WriteMessage("\n[iSurvey] Tra cao độ \"" + model.Name + "\". Chọn điểm trên lưới; Enter để kết thúc.");
        ObjectId labelId = ObjectId.Null;
        while (true)
        {
            var options = new PromptPointOptions("\nChọn vị trí cần tra cao độ [Enter để xong]: ")
            {
                AllowNone = true
            };
            var picked = doc.Editor.GetPoint(options);
            if (picked.Status == PromptStatus.None || picked.Status == PromptStatus.Cancel)
                break;
            if (picked.Status != PromptStatus.OK)
                break;
            if (!TerrainService.TryQuery(model, picked.Value.X, picked.Value.Y, out var z))
            {
                doc.Editor.WriteMessage("\n[iSurvey] Điểm nằm ngoài lưới.");
                continue;
            }

            var text = z.ToString("0.000", System.Globalization.CultureInfo.CurrentCulture);
            doc.Editor.WriteMessage("\n[iSurvey] Z = " + text + " (" + model.Name + ").");
            labelId = TerrainService.ShowElevationMark(doc, model, picked.Value.X, picked.Value.Y, z, labelId);
        }

        TerrainService.EraseElevationMark(doc, labelId);
    }
```
