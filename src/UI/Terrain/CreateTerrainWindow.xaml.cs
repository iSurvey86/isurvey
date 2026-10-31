using System.Windows;
using System.Windows.Input;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.UI.Terrain;

public sealed class CreateTerrainRequest
{
    public string Name { get; init; } = "";

    public string Layer { get; init; } = "iSurvey_MoHinh";
}

public partial class CreateTerrainWindow : Window
{
    public CreateTerrainWindow(string suggestedName)
    {
        InitializeComponent();
        NameBox.Text = suggestedName;
        NameBox.SelectAll();
    }

    public CreateTerrainRequest? Request { get; private set; }

    public static CreateTerrainRequest? Prompt(string suggestedName)
    {
        var window = new CreateTerrainWindow(suggestedName);
        AcadApp.ShowModalWindow(window);
        return window.Request;
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (name.Length == 0)
        {
            System.Windows.MessageBox.Show(this, "Hãy nhập tên mô hình.", "iSurvey");
            return;
        }

        var layer = LayerBox.Text.Trim();
        if (layer.Length == 0 || layer.IndexOfAny(new[] { '<', '>', '/', '\\', '"', ':', ';', '?', '*', '|', ',', '=' }) >= 0)
        {
            System.Windows.MessageBox.Show(this, "Tên lớp không hợp lệ.", "iSurvey");
            return;
        }

        Request = new CreateTerrainRequest { Name = name, Layer = layer };
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
