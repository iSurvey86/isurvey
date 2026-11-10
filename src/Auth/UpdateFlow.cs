using iSurvey.UI;
using iSurvey.UI.Account;

namespace iSurvey.Auth;

/// <summary>
/// Luồng cập nhật dùng chung: lệnh UPD và kiểm tra tự động khi mở Civil.
/// </summary>
public static class UpdateFlow
{
    /// <summary>
    /// Lệnh UPD thủ công: luôn phản hồi (lỗi / đang mới nhất / có bản mới).
    /// </summary>
    public static void RunManualCheck()
    {
        AuthManager.Print("\n[iSurvey] Đang kiểm tra bản cập nhật...");
        var result = UpdateCheckService.Check();
        LogCheck(result, source: "upd");

        if (!result.Ok)
        {
            DarkDialog.ShowInfo(
                "KIỂM TRA CẬP NHẬT",
                "Không kiểm tra được",
                [("Phiên bản hiện tại", AuthManager.AppVersion)],
                note: result.Message);
            return;
        }

        if (!result.UpdateAvailable || result.Manifest is null)
        {
            DarkDialog.ShowInfo(
                "KIỂM TRA CẬP NHẬT",
                "Bạn đang dùng phiên bản mới nhất",
                [("Phiên bản hiện tại", AuthManager.AppVersion)]);
            return;
        }

        PromptAndApply(result.Manifest, result.ForceUpdate, source: "upd");
    }

    /// <summary>
    /// Khi đã biết có bản mới: hiện hộp như UPD; tải + helper nếu người dùng cập nhật ngay.
    /// </summary>
    public static void PromptAndApply(UpdateCheckService.Manifest man, bool forceUpdate, string source)
    {
        var setupDir = UpdateWindow.ShowAvailable(man, forceUpdate);
        if (string.IsNullOrEmpty(setupDir))
            return;

        UpdateApplyService.Trace("quit_prepare setup=" + setupDir + " source=" + source);
        var launched = UpdateApplyService.LaunchHelper(setupDir!, man.Latest);
        var quitDetail = new
        {
            current = AuthManager.AppVersion,
            latest = man.Latest,
            ok = launched.Ok,
            message = launched.Message,
            stage = "quit",
            source,
        };
        try
        {
            var access = AuthManager.AcquireAccessToken();
            if (access is not null)
                ActivityLogger.SendNow(access, "update_quit", "UPD", quitDetail, null, CancellationToken.None);
            else
                ActivityLogger.Enqueue("update_quit", "UPD", quitDetail);
        }
        catch
        {
            ActivityLogger.Enqueue("update_quit", "UPD", quitDetail);
        }

        ActivityLogger.FlushNow();

        if (!launched.Ok)
        {
            UpdateApplyService.Trace("helper_fail " + launched.Message);
            DarkDialog.ShowInfo("CẬP NHẬT iSurvey", "Không chạy được trình cài", note: launched.Message);
            AuthManager.Print("\n[iSurvey] " + launched.Message);
            return;
        }

        AuthManager.Print("\n[iSurvey] Đang đóng AutoCAD/Civil để cài đặt...");
    }

    public static void LogCheck(UpdateCheckService.CheckResult result, string source)
    {
        ActivityLogger.Enqueue("update_check", detail: new
        {
            current = AuthManager.AppVersion,
            latest = result.Manifest?.Latest,
            available = result.UpdateAvailable,
            ok = result.Ok,
            source,
        });
    }
}
