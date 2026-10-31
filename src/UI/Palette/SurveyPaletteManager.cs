using System.Drawing;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Windows;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.UI.Palette;

/// <summary>
/// Giữ một PaletteSet cho cả phiên Civil. GUID cố định để AutoCAD nhớ neo và kích thước trong profile.
/// </summary>
internal static class SurveyPaletteManager
{
    private static readonly Guid PaletteId = new("7B2E9A14-6C31-4F08-A5D2-1E8C4B70F6A3");

    private static PaletteSet? _set;
    private static SurveyPaletteView? _view;
    private static PaletteViewModel? _model;
    private static bool _hooked;

    public static void Reload() => LoadCurrent();

    public static void Toggle()
    {
        Ensure();
        if (_set is null)
            return;
        _set.Visible = !_set.Visible;
        if (_set.Visible)
            LoadCurrent();
    }

    public static void Shutdown()
    {
        Unhook();
        try
        {
            if (_set is not null)
                _set.Visible = false;
        }
        catch
        {
            // Civil đang tắt
        }
    }

    private static void Ensure()
    {
        if (_set is not null)
            return;

        _model = new PaletteViewModel();
        _view = new SurveyPaletteView { DataContext = _model };
        _set = new PaletteSet("iSurvey", PaletteId)
        {
            Style = PaletteSetStyles.ShowPropertiesMenu
                    | PaletteSetStyles.ShowAutoHideButton
                    | PaletteSetStyles.ShowCloseButton,
            MinimumSize = new Size(260, 420),
            Size = new Size(300, 700),
            DockEnabled = DockSides.Left | DockSides.Right | DockSides.None,
            KeepFocus = false
        };
        _set.AddVisual("iSurvey", _view);
        _set.Dock = DockSides.Left;
        Hook();
        LoadCurrent();
    }

    private static void Hook()
    {
        if (_hooked)
            return;
        var docs = AcadApp.DocumentManager;
        docs.DocumentBecameCurrent += OnBecameCurrent;
        docs.DocumentToBeDestroyed += OnDestroy;
        _hooked = true;
    }

    private static void Unhook()
    {
        if (!_hooked)
            return;
        var docs = AcadApp.DocumentManager;
        docs.DocumentBecameCurrent -= OnBecameCurrent;
        docs.DocumentToBeDestroyed -= OnDestroy;
        _hooked = false;
    }

    private static void OnBecameCurrent(object? sender, DocumentCollectionEventArgs e) =>
        Run(() => _model?.Load(e.Document));

    private static void OnDestroy(object? sender, DocumentCollectionEventArgs e)
    {
        if (e.Document == AcadApp.DocumentManager.MdiActiveDocument)
            Run(() => _model?.Load(null));
    }

    private static void LoadCurrent() =>
        Run(() => _model?.Load(AcadApp.DocumentManager.MdiActiveDocument));

    private static void Run(Action action)
    {
        var view = _view;
        if (view is null)
        {
            action();
            return;
        }

        if (view.Dispatcher.CheckAccess())
            action();
        else
            view.Dispatcher.Invoke(action);
    }
}
