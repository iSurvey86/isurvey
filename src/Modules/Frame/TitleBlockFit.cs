using System.Globalization;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Modules.Frame;

/// <summary>
/// Ô CHU_DAU_TU và DAI_DIEN_CDT là MText rộng bằng ô khung tên, nên tên dài bị xuống dòng.
/// Co hệ số rộng (\W) để luôn một dòng, không đổi chiều cao chữ.
/// Tên bản vẽ (TEN_BAN_VE) giữ xuống dòng trong ô.
/// </summary>
internal static class TitleBlockFit
{
    internal const string LogoLayer = "KB_LOGO_VE";
    internal const string LogoOwnerLayer = "KB_LOGO_CDT";
    internal const string LogoDesignLayer = "KB_LOGO_TK";
    private const string LogoBlock = "KB_LOGO_EVN";

    private const string RegApp = "ISURVEY_FIT";
    private const double MinFactor = 0.35;

    private static readonly HashSet<string> OneLineTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "CHU_DAU_TU",
        "DAI_DIEN_CDT"
    };

    private static readonly HashSet<string> EditCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "EATTEDIT", "ATTEDIT", "-ATTEDIT", "TEXTEDIT", "DDEDIT", "MTEDIT", "PROPERTIES", "BEDIT", "ATTSYNC"
    };

    private static int _busy;

    public static void Watch()
    {
        var docs = AcadApp.DocumentManager;
        docs.DocumentCreated += (_, e) => Hook(e.Document);
        foreach (Document doc in docs)
            Hook(doc);
    }

    public static int FitDocument(Document doc)
    {
        if (Interlocked.Exchange(ref _busy, 1) == 1)
            return 0;
        try
        {
            using var _ = doc.LockDocument();
            using var tr = doc.Database.TransactionManager.StartTransaction();
            var n = FitDatabase(tr, doc.Database);
            tr.Commit();
            if (n > 0)
                doc.Editor.Regen();
            return n;
        }
        catch
        {
            return 0;
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);
        }
    }

    public static void Fit(Transaction tr, AttributeReference ar, BlockReference owner) =>
        FitOne(tr, ar, owner);

    internal readonly struct LogoSplit
    {
        public int Count { get; init; }

        /// <summary>Nét logo vừa được chuyển từ lớp chung sang lớp 0, nên lần này phải ẩn hẳn chứ không đảo trạng thái.</summary>
        public bool ReleasedSharedLayer { get; init; }
    }

    /// <summary>
    /// Tách hai logo trong khung tên sang hai layer. Nét trong khối logo đang nằm trên
    /// KB_LOGO_VE nên phải đưa về layer 0, lúc đó mỗi hình chèn mới ẩn theo lớp của nó.
    /// </summary>
    public static LogoSplit SeparateLogos(Transaction tr, Database db)
    {
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        if (!bt.Has(LogoBlock))
            return new LogoSplit();

        var released = MoveLogoGeometryToLayerZero(tr, bt);
        EnsureLogoLayer(tr, db, LogoOwnerLayer);
        EnsureLogoLayer(tr, db, LogoDesignLayer);

        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (lt.Has(LogoLayer))
        {
            var old = (LayerTableRecord)tr.GetObject(lt[LogoLayer], OpenMode.ForWrite);
            old.IsFrozen = false;
            old.IsOff = false;
        }

        var count = 0;
        foreach (ObjectId btrId in bt)
        {
            var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
            if (btr.Name.Equals(LogoBlock, StringComparison.OrdinalIgnoreCase))
                continue;

            var logos = new List<BlockReference>();
            Point3d? ownerPt = null;
            Point3d? designPt = null;
            foreach (ObjectId id in btr)
            {
                switch (tr.GetObject(id, OpenMode.ForRead))
                {
                    case BlockReference br when IsLogo(tr, br):
                        logos.Add(br);
                        break;
                    case AttributeDefinition ad when ad.Tag.Equals("CHU_DAU_TU", StringComparison.OrdinalIgnoreCase):
                        ownerPt = TextPoint(ad);
                        break;
                    case AttributeDefinition ad when ad.Tag.Equals("DON_VI_TK", StringComparison.OrdinalIgnoreCase):
                        designPt = TextPoint(ad);
                        break;
                }
            }

            foreach (var br in logos)
            {
                var layer = OwnerLogo(br.Position, ownerPt, designPt, logos) ? LogoOwnerLayer : LogoDesignLayer;
                if (!br.Layer.Equals(layer, StringComparison.OrdinalIgnoreCase))
                {
                    br.UpgradeOpen();
                    br.Layer = layer;
                }

                count++;
            }
        }

        return new LogoSplit { Count = count, ReleasedSharedLayer = released };
    }

    public static string ToggleLogoLayer(Transaction tr, Database db, bool owner, bool forceHide)
    {
        var name = owner ? LogoOwnerLayer : LogoDesignLayer;
        var other = owner ? LogoDesignLayer : LogoOwnerLayer;
        var label = owner ? "logo chủ đầu tư" : "logo đơn vị thiết kế";
        if (forceHide)
        {
            SetLayerOff(tr, db, other, false);
            SetLayerOff(tr, db, name, true);
            return "đã ẩn " + label;
        }

        var hidden = SetLayerOff(tr, db, name, null);
        return hidden ? "đã ẩn " + label : "đã hiện " + label;
    }

    /// <summary>forceOff null thì đảo trạng thái. Trả về lớp có đang tắt hay không.</summary>
    private static bool SetLayerOff(Transaction tr, Database db, string name, bool? forceOff)
    {
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        var layer = (LayerTableRecord)tr.GetObject(lt[name], OpenMode.ForWrite);
        if (layer.IsFrozen)
            layer.IsFrozen = false;
        layer.IsOff = forceOff ?? !layer.IsOff;
        return layer.IsOff;
    }

    /// <summary>
    /// Nét logo tô màu riêng nhưng đang để layer KB_LOGO_VE. Chuyển về layer 0 để mỗi
    /// chỗ chèn ăn theo layer của hình chèn, không tắt cả hai logo một lúc.
    /// </summary>
    private static bool MoveLogoGeometryToLayerZero(Transaction tr, BlockTable bt)
    {
        var btr = (BlockTableRecord)tr.GetObject(bt[LogoBlock], OpenMode.ForRead);
        var moved = false;
        foreach (ObjectId id in btr)
        {
            if (tr.GetObject(id, OpenMode.ForRead) is not Entity ent)
                continue;
            if (ent.Layer.Equals("0", StringComparison.OrdinalIgnoreCase))
                continue;
            if (ent.Color.IsByLayer)
            {
                var src = (LayerTableRecord)tr.GetObject(ent.LayerId, OpenMode.ForRead);
                ent.UpgradeOpen();
                ent.Color = src.Color;
            }
            else
            {
                ent.UpgradeOpen();
            }

            ent.Layer = "0";
            moved = true;
        }

        return moved;
    }

    private static bool OwnerLogo(Point3d logo, Point3d? owner, Point3d? design, List<BlockReference> logos)
    {
        if (owner is null && design is null)
            return logo.Y >= logos.Average(p => p.Position.Y);
        if (design is null)
            return true;
        if (owner is null)
            return false;
        var dOwner = Dist2(logo, owner.Value);
        var dDesign = Dist2(logo, design.Value);
        return dOwner <= dDesign;
    }

    private static bool IsLogo(Transaction tr, BlockReference br)
    {
        var def = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
        return def.Name.Equals(LogoBlock, StringComparison.OrdinalIgnoreCase);
    }

    private static Point3d TextPoint(AttributeDefinition ad) =>
        ad.HorizontalMode == TextHorizontalMode.TextLeft && ad.VerticalMode == TextVerticalMode.TextBase
            ? ad.Position
            : ad.AlignmentPoint;

    private static double Dist2(Point3d a, Point3d b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    private static void EnsureLogoLayer(Transaction tr, Database db, string name)
    {
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (lt.Has(name))
            return;
        lt.UpgradeOpen();
        var layer = new LayerTableRecord { Name = name };
        if (lt.Has(LogoLayer))
        {
            var src = (LayerTableRecord)tr.GetObject(lt[LogoLayer], OpenMode.ForRead);
            layer.Color = src.Color;
            layer.LinetypeObjectId = src.LinetypeObjectId;
        }

        lt.Add(layer);
        tr.AddNewlyCreatedDBObject(layer, true);
    }

    private static void Hook(Document doc)
    {
        doc.CommandEnded += (_, e) =>
        {
            var name = (e.GlobalCommandName ?? "").Trim().TrimStart('_', '.');
            if (!EditCommands.Contains(name))
                return;
            FitDocument(doc);
        };
    }

    private static int FitDatabase(Transaction tr, Database db)
    {
        EnsureApp(db, tr);
        var n = 0;
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        foreach (ObjectId btrId in bt)
        {
            var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
            foreach (ObjectId id in btr)
            {
                var obj = tr.GetObject(id, OpenMode.ForRead);
                if (obj is BlockReference br)
                {
                    foreach (ObjectId aid in br.AttributeCollection)
                    {
                        if (tr.GetObject(aid, OpenMode.ForRead) is AttributeReference ar && FitOne(tr, ar, br))
                            n++;
                    }
                }
                else if (obj is AttributeDefinition ad && OneLineTags.Contains(ad.Tag) && FitDefinition(tr, ad))
                {
                    n++;
                }
            }
        }

        return n;
    }

    private static bool FitOne(Transaction tr, AttributeReference ar, BlockReference owner)
    {
        if (!OneLineTags.Contains(ar.Tag) || ar.IsConstant)
            return false;
        var scale = Math.Abs(owner.ScaleFactors.X);
        if (scale < 1e-9)
            scale = 1;
        var fallback = DefinitionCell(tr, owner, ar.Tag);
        return Apply(tr, ar, scale, ar.IsMTextAttribute ? ar.MTextAttribute : null, mt => ar.MTextAttribute = mt, fallback);
    }

    private static bool FitDefinition(Transaction tr, AttributeDefinition ad)
    {
        if (ad.Constant)
            return false;
        return Apply(tr, ad, 1, ad.IsMTextAttributeDefinition ? ad.MTextAttributeDefinition : null, mt => ad.MTextAttributeDefinition = mt, 0);
    }

    private static bool Apply(Transaction tr, DBText text, double scale, MText? mtext, Action<MText> writeMText, double fallbackCell)
    {
        var plain = Plain(mtext, text.TextString);
        if (string.IsNullOrWhiteSpace(plain))
            return false;

        var cell = CellWidth(text, mtext, scale);
        if (cell < 1)
            cell = fallbackCell;
        if (cell < 1)
            return false;
        Remember(tr, text, cell);

        var natural = NaturalWidth(text.Database, plain, text.Height, text.TextStyleId);
        if (natural < 1e-6)
            return false;

        var factor = Math.Min(1, cell * scale / natural);
        if (factor < MinFactor)
            factor = MinFactor;

        var body = factor < 0.995
            ? "\\W" + factor.ToString("0.###", CultureInfo.InvariantCulture) + ";" + plain
            : plain;

        if (mtext is null)
        {
            if (Math.Abs(text.WidthFactor - factor) < 0.01 && string.Equals(text.TextString, plain, StringComparison.Ordinal))
                return false;
            text.UpgradeOpen();
            text.WidthFactor = factor;
            text.TextString = plain;
            return true;
        }

        if (mtext.Width < 1e-6 && string.Equals(mtext.Contents, body, StringComparison.Ordinal))
            return false;

        text.UpgradeOpen();
        mtext.Width = 0;
        mtext.Contents = body;
        writeMText(mtext);
        return true;
    }

    private static double DefinitionCell(Transaction tr, BlockReference owner, string tag)
    {
        var btr = (BlockTableRecord)tr.GetObject(owner.BlockTableRecord, OpenMode.ForRead);
        foreach (ObjectId id in btr)
        {
            if (tr.GetObject(id, OpenMode.ForRead) is not AttributeDefinition ad
                || !ad.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase))
                continue;
            var saved = ReadCell(ad);
            if (saved > 1)
                return saved;
            if (ad.IsMTextAttributeDefinition && ad.MTextAttributeDefinition.Width > 1)
                return ad.MTextAttributeDefinition.Width;
        }

        return 0;
    }

    /// <summary>Bề rộng ô, theo đơn vị của định nghĩa block (mm trên khung mẫu).</summary>
    private static double CellWidth(DBObject obj, MText? mtext, double scale)
    {
        var saved = ReadCell(obj);
        if (saved > 1)
            return saved;
        if (mtext is not null && mtext.Width > 1 && scale > 1e-9)
            return mtext.Width / scale;
        return 0;
    }

    private static void Remember(Transaction tr, DBObject obj, double cell)
    {
        if (ReadCell(obj) > 1)
            return;
        EnsureApp(obj.Database, tr);
        if (!obj.IsWriteEnabled)
            obj.UpgradeOpen();
        obj.XData = new ResultBuffer(
            new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegApp),
            new TypedValue((int)DxfCode.ExtendedDataReal, cell));
    }

    private static double ReadCell(DBObject obj)
    {
        var data = obj.GetXDataForApplication(RegApp);
        if (data is null)
            return 0;
        foreach (TypedValue tv in data)
        {
            if (tv.TypeCode == (int)DxfCode.ExtendedDataReal)
                return Convert.ToDouble(tv.Value, CultureInfo.InvariantCulture);
        }

        return 0;
    }

    private static void EnsureApp(Database db, Transaction tr)
    {
        var rat = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
        if (rat.Has(RegApp))
            return;
        rat.UpgradeOpen();
        var rec = new RegAppTableRecord { Name = RegApp };
        rat.Add(rec);
        tr.AddNewlyCreatedDBObject(rec, true);
    }

    private static string Plain(MText? mtext, string fallback)
    {
        var raw = mtext?.Text;
        if (string.IsNullOrWhiteSpace(raw))
            raw = fallback ?? "";
        raw = Regex.Replace(raw, @"\\W[^;]*;", "");
        return raw.Replace("\r", "").Replace("\n", "").Trim();
    }

    private static double NaturalWidth(Database db, string text, double height, ObjectId styleId)
    {
        if (height < 1e-9)
            return 0;
        using var tr = db.TransactionManager.StartTransaction();
        var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
        var probe = new DBText
        {
            TextString = text,
            Height = height,
            WidthFactor = 1,
            Position = Point3d.Origin
        };
        if (!styleId.IsNull)
            probe.TextStyleId = styleId;
        ms.AppendEntity(probe);
        tr.AddNewlyCreatedDBObject(probe, true);
        double width;
        try
        {
            var ext = probe.GeometricExtents;
            width = ext.MaxPoint.X - ext.MinPoint.X;
        }
        catch
        {
            width = 0;
        }

        probe.Erase();
        tr.Commit();
        return width;
    }
}
