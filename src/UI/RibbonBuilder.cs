using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.Windows;
using iSurvey.Auth;
using System.Windows.Media.Imaging;

namespace iSurvey.UI;

/// <summary>Tạo tab Ribbon iSurvey — khởi tạo an toàn khi Ribbon chưa sẵn sàng.</summary>
public static class RibbonBuilder
{
    private const string TabId = "ISURVEY_RIBBON_TAB";
    private const string LoginCommand = "ISURVEY_LOGIN";
    private const string LogoutCommand = "ISURVEY_LOGOUT";

    private static bool _subscribed;

    /// <summary>Nút nghiệp vụ: bị khóa (IsEnabled=false) khi chưa đăng nhập.</summary>
    private static readonly List<RibbonItem> GatedButtons = [];

    private static RibbonButton? _accountButton;
    private static RibbonButton? _logoutButton;

    /// <summary>Panel Trợ lý AI đã có trên Ribbon hay chưa (đồng bộ khi đăng nhập/đăng xuất Admin).</summary>
    private static bool _aiPanelPresent;
    private static bool _rebuilding;

    /// <summary>Mô tả một nút. Implemented=true: gửi lệnh thật; false: in thông báo "đang cập nhật".</summary>
    private sealed record Btn(string Text, string Command, Func<int, BitmapSource> Icon, bool Implemented,
        string? Tip = null);

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
        _aiPanelPresent = false;
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
            _aiPanelPresent = false;

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
                    new Btn("Chèn Ảnh Vệ Tinh", "ISURVEY_MAP_SAT", RibbonIcons.MapInsert, true),
                    new Btn("Cập Nhật Ảnh GE", "ISURVEY_UPDATE_GE", RibbonIcons.UpdateGe, true),
                    new Btn("Xóa Ảnh Vệ Tinh", "ISURVEY_DELETE_GE", RibbonIcons.MapDelete, true)
                ],
                secondSmall:
                [
                    new Btn("Nhập DGN ➔ CAD", "ISURVEY_DGN_TO_CAD", RibbonIcons.DgnToCad, true,
                        "Nhập file MicroStation (.dgn V7/V8) vào bản vẽ hiện hành hoặc chuyển hàng loạt sang .dwg (Lệnh tắt: D2C)"),
                    new Btn("Xuất CAD ➔ DGN", "ISURVEY_CAD_TO_DGN", RibbonIcons.CadToDgn, true,
                        "Xuất bản vẽ AutoCAD (.dwg) sang MicroStation (.dgn V8) chuẩn Seed VN-2000 (Lệnh tắt: C2D)"),
                    new Btn("Cấu Hình DGN", "ISURVEY_DGN_CONFIG", RibbonIcons.DgnConfig, true,
                        "Thiết lập mặc định Đơn vị DGN, Ánh xạ Level ➔ Layer và Chuyển mã Font TCVN3 ➔ Unicode (Lệnh tắt: DGNCFG)")
                ]);

            // Panel 2 — UAV & LiDAR (SplitButton gọn cho màn 14")
            AddUavLidarPanel(tab);

            // Panel 3 — Khung & Cắt Bình Đồ
            AddPanel(tab, "Khung & Cắt Bình Đồ",
                [
                    new Btn("Khung\nBản Đồ", "ISURVEY_KHUNG", RibbonIcons.KhungChuan, true,
                        "Chèn khung bản đồ chuẩn TCVN 7285 khổ A0–A4, tỷ lệ 1/200…1/5000: khung tên, lưới tọa độ, mũi tên Bắc, thước tỷ lệ (KHUNG)"),
                    new Btn("Khung Bản Vẽ\nKỹ Thuật", "ISURVEY_KHUNG_BV", RibbonIcons.KhungBanVe, true,
                        "Chèn khung bản vẽ kỹ thuật chuẩn TCVN 7285 khổ A0–A4, tỷ lệ 1/200…1/5000: khung tên + logo, không có lưới tọa độ (KHUNGBV)"),
                    new Btn("Tạo Khung\nLưới TĐ", "TAOKHUNG", RibbonIcons.TaoKhung, false),
                    new Btn("Cắt Bình Đồ", "CATBD", RibbonIcons.CatBd, true)
                ],
                [
                    new Btn("Rải Khung Theo Tuyến", "RAIKHUNG", RibbonIcons.RaiKhung, false),
                    new Btn("Thiết Lập Tỉ Lệ Khung", "TILEKHUNG", RibbonIcons.TileKhung, false),
                    new Btn("Bảo Vệ Layer Khung", "KHOAKHUNG", RibbonIcons.KhoaKhung, false)
                ]);

            // Panel 4 — Biên tập Địa hình
            AddPanel(tab, "Biên tập Địa hình",
                [
                    new Btn("Ghi Số\nĐồng Mức", "GHISODM", RibbonIcons.GhiSoDm, false)
                ],
                [
                    new Btn("Lọc Giãn Điểm Mia", "LOCDIEM", RibbonIcons.LocDiem, false),
                    new Btn("Ẩn / Hiện Lưới TIN", "ANTIN", RibbonIcons.AnTin, false),
                    new Btn("Chuẩn Hóa Mã Font", "CHUYENFONT", RibbonIcons.ChuyenFont, false)
                ]);

            // Panel 5 — Tuyến & Cột Điện
            AddPanel(tab, "Tuyến & Cột Điện",
                [
                    new Btn("Rải Móng &\nBiên Đất", "RAIMONG", RibbonIcons.RaiMong, false),
                    new Btn("Đánh Số\nGóc Tuyến", "ISURVEY_NUMBER_VERTICES", RibbonIcons.DanhSoGoc, true,
                        "Đánh số góc tim tuyến — chấm đỏ + chữ né đường (GOC)"),
                    new Btn("Duỗi Thẳng\nTuyến", "ISURVEY_STRIP_MAP", RibbonIcons.DuoiTuyen, true,
                        "Duỗi ảnh UAV và đối tượng CAD theo tim tuyến, xuất bảng góc (DUOITUYEN)")
                ],
                [
                    new Btn("Tạo Biên Đường Tạm", "BIENDUONGTAM", RibbonIcons.BienDuongTam, false),
                    new Btn("Kẻ Tuyến 110kV/220kV", "KETUYEN", RibbonIcons.KeTuyen, false),
                    new Btn("Đánh Số Hiệu Cột", "DANHSOCOT", RibbonIcons.DanhSoCot, false)
                ]);

            // Panel 6 — Hồ sơ & QA/QC
            AddPanel(tab, "Hồ sơ & QA/QC",
                [
                    new Btn("Bảng TĐ &\nDiện Tích", "BANGTOADO", RibbonIcons.BangToaDo, false)
                ],
                [
                    new Btn("Kiểm Tra Lỗi Cao Độ", "KIEMTRACAODO", RibbonIcons.KiemTraCaoDo, false),
                    new Btn("Quét Cấu Trúc (AutoScan)", "AUTOSCAN", RibbonIcons.AutoScan, true),
                    new Btn("Xuất Thống Kê Excel", "XUATEXCEL", RibbonIcons.XuatExcel, false)
                ]);

            // Panel 7 — Tài khoản & Hệ thống (luôn bật; nút Đăng xuất chỉ bật khi đã đăng nhập)
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

            // Panel Admin — Trợ lý AI: chỉ dựng khi email trong danh sách Admin
            if (AuthManager.IsAdmin())
            {
                AddAiAssistantPanel(tab);
                _aiPanelPresent = true;
            }

            ribbon.Tabs.Add(tab);
            UpdateAuthState();
        }
        catch (System.Exception ex)
        {
            AcadApp.DocumentManager.MdiActiveDocument?
                .Editor.WriteMessage($"\n[iSurvey] Khong tao duoc Ribbon: {ex.Message}");
        }
    }

    /// <summary>Gỡ tab và dựng lại — dùng khi quyền Admin đổi sau đăng nhập/đăng xuất.</summary>
    private static void RebuildRibbonTab()
    {
        if (_rebuilding)
            return;
        _rebuilding = true;
        try
        {
            var ribbon = ComponentManager.Ribbon;
            if (ribbon is null)
                return;
            var existing = ribbon.FindTab(TabId);
            if (existing is not null)
                ribbon.Tabs.Remove(existing);
            GatedButtons.Clear();
            _accountButton = null;
            _logoutButton = null;
            _aiPanelPresent = false;
            TryBuild();
        }
        finally
        {
            _rebuilding = false;
        }
    }

    /// <summary>Panel thử nghiệm Trợ lý AI (Admin-Only).</summary>
    private static void AddAiAssistantPanel(RibbonTab tab)
    {
        AddPanel(tab, "Trợ lý AI",
            [
                new Btn("Trợ Lý\nAI", "ISURVEY_AI_ASSIST", RibbonIcons.AiAssist, true,
                    "Trợ lý AI khảo sát (đang phát triển — chỉ Admin)")
            ],
            [
                new Btn("Cấu Hình AI", "ISURVEY_AI_SETTINGS", RibbonIcons.AiSettings, true,
                    "Thiết lập Trợ lý AI (đang phát triển — chỉ Admin)")
            ]);
    }

    /// <summary>
    /// Đồng bộ Ribbon với trạng thái đăng nhập (không cần khởi động lại Civil):
    /// khóa/mở Panel 1–5, đổi nút tài khoản (nhãn, icon, tooltip), bật/tắt nút Đăng xuất.
    /// </summary>
    public static void UpdateAuthState()
    {
        try
        {
            // Đăng nhập/đăng xuất Admin → hiện/ẩn cả panel (dựng lại tab).
            var wantAi = AuthManager.IsAdmin();
            if (wantAi != _aiPanelPresent && ComponentManager.Ribbon?.FindTab(TabId) is not null)
            {
                RebuildRibbonTab();
                return;
            }

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

    private static void SetIcons(RibbonCommandItem button, Func<int, BitmapSource> icon)
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

    /// <summary>
    /// Panel UAV &amp; LiDAR: 2 RibbonSplitButton (Ảnh / DEM) + 3 nút nhỏ Point Cloud xếp dọc.
    /// Chiều ngang gọn (~130px) cho laptop 14 inch.
    /// </summary>
    private static void AddUavLidarPanel(RibbonTab tab)
    {
        var source = new RibbonPanelSource { Title = "UAV & LiDAR" };

        source.Items.Add(CreateSplitButton(
            text: "Chèn Ảnh\nUAV (Geo)",
            mainCommand: "ISURVEY_UAV_INSERT",
            mainIcon: RibbonIcons.UavInsert,
            mainImplemented: true,
            tip: "Chèn ảnh UAV / orthophoto georeference (đang phát triển)",
            dropdown:
            [
                new Btn("Cắt Ảnh Theo Biên", "ISURVEY_UAV_CLIP", RibbonIcons.UavClip, true,
                    "Chọn ảnh → 1=Polyline biên (batch) / 2=Auto-Clip nền trắng-đen (UAVCLIP)"),
                new Btn("Tắt/Bật Khung Ảnh", "ISURVEY_UAV_FRAME_TOGGLE", RibbonIcons.UavFrame, true,
                    "Bật hoặc tắt khung bao quanh RasterImage"),
                new Btn("Đưa Ảnh Xuống Dưới", "ISURVEY_UAV_SEND_BACK", RibbonIcons.UavSendBack, true,
                    "Đưa RasterImage xuống dưới cùng DrawOrder")
            ]));

        source.Items.Add(CreateSplitButton(
            text: "Nhập DEM\n& Bề Mặt",
            mainCommand: "ISURVEY_UAV_DEM",
            mainIcon: RibbonIcons.UavDem,
            mainImplemented: true,
            tip: "Nhập DEM / bề mặt UAV (đang phát triển)",
            dropdown:
            [
                new Btn("Vẽ Đường Đồng Mức", "ISURVEY_UAV_CONTOUR", RibbonIcons.UavContour, true,
                    "Nội suy đường đồng mức từ DEM (đang phát triển)"),
                new Btn("Hệ Tọa Độ UAV", "ISURVEY_UAV_CRS", RibbonIcons.UavCrs, true,
                    "Thiết lập hệ tọa độ ảnh / DEM UAV (đang phát triển)")
            ]));

        source.Items.Add(CreateRows(
            [
                new Btn("Nhập Point Cloud", "ISURVEY_PC_IMPORT", RibbonIcons.PcImport, true,
                    "Nhập đám mây điểm (đang phát triển)"),
                new Btn("Lọc Điểm Mặt Đất", "ISURVEY_PC_FILTER", RibbonIcons.PcFilter, true,
                    "Lọc điểm mặt đất từ Point Cloud (đang phát triển)"),
                new Btn("Rải Lưới Điểm", "ISURVEY_PC_GRID", RibbonIcons.PcGrid, true,
                    "Rải lưới điểm từ Point Cloud (đang phát triển)")
            ],
            gated: true));

        tab.Panels.Add(new RibbonPanel { Source = source });
    }

    private static RibbonSplitButton CreateSplitButton(
        string text,
        string mainCommand,
        Func<int, BitmapSource> mainIcon,
        bool mainImplemented,
        string tip,
        IReadOnlyList<Btn> dropdown)
    {
        var split = new RibbonSplitButton
        {
            Text = text,
            ShowText = true,
            Size = RibbonItemSize.Large,
            Orientation = System.Windows.Controls.Orientation.Vertical,
            IsSplit = true,
            IsSynchronizedWithCurrentItem = false,
            CommandHandler = mainImplemented
                ? new RibbonCommandHandler(mainCommand + "\n")
                : new RibbonPlaceholderHandler(mainCommand),
            ToolTip = tip
        };

        SetIcons(split, mainIcon);
        GatedButtons.Add(split);

        foreach (var item in dropdown)
            split.Items.Add(CreateButton(item, isLarge: false, gated: true));

        return split;
    }

    /// <summary>Một panel: các nút Large (32x32, dọc) + một cụm nút nhỏ (16x16, ngang) xếp nhiều dòng.</summary>
    private static void AddPanel(RibbonTab tab, string title, IReadOnlyList<Btn> large, IReadOnlyList<Btn>? small,
        bool gated = true, IReadOnlyList<Btn>? secondSmall = null)
    {
        var source = new RibbonPanelSource { Title = title };

        foreach (var b in large)
            source.Items.Add(CreateButton(b, isLarge: true, gated));

        if (small is { Count: > 0 })
            source.Items.Add(CreateRows(small, gated));

        // Nhóm thứ hai (ví dụ DGN) ngăn cách bằng vạch đứng.
        if (secondSmall is { Count: > 0 })
        {
            source.Items.Add(new RibbonSeparator());
            source.Items.Add(CreateRows(secondSmall, gated));
        }

        tab.Panels.Add(new RibbonPanel { Source = source });
    }

    private static RibbonRowPanel CreateRows(IReadOnlyList<Btn> buttons, bool gated)
    {
        var rows = new RibbonRowPanel();
        for (var i = 0; i < buttons.Count; i++)
        {
            if (i > 0)
                rows.Items.Add(new RibbonRowBreak());
            rows.Items.Add(CreateButton(buttons[i], isLarge: false, gated));
        }

        return rows;
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
            ToolTip = b.Tip ?? $"{b.Text.Replace("\n", " ")} ({b.Command})"
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
