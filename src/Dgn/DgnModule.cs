using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using iSurvey.Auth;
using iSurvey.Core;
using iSurvey.Modules.Map;

namespace iSurvey.Dgn;

/// <summary>Module DGN ➔ CAD (Giai đoạn 1): nhập một file .dgn (V7/V8) vào Model Space hiện hành.</summary>
public sealed class DgnModule : ISurveyModule
{
    public string DisplayName => "DGN ➔ CAD";

    public void Initialize() { }

    public void Terminate() { }

    [CommandMethod("ISURVEY_DGN_TO_CAD")]
    public void DgnToCadCommand()
    {
        if (!AuthManager.EnsureAuthenticated())
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        var ed = doc.Editor;
        if (!MapModule.IsModelSpace(doc.Database))
        {
            ed.WriteMessage("\n[iSurvey] Hãy mở Model Space trước khi nhập DGN.");
            return;
        }

        DgnImportOptions? options;
        try
        {
            var drawingPath = string.IsNullOrWhiteSpace(doc.Database.Filename) ? null : doc.Database.Filename;
            options = DgnImportDialog.ShowImport(drawingPath);
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] Không mở được hộp thoại DGN: {ex.Message}");
            return;
        }

        if (options is null)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy nhập DGN.");
            return;
        }

        if (options.IsBatch)
        {
            DgnBatchService.Start(doc, options, (results, combined) =>
                WriteBatchSummary(AcadApp.DocumentManager.MdiActiveDocument?.Editor ?? ed, results, combined));
            return;
        }

        // DGNIMPORT hoàn tất bất đồng bộ: Start trả về ngay, báo cáo in ra khi hậu xử lý xong.
        DgnImportService.Start(doc, options, report =>
            WriteSummary(AcadApp.DocumentManager.MdiActiveDocument?.Editor ?? ed, report));
    }

    /// <summary>CAD ➔ DGN (C2D). UsePickSet: giữ các đối tượng người dùng đã chọn trước khi gõ lệnh.</summary>
    [CommandMethod("ISURVEY_CAD_TO_DGN", CommandFlags.UsePickSet)]
    public void CadToDgnCommand()
    {
        if (!AuthManager.EnsureAuthenticated())
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        var ed = doc.Editor;
        if (!MapModule.IsModelSpace(doc.Database))
        {
            ed.WriteMessage("\n[iSurvey] Hãy mở Model Space trước khi xuất DGN.");
            return;
        }

        // Chụp vùng chọn (PICKFIRST) NGAY, trước khi mở hộp thoại.
        ObjectId[] selected = [];
        try
        {
            var picked = ed.SelectImplied();
            if (picked.Status == PromptStatus.OK)
                selected = picked.Value.GetObjectIds();
        }
        catch
        {
            // không có vùng chọn
        }

        DgnExportOptions? options;
        try
        {
            var drawingPath = string.IsNullOrWhiteSpace(doc.Database.Filename) ? null : doc.Database.Filename;
            options = DgnExportDialog.ShowExport(drawingPath, selected.Length);
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\n[iSurvey] Không mở được hộp thoại xuất DGN: {ex.Message}");
            return;
        }

        if (options is null)
        {
            ed.WriteMessage("\n[iSurvey] Đã hủy xuất DGN.");
            return;
        }

        options.SelectedIds = selected;
        DgnExportService.Start(doc, options, report =>
            WriteExportSummary(AcadApp.DocumentManager.MdiActiveDocument?.Editor ?? ed, report));
    }

    private static void WriteExportSummary(Autodesk.AutoCAD.EditorInput.Editor ed, DgnExportReport r)
    {
        if (r.Error is not null)
        {
            ed.WriteMessage($"\n[iSurvey][C2D] KHÔNG hoàn tất: {r.Error}");
            if (r.LogPath is not null)
                ed.WriteMessage($"\n[iSurvey][C2D] Log chi tiết: {r.LogPath}");
            ShowExportResultBox(r);
            return;
        }

        ed.WriteMessage(
            $"\n[iSurvey][C2D] Hoàn tất: {r.Entities} đối tượng ➔ {r.OutputPath} " +
            $"({r.OutputBytes / 1024.0:0.#} KB, {r.Elapsed.TotalSeconds:0.#}s).");
        ed.WriteMessage($"\n[iSurvey][C2D] Seed: {r.SeedSource}.");
        if (r.UsedScratchCopy)
            ed.WriteMessage("\n[iSurvey][C2D] Đã xuất từ bản sao tạm — bản vẽ đang mở không bị thay đổi.");
        if (r.LogPath is not null)
            ed.WriteMessage($"\n[iSurvey][C2D] Log chi tiết: {r.LogPath}");
        ShowExportResultBox(r);
    }

    /// <summary>Hộp thoại báo kết quả: thành công (có nút mở thư mục) hoặc lỗi.</summary>
    private static void ShowExportResultBox(DgnExportReport r)
    {
        try
        {
            var owner = new Win32Owner(AcadApp.MainWindow.Handle);
            if (r.Error is not null)
            {
                System.Windows.Forms.MessageBox.Show(owner,
                    "Xuất DGN KHÔNG hoàn tất.\n\n" + r.Error +
                    (r.LogPath is null ? "" : "\n\nLog: " + r.LogPath),
                    "iSurvey — Xuất CAD ➔ DGN",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }

            var answer = System.Windows.Forms.MessageBox.Show(owner,
                "Xuất DGN thành công!\n\n" +
                $"File: {r.OutputPath}\n" +
                $"{r.Entities} đối tượng • {r.OutputBytes / 1024.0:0.#} KB • {r.Elapsed.TotalSeconds:0.#}s\n" +
                $"Seed: {r.SeedSource}\n\n" +
                "Mở thư mục chứa file?",
                "iSurvey — Xuất CAD ➔ DGN",
                System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Information);

            if (answer == System.Windows.Forms.DialogResult.Yes && !string.IsNullOrEmpty(r.OutputPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{r.OutputPath}\"",
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // hộp thoại chỉ là tiện ích — kết quả đã có trên Command Line
        }
    }

    private sealed class Win32Owner(IntPtr handle) : System.Windows.Forms.IWin32Window
    {
        public IntPtr Handle { get; } = handle;
    }

    [CommandMethod("ISURVEY_DGN_CONFIG")]
    public void DgnConfigCommand()
    {
        if (!AuthManager.EnsureAuthenticated())
            return;

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        var ed = doc?.Editor;
        try
        {
            var drawingPath = doc is null || string.IsNullOrWhiteSpace(doc.Database.Filename)
                ? null
                : doc.Database.Filename;
            if (DgnImportDialog.ShowConfig(drawingPath))
                ed?.WriteMessage("\n[iSurvey] Đã lưu cấu hình DGN.");
            else
                ed?.WriteMessage("\n[iSurvey] Đã hủy cấu hình DGN.");
        }
        catch (System.Exception ex)
        {
            ed?.WriteMessage($"\n[iSurvey] Không mở được cấu hình DGN: {ex.Message}");
        }
    }

    private static void WriteBatchSummary(
        Autodesk.AutoCAD.EditorInput.Editor ed, IReadOnlyList<DgnBatchFileResult> results, string? combined)
    {
        var ok = results.Count(r => r.Status == DgnBatchStatus.Ok);
        var skipped = results.Count(r => r.Status == DgnBatchStatus.Skipped);
        var failed = results.Count(r => r.Status == DgnBatchStatus.Failed);
        var seconds = results.Sum(r => r.Elapsed.TotalSeconds);

        ed.WriteMessage(
            $"\n[iSurvey][DGN] BATCH xong: {ok} thành công, {skipped} bỏ qua, {failed} lỗi " +
            $"(tổng {results.Count} file, {seconds:0.#}s).");

        if (combined is not null)
        {
            ed.WriteMessage(
                $"\n[iSurvey][DGN] Đã ghép {ok} file vào bản vẽ '{Path.GetFileName(combined)}' (CHƯA LƯU — hãy Save As).");
        }

        foreach (var r in results.Where(r => r.Status == DgnBatchStatus.Ok && r.Output is not null))
            ed.WriteMessage($"\n[iSurvey][DGN]   ✓ {r.Output}");

        foreach (var r in results.Where(r => r.Status == DgnBatchStatus.Failed))
        {
            ed.WriteMessage($"\n[iSurvey][DGN]   ✗ {Path.GetFileName(r.Source)}: {r.Message}");
            if (r.LogPath is not null)
                ed.WriteMessage($"\n[iSurvey][DGN]     log: {r.LogPath}");
        }

        ed.WriteMessage("\n[iSurvey][DGN] Log từng file: %LocalAppData%\\iSurvey\\logs\\dgn_*.log");
    }

    private static void WriteSummary(Autodesk.AutoCAD.EditorInput.Editor ed, DgnImportReport r)
    {
        if (r.Error is not null)
        {
            ed.WriteMessage($"\n[iSurvey][DGN] KHÔNG hoàn tất: {r.Error}");
            if (r.LogPath is not null)
                ed.WriteMessage($"\n[iSurvey][DGN] Log chi tiết: {r.LogPath}");
            return;
        }

        ed.WriteMessage(
            $"\n[iSurvey][DGN] Hoàn tất: {r.ImportedEntities} đối tượng nhập; Explode {r.ExplodedCells} cell; " +
            $"layer đổi tên {r.LayersRenamed}, gộp {r.LayersMerged}; font TCVN3 ➔ Unicode {r.TextsConverted} chuỗi; " +
            $"dọn {r.LinetypesPurged} linetype + {r.LayersPurged} layer + {r.BlocksPurged} block.");

        if (r.Extents is { } e)
        {
            ed.WriteMessage(
                $"\n[iSurvey][DGN] Vùng bao: ({e.MinPoint.X:0.###}, {e.MinPoint.Y:0.###}) – ({e.MaxPoint.X:0.###}, {e.MaxPoint.Y:0.###})");
        }

        foreach (var warning in r.Warnings)
            ed.WriteMessage($"\n[iSurvey][DGN] Cảnh báo: {warning}");

        if (r.ExplodeFailures > 0)
            ed.WriteMessage($"\n[iSurvey][DGN] {r.ExplodeFailures} cell không Explode được (giữ nguyên là block).");

        ed.WriteMessage("\n[iSurvey][DGN] Gõ UNDO 2 lần để hoàn tác (lần 1: hậu xử lý, lần 2: DGNIMPORT).");
        if (r.LogPath is not null)
            ed.WriteMessage($"\n[iSurvey][DGN] Log chi tiết: {r.LogPath}");
    }
}
