using Autodesk.AutoCAD.Runtime;
using iSurvey.Modules.Map;
using iSurvey.UI;

[assembly: ExtensionApplication(typeof(iSurvey.Main))]
#if DEVRELOAD
[assembly: CommandClass(typeof(iSurvey.NoCommands))]
#else
[assembly: CommandClass(typeof(MapModule))]
#endif

namespace iSurvey;

/// <summary>Điểm vào add-in iSurvey — khởi tạo Ribbon khi AutoCAD nạp DLL.</summary>
public sealed class Main : IExtensionApplication
{
    private MapModule? _mapModule;

    public void Initialize()
    {
        _mapModule = new MapModule();
        _mapModule.Initialize();
        RibbonBuilder.EnsureRibbon();
    }

    public void Terminate()
    {
        _mapModule?.Terminate();
        _mapModule = null;
        RibbonBuilder.Cleanup();
    }
}
