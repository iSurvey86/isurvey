using System.Text.Json;
using Autodesk.AutoCAD.DatabaseServices;

namespace iSurvey.Modules.Terrain;

/// <summary>Lưu mô hình trong Named Objects Dictionary của bản vẽ đang mở.</summary>
public static class TerrainStore
{
    private const string DictName = "ISURVEY_DTM";
    private const int Chunk = 1800;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false
    };

    public static List<TerrainRecord> Load(Database db)
    {
        var list = new List<TerrainRecord>();
        using var tr = db.TransactionManager.StartTransaction();
        var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
        if (!nod.Contains(DictName))
        {
            tr.Commit();
            return list;
        }

        var dict = (DBDictionary)tr.GetObject(nod.GetAt(DictName), OpenMode.ForRead);
        foreach (var entry in dict)
        {
            if (tr.GetObject(entry.Value, OpenMode.ForRead) is not Xrecord record || record.Data is null)
                continue;
            var text = ReadText(record.Data);
            if (text.Length == 0)
                continue;
            try
            {
                var model = JsonSerializer.Deserialize<TerrainRecord>(text, Json);
                if (model is null || string.IsNullOrWhiteSpace(model.Id))
                    continue;
                model.Points ??= new List<string>();
                model.Breaklines ??= new List<string>();
                model.Contours ??= new List<string>();
                model.Mesh ??= new List<double>();
                list.Add(model);
            }
            catch (JsonException)
            {
                // Bỏ mục hỏng, không chặn các mô hình còn lại.
            }
        }

        tr.Commit();
        list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
        return list;
    }

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

    public static TerrainRecord? Find(Database db, string id)
    {
        foreach (var model in Load(db))
        {
            if (string.Equals(model.Id, id, StringComparison.OrdinalIgnoreCase))
                return model;
        }

        return null;
    }

    private static DBDictionary EnsureDict(Transaction tr, Database db)
    {
        var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
        if (nod.Contains(DictName))
            return (DBDictionary)tr.GetObject(nod.GetAt(DictName), OpenMode.ForRead);
        nod.UpgradeOpen();
        var dict = new DBDictionary();
        nod.SetAt(DictName, dict);
        tr.AddNewlyCreatedDBObject(dict, true);
        return dict;
    }

    private static string ReadText(ResultBuffer data)
    {
        var text = "";
        foreach (TypedValue value in data)
        {
            if (value.TypeCode == (int)DxfCode.Text && value.Value is string part)
                text += part;
        }

        return text;
    }
}
