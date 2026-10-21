using Autodesk.AutoCAD.Runtime;
using iSurvey.Auth;
using iSurvey.Dgn;
using iSurvey.Modules.Export;
using iSurvey.Modules.Import;
using iSurvey.Modules.Map;
using iSurvey.Modules.Plan;
using iSurvey.Modules.Scan;
using iSurvey.Modules.Tuyen;
using iSurvey.Modules.Ai;
using iSurvey.Modules.Frame;
using iSurvey.Modules.Uav;
using iSurvey.UI;

[assembly: ExtensionApplication(typeof(iSurvey.Main))]
#if DEVRELOAD
[assembly: CommandClass(typeof(iSurvey.NoCommands))]
#else
[assembly: CommandClass(typeof(MapModule))]
[assembly: CommandClass(typeof(ExportModule))]
[assembly: CommandClass(typeof(KmlImportModule))]
[assembly: CommandClass(typeof(DgnModule))]
[assembly: CommandClass(typeof(UavModule))]
[assembly: CommandClass(typeof(AiAssistantModule))]
[assembly: CommandClass(typeof(VertexNumberingService))]
[assembly: CommandClass(typeof(StripMapService))]
[assembly: CommandClass(typeof(FrameService))]
[assembly: CommandClass(typeof(PlanModule))]
[assembly: CommandClass(typeof(ScanModule))]
[assembly: CommandClass(typeof(AuthModule))]
[assembly: CommandClass(typeof(iSurvey.CommandAliases))]
#endif

namespace iSurvey;

/// <summary>Điểm vào add-in iSurvey — khởi tạo Ribbon khi AutoCAD nạp DLL.</summary>
public sealed class Main : IExtensionApplication
{
    private MapModule? _mapModule;
    private ExportModule? _exportModule;

    public void Initialize()
    {
        // Chỉ đọc license.dat (không mạng, không dialog) — Ribbon dựa vào trạng thái này để khóa/mở nút.
        AuthManager.Initialize();
        _mapModule = new MapModule();
        _mapModule.Initialize();
        _exportModule = new ExportModule();
        _exportModule.Initialize();
        RibbonBuilder.EnsureRibbon();

        // FILEDIA/CMDDIA = 0 (do script/lệnh khác bỏ quên) làm hộp thoại không mở được → báo và đặt lại 1 (một lần, lúc rảnh).
        Autodesk.AutoCAD.ApplicationServices.Application.Idle += OnFirstIdle;

        // Kiểm tra lại giấy phép (24 giờ/lần) chạy NỀN, timeout ngắn: không chặn Initialize() / thời gian mở Civil.
        AuthManager.StartBackgroundRecheck();
    }

    private static void OnFirstIdle(object? sender, EventArgs e)
    {
        var acad = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager;
        if (acad.MdiActiveDocument is null)
            return;   // chưa có bản vẽ: đợi lần Idle sau

        Autodesk.AutoCAD.ApplicationServices.Application.Idle -= OnFirstIdle;
        try
        {
            var fixedVars = new List<string>();
            foreach (var name in new[] { "FILEDIA", "CMDDIA" })
            {
                var cur = Convert.ToInt32(Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable(name));
                if (cur != 0)
                    continue;
                Autodesk.AutoCAD.ApplicationServices.Application.SetSystemVariable(name, 1);
                fixedVars.Add(name);
            }

            if (fixedVars.Count > 0)
            {
                acad.MdiActiveDocument.Editor.WriteMessage(
                    $"\n[iSurvey] {string.Join(", ", fixedVars)} đang = 0 (làm hộp thoại không hiện) — đã đặt lại 1.");
            }
        }
        catch
        {
            // không chặn việc nạp add-in
        }
    }

    public void Terminate()
    {
        Autodesk.AutoCAD.ApplicationServices.Application.Idle -= OnFirstIdle;
        _exportModule?.Terminate();
        _exportModule = null;
        _mapModule?.Terminate();
        _mapModule = null;
        RibbonBuilder.Cleanup();
    }
}
