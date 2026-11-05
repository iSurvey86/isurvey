using System.Globalization;
using System.Windows;
using System.Windows.Input;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.UI.Tuyen;

public sealed class CreateRouteRequest
{
    public string Name { get; init; } = "";

    public double StartChainage { get; init; }

    public string TerrainId { get; init; } = "";
}

public sealed class RouteModelChoice
{
    public string Id { get; init; } = "";

    public string Name { get; init; } = "";

    public override string ToString() => Name;
}

public partial class CreateRouteWindow : Window
{
    public CreateRouteRequest? Request { get; private set; }

    public CreateRouteWindow(string suggestedName, IReadOnlyList<RouteModelChoice> models, string? preferredTerrainId = null)
    {
        InitializeComponent();
        NameBox.Text = suggestedName;
        NameBox.SelectAll();
        ModelBox.Items.Add(new RouteModelChoice { Id = "", Name = "Chưa gắn" });
        foreach (var model in models)
            ModelBox.Items.Add(model);

        var index = 0;
        if (!string.IsNullOrWhiteSpace(preferredTerrainId))
        {
            for (var i = 1; i < ModelBox.Items.Count; i++)
            {
                if (ModelBox.Items[i] is RouteModelChoice choice
                    && string.Equals(choice.Id, preferredTerrainId, StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }
        }
        else if (models.Count > 0)
            index = 1;

        ModelBox.SelectedIndex = index;
    }

    public static CreateRouteRequest? Prompt(
        string suggestedName,
        IReadOnlyList<RouteModelChoice> models,
        string? preferredTerrainId = null)
    {
        var window = new CreateRouteWindow(suggestedName, models, preferredTerrainId);
        AcadApp.ShowModalWindow(window);
        return window.Request;
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (name.Length == 0)
        {
            System.Windows.MessageBox.Show(this, "Hãy nhập tên tuyến.", "iSurvey");
            return;
        }

        var raw = ChainBox.Text.Trim().Replace(',', '.');
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var chainage))
        {
            System.Windows.MessageBox.Show(this, "Lý trình gốc không phải số.", "iSurvey");
            return;
        }

        var terrainId = ModelBox.SelectedItem is RouteModelChoice choice ? choice.Id : "";
        Request = new CreateRouteRequest
        {
            Name = name,
            StartChainage = chainage,
            TerrainId = terrainId
        };
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Request = null;
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
        Request = null;
        Close();
    }
}
