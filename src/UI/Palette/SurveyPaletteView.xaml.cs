using System.Windows.Controls;
using System.Windows.Input;

namespace iSurvey.UI.Palette;

/// <summary>Vỏ WPF. Logic nằm ở ViewModel và SurveyPaletteManager.</summary>
public partial class SurveyPaletteView
{
    public SurveyPaletteView()
    {
        InitializeComponent();
    }

    private void Node_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is TreeViewItem item)
            item.IsSelected = true;
    }
}
