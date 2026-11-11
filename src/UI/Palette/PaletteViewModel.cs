using System.Collections.ObjectModel;
using Autodesk.AutoCAD.ApplicationServices;
using iSurvey.Modules.Terrain;
using iSurvey.Modules.Tuyen;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.UI.Palette;

/// <summary>Trạng thái bảng: tab đang chọn, cây khảo sát, mục đang chọn.</summary>
internal sealed class PaletteViewModel : ObservableObject
{
    private TreeNodeViewModel? _selectedNode;
    private int _selectedTabIndex;
    private string _drawingName = "Chưa mở bản vẽ";

    public PaletteViewModel()
    {
        FrameActions = Actions(
            ("Khung bản đồ", "ISURVEY_KHUNG"),
            ("Khung bản vẽ kỹ thuật", "ISURVEY_KHUNG_BV"),
            ("Rải khung theo tuyến", "RAIKHUNG"),
            ("Rải khung mặt cắt", "RAIMCD"));
        UavActions = Actions(
            ("Chèn ảnh UAV", "ISURVEY_UAV_INSERT"),
            ("Cắt ảnh theo biên", "ISURVEY_UAV_CLIP"),
            ("Mô hình DEM", "ISURVEY_UAV_DEM"));
        MapActions = Actions(
            ("Nhập KML/KMZ", "ISURVEY_IMPORT_KML"),
            ("Xuất KML/KMZ", "ISURVEY_EXPORT_KML"),
            ("Chèn ảnh vệ tinh", "ISURVEY_MAP_SAT"),
            ("Nhập TAB", "ISURVEY_TAB_TO_CAD"),
            ("Nhập DGN", "ISURVEY_DGN_TO_CAD"),
            ("Xuất DGN", "ISURVEY_CAD_TO_DGN"));
        Load(null);
    }

    public ObservableCollection<TreeNodeViewModel> TreeNodes { get; } = new();

    public ObservableCollection<NodeAction> FrameActions { get; }

    public ObservableCollection<NodeAction> UavActions { get; }

    public ObservableCollection<NodeAction> MapActions { get; }

    public string[] TabNames { get; } = ["Khảo sát", "Khung in", "UAV", "Bản đồ"];

    public string DrawingName
    {
        get => _drawingName;
        private set => Set(ref _drawingName, value);
    }

    public string SelectedTab => TabNames[ClampTab(_selectedTabIndex)];

    private int ClampTab(int index)
    {
        if (index < 0)
            return 0;
        if (index >= TabNames.Length)
            return TabNames.Length - 1;
        return index;
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (!Set(ref _selectedTabIndex, value))
                return;
            Raise(nameof(SelectedTab));
        }
    }

    public TreeNodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (!Set(ref _selectedNode, value))
                return;
            Raise(nameof(HasSelection));
        }
    }

    public bool HasSelection => _selectedNode is not null;

    public void Load(Document? document)
    {
        DrawingName = document is null
            ? "Không có bản vẽ đang mở"
            : "Bản vẽ: " + document.Name;
        SelectedNode = null;
        TreeNodes.Clear();
        TreeNodes.Add(Points());
        TreeNodes.Add(Terrain(document));
        TreeNodes.Add(Route(document));
        TreeNodes.Add(Profile());
        LinkGuides(TreeNodes, connectUp: false);
    }

    private static void LinkGuides(System.Collections.Generic.IList<TreeNodeViewModel> nodes, bool connectUp)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            nodes[i].SetGuide(connectUp || i > 0, i < nodes.Count - 1);
            LinkGuides(nodes[i].Children, connectUp: true);
        }
    }

    private static TreeNodeViewModel Points()
    {
        var node = new TreeNodeViewModel(
            "Điểm đo",
            "Phải chuột để nhập hoặc xuất.",
            "Chưa đọc điểm trong bản vẽ. Pha này chỉ mở bảng.",
            "Point");
        node.Actions.Add(new NodeAction("Nhập điểm", PaletteCommands.Run("NHAPDIEM")));
        node.Actions.Add(new NodeAction("Xuất điểm", PaletteCommands.Run("XUATDIEM")));
        return node;
    }

    private static TreeNodeViewModel Terrain(Document? document)
    {
        List<TerrainRecord> models;
        try
        {
            models = document is null ? new List<TerrainRecord>() : TerrainStore.Load(document.Database);
        }
        catch (System.Exception)
        {
            models = new List<TerrainRecord>();
        }

        var node = new TreeNodeViewModel(
            "Mô hình địa hình",
            "Phải chuột để tạo...",
            models.Count == 0 ? "Chưa có mô hình trong bản vẽ này." : models.Count + " mô hình.",
            "Terrain");
        node.Actions.Add(new NodeAction("Tạo mô hình...", PaletteCommands.Run("TAOMOHINH")));
        foreach (var model in models)
        {
            var child = new TreeNodeViewModel(model.Name, "", model.Summary(), "Mesh")
            {
                Tag = model.Id
            };
            child.Actions.Add(new NodeAction("Xây dựng TIN", PaletteCommands.ForModel("XAYMOHINH", model.Id, null)));
            child.Actions.Add(new NodeAction("Ẩn / hiện lưới", PaletteCommands.ForModel("ANTIN", model.Id, null)));
            child.Actions.Add(new NodeAction("Tra cao độ", PaletteCommands.ForModel("TRACAOD", model.Id, null)));
            var spots = new TreeNodeViewModel(
                "Điểm (" + model.Points.Count + ")",
                "",
                model.Points.Count + " đối tượng điểm khống chế cao độ.",
                "Spot");
            spots.Actions.Add(new NodeAction("Thêm điểm", PaletteCommands.ForModel("THEMMOHINH", model.Id, "point")));
            spots.Actions.Add(new NodeAction("Gỡ đã chọn...", PaletteCommands.ForModel("GOMOHINH", model.Id, "point")));
            spots.Actions.Add(new NodeAction("Xóa toàn bộ điểm", PaletteCommands.ForModel("XOATHANHPHAN", model.Id, "point")));
            var breaks = new TreeNodeViewModel(
                "Đứt gãy (" + model.Breaklines.Count + ")",
                "",
                model.Breaklines.Count + " đường đứt gãy. Cạnh bắt buộc, ví dụ mép ta luy.",
                "Break");
            breaks.Actions.Add(new NodeAction("Thêm đứt gãy", PaletteCommands.ForModel("THEMMOHINH", model.Id, "break")));
            breaks.Actions.Add(new NodeAction("Gỡ đã chọn...", PaletteCommands.ForModel("GOMOHINH", model.Id, "break")));
            breaks.Actions.Add(new NodeAction("Xóa toàn bộ đứt gãy", PaletteCommands.ForModel("XOATHANHPHAN", model.Id, "break")));
            var contours = new TreeNodeViewModel(
                "Đường đồng mức (" + model.Contours.Count + ")",
                "",
                model.Contours.Count + " đường đồng mức. Cạnh bắt buộc, cao độ không đổi dọc cạnh.",
                "Contour");
            contours.Actions.Add(new NodeAction("Thêm đường đồng mức", PaletteCommands.ForModel("THEMMOHINH", model.Id, "contour")));
            contours.Actions.Add(new NodeAction("Gán cao độ đồng mức (GHISODM)", PaletteCommands.ForModel("GHISODM", model.Id, null)));
            contours.Actions.Add(new NodeAction("Gỡ đã chọn...", PaletteCommands.ForModel("GOMOHINH", model.Id, "contour")));
            contours.Actions.Add(new NodeAction("Xóa toàn bộ đường đồng mức", PaletteCommands.ForModel("XOATHANHPHAN", model.Id, "contour")));
            var bao = model.BoundaryCount;
            var boundary = new TreeNodeViewModel(
                "Đường bao (" + bao + ")",
                "",
                bao + " đường bao kín. Chỉ giữ tam giác nằm trong các đường bao.",
                "Boundary");
            boundary.Actions.Add(new NodeAction("Thêm đường bao", PaletteCommands.ForModel("THEMMOHINH", model.Id, "boundary")));
            boundary.Actions.Add(new NodeAction("Gỡ đã chọn...", PaletteCommands.ForModel("GOMOHINH", model.Id, "boundary")));
            boundary.Actions.Add(new NodeAction("Xóa toàn bộ đường bao", PaletteCommands.ForModel("XOATHANHPHAN", model.Id, "boundary")));
            child.Children.Add(spots);
            child.Children.Add(breaks);
            child.Children.Add(contours);
            child.Children.Add(boundary);
            node.Children.Add(child);
        }

        return node;
    }

    private static TreeNodeViewModel Route(Document? document)
    {
        List<RouteRecord> routes;
        try
        {
            routes = document is null ? new List<RouteRecord>() : RouteStore.Load(document.Database);
        }
        catch (System.Exception)
        {
            routes = new List<RouteRecord>();
        }

        var node = new TreeNodeViewModel(
            "Tuyến",
            "Phải chuột để tạo...",
            routes.Count == 0 ? "Chưa có tuyến trong bản vẽ này." : routes.Count + " tuyến.",
            "Route");
        node.Actions.Add(new NodeAction("Tạo tuyến...", PaletteCommands.Run("TAOTUYEN")));
        node.Actions.Add(new NodeAction("Thiết lập kiểu cọc...", PaletteCommands.Run("THIETLAPCOC")));
        foreach (var route in routes)
        {
            var child = new TreeNodeViewModel(route.Name, "", route.Summary(), "Route")
            {
                Tag = route.Id
            };
            child.Actions.Add(new NodeAction("Phát sinh cọc...", PaletteCommands.ForRoute("PHATSINHCOC", route.Id)));
            child.Actions.Add(new NodeAction("Danh sách cọc...", PaletteCommands.ForRoute("BANGCOC", route.Id)));
            child.Actions.Add(new NodeAction("Thêm một cọc...", PaletteCommands.ForRoute("THEMCOC", route.Id)));
            child.Actions.Add(new NodeAction("Xóa một cọc...", PaletteCommands.ForRoute("XOACOC1", route.Id)));
            child.Actions.Add(new NodeAction("Thiết lập kiểu cọc...", PaletteCommands.Run("THIETLAPCOC")));
            child.Actions.Add(new NodeAction("Đảo đầu tuyến", PaletteCommands.ForRoute("DAODAUTUYEN", route.Id)));
            child.Actions.Add(new NodeAction("Xóa hết cọc", PaletteCommands.ForRoute("XOACOC", route.Id)));
            var stations = route.Stations?.Count ?? 0;
            var withZ = 0;
            if (route.Stations is not null)
            {
                foreach (var station in route.Stations)
                {
                    if (station.HasZ)
                        withZ++;
                }
            }

            var coc = new TreeNodeViewModel(
                "Cọc (" + stations + ")",
                "",
                stations == 0
                    ? "Chưa có cọc. Chuột phải để phát sinh, thêm hoặc xem danh sách."
                    : stations + " cọc, " + withZ + " có cao độ Z từ mô hình.",
                "Spot")
            {
                Tag = route.Id
            };
            coc.Actions.Add(new NodeAction("Danh sách cọc...", PaletteCommands.ForRoute("BANGCOC", route.Id)));
            coc.Actions.Add(new NodeAction("Thêm một cọc...", PaletteCommands.ForRoute("THEMCOC", route.Id)));
            coc.Actions.Add(new NodeAction("Xóa một cọc...", PaletteCommands.ForRoute("XOACOC1", route.Id)));
            coc.Actions.Add(new NodeAction("Phát sinh cọc...", PaletteCommands.ForRoute("PHATSINHCOC", route.Id)));
            coc.Actions.Add(new NodeAction("Xóa hết cọc", PaletteCommands.ForRoute("XOACOC", route.Id)));
            child.Children.Add(coc);
            node.Children.Add(child);
        }

        return node;
    }

    private static TreeNodeViewModel Profile()
    {
        List<iSurvey.Modules.Profile.ProfileRecord> profiles;
        try
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            profiles = doc is null
                ? new List<iSurvey.Modules.Profile.ProfileRecord>()
                : iSurvey.Modules.Profile.ProfileStore.Load(doc.Database);
        }
        catch
        {
            profiles = new List<iSurvey.Modules.Profile.ProfileRecord>();
        }

        var node = new TreeNodeViewModel(
            "Trắc dọc",
            "Phải chuột để tạo...",
            profiles.Count == 0
                ? "Chưa có mặt cắt. Vẽ bằng VEMCD. RAIMCD chỉ rải khung in."
                : profiles.Count + " mặt cắt.",
            "Profile");
        node.Actions.Add(new NodeAction("Vẽ mặt cắt dọc...", PaletteCommands.Run("VEMCD")));
        foreach (var profile in profiles)
        {
            var child = new TreeNodeViewModel(profile.Name, "", profile.Summary(), "Profile")
            {
                Tag = profile.Id
            };
            child.Actions.Add(new NodeAction("Vẽ mặt cắt dọc...", PaletteCommands.Run("VEMCD")));
            node.Children.Add(child);
        }

        return node;
    }

    private static ObservableCollection<NodeAction> Actions(params (string Title, string Command)[] items)
    {
        var list = new ObservableCollection<NodeAction>();
        foreach (var item in items)
            list.Add(new NodeAction(item.Title, PaletteCommands.Run(item.Command)));
        return list;
    }
}
