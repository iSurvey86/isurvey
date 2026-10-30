using System.Windows;
using System.Windows.Controls;

namespace iSurvey.UI;

/// <summary>Lấy kiểu nút bo tròn cho hộp thoại dựng bằng code.</summary>
internal static class Cta
{
    private static ResourceDictionary? _styles;

    public static Style Style(string key)
    {
        _styles ??= new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/iSurvey;component/src/UI/CtaStyles.xaml", UriKind.Absolute)
        };
        return (Style)_styles[key];
    }
}
