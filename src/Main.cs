using Autodesk.AutoCAD.Runtime;
using iSurvey.Auth;
using iSurvey.Modules.Export;
using iSurvey.Modules.Import;
using iSurvey.Modules.Map;
using iSurvey.Modules.Plan;
using iSurvey.Modules.Scan;
using iSurvey.UI;

[assembly: ExtensionApplication(typeof(iSurvey.Main))]
#if DEVRELOAD
[assembly: CommandClass(typeof(iSurvey.NoCommands))]
#else
[assembly: CommandClass(typeof(MapModule))]
[assembly: CommandClass(typeof(ExportModule))]
[assembly: CommandClass(typeof(KmlImportModule))]
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

        // Kiểm tra lại giấy phép (24 giờ/lần) chạy NỀN, timeout ngắn: không chặn Initialize() / thời gian mở Civil.
        AuthManager.StartBackgroundRecheck();
    }

    public void Terminate()
    {
        _exportModule?.Terminate();
        _exportModule = null;
        _mapModule?.Terminate();
        _mapModule = null;
        RibbonBuilder.Cleanup();
    }
}
