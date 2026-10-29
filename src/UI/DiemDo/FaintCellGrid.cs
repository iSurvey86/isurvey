using System.Windows;
using System.Windows.Media;

namespace iSurvey.UI.DiemDo;

/// <summary>Lưới ô mờ phía sau bảng. Kẻ cả vùng chưa có dòng.</summary>
public sealed class FaintCellGrid : FrameworkElement
{
    private double[] _columns = [];

    public double RowHeight { get; set; } = 26;

    public void Apply(double headerHeight, IReadOnlyList<double> widths)
    {
        var next = widths.ToArray();
        var width = 0.0;
        foreach (var column in next)
            width += column;
        var margin = new Thickness(0, headerHeight, 0, 0);
        var same = Math.Abs(Width - width) < 0.5
            && Math.Abs(Margin.Top - margin.Top) < 0.5
            && _columns.Length == next.Length;
        if (same)
        {
            for (var i = 0; i < next.Length; i++)
            {
                if (Math.Abs(_columns[i] - next[i]) >= 0.5)
                {
                    same = false;
                    break;
                }
            }
        }

        if (same)
            return;
        _columns = next;
        Margin = margin;
        Width = width > 0 ? width : double.NaN;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 0 || height <= 0)
            return;

        var pen = new System.Windows.Media.Pen(new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xC4, 0xC4, 0xC4)), 1);
        pen.Freeze();
        var row = RowHeight > 0 ? RowHeight : 26;
        for (var y = 0.0; y <= height; y += row)
            dc.DrawLine(pen, new System.Windows.Point(0, Snap(y)), new System.Windows.Point(width, Snap(y)));

        var x = 0.0;
        dc.DrawLine(pen, new System.Windows.Point(Snap(0), 0), new System.Windows.Point(Snap(0), height));
        foreach (var column in _columns)
        {
            x += column;
            dc.DrawLine(pen, new System.Windows.Point(Snap(x), 0), new System.Windows.Point(Snap(x), height));
        }
    }

    private static double Snap(double value) => Math.Round(value) + 0.5;
}
