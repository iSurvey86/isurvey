# Evidence `USER-BF-ISURVEY_FIT_TITLE`

- **file:** `src/Modules/Frame/FrameService.cs`
- **lines:** 297-308
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler FitTitleCommand

```csharp
{
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;
        var n = TitleBlockFit.FitDocument(doc);
        doc.Editor.WriteMessage(n > 0
            ? $"\n[iSurvey] Đã co {n} ô chủ đầu tư về một dòng."
            : "\n[iSurvey] Không có ô chủ đầu tư nào cần co.");
    }
```
