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

    /// <summary>Danh sách mô hình cho UI / tạo tuyến — không giữ tọa độ Mesh (tránh đơ file lớn).</summary>
    public static List<TerrainRecord> Load(Database db) => Load(db, includeMeshCoordinates: false);

    /// <param name="includeMeshCoordinates">true chỉ khi thật sự cần Mesh trong bộ nhớ (hiếm — Z ưu tiên Face).</param>
    public static List<TerrainRecord> Load(Database db, bool includeMeshCoordinates)
    {
        var list = LoadInStoreOrder(db, includeMeshCoordinates);
        list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
        return list;
    }

    /// <summary>Thứ tự ghi vào dictionary (mô hình tạo sau đứng sau).</summary>
    public static List<TerrainRecord> LoadInStoreOrder(Database db) =>
        LoadInStoreOrder(db, includeMeshCoordinates: false);

    public static List<TerrainRecord> LoadInStoreOrder(Database db, bool includeMeshCoordinates)
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
                // Bản cũ từng nhồi cả tọa độ Mesh vào JSON — bỏ mảng trước khi parse để không đơ UI/tuyến.
                var meshCountHint = -1;
                if (!includeMeshCoordinates)
                    text = StripJsonArrayProperty(text, "Mesh", out meshCountHint);
                var model = JsonSerializer.Deserialize<TerrainRecord>(text, Json);
                if (model is null || string.IsNullOrWhiteSpace(model.Id))
                    continue;
                model.Points ??= new List<string>();
                model.Breaklines ??= new List<string>();
                model.Contours ??= new List<string>();
                model.Boundaries ??= new List<string>();
                model.Mesh ??= new List<double>();
                if (model.StoredTriangleCount <= 0 && meshCountHint > 0)
                    model.StoredTriangleCount = meshCountHint;
                if (model.Mesh.Count >= 9 && model.StoredTriangleCount <= 0)
                    model.StoredTriangleCount = model.Mesh.Count / 9;
                if (!includeMeshCoordinates)
                    model.DropMeshCoordinates();
                list.Add(model);
            }
            catch (JsonException)
            {
                // Bỏ mục hỏng, không chặn các mô hình còn lại.
            }
        }

        tr.Commit();
        return list;
    }

    public static TerrainRecord? LastCreated(Database db)
    {
        var list = LoadInStoreOrder(db, includeMeshCoordinates: false);
        return list.Count == 0 ? null : list[list.Count - 1];
    }

    public static void Save(Database db, TerrainRecord model)
    {
        // Không ghi tọa độ mọi tam giác vào NOD — file lớn làm đơ Load/UI/tạo tuyến.
        // Face trên bản vẽ + StoredTriangleCount đủ dùng.
        model.DropMeshCoordinates();
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
        foreach (var model in Load(db, includeMeshCoordinates: false))
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
        var sb = new System.Text.StringBuilder();
        foreach (TypedValue value in data)
        {
            if (value.TypeCode == (int)DxfCode.Text && value.Value is string part)
                sb.Append(part);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Cắt property mảng JSON (vd. "Mesh":[...]) khỏi chuỗi — không parse từng số.
    /// Ước số phần tử (đếm dấu phẩy) để suy số tam giác nếu thiếu StoredTriangleCount.
    /// </summary>
    private static string StripJsonArrayProperty(string json, string propertyName, out int elementCountHint)
    {
        elementCountHint = -1;
        var key = "\"" + propertyName + "\"";
        var keyAt = json.IndexOf(key, StringComparison.Ordinal);
        if (keyAt < 0)
            return json;
        var colon = json.IndexOf(':', keyAt + key.Length);
        if (colon < 0)
            return json;
        var i = colon + 1;
        while (i < json.Length && char.IsWhiteSpace(json[i]))
            i++;
        if (i >= json.Length || json[i] != '[')
            return json;
        var start = keyAt;
        var depth = 0;
        for (; i < json.Length; i++)
        {
            var c = json[i];
            if (c == '[')
                depth++;
            else if (c == ']')
            {
                depth--;
                if (depth == 0)
                {
                    i++;
                    break;
                }
            }
        }

        if (depth != 0)
            return json;
        // Ước số tam giác từ độ dài mảng (tránh đếm từng dấu phẩy trên Mesh hàng chục MB).
        var arrayOpen = colon + 1;
        while (arrayOpen < i && char.IsWhiteSpace(json[arrayOpen]))
            arrayOpen++;
        if (arrayOpen < i && json[arrayOpen] == '[')
        {
            var bodyStart = arrayOpen + 1;
            var bodyEnd = i - 1;
            while (bodyStart < bodyEnd && char.IsWhiteSpace(json[bodyStart]))
                bodyStart++;
            while (bodyEnd > bodyStart && char.IsWhiteSpace(json[bodyEnd - 1]))
                bodyEnd--;
            if (bodyStart >= bodyEnd)
                elementCountHint = 0;
            else
            {
                // ~12 ký tự / số (vd. "-123.456789,") → 9 số / tam giác.
                var approxNumbers = Math.Max(1, (bodyEnd - bodyStart) / 12);
                if (approxNumbers >= 9)
                    elementCountHint = approxNumbers / 9;
            }
        }

        var end = i;
        while (end < json.Length && char.IsWhiteSpace(json[end]))
            end++;
        if (end < json.Length && json[end] == ',')
            end++;
        else if (start > 0)
        {
            var before = start - 1;
            while (before >= 0 && char.IsWhiteSpace(json[before]))
                before--;
            if (before >= 0 && json[before] == ',')
                start = before;
        }

        return json.Remove(start, end - start);
    }
}
