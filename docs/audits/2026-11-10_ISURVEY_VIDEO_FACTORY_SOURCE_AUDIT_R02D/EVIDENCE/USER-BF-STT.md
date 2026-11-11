# Evidence `USER-BF-STT`

- **file:** `src/Modules/Tuyen/SequenceNumberService.cs`
- **lines:** 29-116
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** handler SequenceCommand

```csharp
{
        if (!AuthManager.EnsureAuthenticated("STT"))
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        var request = SequenceWindow.Prompt();
        if (request is null)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy.");
            return;
        }

        try
        {
            AcadApp.SetSystemVariable("WIPEOUTFRAME", 2);
        }
        catch
        {
            // bản CAD không có biến này vẫn đặt số được
        }

        // Đóng hộp thoại để lại Esc hoặc Enter. Nếu không xóa, GetPoint nhận ngay và lệnh thoát.
        try
        {
            ClearUserBreak();
            EatPendingInput();
        }
        catch
        {
            // bản CAD không xuất hàm này thì vòng lặp bên dưới bỏ qua tín hiệu đến trong 400 ms
        }

        var placed = 0;
        var number = request.Start;
        var ed = doc.Editor;
        var skippedPhantom = false;
        while (true)
        {
            var label = request.Prefix + number + request.Suffix;
            var ppo = new PromptPointOptions($"\niSurvey: Click điểm chèn {label} (Esc để thoát): ")
            {
                AllowNone = true
            };
            var asked = DateTime.UtcNow;
            var ppr = ed.GetPoint(ppo);
            // Đóng hộp thoại để lại Enter/Esc trong hàng đợi, GetPoint nhận ngay và thoát.
            if (!skippedPhantom
                && ppr.Status != PromptStatus.OK
                && (DateTime.UtcNow - asked).TotalMilliseconds < 400)
            {
                skippedPhantom = true;
                continue;
            }

            if (ppr.Status != PromptStatus.OK)
                break;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var space = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);
                EnsureLayer(tr, doc.Database);
                var radius = request.Height * RadiusFactor;
                if (request.UseWipeout)
                    AddWipeout(tr, space, ppr.Value, radius);
                if (request.DrawCircle)
                    AddCircle(tr, space, ppr.Value, radius);
                var styleId = EnsureTextStyle(tr, doc.Database, request);
                var textId = AddText(tr, space, ppr.Value, label, request.Height, request.WidthFactor, styleId);
                var order = (DrawOrderTable)tr.GetObject(space.DrawOrderTableId, OpenMode.ForWrite);
                order.MoveToTop(new ObjectIdCollection { textId });
                tr.Commit();
            }

            placed++;
            number += request.Step;
        }

        ed.WriteMessage(placed == 0
            ? "\n[iSurvey] Đã hủy."
            : $"\n[iSurvey] Đã ghi {placed} số trên layer {LayerName}.");
        if (placed > 0)
            ActivityLogger.Result("STT", $"đã ghi {placed} số trên layer {LayerName}");
    }
```
