using System.Windows;
using System.Windows.Input;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.UI.Terrain;

public sealed class PointSourceChoice
{
    public bool Points { get; init; } = true;

    public bool Blocks { get; init; } = true;

    /// <summary>XY từ block, Z từ chữ số gần nhất. Không ghi Z vào entity CAD.</summary>
    public bool BlockElevFromLabels { get; init; }

    public bool Labels { get; init; } = true;

    public bool Chains { get; init; }
}

public partial class AddPointSourcesWindow : Window
{
    public PointSourceChoice? Choice { get; private set; }

    public AddPointSourcesWindow()
    {
        InitializeComponent();
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
