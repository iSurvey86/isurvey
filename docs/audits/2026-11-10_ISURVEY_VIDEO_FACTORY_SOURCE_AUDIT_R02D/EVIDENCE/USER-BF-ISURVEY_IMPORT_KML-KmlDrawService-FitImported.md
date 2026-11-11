# Evidence `USER-BF-ISURVEY_IMPORT_KML-KmlDrawService-FitImported`

- **file:** `src/Modules/Import/KmlDrawService.cs`
- **lines:** 261-288
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** called from BF-ISURVEY_IMPORT_KML

```csharp
{
        if (!report.HasExtents)
            return;

        try
        {
            var view = ed.GetCurrentView();
            var width = Math.Max(report.MaxX - report.MinX, 20);
            var height = Math.Max(report.MaxY - report.MinY, 20);
            width *= 1.08;
            height *= 1.08;
            var aspect = view.Height > 1e-9 ? view.Width / view.Height : 1.6;
            if (width / height > aspect)
                height = width / aspect;
            else
                width = height * aspect;

            view.CenterPoint = new Point2d((report.MinX + report.MaxX) / 2.0, (report.MinY + report.MaxY) / 2.0);
            view.Width = width;
            view.Height = height;
            ed.SetCurrentView(view);
        }
        catch
        {
            // Không chặn lệnh nếu không đổi được khung nhìn.
        }
    }
```
