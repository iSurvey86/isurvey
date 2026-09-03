using Autodesk.AutoCAD.Runtime;
using iSurvey.Modules.Export;
using iSurvey.Modules.Map;
using iSurvey.UI;

[assembly: ExtensionApplication(typeof(iSurvey.Main))]
#if DEVRELOAD
[assembly: CommandClass(typeof(iSurvey.NoCommands))]
#else
[assembly: CommandClass(typeof(MapModule))]
[assembly: CommandClass(typeof(ExportModule))]
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
        _mapModule = new MapModule();
        _mapModule.Initialize();
        _exportModule = new ExportModule();
        _exportModule.Initialize();
        RibbonBuilder.EnsureRibbon();
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
