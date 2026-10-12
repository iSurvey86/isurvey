using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.Windows;
using System.Windows.Media.Imaging;

namespace iSurvey.UI;

/// <summary>Tạo tab Ribbon iSurvey (5 panel) — khởi tạo an toàn khi Ribbon chưa sẵn sàng.</summary>
public static class RibbonBuilder
{
    private const string TabId = "ISURVEY_RIBBON_TAB";
    private static bool _subscribed;

    /// <summary>Mô tả một nút. Implemented=true: gửi lệnh thật; false: in thông báo "đang cập nhật".</summary>
    private sealed record Btn(string Text, string Command, Func<int, BitmapSource> Icon, bool Implemented);

    public static void EnsureRibbon()
    {
        if (!_subscribed)
        {
            ComponentManager.ItemInitialized += OnItemInitialized!;
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

            var tab = new RibbonTab
            {
                Title = "iSurvey",
                Id = TabId
            };

            // Panel 1 — Bản đồ
            AddPanel(tab, "Bản đồ",
                [
                    new Btn("Chèn GE", "ISURVEY_MAP", RibbonIcons.MapInsert, true),
                    new Btn("Xóa GE", "ISURVEY_DELETE_GE", RibbonIcons.MapDelete, true),
                    new Btn("Xuất GE", "ISURVEY_EXPORT_KML", RibbonIcons.ExportGe, true)
                ],
                null);

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

            ribbon.Tabs.Add(tab);
        }
        catch (System.Exception ex)
        {
            AcadApp.DocumentManager.MdiActiveDocument?
                .Editor.WriteMessage($"\n[iSurvey] Khong tao duoc Ribbon: {ex.Message}");
        }
    }

    /// <summary>Một panel: các nút Large (32x32, dọc) + một cụm 3 nút nhỏ (16x16, ngang) xếp 3 dòng.</summary>
    private static void AddPanel(RibbonTab tab, string title, IReadOnlyList<Btn> large, IReadOnlyList<Btn>? small)
    {
        var source = new RibbonPanelSource { Title = title };

        foreach (var b in large)
            source.Items.Add(CreateButton(b, isLarge: true));

        if (small is { Count: > 0 })
        {
            var rows = new RibbonRowPanel();
            for (var i = 0; i < small.Count; i++)
            {
                if (i > 0)
                    rows.Items.Add(new RibbonRowBreak());
                rows.Items.Add(CreateButton(small[i], isLarge: false));
            }

            source.Items.Add(rows);
        }

        tab.Panels.Add(new RibbonPanel { Source = source });
    }

    private static RibbonButton CreateButton(Btn b, bool isLarge)
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

        try
        {
            button.LargeImage = b.Icon(32);
            button.Image = b.Icon(16);
        }
        catch
        {
            // Icon tùy chọn — bỏ qua nếu lỗi
        }

        return button;
    }
}
