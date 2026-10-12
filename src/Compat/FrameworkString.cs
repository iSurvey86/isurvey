#if NETFRAMEWORK
using System.Text;

namespace iSurvey.Compat;

/// <summary>
/// string.Contains/Replace kèm StringComparison có từ .NET Core 2.1, không có trên .NET Framework 4.8.
/// </summary>
internal static class FrameworkString
{
    public static bool Contains(this string source, string value, StringComparison comparison) =>
        source.IndexOf(value, comparison) >= 0;

    public static string Replace(this string source, string oldValue, string newValue, StringComparison comparison)
    {
        if (string.IsNullOrEmpty(oldValue))
            throw new ArgumentException("Old value must not be empty.", nameof(oldValue));

        newValue ??= "";
        var result = new StringBuilder(source.Length);
        var index = 0;
        while (index < source.Length)
        {
            var found = source.IndexOf(oldValue, index, comparison);
            if (found < 0)
            {
                result.Append(source, index, source.Length - index);
                break;
            }

            result.Append(source, index, found - index);
            result.Append(newValue);
            index = found + oldValue.Length;
        }

        return result.ToString();
    }
}
#endif
