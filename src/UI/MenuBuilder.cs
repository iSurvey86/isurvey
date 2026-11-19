using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using iSurvey.Auth;

namespace iSurvey.UI;

/// <summary>
/// Menu cổ điển «iSurvey» trên thanh MenuBar (MENUBAR=1 / workspace không dùng Ribbon).
/// Dùng COM AcadApplication — không phụ thuộc file .cuix; dựng lại khi WSCURRENT / MENUBAR đổi.
/// </summary>
public static class MenuBuilder
{
    private const string MenuName = "iSurvey";
    private const string LoginCommand = "ISURVEY_LOGIN";
    private const string LogoutCommand = "ISURVEY_LOGOUT";

    private static bool _subscribed;
    private static bool _rebuilding;
    private static bool _aiSectionPresent;
    private static bool _idleAttachHooked;
    private static int _idleAttachTries;

    /// <summary>Mục bị khóa khi chưa đăng nhập.</summary>
    private static readonly List<object> GatedItems = [];

    private static object? _logoutItem;
    private static object? _accountItem;

    private sealed record Item(string Label, string Command, bool Gated = true);
    private sealed record Section(string Title, Item[] Items);

    public static void EnsureMenu()
    {
        if (!_subscribed)
        {
            AuthManager.StateChanged += UpdateAuthState;
            AcadApp.SystemVariableChanged += OnSystemVariableChanged;
            _subscribed = true;
        }

        TryBuild();
        ScheduleAttachRetry();
    }

    public static void ReloadMenu()
    {
        EnsureMenu();
        RemoveMenu();
        TryBuild();
        ScheduleAttachRetry();
    }

    public static void Cleanup()
    {
        if (_subscribed)
        {
            AuthManager.StateChanged -= UpdateAuthState;
            AcadApp.SystemVariableChanged -= OnSystemVariableChanged;
            _subscribed = false;
        }

        if (_idleAttachHooked)
        {
            AcadApp.Idle -= OnIdleAttach;
            _idleAttachHooked = false;
        }

        RemoveMenu();
        GatedItems.Clear();
        _logoutItem = null;
        _accountItem = null;
        _aiSectionPresent = false;
    }

    public static void UpdateAuthState()
    {
        try
        {
            var wantAi = AuthManager.IsAdmin();
            if (wantAi != _aiSectionPresent)
            {
                try
                {
                    dynamic? acad = AcadApp.AcadApplication;
                    if (acad is not null && FindPopup(acad.MenuGroups.Item(0), MenuName) is not null)
                    {
                        ReloadMenu();
                        return;
                    }
                }
                catch { /* MenuBar chưa sẵn */ }
            }

            var authed = AuthManager.License is not null;
            foreach (var raw in GatedItems)
            {
                try { ((dynamic)raw).Enable = authed; } catch { /* mục đã hủy */ }
            }

            if (_logoutItem is not null)
            {
                try { ((dynamic)_logoutItem).Enable = authed; } catch { /* ignore */ }
            }

            if (_accountItem is not null)
            {
                try
                {
                    var label = authed
                        ? ShortEmail(AuthManager.License?.Email) ?? "Tài khoản"
                        : "Đăng Nhập / Kích Hoạt";
                    ((dynamic)_accountItem).Label = label;
                    ((dynamic)_accountItem).Enable = true;
                }
                catch { /* ignore */ }
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[iSurvey] MenuBuilder.UpdateAuthState: " + ex.Message);
        }
    }

    private static void OnSystemVariableChanged(object sender, Autodesk.AutoCAD.ApplicationServices.SystemVariableChangedEventArgs e)
    {
        if (string.Equals(e.Name, "MENUBAR", StringComparison.OrdinalIgnoreCase))
        {
            // Chỉ gắn lại lên thanh menu — không xóa/tạo lại (tránh mất khi MENUBAR vừa bật).
            try { AttachToMenuBar(); } catch { /* ignore */ }
            ScheduleAttachRetry();
            return;
        }

        if (!string.Equals(e.Name, "WSCURRENT", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            ReloadMenu();
        }
        catch
        {
            // MenuBar chưa sẵn
        }
    }

    private static void ScheduleAttachRetry()
    {
        _idleAttachTries = 0;
        if (_idleAttachHooked)
            return;
        AcadApp.Idle += OnIdleAttach;
        _idleAttachHooked = true;
    }

    private static void OnIdleAttach(object? sender, EventArgs e)
    {
        _idleAttachTries++;
        var attached = false;
        try { attached = AttachToMenuBar(); } catch { /* ignore */ }

        if (attached || _idleAttachTries >= 25)
        {
            AcadApp.Idle -= OnIdleAttach;
            _idleAttachHooked = false;
        }
    }

    /// <summary>Gắn popup iSurvey lên MenuBar nếu chưa có. Trả về true khi đã nằm trên thanh.</summary>
    private static bool AttachToMenuBar()
    {
        dynamic? acad = AcadApp.AcadApplication;
        if (acad is null)
            return false;

        if (FindOnMenuBar(acad, MenuName) is not null)
            return true;

        dynamic? popup = FindPopup(acad.MenuGroups.Item(0), MenuName);
        if (popup is null)
            return false;

        try
        {
            if (popup.OnMenuBar == true)
                return true;
        }
        catch { /* một số bản không có OnMenuBar */ }

        // MENUBAR=0 → MenuBar.Count có thể 0; Insert vẫn thường thành công và hiện khi bật lại.
        popup.InsertInMenuBar(MenuBarInsertIndex(acad));
        return FindOnMenuBar(acad, MenuName) is not null;
    }

    private static void TryBuild()
    {
        if (_rebuilding)
            return;
        _rebuilding = true;
        try
        {
            dynamic? acad = AcadApp.AcadApplication;
            if (acad is null)
                return;

            dynamic menuGroups = acad.MenuGroups;
            dynamic hostGroup = menuGroups.Item(0);

            // Đã có popup → chỉ cần gắn MenuBar + đồng bộ Enable.
            dynamic? existing = FindPopup(hostGroup, MenuName);
            if (existing is not null)
            {
                try { AttachToMenuBar(); } catch { /* MenuBar đang tắt / chưa sẵn */ }
                UpdateAuthState();
                return;
            }

            GatedItems.Clear();
            _logoutItem = null;
            _accountItem = null;
            _aiSectionPresent = false;

            dynamic root = hostGroup.Menus.Add(MenuName);
            var index = 0;

            foreach (var section in BuildSections())
            {
                dynamic sub = root.AddSubMenu(index++, section.Title);
                var subIndex = 0;
                foreach (var item in section.Items)
                    AddCommandItem(sub, ref subIndex, item);
            }

            // Tài khoản & hệ thống (không khóa cả nhóm — từng mục tự Enable).
            dynamic account = root.AddSubMenu(index++, "Tài khoản & Hệ thống");
            var ai = 0;
            _accountItem = AddCommandItem(account, ref ai, new Item("Đăng Nhập / Kích Hoạt", LoginCommand, Gated: false));
            AddCommandItem(account, ref ai, new Item("Kiểm Tra Cập Nhật", "ISURVEY_UPDATE", Gated: false));
            AddCommandItem(account, ref ai, new Item("Thông Tin & Giấy Phép", "ISURVEY_ABOUT", Gated: false));
            AddCommandItem(account, ref ai, new Item("Hướng dẫn", "ISURVEY_HDSD", Gated: false));
            AddCommandItem(account, ref ai, new Item("Nạp lại Menu / Ribbon", "ISURVEY_RIBBON", Gated: false));
            _logoutItem = AddCommandItem(account, ref ai, new Item("Đăng Xuất / Đổi Máy", LogoutCommand, Gated: false));

            if (AuthManager.IsAdmin())
            {
                dynamic aiMenu = root.AddSubMenu(index++, "Trợ lý AI");
                var aii = 0;
                AddCommandItem(aiMenu, ref aii, new Item("Trợ Lý AI", "ISURVEY_AI_ASSIST"));
                AddCommandItem(aiMenu, ref aii, new Item("Cấu Hình AI", "ISURVEY_AI_SETTINGS"));
                _aiSectionPresent = true;
            }

            try { AttachToMenuBar(); }
            catch
            {
                // Menu vẫn nằm trong group; Idle / MENUBAR=1 sẽ gắn lại.
            }

            UpdateAuthState();
        }
        catch (System.Exception ex)
        {
            AcadApp.DocumentManager.MdiActiveDocument?
                .Editor.WriteMessage($"\n[iSurvey] Khong tao duoc menu: {ex.Message}");
        }
        finally
        {
            _rebuilding = false;
        }
    }

    private static Section[] BuildSections() =>
    [
        new("Dữ liệu",
        [
            new("Bảng Dữ liệu", "ISURVEY_PALETTE")
        ]),
        new("Bản đồ",
        [
            new("Nhập File KML/KMZ", "ISURVEY_IMPORT_KML"),
            new("Xuất File KML/KMZ", "ISURVEY_EXPORT_KML"),
            new("Chèn Ảnh Vệ Tinh", "ISURVEY_MAP_SAT"),
            new("Cập Nhật Ảnh GE", "ISURVEY_UPDATE_GE"),
            new("Xóa Ảnh Vệ Tinh", "ISURVEY_DELETE_GE"),
            new("Nhập TAB ➔ CAD", "ISURVEY_TAB_TO_CAD"),
            new("Nhập DGN ➔ CAD", "ISURVEY_DGN_TO_CAD"),
            new("Xuất CAD ➔ DGN", "ISURVEY_CAD_TO_DGN")
        ]),
        new("UAV & LiDAR",
        [
            new("Chèn Ảnh UAV (Geo)", "ISURVEY_UAV_INSERT"),
            new("Cắt Ảnh Theo Biên", "ISURVEY_UAV_CLIP"),
            new("Tắt/Bật Khung Ảnh", "ISURVEY_UAV_FRAME_TOGGLE"),
            new("Đưa Ảnh Xuống Dưới", "ISURVEY_UAV_SEND_BACK"),
            new("Nhập DEM & Bề Mặt", "ISURVEY_UAV_DEM"),
            new("Vẽ Đường Đồng Mức", "ISURVEY_UAV_CONTOUR"),
            new("Hệ Tọa Độ UAV", "ISURVEY_UAV_CRS"),
            new("Nhập Point Cloud", "ISURVEY_PC_IMPORT"),
            new("Lọc Điểm Mặt Đất", "ISURVEY_PC_FILTER"),
            new("Rải Lưới Điểm", "ISURVEY_PC_GRID")
        ]),
        new("Khung & Cắt Bình Đồ",
        [
            new("Khung Bản Đồ", "ISURVEY_KHUNG"),
            new("Khung Bản Vẽ Kỹ Thuật", "ISURVEY_KHUNG_BV"),
            new("Tạo Khung Lưới TĐ", "TAOKHUNG"),
            new("Cắt Bình Đồ", "CATBD"),
            new("Rải Khung Theo Tuyến", "RAIKHUNG"),
            new("Vẽ Mặt Cắt Dọc", "VEMCD"),
            new("Rải Khung Mặt Cắt", "RAIMCD"),
            new("Thiết Lập Tỉ Lệ Khung", "TILEKHUNG"),
            new("Bảo Vệ Layer Khung", "KHOAKHUNG")
        ]),
        new("Biên tập Địa hình",
        [
            new("Ghi Số Đồng Mức", "GHISODM"),
            new("Nhập điểm đo", "NHAPDIEM"),
            new("Xuất điểm đo", "XUATDIEM"),
            new("Nâng hạ cao độ điểm đo", "NANGLCAO"),
            new("Tạo điểm từ đối tượng AutoCAD", "TAODIEMCAD"),
            new("Hiện điểm đo theo mã", "HIENDIEMMA"),
            new("Lọc điểm đo theo mã", "LOCDIEMMA"),
            new("Chèn địa vật theo mã điểm", "CHENDIAVAT"),
            new("Tắt điểm đo trên bình đồ", "TATDIEMBD"),
            new("Hiện điểm đo đã tắt", "HIENDIEMTAT"),
            new("Xóa điểm trong vùng", "XOADIEMVUNG"),
            new("Nối điểm tự động", "NOIDIEM"),
            new("Gán cao độ điểm theo địa hình", "GANCAO"),
            new("Phát sinh điểm trong vùng", "PHATSINHDIEM"),
            new("Định vị điểm mia theo cọc", "DINHVIMIA"),
            new("Xoay điểm mia theo Polyline", "XOAYMIA"),
            new("Lọc Giãn Điểm Mia", "LOCDIEM"),
            new("Ẩn / Hiện Lưới TIN", "ANTIN"),
            new("Chuẩn Hóa Mã Font", "CHUYENFONT")
        ]),
        new("Tuyến & Cột Điện",
        [
            new("Rải Móng & Biên Đất", "RAIMONG"),
            new("Đánh Số Góc Tuyến", "ISURVEY_NUMBER_VERTICES"),
            new("Duỗi Thẳng Tuyến", "ISURVEY_STRIP_MAP"),
            new("Tạo Biên Đường Tạm", "BIENDUONGTAM"),
            new("Kẻ Tuyến 110kV/220kV", "KETUYEN"),
            new("Đánh Số Thứ Tự", "STT")
        ]),
        new("Hồ sơ & QA/QC",
        [
            new("Bảng TĐ & Diện Tích", "BANGTOADO"),
            new("Kiểm Tra Lỗi Cao Độ", "KIEMTRACAODO"),
            new("Quét Cấu Trúc (AutoScan)", "AUTOSCAN"),
            new("Xuất Thống Kê Excel", "XUATEXCEL")
        ])
    ];

    private static object AddCommandItem(dynamic parent, ref int index, Item item)
    {
        // ^C^C hủy lệnh đang chạy; khoảng trắng cuối = Enter.
        var macro = "^C^C" + item.Command + " ";
        dynamic menuItem = parent.AddMenuItem(index++, item.Label, macro);
        if (item.Gated)
            GatedItems.Add(menuItem);
        return menuItem;
    }

    private static void RemoveMenu()
    {
        try
        {
            dynamic? acad = AcadApp.AcadApplication;
            if (acad is null)
                return;

            dynamic menuGroups = acad.MenuGroups;
            dynamic hostGroup = menuGroups.Item(0);
            dynamic? popup = FindPopup(hostGroup, MenuName);
            if (popup is null)
                return;

            try { popup.RemoveFromMenuBar(); } catch { /* chưa gắn */ }
            try { popup.Delete(); } catch { /* ignore */ }
        }
        catch
        {
            // thoát AutoCAD / COM busy
        }

        GatedItems.Clear();
        _logoutItem = null;
        _accountItem = null;
        _aiSectionPresent = false;
    }

    private static dynamic? FindPopup(dynamic? hostGroup, string name)
    {
        try
        {
            dynamic menus = hostGroup.Menus;
            var count = (int)menus.Count;
            for (var i = 0; i < count; i++)
            {
                dynamic m = menus.Item(i);
                if (string.Equals((string)m.Name, name, StringComparison.OrdinalIgnoreCase))
                    return m;
            }
        }
        catch { /* ignore */ }
        return null;
    }

    private static dynamic? FindOnMenuBar(dynamic acad, string name)
    {
        try
        {
            dynamic bar = acad.MenuBar;
            var count = (int)bar.Count;
            for (var i = 0; i < count; i++)
            {
                dynamic m = bar.Item(i);
                if (string.Equals((string)m.Name, name, StringComparison.OrdinalIgnoreCase))
                    return m;
            }
        }
        catch { /* MENUBAR=0 */ }
        return null;
    }

    private static int MenuBarInsertIndex(dynamic acad)
    {
        try
        {
            // Index 1-based; thêm cuối thanh menu.
            return (int)acad.MenuBar.Count + 1;
        }
        catch
        {
            return 1;
        }
    }

    private static string? ShortEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;
        var at = email.IndexOf('@');
        var local = at > 0 ? email[..at] : email;
        return local.Length <= 18 ? local : local[..16] + "…";
    }
}
