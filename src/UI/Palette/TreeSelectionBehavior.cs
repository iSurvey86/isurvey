using System.Windows;
using WpfTree = System.Windows.Controls.TreeView;

namespace iSurvey.UI.Palette;

/// <summary>Đưa mục đang chọn trên cây sang ViewModel. View không viết logic.</summary>
internal static class TreeSelectionBehavior
{
    private static readonly DependencyProperty HookedProperty =
        DependencyProperty.RegisterAttached("Hooked", typeof(bool), typeof(TreeSelectionBehavior));

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.RegisterAttached(
            "SelectedItem",
            typeof(object),
            typeof(TreeSelectionBehavior),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHook));

    public static void SetSelectedItem(DependencyObject element, object? value) =>
        element.SetValue(SelectedItemProperty, value);

    public static object? GetSelectedItem(DependencyObject element) =>
        element.GetValue(SelectedItemProperty);

    private static void OnHook(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not WpfTree tree || (bool)tree.GetValue(HookedProperty))
            return;
        tree.SetValue(HookedProperty, true);
        tree.SelectedItemChanged += OnSelected;
    }

    private static void OnSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (sender is WpfTree tree)
            tree.SetCurrentValue(SelectedItemProperty, e.NewValue);
    }
}
