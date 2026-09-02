using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using iSurvey.Models;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Modules.Map;

/// <summary>Refresh ảnh nền theo khung nhìn hiện tại.</summary>
public sealed class BasemapRefreshService
{
    private readonly CoordinateService _coordinates = new();

    public RefreshResult Refresh(Document doc, double centralMeridian, string basemapId)
    {
        var ed = doc.Editor;
        var db = doc.Database;

        if (!MapModule.IsModelSpace(db))
            throw new InvalidOperationException("Chỉ làm việc trong Model Space.");

        var viewport = ViewportSnapshot.Capture(doc);
        var viewWgs = viewport.ToWgs84Bounds(_coordinates, centralMeridian);
        var wgsBounds = viewWgs;

        if (BasemapSession.HasBoundaryClip)
        {
            var boundaryWgs = BoundaryBoundsHelper.ToWgs84Bounds(
                BasemapSession.ClipPolygonWcs!, _coordinates, centralMeridian);
            wgsBounds = BoundaryBoundsHelper.Intersect(viewWgs, boundaryWgs);
            if (!BoundaryBoundsHelper.IsValid(wgsBounds))
                throw new InvalidOperationException("Đường bao không giao với khung nhìn hiện tại.");
        }

        var zoom = TileMath.FitZoom(wgsBounds, viewport.ScreenWidth, viewport.ScreenHeight);
        var tileList = TileMath.EnumerateTiles(wgsBounds, zoom).ToList();

        if (BasemapSession.HasBoundaryClip)
        {
            var clip = BasemapSession.ClipPolygonWcs!;
            tileList = tileList
                .Where(t => TileBoundaryFilter.IntersectsTile(
                    t.X, t.Y, zoom, _coordinates, centralMeridian, clip))
                .ToList();
        }

        var total = tileList.Count;

        if (total == 0 && BasemapSession.HasBoundaryClip)
            throw new InvalidOperationException("Không có tile nào giao đường bao trong khung nhìn — hãy zoom/pan lại.");

        var latMid = (wgsBounds.MinLatitude + wgsBounds.MaxLatitude) / 2.0;
        var mpp = TileMath.MetersPerPixelAtZoom(zoom, latMid);
        var scopeLabel = BasemapSession.HasBoundaryClip ? "đường bao" : "khung nhìn";
        ed.WriteMessage($"\n[iSurvey] Refresh ({scopeLabel}) — {basemapId}, zoom {zoom}, {total} tile (~{mpp:0.##} m/px).");

        ed.WriteMessage("\n[iSurvey] Đang tải tile...");
        IReadOnlyList<(int X, int Y, int Z, string Path)> ready;

        try
        {
            using var downloader = new TileDownloader(basemapId);
            var progress = new Progress<(int Done, int Total)>(p =>
            {
                if (p.Done % 24 == 0 || p.Done == p.Total)
                {
                    ed.WriteMessage($"\n[iSurvey] ... {p.Done}/{p.Total}");
                    try { ed.UpdateScreen(); } catch { /* bỏ qua */ }
                }
            });
            ready = downloader.EnsureTilesAsync(tileList, zoom, progress).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Lỗi tải tile: {ex.Message}", ex);
        }

        var failed = total - ready.Count;
        ed.WriteMessage($"\n[iSurvey] Tile: {ready.Count}/{total} OK, {failed} lỗi.");

        if (ready.Count == 0)
            throw new InvalidOperationException("Không tải được tile nào — kiểm tra mạng.");

        ed.WriteMessage("\n[iSurvey] Gắn tile...");
        RasterClearService.ClearAll(db);
        var attach = TileAttachService.AttachTiles(
            db, basemapId, ready, _coordinates, centralMeridian, BasemapSession.ClipPolygonWcs);
        ed.Regen();

        var clipNote = BasemapSession.HasBoundaryClip ? " (clip đường bao)" : string.Empty;
        ed.WriteMessage($"\n[iSurvey] Đã gắn {attach.Attached} tile{clipNote} (lỗi {attach.Failed}). Zoom in sẽ tự nét hơn (AutoRefresh).");

        return new RefreshResult
        {
            Zoom = zoom,
            TileCount = attach.Attached,
            FailedTileCount = failed + attach.Failed,
            Bounds = wgsBounds
        };
    }

    public void Clear(Document doc)
    {
        var count = RasterClearService.ClearAll(doc.Database);
        doc.Editor.Regen();
        doc.Editor.WriteMessage(count > 0
            ? $"\n[iSurvey] Đã xóa {count} tile."
            : "\n[iSurvey] Không có tile iSurvey.");
        BasemapSession.Deactivate();
    }
}

public sealed class RefreshResult
{
    public int Zoom { get; init; }
    public int TileCount { get; init; }
    public int FailedTileCount { get; init; }
    public GeoBounds Bounds { get; init; }
}
