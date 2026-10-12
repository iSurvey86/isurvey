using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using iSurvey.Core;
using iSurvey.UI.Account;

namespace iSurvey.Auth;

/// <summary>
/// Lệnh tài khoản & hệ thống (Panel 6): đăng nhập, cập nhật, thông tin giấy phép, đăng xuất, sinh OTP cho Admin.
/// Các lệnh này KHÔNG bị khóa bởi đăng nhập. Thông báo Command Line dùng tiếng Việt không dấu (ASCII).
/// </summary>
public sealed class AuthModule : ISurveyModule
{
    public string DisplayName => "Tai khoan";

    public void Initialize() { }

    public void Terminate() { }

    /// <summary>Chưa đăng nhập: mở cửa sổ Login. Đã đăng nhập: hiện thông tin tài khoản.</summary>
    [CommandMethod("ISURVEY_LOGIN")]
    public void LoginCommand()
    {
        AuthManager.Refresh();
        if (AuthManager.IsAuthenticated)
            ShowAbout();
        else
            AuthManager.ShowLogin();
    }

    [CommandMethod("ISURVEY_ABOUT")]
    public void AboutCommand()
    {
        AuthManager.Refresh();
        ShowAbout();
    }

    [CommandMethod("ISURVEY_UPDATE")]
    public void UpdateCommand()
    {
        DarkDialog.ShowInfo(
            "KIỂM TRA CẬP NHẬT",
            "Bạn đang dùng phiên bản mới nhất",
            [("Phiên bản hiện tại", AuthManager.AppVersion)],
            note: "Chức năng tự kiểm tra bản mới trực tuyến sẽ được bổ sung ở phiên bản sau.");
    }

    [CommandMethod("ISURVEY_LOGOUT")]
    public void LogoutCommand()
    {
        AuthManager.Refresh();
        if (!AuthManager.IsAuthenticated)
        {
            AuthManager.Print("\n[iSurvey] Chua dang nhap.");
            return;
        }

        var email = AuthManager.License!.Email;
        var ok = DarkDialog.Confirm(
            "ĐĂNG XUẤT / ĐỔI MÁY",
            $"Đăng xuất tài khoản {email} khỏi máy này?\n\nGiấy phép trên máy sẽ bị xóa và các tính năng bị khóa cho tới khi kích hoạt lại.\nNếu đang kích hoạt trực tuyến, suất thiết bị sẽ được trả lại để dùng trên máy khác.",
            "Đăng xuất");
        if (!ok)
        {
            AuthManager.Print("\n[iSurvey] Da huy dang xuat.");
            return;
        }

        AuthManager.SignOut();
        AuthManager.Print("\n[iSurvey] Da dang xuat. Cac tinh nang da bi khoa.");
    }

    /// <summary>
    /// Dành cho Admin: nhập Gmail người dùng → in mã 6 số của ngày hôm nay.
    /// Chỉ chạy khi auth.config.json trên máy này có khóa AdminSecret.
    /// </summary>
    [CommandMethod("ISURVEY_GENOTP")]
    public void GenerateOtpCommand()
    {
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;

        var ed = doc.Editor;
        var config = AuthConfig.Load();
        if (!config.HasAdminSecret)
        {
            ed.WriteMessage("\n[iSurvey] Lenh nay chi chay tren may Admin (can khoa AdminSecret trong auth.config.json).");
            return;
        }

        var pso = new PromptStringOptions("\nNhap Gmail cua nguoi dung can cap ma: ") { AllowSpaces = false };
        var res = ed.GetString(pso);
        if (res.Status != PromptStatus.OK)
            return;

        if (!OtpAuthService.TryNormalizeGmail(res.StringResult, out var email))
        {
            ed.WriteMessage("\n[iSurvey] Gmail khong hop le (chi nhan @gmail.com).");
            return;
        }

        var now = DateTime.Now;
        var code = OtpAuthService.ComputeAdminCode(config.AdminSecret!, email, now);
        ed.WriteMessage($"\n[iSurvey] Ma kich hoat cho {email} ngay {now:dd/MM/yyyy}: {code}");
        ed.WriteMessage("\n[iSurvey] (Ma chi co hieu luc trong ngay hom nay; gui cho nguoi dung de nhap vao o xac thuc.)");
    }

    private static void ShowAbout()
    {
        var lic = AuthManager.License;
        if (lic is null)
        {
            DarkDialog.ShowInfo(
                "THÔNG TIN & GIẤY PHÉP",
                "iSurvey",
                [
                    ("Phiên bản", AuthManager.AppVersion),
                    ("Trạng thái", "Chưa kích hoạt"),
                    ("Mã thiết bị (HWID)", HardwareId.Current)
                ],
                note: "Bấm \"Đăng Nhập / Kích Hoạt\" trên Ribbon để kích hoạt bản quyền.");
            return;
        }

        DarkDialog.ShowInfo(
            "THÔNG TIN & GIẤY PHÉP",
            "iSurvey",
            [
                ("Phiên bản", AuthManager.AppVersion),
                ("Gmail kích hoạt", lic.Email),
                ("Trạng thái", "Đã kích hoạt"),
                ("Mã thiết bị (HWID)", lic.HardwareId),
                ("Ngày kích hoạt", lic.ActivatedAt.LocalDateTime.ToString("dd/MM/yyyy HH:mm")),
                ("Hình thức", lic.Mode == LicenseMode.Online ? "Trực tuyến (Supabase)" : "Ngoại tuyến (mã Admin)"),
                ("Xác minh gần nhất", lic.VerifiedAt.LocalDateTime.ToString("dd/MM/yyyy HH:mm")),
                ("Hạn dùng khi không có mạng", lic.ExpiresAt.LocalDateTime.ToString("dd/MM/yyyy HH:mm"))
            ],
            note: lic.Mode == LicenseMode.Online
                ? "iSurvey tự xác minh lại ngầm mỗi 24 giờ khi mở Civil 3D. Mất mạng vẫn dùng được tới hạn trên."
                : "Giấy phép ngoại tuyến chỉ có hiệu lực 7 ngày; hãy kích hoạt trực tuyến khi có mạng.");
    }
}
