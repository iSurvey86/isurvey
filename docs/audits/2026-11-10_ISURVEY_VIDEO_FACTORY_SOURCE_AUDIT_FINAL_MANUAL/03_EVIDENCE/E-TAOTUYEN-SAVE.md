# E-TAOTUYEN-SAVE — RouteStore.Save

- **file:** `src/Modules/Tuyen/RouteStore.cs`
- **lines:** 60-87
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** Xrecord JSON NOD

```csharp
    public static void Save(Database db, RouteRecord route)
    {
        using var tr = db.TransactionManager.StartTransaction();
        var dict = EnsureDict(tr, db);
        var json = JsonSerializer.Serialize(route, Json);
        var values = new ResultBuffer();
        for (var i = 0; i < json.Length; i += Chunk)
        {
            var take = Math.Min(Chunk, json.Length - i);
            values.Add(new TypedValue((int)DxfCode.Text, json.Substring(i, take)));
        }

        if (!dict.IsWriteEnabled)
            dict.UpgradeOpen();
        if (dict.Contains(route.Id))
        {
            var record = (Xrecord)tr.GetObject(dict.GetAt(route.Id), OpenMode.ForWrite);
            record.Data = values;
        }
        else
        {
            var record = new Xrecord { Data = values };
            dict.SetAt(route.Id, record);
            tr.AddNewlyCreatedDBObject(record, true);
        }

        tr.Commit();
    }
```
