using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.Windows;
using System.Windows.Media.Imaging;

namespace iSurvey.UI;

/// <summary>Tạo tab Ribbon iSurvey — khởi tạo an toàn khi Ribbon chưa sẵn sàng.</summary>
public static class RibbonBuilder
{
    private const string TabId = "ISURVEY_RIBBON_TAB";
    private static bool _subscribed;

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

            var panelSource = new RibbonPanelSource
            {
                Title = "Bản đồ"
            };

            var button = new RibbonButton
            {
                Text = "Chèn Google Earth",
                ShowText = true,
                Size = RibbonItemSize.Large,
                Orientation = System.Windows.Controls.Orientation.Vertical,
                CommandHandler = new RibbonCommandHandler("ISURVEY_MAP\n")
            };

            try
            {
                button.LargeImage = RibbonIcons.MapInsert(32);
                button.Image = RibbonIcons.MapInsert(16);
            }
            catch
            {
                // Icon tùy chọn — bỏ qua nếu lỗi
            }

            panelSource.Items.Add(button);

            var deleteButton = new RibbonButton
            {
                Text = "Xóa GE",
                ShowText = true,
                Size = RibbonItemSize.Large,
                Orientation = System.Windows.Controls.Orientation.Vertical,
                CommandHandler = new RibbonCommandHandler("ISURVEY_DELETE_GE\n")
            };

            try
            {
                deleteButton.LargeImage = RibbonIcons.MapDelete(32);
                deleteButton.Image = RibbonIcons.MapDelete(16);
            }
            catch
            {
                // Icon tùy chọn
            }

            panelSource.Items.Add(deleteButton);

            var exportButton = new RibbonButton
            {
                Text = "Xuất GE",
                ShowText = true,
                Size = RibbonItemSize.Large,
                Orientation = System.Windows.Controls.Orientation.Vertical,
                CommandHandler = new RibbonCommandHandler("ISURVEY_EXPORT_KML\n")
            };

            try
            {
                exportButton.LargeImage = RibbonIcons.ExportGe(32);
                exportButton.Image = RibbonIcons.ExportGe(16);
            }
            catch
            {
                // Icon tuỳ chọn
            }

            panelSource.Items.Add(exportButton);

            var panel = new RibbonPanel { Source = panelSource };
            tab.Panels.Add(panel);
            ribbon.Tabs.Add(tab);
        }
        catch (System.Exception ex)
        {
            AcadApp.DocumentManager.MdiActiveDocument?
                .Editor.WriteMessage($"\n[iSurvey] Không tạo được Ribbon: {ex.Message}");
        }
    }
}
