using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.Windows;
using iSurvey.Auth;
using System.Windows.Media.Imaging;

namespace iSurvey.UI;

/// <summary>Tạo tab Ribbon iSurvey (6 panel) — khởi tạo an toàn khi Ribbon chưa sẵn sàng.</summary>
public static class RibbonBuilder
{
    private const string TabId = "ISURVEY_RIBBON_TAB";
    private const string LoginCommand = "ISURVEY_LOGIN";
    private const string LogoutCommand = "ISURVEY_LOGOUT";

    private static bool _subscribed;

    /// <summary>Nút Panel 1–5: bị khóa (IsEnabled=false) khi chưa đăng nhập.</summary>
    private static readonly List<RibbonButton> GatedButtons = [];

    private static RibbonButton? _accountButton;
    private static RibbonButton? _logoutButton;

    /// <summary>Mô tả một nút. Implemented=true: gửi lệnh thật; false: in thông báo "đang cập nhật".</summary>
    private sealed record Btn(string Text, string Command, Func<int, BitmapSource> Icon, bool Implemented);

    public static void EnsureRibbon()
    {
        if (!_subscribed)
        {
            ComponentManager.ItemInitialized += OnItemInitialized!;
            AuthManager.StateChanged += UpdateAuthState;
            _subscribed = true;
        }

        TryBuild();
    }

    /// <summary>Gỡ Ribbon và handler — gọi từ Terminate() khi reload qua DevReload.</summary>
    public static void Cleanup()
    {
        if (_subscribed)
        {
            ComponentManager.ItemInitialized -= OnItemInitialized!;
            AuthManager.StateChanged -= UpdateAuthState;
            _subscribed = false;
        }

        try
        {
            var ribbon = ComponentManager.Ribbon;
            var tab = ribbon?.FindTab(TabId);
            if (tab is not null)
                ribbon!.Tabs.Remove(tab);
        }
        catch
        {
            // Ribbon có thể đã bị hủy khi thoát AutoCAD
        }

        GatedButtons.Clear();
        _accountButton = null;
        _logoutButton = null;
    }

    private static void OnItemInitialized(object? sender, RibbonItemEventArgs e)
    {
        TryBuild();
    }

    private static void TryBuild()
    {
        try
        {
            var ribbon = ComponentManager.Ribbon;
            if (ribbon is null)
                return;

            if (ribbon.FindTab(TabId) is not null)
                return;

            GatedButtons.Clear();
            _accountButton = null;
            _logoutButton = null;

            var tab = new RibbonTab
            {
                Title = "iSurvey",
                Id = TabId
            };

            // Panel 1 — Bản đồ
            AddPanel(tab, "Bản đồ",
                [
                    new Btn("Nhập File\nKML/KMZ", "ISURVEY_IMPORT_KML", RibbonIcons.ImportKml, true),
                    new Btn("Xuất File\nKML/KMZ", "ISURVEY_EXPORT_KML", RibbonIcons.ExportKml, true)
                ],
                [
                    new Btn("Chèn Ảnh Vệ Tinh (GE)", "ISURVEY_MAP_SAT", RibbonIcons.MapInsert, true),
                    new Btn("Cập Nhật Ảnh GE", "ISURVEY_UPDATE_GE", RibbonIcons.UpdateGe, true),
                    new Btn("Xóa Ảnh Vệ Tinh", "ISURVEY_DELETE_GE", RibbonIcons.MapDelete, true)
                ]);

            // Panel 2 — Khung & Cắt Bình Đồ
            AddPanel(tab, "Khung & Cắt Bình Đồ",
                [
                    new Btn("Tạo Khung\nLưới TĐ", "TAOKHUNG", RibbonIcons.TaoKhung, false),
                    new Btn("Cắt Bình Đồ", "CATBD", RibbonIcons.CatBd, true)
                ],
                [
                    new Btn("Rải Khung Theo Tuyến", "RAIKHUNG", RibbonIcons.RaiKhung, false),
                    new Btn("Thiết Lập Tỉ Lệ Khung", "TILEKHUNG", RibbonIcons.TileKhung, false),
                    new Btn("Bảo Vệ Layer Khung", "KHOAKHUNG", RibbonIcons.KhoaKhung, false)
                ]);

            // Panel 3 — Biên tập Địa hình
            AddPanel(tab, "Biên tập Địa hình",
                [
                    new Btn("Ghi Số\nĐồng Mức", "GHISODM", RibbonIcons.GhiSoDm, false)
                ],
                [
                    new Btn("Lọc Giãn Điểm Mia", "LOCDIEM", RibbonIcons.LocDiem, false),
                    new Btn("Ẩn / Hiện Lưới TIN", "ANTIN", RibbonIcons.AnTin, false),
                    new Btn("Chuẩn Hóa Mã Font", "CHUYENFONT", RibbonIcons.ChuyenFont, false)
                ]);

            // Panel 4 — Tuyến & Cột Điện
            AddPanel(tab, "Tuyến & Cột Điện",
                [
                    new Btn("Rải Móng &\nBiên Đất", "RAIMONG", RibbonIcons.RaiMong, false)
                ],
                [
                    new Btn("Tạo Biên Đường Tạm", "BIENDUONGTAM", RibbonIcons.BienDuongTam, false),
                    new Btn("Kẻ Tuyến 110kV/220kV", "KETUYEN", RibbonIcons.KeTuyen, false),
                    new Btn("Đánh Số Hiệu Cột", "DANHSOCOT", RibbonIcons.DanhSoCot, false)
                ]);

            // Panel 5 — Hồ sơ & QA/QC
            AddPanel(tab, "Hồ sơ & QA/QC",
                [
                    new Btn("Bảng TĐ &\nDiện Tích", "BANGTOADO", RibbonIcons.BangToaDo, false)
                ],
                [
                    new Btn("Kiểm Tra Lỗi Cao Độ", "KIEMTRACAODO", RibbonIcons.KiemTraCaoDo, false),
                    new Btn("Quét Cấu Trúc (AutoScan)", "AUTOSCAN", RibbonIcons.AutoScan, true),
                    new Btn("Xuất Thống Kê Excel", "XUATEXCEL", RibbonIcons.XuatExcel, false)
                ]);

            // Panel 6 — Tài khoản & Hệ thống (luôn bật; nút Đăng xuất chỉ bật khi đã đăng nhập)
            AddPanel(tab, "Tài khoản & Hệ thống",
                [
                    new Btn("Đăng Nhập\nKích Hoạt", LoginCommand, AuthIcons.UserLocked, true)
                ],
                [
                    new Btn("Kiểm Tra Cập Nhật", "ISURVEY_UPDATE", AuthIcons.Refresh, true),
                    new Btn("Thông Tin & Giấy Phép", "ISURVEY_ABOUT", AuthIcons.Info, true),
                    new Btn("Đăng Xuất / Đổi Máy", LogoutCommand, AuthIcons.Power, true)
                ],
                gated: false);

            ribbon.Tabs.Add(tab);
            UpdateAuthState();
        }
        catch (System.Exception ex)
        {
            AcadApp.DocumentManager.MdiActiveDocument?
                .Editor.WriteMessage($"\n[iSurvey] Khong tao duoc Ribbon: {ex.Message}");
        }
    }

    /// <summary>
    /// Đồng bộ Ribbon với trạng thái đăng nhập (không cần khởi động lại Civil):
    /// khóa/mở Panel 1–5, đổi nút tài khoản (nhãn, icon, tooltip), bật/tắt nút Đăng xuất.
    /// </summary>
    public static void UpdateAuthState()
    {
        try
        {
            var license = AuthManager.License;
            var authed = license is not null;

            foreach (var button in GatedButtons)
                button.IsEnabled = authed;

            if (_logoutButton is not null)
                _logoutButton.IsEnabled = authed;

            if (_accountButton is null)
                return;

            if (license is null)
            {
                _accountButton.Text = "Đăng Nhập\nKích Hoạt";
                _accountButton.ToolTip = "Click để đăng nhập Gmail và kích hoạt bản quyền iSurvey";
                SetIcons(_accountButton, AuthIcons.UserLocked);
            }
            else
            {
                _accountButton.Text = $"{ShortName(license.Email)}\n● Đã kích hoạt";
                _accountButton.ToolTip =
                    $"Tài khoản: {license.Email} | Trạng thái: Đã kích hoạt | " +
                    $"Mã thiết bị (HWID): {license.HardwareId} | Phiên bản: {AuthManager.AppVersion}";
                SetIcons(_accountButton, AuthIcons.UserActive);
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[iSurvey] UpdateAuthState: " + ex.Message);
        }
    }

    /// <summary>Phần trước '@', cắt gọn để nút Ribbon không bị quá rộng.</summary>
    private static string ShortName(string email)
    {
        var at = email.IndexOf('@');
        var name = at > 0 ? email[..at] : email;
        return name.Length > 12 ? name[..11] + "…" : name;
    }

    private static void SetIcons(RibbonButton button, Func<int, BitmapSource> icon)
    {
        try
        {
            button.LargeImage = icon(32);
            button.Image = icon(16);
        }
        catch
        {
            // Icon tùy chọn — bỏ qua nếu lỗi
        }
    }

    /// <summary>Một panel: các nút Large (32x32, dọc) + một cụm nút nhỏ (16x16, ngang) xếp nhiều dòng.</summary>
    private static void AddPanel(RibbonTab tab, string title, IReadOnlyList<Btn> large, IReadOnlyList<Btn>? small,
        bool gated = true)
    {
        var source = new RibbonPanelSource { Title = title };

        foreach (var b in large)
            source.Items.Add(CreateButton(b, isLarge: true, gated));

        if (small is { Count: > 0 })
        {
            var rows = new RibbonRowPanel();
            for (var i = 0; i < small.Count; i++)
            {
                if (i > 0)
                    rows.Items.Add(new RibbonRowBreak());
                rows.Items.Add(CreateButton(small[i], isLarge: false, gated));
            }

            source.Items.Add(rows);
        }

        tab.Panels.Add(new RibbonPanel { Source = source });
    }

    private static RibbonButton CreateButton(Btn b, bool isLarge, bool gated)
    {
        var button = new RibbonButton
        {
            Text = b.Text,
            ShowText = true,
            Size = isLarge ? RibbonItemSize.Large : RibbonItemSize.Standard,
            Orientation = isLarge
                ? System.Windows.Controls.Orientation.Vertical
                : System.Windows.Controls.Orientation.Horizontal,
            CommandHandler = b.Implemented
                ? new RibbonCommandHandler(b.Command + "\n")
                : new RibbonPlaceholderHandler(b.Command),
            ToolTip = $"{b.Text.Replace("\n", " ")} ({b.Command})"
        };

        SetIcons(button, b.Icon);

        if (gated)
            GatedButtons.Add(button);
        else if (b.Command == LoginCommand)
            _accountButton = button;
        else if (b.Command == LogoutCommand)
            _logoutButton = button;

        return button;
    }
}
