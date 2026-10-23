using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using iSurvey.Auth;
using iSurvey.Core;
using iSurvey.Modules.Map;

namespace iSurvey.Modules.Plan;

/// <summary>
/// Module bình đồ — lệnh CATBD (cắt bình đồ theo biên, không dùng TRIM, nội suy Z).
/// Thông báo Command Line dùng tiếng Việt không dấu (ASCII).
/// </summary>
public sealed class PlanModule : ISurveyModule
{
    /// <summary>Layer bảo vệ tuyệt đối (không phân biệt hoa thường).</summary>
    private static readonly HashSet<string> ProtectedLayers = new(StringComparer.OrdinalIgnoreCase)
    {
        "KHUNGN", "KHUNGT", "LUOIN", "LUOIT", "TEXTLUOI", "TEXTTIEUDE", "TEXTGHICHU"
    };

    private const double BoundaryArcStep = 5.0 * Math.PI / 180.0;
    private const double CloseTolerance = 1e-4;

    public string DisplayName => "Binh do";

    public void Initialize() { }

    public void Terminate() { }

    private sealed class Stats
    {
        public int Protected;
        public int KeptInside;
        public int Erased;
        public int Cut;
        public int NewPieces;
        public int Skipped;
        public int Errors;
        public string? FirstError;
    }

    [CommandMethod("CATBD")]
    public void CutPlanCommand()
    {
        if (!AuthManager.EnsureAuthenticated(track: false))
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        var ed = doc.Editor;
        var db = doc.Database;

        if (!MapModule.IsModelSpace(db))
        {
            ed.WriteMessage("\n[iSurvey] Hay mo tab Model hoac vao Model Space (lenh MSPACE).");
            return;
        }

        // Buoc 1: duong ranh gioi cat khep kin
        var peo = new PromptEntityOptions("\niSurvey: Chọn đường ranh giới cắt khép kín: ")
        {
            AllowNone = false
        };
        peo.SetRejectMessage("\niSurvey: Chỉ chọn LWPolyline hoặc Polyline.");
        peo.AddAllowedClass(typeof(Polyline), true);
        peo.AddAllowedClass(typeof(Polyline2d), true);
        peo.AddAllowedClass(typeof(Polyline3d), true);

        var pe = ed.GetEntity(peo);
        if (pe.Status != PromptStatus.OK)
        {
            ed.WriteMessage("\n[iSurvey] Da huy CATBD.");
            return;
        }

        // Buoc 2: quet chon toan bo ban ve
        var pso = new PromptSelectionOptions
        {
            MessageForAdding = "\niSurvey: Quét chọn bản vẽ cần cắt (gõ ALL để chọn hết): "
        };
        var ps = ed.GetSelection(pso);
        if (ps.Status != PromptStatus.OK)
        {
            ed.WriteMessage("\n[iSurvey] Da huy CATBD.");
            return;
        }

        try
        {
            var stats = Run(db, pe.ObjectId, ps.Value.GetObjectIds());
            ed.WriteMessage(
                $"\n[iSurvey] CATBD xong: bao ve {stats.Protected}, giu trong bien {stats.KeptInside}, " +
                $"xoa {stats.Erased}, cat {stats.Cut} (tao {stats.NewPieces} doan moi), " +
                $"bo qua {stats.Skipped}, loi {stats.Errors}.");
            ActivityLogger.Result("CATBD",
                $"giữ {stats.KeptInside}, xóa {stats.Erased}, cắt {stats.Cut} (tạo {stats.NewPieces}), bỏ qua {stats.Skipped}, lỗi {stats.Errors}, bản vẽ {ActivityLogger.FileName(db.Filename)}");
            if (stats.FirstError is not null)
                ed.WriteMessage($"\n[iSurvey] Loi dau tien: {stats.FirstError}");
            ed.WriteMessage("\n[iSurvey] Go UNDO de hoan tac toan bo lan cat nay.");
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] CATBD that bai: {ex.Message}");
            iSurvey.Auth.ActivityLogger.TrackError(ex);
        }
    }

    private static Stats Run(Database db, ObjectId boundaryId, ObjectId[] ids)
    {
        var stats = new Stats();

        using var tr = db.TransactionManager.StartTransaction();

        var boundaryEnt = (Entity)tr.GetObject(boundaryId, OpenMode.ForRead);
        var poly = BuildBoundary(boundaryEnt, tr);

        var relock = UnlockLockedLayers(db, tr);

        var pm = new ProgressMeter();
        pm.Start("iSurvey CATBD");
        pm.SetLimit(Math.Max(1, ids.Length));
        try
        {
            for (var i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                if (id != boundaryId)
                {
                    try
                    {
                        ProcessOne(id, poly, tr, stats);
                    }
                    catch (System.Exception ex)
                    {
                        stats.Errors++;
                        stats.FirstError ??= ex.Message;
                    }
                }

                if (i % 200 == 0)
                    pm.MeterProgress();
            }
        }
        finally
        {
            pm.Stop();
        }

        foreach (var lid in relock)
        {
            var ltr = (LayerTableRecord)tr.GetObject(lid, OpenMode.ForWrite);
            ltr.IsLocked = true;
        }

        tr.Commit();
        return stats;
    }

    private static ClipPolygon BuildBoundary(Entity boundaryEnt, Transaction tr)
    {
        var info = PathExtractor.TryGet(boundaryEnt, tr)
                   ?? throw new InvalidOperationException("Khong doc duoc duong ranh gioi (Normal phai la truc Z).");

        var first = info.Segs[0].A;
        var last = info.Segs[^1].B;
        var dx = last.X - first.X;
        var dy = last.Y - first.Y;
        if (!info.Closed && Math.Sqrt(dx * dx + dy * dy) > CloseTolerance)
            throw new InvalidOperationException("Duong ranh gioi chua khep kin (dung PEDIT -> Close).");

        var pts = SegmentCutter.Tessellate(info.Segs, BoundaryArcStep);
        if (pts.Count < 3)
            throw new InvalidOperationException("Duong ranh gioi can it nhat 3 dinh.");

        return new ClipPolygon(pts);
    }

    /// <summary>Mo khoa tam thoi layer khoa de sua/xoa duoc; tra ve danh sach de khoa lai.</summary>
    private static List<ObjectId> UnlockLockedLayers(Database db, Transaction tr)
    {
        var locked = new List<ObjectId>();
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        foreach (ObjectId id in lt)
        {
            var ltr = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
            if (!ltr.IsLocked)
                continue;

            ltr.UpgradeOpen();
            ltr.IsLocked = false;
            locked.Add(id);
        }

        return locked;
    }

    private static void ProcessOne(ObjectId id, ClipPolygon poly, Transaction tr, Stats stats)
    {
        if (tr.GetObject(id, OpenMode.ForRead) is not Entity ent)
            return;

        if (ProtectedLayers.Contains(ent.Layer))
        {
            stats.Protected++;
            return;
        }

        switch (ent)
        {
            case Circle:
                Erase(ent, stats);
                return;

            case Line:
            case Polyline:
            case Polyline2d:
            case Polyline3d:
                CutPathEntity(ent, poly, tr, stats);
                return;

            case DBPoint pt:
                KeepOrErase(ent, poly.Contains(pt.Position.X, pt.Position.Y), stats);
                return;

            case DBText text:
            {
                var p = TextReferencePoint(text);
                KeepOrErase(ent, poly.Contains(p.X, p.Y), stats);
                return;
            }

            case MText mtext:
                KeepOrErase(ent, poly.Contains(mtext.Location.X, mtext.Location.Y), stats);
                return;

            default:
            {
                // INSERT va cac loai khac: tam hop bao (GeometricExtents); khong cat.
                if (!TryExtentsCenter(ent, out var c))
                {
                    if (ent is BlockReference br)
                    {
                        KeepOrErase(ent, poly.Contains(br.Position.X, br.Position.Y), stats);
                        return;
                    }

                    stats.Skipped++;
                    return;
                }

                KeepOrErase(ent, poly.Contains(c.X, c.Y), stats);
                return;
            }
        }
    }

    /// <summary>
    /// TEXT co can le (DXF 72/73 khac 0) va AlignmentPoint (DXF 11) khac (0,0): dung AlignmentPoint thay vi Position.
    /// </summary>
    private static Point3d TextReferencePoint(DBText text)
    {
        var aligned = text.HorizontalMode != TextHorizontalMode.TextLeft
                      || text.VerticalMode != TextVerticalMode.TextBase;
        var ap = text.AlignmentPoint;
        if (aligned && (Math.Abs(ap.X) > 0 || Math.Abs(ap.Y) > 0))
            return ap;

        return text.Position;
    }

    private static bool TryExtentsCenter(Entity ent, out Point3d center)
    {
        try
        {
            var ext = ent.GeometricExtents;
            center = new Point3d(
                (ext.MinPoint.X + ext.MaxPoint.X) / 2.0,
                (ext.MinPoint.Y + ext.MaxPoint.Y) / 2.0,
                (ext.MinPoint.Z + ext.MaxPoint.Z) / 2.0);
            return true;
        }
        catch
        {
            center = Point3d.Origin;
            return false;
        }
    }

    private static void KeepOrErase(Entity ent, bool inside, Stats stats)
    {
        if (inside)
            stats.KeptInside++;
        else
            Erase(ent, stats);
    }

    private static void Erase(Entity ent, Stats stats)
    {
        ent.UpgradeOpen();
        ent.Erase();
        stats.Erased++;
    }

    private static void CutPathEntity(Entity ent, ClipPolygon poly, Transaction tr, Stats stats)
    {
        var info = PathExtractor.TryGet(ent, tr);
        if (info is null)
        {
            stats.Skipped++;
            return;
        }

        var result = SegmentCutter.CutPath(poly, info.Segs, info.Closed);
        if (result.Unchanged)
        {
            stats.KeptInside++;
            return;
        }

        if (result.Chains.Count == 0)
        {
            Erase(ent, stats);
            return;
        }

        var btr = (BlockTableRecord)tr.GetObject(ent.OwnerId, OpenMode.ForWrite);
        var created = PathWriter.Write(info, ent, result.Chains, btr, tr);

        ent.UpgradeOpen();
        ent.Erase();
        stats.Cut++;
        stats.NewPieces += created;
    }
}
