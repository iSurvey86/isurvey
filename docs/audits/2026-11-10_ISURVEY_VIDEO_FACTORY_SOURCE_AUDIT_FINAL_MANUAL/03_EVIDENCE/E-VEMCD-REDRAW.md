# E-VEMCD-REDRAW — VEMCD erase old + Draw + Save

- **file:** `src/Modules/Profile/ProfileModule.cs`
- **lines:** 163-208
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** eraseHandles → Draw → ProfileStore.Save

```csharp
        var existingProfiles = ProfileStore.Load(doc.Database);
        var origin = insert;
        var drawn = 0;
        foreach (var route in targets)
        {
            ProfileRecord? existing = null;
            foreach (var p in existingProfiles)
            {
                if (string.Equals(p.RouteId, route.Id, StringComparison.OrdinalIgnoreCase))
                {
                    existing = p;
                    break;
                }
            }

            // Đã có bản → xóa rồi vẽ lại tại điểm vừa pick (không chồng bản).
            var profile = ProfileDrawService.Draw(
                doc, route, options, origin,
                existingId: existing?.Id,
                eraseHandles: existing?.EntityHandles);
            ProfileStore.Save(doc.Database, profile);
            if (existing is not null)
            {
                ed.WriteMessage("\n[iSurvey] Đã cập nhật «" + profile.Name + "». " + profile.Summary());
                ActivityLogger.Result("ISURVEY_PROFILE_DRAW", "redraw " + profile.Name + " " + profile.Summary());
            }
            else
            {
                ed.WriteMessage(
                    "\n[iSurvey] Đã vẽ «" + profile.Name + "». "
                    + profile.Summary()
                    + " Rải khung bằng RAIMCD tại góc dưới-trái bảng.");
                ActivityLogger.Result("ISURVEY_PROFILE_DRAW", profile.Name + " " + profile.Summary());
            }

            drawn++;
            if (!options.DrawMultipleRoutes || drawn >= targets.Count)
                continue;

            // Dàn trang xuống: insert tiếp theo sao cho đỉnh MCD sau ≈ Y_min_trước − 50.
            var yMin = ProfileDrawService.GetProfileYMin(doc, profile.EntityHandles, profile.InsertY);
            var nextBandH = ProfileBandRow.TotalHeight(options.Rows);
            // Dự phòng chiều cao đất (cùng pad cũ 200) để MCD sau không chồng lên.
            var nextHeightGuess = nextBandH + 200;
            origin = new Point3d(insert.X, yMin - ProfileDrawService.BatchRouteGapY - nextHeightGuess, 0);
        }
```
