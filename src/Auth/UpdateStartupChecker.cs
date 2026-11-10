using System.Threading;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Auth;

/// <summary>
/// Khi mở Civil/CAD: kiểm tra version.json nền; chỉ hiện hộp nếu có bản mới.
/// Mỗi phiên process hỏi tối đa một lần; lần mở ứng dụng sau hỏi lại.
/// </summary>
public static class UpdateStartupChecker
{
    private static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(4);
    private static int _started;

    public static void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(StartDelay).ConfigureAwait(false);
                var result = UpdateCheckService.Check();
                UpdateFlow.LogCheck(result, source: "auto");

                if (!result.Ok || !result.UpdateAvailable || result.Manifest is null)
                    return;

                var man = result.Manifest;
                var force = result.ForceUpdate;
                AuthManager.PostToUi(() => ShowWhenReady(man, force));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[iSurvey] Auto update check: " + ex.Message);
            }
        });
    }

    /// <summary>
    /// ShowModalWindow cần đã có bản vẽ. Chưa có thì đợi Idle (một lần).
    /// </summary>
    private static void ShowWhenReady(UpdateCheckService.Manifest man, bool force)
    {
        try
        {
            if (AcadApp.DocumentManager.MdiActiveDocument is not null)
            {
                UpdateFlow.PromptAndApply(man, force, source: "auto");
                return;
            }

            void OnIdle(object? sender, EventArgs e)
            {
                if (AcadApp.DocumentManager.MdiActiveDocument is null)
                    return;

                AcadApp.Idle -= OnIdle;
                try
                {
                    UpdateFlow.PromptAndApply(man, force, source: "auto");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("[iSurvey] Auto update UI: " + ex.Message);
                }
            }

            AcadApp.Idle += OnIdle;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[iSurvey] Auto update UI: " + ex.Message);
        }
    }
}
