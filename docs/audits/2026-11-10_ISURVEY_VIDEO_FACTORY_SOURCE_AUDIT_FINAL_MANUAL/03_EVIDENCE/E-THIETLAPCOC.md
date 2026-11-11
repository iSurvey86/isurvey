# E-THIETLAPCOC — THIETLAPCOC StationStyle

- **file:** `src/Modules/Tuyen/RouteModule.cs`
- **lines:** 62-90
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** Window → Store.Save

```csharp
    private static void StationStyle()
    {
        if (!AuthManager.EnsureAuthenticated("THIETLAPCOC"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        StationStyleSettings? saved;
        try
        {
            saved = StationStyleWindow.Prompt(StationStyleStore.Load());
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Không mở được hộp thoại: " + ex.Message);
            return;
        }

        if (saved is null)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy thiết lập kiểu cọc.");
            return;
        }

        StationStyleStore.Save(saved);
        doc.Editor.WriteMessage(
            "\n[iSurvey] Đã lưu kiểu cọc. Chạy PHATSINHCOC để vẽ lại theo thông số mới.");
    }
```
