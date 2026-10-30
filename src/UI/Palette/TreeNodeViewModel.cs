using System.Collections.ObjectModel;

namespace iSurvey.UI.Palette;

/// <summary>Một nút trên cây Khảo sát. Pha UI chưa đọc đối tượng CAD.</summary>
internal sealed class TreeNodeViewModel : ObservableObject
{
    public TreeNodeViewModel(string name, string emptyHint, string detail, string iconKind)
    {
        Name = name;
        EmptyHint = emptyHint;
        Detail = detail;
        IconKind = iconKind;
        Children.CollectionChanged += (_, _) => Raise(nameof(HasChildren));
    }

    public string Name { get; }

    /// <summary>Point, Terrain, Route hoặc Profile.</summary>
    public string IconKind { get; }

    public string EmptyHint { get; }

    public string Detail { get; }

    public ObservableCollection<TreeNodeViewModel> Children { get; } = new();

    public ObservableCollection<NodeAction> Actions { get; } = new();

    public bool HasChildren => Children.Count > 0;
}

/// <summary>Một mục menu chuột phải.</summary>
internal sealed class NodeAction
{
    public NodeAction(string title, RelayCommand command)
    {
        Title = title;
        Command = command;
    }

    public string Title { get; }

    public RelayCommand Command { get; }
}
