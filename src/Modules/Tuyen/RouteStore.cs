using System.Text.Json;
using Autodesk.AutoCAD.DatabaseServices;

namespace iSurvey.Modules.Tuyen;

/// <summary>Lưu tuyến trong Named Objects Dictionary của bản vẽ đang mở.</summary>
public static class RouteStore
{
    private const string DictName = "ISURVEY_ROUTE";
    private const int Chunk = 1800;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false
    };

    public static List<RouteRecord> Load(Database db)
    {
        var list = new List<RouteRecord>();
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
                var route = JsonSerializer.Deserialize<RouteRecord>(text, Json);
                if (route is null || string.IsNullOrWhiteSpace(route.Id))
                    continue;
                route.Name ??= "";
                route.PolylineHandle ??= "";
                route.TerrainId ??= "";
                route.Layer ??= "iSurvey_Tuyen";
                route.Stations ??= new List<StationRecord>();
                // Reversed mặc định false khi bản ghi cũ chưa có trường.
                list.Add(route);
            }
            catch (JsonException)
            {
            }
        }

        tr.Commit();
        list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
        return list;
    }

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
