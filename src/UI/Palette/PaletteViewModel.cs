using System.Collections.ObjectModel;
using Autodesk.AutoCAD.ApplicationServices;
using iSurvey.Modules.Terrain;

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
        TreeNodes.Add(Route());
        TreeNodes.Add(Profile());
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
            var child = new TreeNodeViewModel(model.Name, "", model.Summary(), "Terrain")
            {
                Tag = model.Id
            };
            child.Actions.Add(new NodeAction("Thêm điểm", PaletteCommands.ForModel("THEMMOHINH", model.Id, "point")));
            child.Actions.Add(new NodeAction("Thêm đứt gãy", PaletteCommands.ForModel("THEMMOHINH", model.Id, "break")));
            child.Actions.Add(new NodeAction("Thêm đường đồng mức", PaletteCommands.ForModel("THEMMOHINH", model.Id, "contour")));
            child.Actions.Add(new NodeAction("Thêm đường bao", PaletteCommands.ForModel("THEMMOHINH", model.Id, "boundary")));
            child.Actions.Add(new NodeAction("Xây dựng", PaletteCommands.ForModel("XAYMOHINH", model.Id, null)));
            child.Actions.Add(new NodeAction("Ẩn / hiện lưới", PaletteCommands.ForModel("ANTIN", model.Id, null)));
            child.Actions.Add(new NodeAction("Tra cao độ", PaletteCommands.ForModel("TRACAOD", model.Id, null)));
            child.Children.Add(new TreeNodeViewModel("Điểm (" + model.Points.Count + ")", "", "Điểm khống chế cao độ.", "Point"));
            child.Children.Add(new TreeNodeViewModel("Đứt gãy (" + model.Breaklines.Count + ")", "", "Cạnh bắt buộc của lưới, ví dụ mép ta luy.", "Route"));
            child.Children.Add(new TreeNodeViewModel("Đường đồng mức (" + model.Contours.Count + ")", "", "Cạnh bắt buộc, cao độ không đổi dọc cạnh.", "Route"));
            var bao = string.IsNullOrEmpty(model.Boundary) ? "chưa có" : "có";
            child.Children.Add(new TreeNodeViewModel("Đường bao (" + bao + ")", "", "Chỉ giữ tam giác nằm trong đường bao.", "Profile"));
            node.Children.Add(child);
        }

        return node;
    }

    private static TreeNodeViewModel Route()
    {
        var node = new TreeNodeViewModel(
            "Tuyến",
            "Phải chuột để tạo...",
            "Chưa có tuyến. Làm sau khi đã có mô hình.",
            "Route");
        node.Actions.Add(new NodeAction("Tạo tuyến...", PaletteCommands.Later(
            "Tạo tuyến chưa có. Cần mô hình địa hình trước.")));
        return node;
    }

    private static TreeNodeViewModel Profile()
    {
        var node = new TreeNodeViewModel(
            "Trắc dọc",
            "Phải chuột để tạo...",
            "Chưa có mặt cắt. Vẽ bằng VEMCD, chưa làm. RAIMCD chỉ rải khung in.",
            "Profile");
        node.Actions.Add(new NodeAction("Vẽ mặt cắt (chưa làm)", PaletteCommands.Later(
            "Vẽ mặt cắt là lệnh VEMCD, chưa làm. RAIMCD chỉ rải khung in.")));
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
