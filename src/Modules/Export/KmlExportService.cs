using System.Diagnostics;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using iSurvey.Models;
using iSurvey.Modules.Map;

namespace iSurvey.Modules.Export;

/// <summary>Điều phối thu thập entity → ghi KML/KMZ.</summary>
internal sealed class KmlExportService
{
    private readonly CoordinateService _coordinates = new();

    /// <summary>Chọn đối tượng (nếu cần) rồi thu hình. Cancelled = người dùng hủy chọn.</summary>
    public KmlCollected Collect(Document doc, KmlExportSettings settings)
    {
        var ed = doc.Editor;
        var db = doc.Database;
        List<ObjectId> ids;

        if (settings.SelectionOnly)
        {
            var peo = new PromptSelectionOptions
            {
                MessageForAdding = "\n[iSurvey] Chọn đối tượng cần xuất (Enter kết thúc):"
            };
            var sel = ed.GetSelection(peo);
            if (sel.Status != PromptStatus.OK || sel.Value is null || sel.Value.Count == 0)
                return new KmlCollected { Cancelled = true, Features = new List<KmlFeature>(), Report = "" };

            ids = sel.Value.GetObjectIds().ToList();
        }
        else
        {
            ids = new List<ObjectId>();
            using var trList = db.TransactionManager.StartTransaction();
            var bt = (BlockTable)trList.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)trList.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            foreach (ObjectId id in ms)
            {
                if (!id.IsErased)
                    ids.Add(id);
            }

            trList.Commit();
        }

        using var tr = db.TransactionManager.StartTransaction();
        var collector = new KmlGeometryCollector(
            _coordinates, settings.CentralMeridian, settings.UseElevationZ, settings.ZoneWidthDegrees);
        var features = collector.Collect(tr, db, ids);
        tr.Commit();

        return new KmlCollected
        {
            Cancelled = false,
            Features = features,
            Report = collector.FormatReport()
        };
    }

    public void Write(Database db, KmlExportSettings settings, IReadOnlyList<KmlFeature> features)
    {
        var docName = string.IsNullOrWhiteSpace(db.Filename)
            ? "iSurvey"
            : Path.GetFileNameWithoutExtension(db.Filename);

        KmlWriter.Write(
            settings.OutputPath,
            settings.UseKmz,
            settings.GroupByLayer,
            settings.UseElevationZ,
            features,
            docName);

        if (settings.OpenAfterExport)
            TryOpen(settings.OutputPath);
    }

    internal sealed class KmlCollected
    {
        public bool Cancelled { get; init; }
        public required List<KmlFeature> Features { get; init; }
        public required string Report { get; init; }
    }

    private static void TryOpen(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch
        {
            // User tự mở file
        }
    }
}
