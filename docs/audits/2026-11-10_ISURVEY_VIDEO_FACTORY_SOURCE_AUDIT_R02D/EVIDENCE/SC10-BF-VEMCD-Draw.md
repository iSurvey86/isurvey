# Evidence `SC10-BF-VEMCD-Draw`

- **file:** `src/Modules/Profile/ProfileModule.cs`
- **lines:** 20-147
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** SOURCE_PARTIAL_VERIFIED

```csharp
{
        if (!AuthManager.EnsureAuthenticated("VEMCD"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;
        var ed = doc.Editor;
        if (doc.Database.TileMode == false)
        {
            ed.WriteMessage("\n[iSurvey] VEMCD chỉ vẽ trên Model Space.");
            return;
        }

        var seeded = TrySeedDemoRoute(doc, ed);

        List<RouteRecord> routes;
        try
        {
            routes = RouteStore.Load(doc.Database);
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage("\n[iSurvey] Không đọc được tuyến: " + ex.Message);
            return;
        }

        if (routes.Count == 0)
        {
            ed.WriteMessage("\n[iSurvey] Chưa có tuyến. Chạy TAOTUYEN rồi PHATSINHCOC trước.");
            return;
        }

        ProfileDrawOptions options;
        ProfileDialogAction action;
        Point3d insert;

        if (seeded)
        {
            var route = routes[0];
            options = ProfileSettingsStore.Load();
            options.RouteId = route.Id;
            options.ProfileName = "MCD " + route.Name;
            var samples = route.Stations!
                .Where(s => s.HasZ)
                .Select(s => new ProfileSampleService.SamplePoint
                {
                    Chainage = s.Chainage,
                    X = s.X,
                    Y = s.Y,
                    Z = s.Z,
                    HasZ = true,
                    IsStation = true,
                    Name = s.Name
                })
                .ToList();
            var bandH = ProfileBandRow.TotalHeight(options.Rows);
            options.Datum = ProfileDrawService.SuggestDatum(
                samples, options.HorizontalScale, options.VerticalScale, bandH);
            action = ProfileDialogAction.Draw;
            insert = Point3d.Origin;
            ed.WriteMessage("\n[iSurvey] Thử nhanh: vẽ tại gốc (0,0), mức " + options.Datum.ToString("0.###") + ".");
        }
        else
        {
            DrawProfileDialogResult? dialog;
            try
            {
                dialog = DrawProfileWindow.Prompt(routes, RouteContext.TakeRoute());
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n[iSurvey] Không mở được hộp thoại: " + ex.Message);
                return;
            }

            if (dialog is null || dialog.Action == ProfileDialogAction.Cancel || dialog.Options is null)
            {
                ed.WriteMessage("\n[iSurvey] Đã hủy vẽ mặt cắt.");
                return;
            }

            options = dialog.Options;
            action = dialog.Action;
            ProfileSettingsStore.Save(options);

            if (action == ProfileDialogAction.Draw)
            {
                // Luôn pick vị trí; nếu đã có MCD thì xóa bản cũ rồi vẽ tại điểm mới.
                var pOpts = new PromptPointOptions("\niSurvey: Chỉ góc dưới-trái cột tên (đáy bảng) trên Model: ")
                {
                    AllowNone = false
                };
                var pRes = ed.GetPoint(pOpts);
                if (pRes.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n[iSurvey] Đã hủy — chưa chọn điểm chèn.");
                    return;
                }

                insert = pRes.Value;
            }
            else
            {
                insert = Point3d.Origin;
            }
        }

        try
        {
            switch (action)
            {
                case ProfileDialogAction.Draw:
                    RunDraw(doc, ed, routes, options, insert);
                    break;
                case ProfileDialogAction.UpdateFromProfile:
                case ProfileDialogAction.UpdateProperties:
                case ProfileDialogAction.UpdateFromTin:
                    RunUpdate(doc, ed, routes, options, action);
                    break;
            }
        }
        cat
/* … truncated for pack size; full method in repo */

```
