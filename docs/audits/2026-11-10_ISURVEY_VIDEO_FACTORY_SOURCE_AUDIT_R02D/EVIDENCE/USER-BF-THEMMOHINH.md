# Evidence `USER-BF-THEMMOHINH`

- **file:** `src/Modules/Terrain/TerrainModule.cs`
- **lines:** 102-163
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler Add

```csharp
{
        if (!AuthManager.EnsureAuthenticated("THEMMOHINH"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null || !ModelSpace(doc.Editor))
            return;
        var model = TerrainService.Choose(doc.Editor, doc.Database);
        if (model is null)
            return;
        var kind = Kind(doc.Editor, "\nThêm vào mô hình [Điểm/Đứt gãy/Đồng mức/Bao]");
        if (kind is null)
            return;
        int added;
        if (kind == "point")
        {
            PointSourceChoice? choice;
            try
            {
                choice = AddPointSourcesWindow.Prompt();
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage("\n[iSurvey] Không mở được hộp thoại: " + ex.Message);
                return;
            }

            if (choice is null)
            {
                doc.Editor.WriteMessage("\n[iSurvey] Đã hủy thêm điểm.");
                return;
            }

            added = TerrainService.AddPoints(
                doc,
                model,
                choice.Points,
                choice.Blocks,
                choice.BlockElevFromLabels,
                choice.Labels,
                choice.Chains);
        }
        else
        {
            added = TerrainService.Add(doc, model, kind);
        }

        var hadMesh = model.TriangleCount > 0;
        if (added > 0 && hadMesh)
            TerrainService.DropMesh(doc, model);

        var label = kind switch
        {
            "contour" => "đường đồng mức",
            "break" => "đường đứt gãy",
            "boundary" => "đường bao",
            _ => "nguồn điểm"
        };
        var rebuild = added > 0 && hadMesh ? " Lưới cũ đã xóa. Hãy xây dựng lại." : "";
        doc.Editor.WriteMessage("\n[iSurvey] Đã gắn " + added + " " + label + " vào \"" + model.Name + "\"." + rebuild);
        SurveyPaletteManager.Reload();
    }
```
