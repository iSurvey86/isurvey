using System.Text.RegularExpressions;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using iSurvey.Auth;
using iSurvey.UI.Frame;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Modules.Frame;

public sealed partial class FrameService
{
    /// <summary>Sửa chữ và chữ ký trên khung đã chèn. Nhắp đúp gọi lệnh này. Không cắt lại tuyến, không dựng lại viewport.</summary>
    [CommandMethod("ISURVEY_FRAME_EDIT")]
    public void EditFrameInfoCommand()
    {
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        var id = FrameDoubleClick.TakePending();
        if (id.IsNull)
        {
            var opt = new PromptEntityOptions("\nChọn khung cần sửa thông tin: ")
            {
                AllowNone = false
            };
            opt.SetRejectMessage("\nHãy chọn block khung (KB_A0 … KB_A4).");
            opt.AddAllowedClass(typeof(BlockReference), exactMatch: false);
            var picked = doc.Editor.GetEntity(opt);
            if (picked.Status != PromptStatus.OK)
                return;
            id = picked.ObjectId;
        }

        try
        {
            EditFrameInfo(doc, id);
        }
        catch (System.Exception ex)
        {
            ActivityLogger.TrackError(ex, "ISURVEY_FRAME_EDIT");
            doc.Editor.WriteMessage($"\n[iSurvey] Sửa khung lỗi: {ex.Message}");
        }
        finally
        {
            FrameDoubleClick.Release();
        }
    }

    private static void EditFrameInfo(Document doc, ObjectId id)
    {
        var ed = doc.Editor;
        Dictionary<string, string> attrs;
        HashSet<string> signs;
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            if (!TryResolveFrame(tr, id, out var frame))
            {
                ed.WriteMessage("\n[iSurvey] Không phải block khung KB_A0 … KB_A4.");
                return;
            }

            id = frame.ObjectId;
            attrs = ReadAttributes(tr, frame);
            signs = SignatureTagsOn(tr, frame);
            tr.Commit();
        }

        var result = RouteLayoutWindow.ShowEdit(attrs, signs);
        if (result is null)
            return;

        var imported = ImportSignatureBlocks(doc, result);
        var drop = new HashSet<string>(result.ClearSignatures, StringComparer.OrdinalIgnoreCase);
        foreach (var tag in result.Signatures.Keys)
            drop.Add(tag);

        int count;
        using (doc.LockDocument())
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            if (!TryResolveFrame(tr, id, out var source))
            {
                ed.WriteMessage("\n[iSurvey] Khung vừa chọn không còn trên bản vẽ.");
                return;
            }

            var space = (BlockTableRecord)tr.GetObject(source.OwnerId, OpenMode.ForWrite);
            var targets = new List<BlockReference>();
            if (result.ApplyToAll)
            {
                var ids = new List<ObjectId>();
                foreach (ObjectId entId in space)
                    ids.Add(entId);
                foreach (var entId in ids)
                {
                    if (!entId.IsValid || entId.IsErased)
                        continue;
                    if (tr.GetObject(entId, OpenMode.ForRead) is BlockReference br && IsFrameName(BlockName(tr, br)))
                        targets.Add(br);
                }
            }
            else
            {
                targets.Add(source);
            }

            foreach (var frame in targets)
            {
                WriteAttributes(tr, frame, result.Attributes);
                if (drop.Count > 0)
                    EraseSignatures(tr, space, frame, drop);
                if (imported.Count > 0)
                    PlaceSignatures(tr, space, frame, imported);
            }

            count = targets.Count;
            tr.Commit();
        }

        SyncSignatureMemory(doc.Database, imported, drop);

        ed.Regen();
        var scope = result.ApplyToAll ? "toàn bộ khung" : "khung đang chọn";
        ed.WriteMessage($"\n[iSurvey] Đã cập nhật thông tin {count} khung ({scope}).");
        ActivityLogger.Result("ISURVEY_FRAME_EDIT", $"sửa {scope}, {count} khung");
    }

    internal static bool IsFrameName(string name) =>
        name.Length == 5
        && name.StartsWith("KB_A", StringComparison.OrdinalIgnoreCase)
        && name[4] is '0' or '1' or '2' or '3' or '4';

    internal static string BlockName(Transaction tr, BlockReference br)
    {
        var defId = br.IsDynamicBlock ? br.DynamicBlockTableRecord : br.BlockTableRecord;
        return ((BlockTableRecord)tr.GetObject(defId, OpenMode.ForRead)).Name;
    }

    private static bool TryResolveFrame(Transaction tr, ObjectId id, out BlockReference frame)
    {
        frame = null!;
        if (id.IsNull || !id.IsValid || id.IsErased)
            return false;
        var obj = tr.GetObject(id, OpenMode.ForRead);
        var br = obj as BlockReference;
        if (obj is AttributeReference ar)
            br = tr.GetObject(ar.OwnerId, OpenMode.ForRead) as BlockReference;
        if (br is null || br.IsErased || !IsFrameName(BlockName(tr, br)))
            return false;
        frame = br;
        return true;
    }

    private static Dictionary<string, string> ReadAttributes(Transaction tr, BlockReference frame)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (ObjectId aid in frame.AttributeCollection)
        {
            if (tr.GetObject(aid, OpenMode.ForRead) is not AttributeReference ar || ar.IsConstant)
                continue;
            if (ar.Tag.Equals(FrameFields.SheetTag, StringComparison.OrdinalIgnoreCase)
                || ar.Tag.Equals(FrameFields.ScaleTag, StringComparison.OrdinalIgnoreCase))
                continue;
            map[ar.Tag] = AttributePlain(ar);
        }

        return map;
    }

    private static string AttributePlain(AttributeReference ar)
    {
        var raw = ar.IsMTextAttribute ? ar.MTextAttribute?.Text ?? ar.TextString : ar.TextString;
        raw = Regex.Replace(raw ?? "", @"\\W[^;]*;", "");
        return FrameFields.Clean(raw);
    }

    private static void WriteAttributes(Transaction tr, BlockReference frame, Dictionary<string, string> values)
    {
        foreach (ObjectId aid in frame.AttributeCollection)
        {
            if (tr.GetObject(aid, OpenMode.ForRead) is not AttributeReference ar || ar.IsConstant)
                continue;
            if (ar.Tag.Equals(FrameFields.SheetTag, StringComparison.OrdinalIgnoreCase)
                || ar.Tag.Equals(FrameFields.ScaleTag, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!values.TryGetValue(ar.Tag, out var text))
                continue;
            if (string.Equals(AttributePlain(ar), text, StringComparison.Ordinal))
                continue;
            ar.UpgradeOpen();
            ar.TextString = text;
            TitleBlockFit.Fit(tr, ar, frame);
        }
    }

    private static HashSet<string> SignatureTagsOn(Transaction tr, BlockReference frame)
    {
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (tr.GetObject(frame.OwnerId, OpenMode.ForRead) is not BlockTableRecord space)
            return tags;
        if (!TryExtents(frame, out var ext))
            return tags;
        foreach (ObjectId id in space)
        {
            if (id == frame.ObjectId || !id.IsValid || id.IsErased)
                continue;
            if (tr.GetObject(id, OpenMode.ForRead) is not BlockReference br)
                continue;
            if (!TrySignatureTag(BlockName(tr, br), out var tag))
                continue;
            if (Inside(CenterOf(br), ext, 3))
                tags.Add(tag);
        }

        return tags;
    }

    private static void EraseSignatures(Transaction tr, BlockTableRecord space, BlockReference frame, HashSet<string> tags)
    {
        if (!TryExtents(frame, out var ext))
            return;
        var doomed = new List<BlockReference>();
        foreach (ObjectId id in space)
        {
            if (id == frame.ObjectId || !id.IsValid || id.IsErased)
                continue;
            if (tr.GetObject(id, OpenMode.ForRead) is not BlockReference br || br.IsErased)
                continue;
            if (!TrySignatureTag(BlockName(tr, br), out var tag) || !tags.Contains(tag))
                continue;
            if (!Inside(CenterOf(br), ext, 3))
                continue;
            doomed.Add(br);
        }

        foreach (var br in doomed)
        {
            if (!br.IsWriteEnabled)
                br.UpgradeOpen();
            br.Erase();
        }
    }

    private static bool TrySignatureTag(string blockName, out string tag)
    {
        tag = "";
        const string prefix = "ISURVEY_CK_";
        if (!blockName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;
        var rest = blockName.Substring(prefix.Length);
        var cut = rest.LastIndexOf('_');
        if (cut <= 0)
            return false;
        tag = rest.Substring(0, cut);
        return tag.Length == 5
            && tag.StartsWith("TEN_", StringComparison.OrdinalIgnoreCase)
            && tag[4] is >= '1' and <= '5';
    }

    private static bool TryExtents(BlockReference br, out Extents3d ext)
    {
        try
        {
            ext = br.GeometricExtents;
            return true;
        }
        catch
        {
            ext = default;
            return false;
        }
    }

    private static Point3d CenterOf(BlockReference br)
    {
        if (!TryExtents(br, out var ext))
            return br.Position;
        return new Point3d((ext.MinPoint.X + ext.MaxPoint.X) / 2, (ext.MinPoint.Y + ext.MaxPoint.Y) / 2, 0);
    }

    private static bool Inside(Point3d p, Extents3d ext, double margin) =>
        p.X >= ext.MinPoint.X - margin && p.X <= ext.MaxPoint.X + margin
        && p.Y >= ext.MinPoint.Y - margin && p.Y <= ext.MaxPoint.Y + margin;
}
