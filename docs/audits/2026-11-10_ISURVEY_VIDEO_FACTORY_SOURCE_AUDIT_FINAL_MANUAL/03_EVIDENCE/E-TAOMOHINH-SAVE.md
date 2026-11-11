# E-TAOMOHINH-SAVE — TerrainStore.Save

- **file:** `src/Modules/Terrain/TerrainStore.cs`
- **lines:** 73-100
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** Xrecord JSON

```csharp
    public static void Save(Database db, TerrainRecord model)
    {
        using var tr = db.TransactionManager.StartTransaction();
        var dict = EnsureDict(tr, db);
        var json = JsonSerializer.Serialize(model, Json);
        var values = new ResultBuffer();
        for (var i = 0; i < json.Length; i += Chunk)
        {
            var take = Math.Min(Chunk, json.Length - i);
            values.Add(new TypedValue((int)DxfCode.Text, json.Substring(i, take)));
        }

        if (!dict.IsWriteEnabled)
            dict.UpgradeOpen();
        if (dict.Contains(model.Id))
        {
            var record = (Xrecord)tr.GetObject(dict.GetAt(model.Id), OpenMode.ForWrite);
            record.Data = values;
        }
        else
        {
            var record = new Xrecord { Data = values };
            dict.SetAt(model.Id, record);
            tr.AddNewlyCreatedDBObject(record, true);
        }

        tr.Commit();
    }
```
