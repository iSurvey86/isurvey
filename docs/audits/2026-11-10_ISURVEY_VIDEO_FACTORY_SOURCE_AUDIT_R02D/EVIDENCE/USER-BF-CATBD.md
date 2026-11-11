# Evidence `USER-BF-CATBD`

- **file:** `src/Modules/Plan/PlanModule.cs`
- **lines:** 47-112
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler CutPlanCommand

```csharp
{
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        var ed = doc.Editor;
        var db = doc.Database;

        if (!MapModule.IsModelSpace(db))
        {
            ed.WriteMessage("\n[iSurvey] Hay mo tab Model hoac vao Model Space (lenh MSPACE).");
            return;
        }

        // Buoc 1: duong ranh gioi cat khep kin
        var peo = new PromptEntityOptions("\niSurvey: Chọn đường ranh giới cắt khép kín: ")
        {
            AllowNone = false
        };
        peo.SetRejectMessage("\niSurvey: Chỉ chọn LWPolyline hoặc Polyline.");
        peo.AddAllowedClass(typeof(Polyline), true);
        peo.AddAllowedClass(typeof(Polyline2d), true);
        peo.AddAllowedClass(typeof(Polyline3d), true);

        var pe = ed.GetEntity(peo);
        if (pe.Status != PromptStatus.OK)
        {
            ed.WriteMessage("\n[iSurvey] Da huy CATBD.");
            return;
        }

        // Buoc 2: quet chon toan bo ban ve
        var pso = new PromptSelectionOptions
        {
            MessageForAdding = "\niSurvey: Quét chọn bản vẽ cần cắt (gõ ALL để chọn hết): "
        };
        var ps = ed.GetSelection(pso);
        if (ps.Status != PromptStatus.OK)
        {
            ed.WriteMessage("\n[iSurvey] Da huy CATBD.");
            return;
        }

        try
        {
            var stats = Run(db, pe.ObjectId, ps.Value.GetObjectIds());
            ed.WriteMessage(
                $"\n[iSurvey] CATBD xong: bao ve {stats.Protected}, giu trong bien {stats.KeptInside}, " +
                $"xoa {stats.Erased}, cat {stats.Cut} (tao {stats.NewPieces} doan moi), " +
                $"bo qua {stats.Skipped}, loi {stats.Errors}.");
            ActivityLogger.Result("CATBD",
                $"giữ {stats.KeptInside}, xóa {stats.Erased}, cắt {stats.Cut} (tạo {stats.NewPieces}), bỏ qua {stats.Skipped}, lỗi {stats.Errors}, bản vẽ {ActivityLogger.FileName(db.Filename)}");
            if (stats.FirstError is not null)
                ed.WriteMessage($"\n[iSurvey] Loi dau tien: {stats.FirstError}");
            ed.WriteMessage("\n[iSurvey] Go UNDO de hoan tac toan bo lan cat nay.");
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] CATBD that bai: {ex.Message}");
            iSurvey.Auth.ActivityLogger.TrackError(ex);
        }
    }
```
