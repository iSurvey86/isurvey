# Evidence `USER-BF-XOATHANHPHAN`

- **file:** `src/Modules/Terrain/TerrainModule.cs`
- **lines:** 220-271
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler Clear

```csharp
{
        if (!AuthManager.EnsureAuthenticated("XOATHANHPHAN"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null || !ModelSpace(doc.Editor))
            return;
        var model = TerrainService.Choose(doc.Editor, doc.Database);
        if (model is null)
            return;
        var kind = Kind(doc.Editor, "\nXóa toàn bộ khỏi mô hình [Điểm/Đứt gãy/Đồng mức/Bao]");
        if (kind is null)
            return;
        var count = TerrainService.CountKind(model, kind);
        if (count == 0)
        {
            doc.Editor.WriteMessage("\n[iSurvey] \"" + model.Name + "\" chưa có " + TerrainService.KindLabel(kind, true) + " để xóa.");
            return;
        }

        var label = TerrainService.KindLabel(kind, true);
        var meshNote = model.TriangleCount > 0
            ? " Lưới hiện có sẽ bị xóa và cần xây dựng lại."
            : "";
        bool ok;
        try
        {
            ok = DarkDialog.Confirm(
                "iSurvey — Xóa khỏi mô hình",
                "Gỡ toàn bộ " + count.ToString("N0") + " " + label + " khỏi \"" + model.Name + "\"?"
                + " Đối tượng trên bản vẽ vẫn giữ." + meshNote,
                "Xóa toàn bộ",
                "Hủy bỏ");
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Không mở được hộp thoại: " + ex.Message);
            return;
        }

        if (!ok)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy xóa " + label + ".");
            return;
        }

        var removed = TerrainService.ClearKind(doc, model, kind);
        doc.Editor.WriteMessage(
            "\n[iSurvey] Đã gỡ " + removed.ToString("N0") + " " + label
            + " khỏi \"" + model.Name + "\". Đối tượng trên bản vẽ vẫn giữ.");
        SurveyPaletteManager.Reload();
    }
```
