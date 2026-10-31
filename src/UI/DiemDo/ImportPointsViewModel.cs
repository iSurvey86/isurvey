using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using iSurvey.Modules.DiemDo;
using WpfBinding = System.Windows.Data.Binding;

namespace iSurvey.UI.DiemDo;

/// <summary>Kiểu dấu điểm AutoCAD. 35 là vòng tròn kèm dấu chéo.</summary>
public enum PointMarker
{
    Dot = 0,
    None = 1,
    CircleCross = 35
}

public sealed class ImportPointsViewModel : INotifyPropertyChanged
{
    private string _filePath = "";
    private string _heightText = "2.5";
    private string _pointSizeText = "5";
    private string _totalText = "0";
    private PointMarker _marker = PointMarker.Dot;
    private SurveyPoint? _selected;

    public ObservableCollection<SurveyPoint> Points { get; } = new();

    public string FilePath
    {
        get => _filePath;
        set => Set(ref _filePath, value ?? "");
    }

    public string HeightText
    {
        get => _heightText;
        set => Set(ref _heightText, value ?? "");
    }

    public string PointSizeText
    {
        get => _pointSizeText;
        set => Set(ref _pointSizeText, value ?? "");
    }

    public string TotalText => _totalText;

    public PointMarker Marker
    {
        get => _marker;
        set => Set(ref _marker, value);
    }

    public SurveyPoint? Selected
    {
        get => _selected;
        set => Set(ref _selected, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Replace(PointFileContent content, bool tellIfEmpty)
    {
        Points.Clear();
        var stt = 1;
        foreach (var point in content.Points)
        {
            point.Stt = stt++;
            Points.Add(point);
        }

        Selected = Points.Count > 0 ? Points[0] : null;
        RefreshTotal();
        if (tellIfEmpty && Points.Count == 0)
            System.Windows.MessageBox.Show(
                "Không thấy dòng tọa độ nào. Cần một số 7 chữ số (X Bắc) và một số 6 chữ số (Y Đông).",
                "iSurvey");
    }

    public void AddPoint()
    {
        var point = new SurveyPoint();
        var index = Selected is null ? Points.Count : Points.IndexOf(Selected) + 1;
        if (index < 0)
            index = Points.Count;
        Points.Insert(index, point);
        Renumber();
        Selected = point;
        RefreshTotal();
    }

    public void DeletePoint()
    {
        if (Selected is null)
            return;
        var index = Points.IndexOf(Selected);
        Points.Remove(Selected);
        Renumber();
        RefreshTotal();
        if (Points.Count == 0)
        {
            Selected = null;
            return;
        }

        Selected = Points[Math.Min(Math.Max(index, 0), Points.Count - 1)];
    }

    public bool TrySettings(out double height, out int mode, out double pointSize, out string error)
    {
        height = 0;
        mode = (int)Marker;
        pointSize = 0;
        error = "";
        if (!TryPositive(HeightText, out height))
        {
            error = "Cao chữ phải lớn hơn 0.";
            return false;
        }

        if (!TryPositive(PointSizeText, out pointSize))
        {
            error = "Point size phải lớn hơn 0.";
            return false;
        }

        return true;
    }

    private void Renumber()
    {
        for (var i = 0; i < Points.Count; i++)
            Points[i].Stt = i + 1;
    }

    private void RefreshTotal()
    {
        _totalText = Points.Count.ToString(CultureInfo.InvariantCulture);
        Notify(nameof(TotalText));
    }

    private static bool TryPositive(string text, out double value)
    {
        var sample = (text ?? "").Trim().Replace(',', '.');
        return double.TryParse(sample, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value > 0;
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        Notify(name);
    }

    private void Notify(string? name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>Ô số trên bảng: hiện dấu chấm, nhận cả dấu phẩy.</summary>
public sealed class SurveyNumberConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double number)
            return number.ToString("0.########", CultureInfo.InvariantCulture);
        return "";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var sample = (value as string ?? "").Trim().Replace(',', '.');
        if (double.TryParse(sample, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            return number;
        return WpfBinding.DoNothing;
    }
}
