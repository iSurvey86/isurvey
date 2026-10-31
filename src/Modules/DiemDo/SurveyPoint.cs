using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace iSurvey.Modules.DiemDo;

/// <summary>Một điểm trên bảng nhập. X là X trắc địa (Bắc), Y là Y trắc địa (Đông). Lúc vẽ CAD mới đảo trục.</summary>
public sealed class SurveyPoint : INotifyPropertyChanged
{
    private int _stt;
    private string _name = "";
    private double _x;
    private double _y;
    private double _z;
    private string _code = "";

    /// <summary>Số thứ tự dòng trong bảng nhập, từ 1. Không phải tên điểm.</summary>
    public int Stt
    {
        get => _stt;
        set => Set(ref _stt, value);
    }

    public string Name
    {
        get => _name;
        set => Set(ref _name, value ?? "");
    }

    public double X
    {
        get => _x;
        set
        {
            if (Set(ref _x, value))
                Notify(nameof(XText));
        }
    }

    public double Y
    {
        get => _y;
        set
        {
            if (Set(ref _y, value))
                Notify(nameof(YText));
        }
    }

    public double Z
    {
        get => _z;
        set
        {
            if (Set(ref _z, value))
                Notify(nameof(ZText));
        }
    }

    public string Code
    {
        get => _code;
        set => Set(ref _code, value ?? "");
    }

    public string XText => X.ToString("0.########", CultureInfo.CurrentCulture);
    public string YText => Y.ToString("0.########", CultureInfo.CurrentCulture);
    public string ZText => Z.ToString("0.########", CultureInfo.CurrentCulture);

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SwapAxes()
    {
        (_x, _y) = (_y, _x);
        Notify(nameof(X));
        Notify(nameof(Y));
        Notify(nameof(XText));
        Notify(nameof(YText));
    }

    public SurveyPoint Copy() => new()
    {
        Name = Name,
        X = X,
        Y = Y,
        Z = Z,
        Code = Code
    };

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        Notify(name);
        return true;
    }

    private void Notify(string? name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
