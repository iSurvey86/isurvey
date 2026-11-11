# E-TRACAOD-MARK — Show/Erase elevation mark

- **file:** `src/Modules/Terrain/TerrainService.cs`
- **lines:** 664-710
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** DBText create/erase

```csharp
    public static ObjectId ShowElevationMark(
        Autodesk.AutoCAD.ApplicationServices.Document doc,
        TerrainRecord model,
        double x,
        double y,
        double z,
        ObjectId previous)
    {
        EraseElevationMark(doc, previous);
        using (doc.LockDocument())
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            EnsureLayer(tr, doc.Database, model.Layer);
            var space = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);
            var label = new DBText();
            label.SetDatabaseDefaults();
            label.Layer = model.Layer;
            label.Position = new Point3d(x, y, z);
            label.Height = Math.Max(EstimateTextHeight(doc), 0.5);
            label.TextString = "Z=" + z.ToString("0.000", CultureInfo.CurrentCulture);
            label.Color = AcadColor.FromColorIndex(ColorMethod.ByAci, 1);
            space.AppendEntity(label);
            tr.AddNewlyCreatedDBObject(label, true);
            tr.Commit();
            doc.Editor.Regen();
            return label.ObjectId;
        }
    }

    public static void EraseElevationMark(Autodesk.AutoCAD.ApplicationServices.Document doc, ObjectId id)
    {
        if (id.IsNull || !id.IsValid || id.IsErased)
            return;
        try
        {
            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                if (tr.GetObject(id, OpenMode.ForWrite, true) is Entity entity && !entity.IsErased)
                    entity.Erase();
                tr.Commit();
            }
        }
        catch (Autodesk.AutoCAD.Runtime.Exception)
        {
        }
    }
```
