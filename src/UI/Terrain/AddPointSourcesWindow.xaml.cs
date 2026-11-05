using System.Windows;
using System.Windows.Input;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.UI.Terrain;

public sealed class PointSourceChoice
{
    public bool Points { get; init; }

    public bool Blocks { get; init; }

    /// <summary>XY từ điểm/block, Z từ chữ số gần nhất. Không ghi Z vào entity CAD.</summary>
    public bool BlockElevFromLabels { get; init; } = true;

    public bool Labels { get; init; }

    public bool Chains { get; init; }
}

public partial class AddPointSourcesWindow : Window
{
    private bool _syncing;

    public PointSourceChoice? Choice { get; private set; }

    public AddPointSourcesWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Gắn sau Loaded — tránh Checked lúc InitializeComponent (UncheckAllBox chưa sẵn).
        PointBox.Checked += Source_Changed;
        PointBox.Unchecked += Source_Changed;
        BlockBox.Checked += Source_Changed;
        BlockBox.Unchecked += Source_Changed;
        BlockElevBox.Checked += Source_Changed;
        BlockElevBox.Unchecked += Source_Changed;
        LabelBox.Checked += Source_Changed;
        LabelBox.Unchecked += Source_Changed;
        ChainBox.Checked += Source_Changed;
        ChainBox.Unchecked += Source_Changed;
        UncheckAllBox.Checked += UncheckAll_Checked;
        UncheckAllBox.Unchecked += UncheckAll_Unchecked;
    }

    public static PointSourceChoice? Prompt()
    {
        var window = new AddPointSourcesWindow();
        AcadApp.ShowModalWindow(window);
        return window.Choice;
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        var points = PointBox.IsChecked == true;
        var blocks = BlockBox.IsChecked == true;
        var blockElev = BlockElevBox.IsChecked == true;
        var labels = LabelBox.IsChecked == true;
        var chains = ChainBox.IsChecked == true;
        if (!points && !blocks && !blockElev && !labels && !chains)
        {
            System.Windows.MessageBox.Show(this, "Hãy chọn ít nhất một loại đối tượng.", "iSurvey");
            return;
        }

        Choice = new PointSourceChoice
        {
            Points = points,
            Blocks = blocks,
            BlockElevFromLabels = blockElev,
            Labels = labels,
            Chains = chains
        };
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Choice = null;
        Close();
    }

    private void UncheckAll_Checked(object sender, RoutedEventArgs e)
    {
        if (_syncing || PointBox is null)
            return;
        _syncing = true;
        PointBox.IsChecked = false;
        BlockBox.IsChecked = false;
        BlockElevBox.IsChecked = false;
        LabelBox.IsChecked = false;
        ChainBox.IsChecked = false;
        _syncing = false;
    }

    private void UncheckAll_Unchecked(object sender, RoutedEventArgs e)
    {
        // Giữ trạng thái nguồn hiện tại; người dùng tự tick lại.
    }

    private void Source_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncing)
            return;
        // Checked chạy cả lúc InitializeComponent — UncheckAllBox có thể chưa tạo.
        if (UncheckAllBox is null || PointBox is null || BlockBox is null)
            return;
        // Có nguồn được tick → tắt Uncheck all.
        if (PointBox.IsChecked == true || BlockBox.IsChecked == true || BlockElevBox.IsChecked == true
            || LabelBox.IsChecked == true || ChainBox.IsChecked == true)
        {
            _syncing = true;
            UncheckAllBox.IsChecked = false;
            _syncing = false;
        }
    }

    private void Header_Drag(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;
        Choice = null;
        Close();
    }
}
