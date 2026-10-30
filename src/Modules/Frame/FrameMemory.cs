using Autodesk.AutoCAD.DatabaseServices;

namespace iSurvey.Modules.Frame;

/// <summary>
/// Nhớ logo, chữ ký và đường dẫn mộc đỏ trong chính file bản vẽ (Named Objects Dictionary).
/// File khác, hoặc file mới, không có mục này nên bắt đầu trống.
/// </summary>
internal static class FrameMemory
{
    private const string DictName = "ISURVEY_FRAME";
    private const string RecName = "STATE";

    internal sealed class Data
    {
        public Dictionary<string, string> Signs { get; } = new(StringComparer.OrdinalIgnoreCase);

        public string? LogoOwner { get; set; }

        public string? LogoDesign { get; set; }

        /// <summary>File DWG chữ ký theo tag TEN_n. Khác với <see cref="Signs"/>, vốn lưu tên block trong bản vẽ.</summary>
        public Dictionary<string, string> SignFiles { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Ảnh PNG mộc đỏ. Chỉ được vẽ lúc xuất PDF, không chèn vào bản vẽ.</summary>
        public string? StampPath { get; set; }

        /// <summary>true = xuất PDF sẽ đóng dấu. Không có ảnh thì bỏ qua.</summary>
        public bool StampOnPdf { get; set; } = true;
    }

    public static Data Load(Database db)
    {
        var data = new Data();
        using var tr = db.TransactionManager.StartTransaction();
        var xr = FindRecord(tr, db, OpenMode.ForRead);
        if (xr?.Data is null)
            return data;
        foreach (TypedValue tv in xr.Data)
        {
            if (tv.TypeCode != (int)DxfCode.Text || tv.Value is not string raw)
                continue;
            if (!Split(raw, out var kind, out var key, out var value))
                continue;
            if (kind == "CK" && IsSignTag(key))
                data.Signs[key] = value;
            else if (kind == "LG" && key == "1")
                data.LogoOwner = value;
            else if (kind == "LG" && key == "2")
                data.LogoDesign = value;
            else if (kind == "CKF" && IsSignTag(key))
                data.SignFiles[key] = value;
            else if (kind == "MD" && key == "P")
                data.StampPath = value;
            else if (kind == "MD" && key == "E")
                data.StampOnPdf = value == "1";
        }

        tr.Commit();
        return data;
    }

    public static void Save(Database db, Data data)
    {
        using var tr = db.TransactionManager.StartTransaction();
        var values = new List<string>();
        foreach (var kv in data.Signs)
        {
            if (IsSignTag(kv.Key) && !string.IsNullOrEmpty(kv.Value))
                values.Add("CK|" + kv.Key + "|" + kv.Value);
        }

        if (!string.IsNullOrEmpty(data.LogoOwner))
            values.Add("LG|1|" + data.LogoOwner);
        if (!string.IsNullOrEmpty(data.LogoDesign))
            values.Add("LG|2|" + data.LogoDesign);
        foreach (var kv in data.SignFiles)
        {
            if (IsSignTag(kv.Key) && !string.IsNullOrEmpty(kv.Value))
                values.Add("CKF|" + kv.Key + "|" + kv.Value);
        }

        if (!string.IsNullOrEmpty(data.StampPath) || !data.StampOnPdf)
        {
            values.Add("MD|E|" + (data.StampOnPdf ? "1" : "0"));
            if (!string.IsNullOrEmpty(data.StampPath))
                values.Add("MD|P|" + data.StampPath);
        }

        var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
        if (values.Count == 0)
        {
            if (nod.Contains(DictName))
            {
                var sub = (DBDictionary)tr.GetObject(nod.GetAt(DictName), OpenMode.ForRead);
                if (sub.Contains(RecName))
                {
                    var old = tr.GetObject(sub.GetAt(RecName), OpenMode.ForWrite);
                    old.Erase();
                }
            }

            tr.Commit();
            return;
        }

        DBDictionary dict;
        if (nod.Contains(DictName))
        {
            dict = (DBDictionary)tr.GetObject(nod.GetAt(DictName), OpenMode.ForRead);
        }
        else
        {
            nod.UpgradeOpen();
            dict = new DBDictionary();
            nod.SetAt(DictName, dict);
            tr.AddNewlyCreatedDBObject(dict, true);
        }

        var rb = new ResultBuffer();
        foreach (var line in values)
            rb.Add(new TypedValue((int)DxfCode.Text, line));

        if (dict.Contains(RecName))
        {
            var xr = (Xrecord)tr.GetObject(dict.GetAt(RecName), OpenMode.ForWrite);
            xr.Data = rb;
        }
        else
        {
            dict.UpgradeOpen();
            var xr = new Xrecord { Data = rb };
            dict.SetAt(RecName, xr);
            tr.AddNewlyCreatedDBObject(xr, true);
        }

        tr.Commit();
    }

    public static void RememberLogo(Database db, bool owner, string path)
    {
        var data = Load(db);
        if (owner)
            data.LogoOwner = path;
        else
            data.LogoDesign = path;
        Save(db, data);
    }

    private static Xrecord? FindRecord(Transaction tr, Database db, OpenMode mode)
    {
        var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
        if (!nod.Contains(DictName))
            return null;
        var sub = (DBDictionary)tr.GetObject(nod.GetAt(DictName), OpenMode.ForRead);
        if (!sub.Contains(RecName))
            return null;
        return (Xrecord)tr.GetObject(sub.GetAt(RecName), mode);
    }

    private static bool Split(string raw, out string kind, out string key, out string value)
    {
        kind = "";
        key = "";
        value = "";
        var i = raw.IndexOf('|');
        if (i <= 0)
            return false;
        var j = raw.IndexOf('|', i + 1);
        if (j <= i + 1 || j >= raw.Length - 1)
            return false;
        kind = raw.Substring(0, i);
        key = raw.Substring(i + 1, j - i - 1);
        value = raw.Substring(j + 1);
        return kind.Length > 0 && key.Length > 0 && value.Length > 0;
    }

    private static bool IsSignTag(string tag) =>
        tag.Length == 5
        && tag.StartsWith("TEN_", StringComparison.OrdinalIgnoreCase)
        && tag[4] is >= '1' and <= '5';
}
