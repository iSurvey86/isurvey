using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Modules.Map;

/// <summary>Tự refresh ảnh nền khi pan/zoom (Application.Idle).</summary>
public static class AutoRefreshController
{
    private static DateTime _nextCheckUtc = DateTime.MinValue;
    private static string? _lastViewSignature;
    private static bool _queued;
    private static DateTime _queuedUtc = DateTime.MinValue;
    private static bool _subscribed;

    private static readonly BasemapRefreshService RefreshService = new();

    public static void Subscribe()
    {
        if (_subscribed)
            return;

        AcadApp.Idle += OnIdle;
        _subscribed = true;
    }

    public static void Unsubscribe()
    {
        if (!_subscribed)
            return;

        AcadApp.Idle -= OnIdle;
        _subscribed = false;
    }

    public static void ResetViewSignature() => _lastViewSignature = null;

    private static void OnIdle(object? sender, EventArgs e)
    {
        try
        {
            if (DateTime.UtcNow < _nextCheckUtc)
                return;

            _nextCheckUtc = DateTime.UtcNow.AddMilliseconds(350);

            if (!BasemapSession.IsActive || !BasemapSession.AutoRefresh
                || string.IsNullOrWhiteSpace(BasemapSession.BasemapId))
                return;

            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc is null || !doc.Database.TileMode)
                return;

            if (Convert.ToInt32(AcadApp.GetSystemVariable("CMDACTIVE"), CultureInfo.InvariantCulture) != 0)
                return;

            if (_queued && DateTime.UtcNow - _queuedUtc > TimeSpan.FromSeconds(12))
                _queued = false;

            if (_queued)
                return;

            var signature = BuildViewSignature(doc);
            if (_lastViewSignature is null)
            {
                _lastViewSignature = signature;
                return;
            }

            if (string.Equals(signature, _lastViewSignature, StringComparison.Ordinal))
                return;

            _lastViewSignature = signature;
            _queued = true;
            _queuedUtc = DateTime.UtcNow;

            try
            {
                RefreshService.Refresh(doc, BasemapSession.CentralMeridian, BasemapSession.BasemapId);
            }
            catch (Exception ex)
            {
                doc.Editor.WriteMessage($"\n[iSurvey] AutoRefresh: {ex.Message}");
            }
            finally
            {
                _queued = false;
            }
        }
        catch
        {
            _queued = false;
        }
    }

    private static string BuildViewSignature(Document doc)
    {
        using var view = doc.Editor.GetCurrentView();
        var c = view.CenterPoint;
        var t = view.Target;
        return string.Format(CultureInfo.InvariantCulture,
            "{0:0.###}|{1:0.###}|{2:0.###}|{3:0.###}|{4:0.######}|{5:0.###}|{6:0.###}",
            c.X, c.Y, view.Width, view.Height, view.ViewTwist, t.X, t.Y);
    }
}
