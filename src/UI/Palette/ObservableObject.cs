using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace iSurvey.UI.Palette;

/// <summary>Gốc thông báo thuộc tính cho bảng dữ liệu. Không thêm thư viện MVVM.</summary>
internal abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void Raise([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        Raise(name);
        return true;
    }
}
