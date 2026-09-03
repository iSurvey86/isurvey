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

    public string Run(Document doc, KmlExportSettings settings)
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
                throw new InvalidOperationException("Không có đối tượng nào được chọn.");

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

        if (features.Count == 0)
            throw new InvalidOperationException(
                $"Không có hình học nào xuất được ({collector.FormatReport()}).");

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

        return collector.FormatReport();
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
