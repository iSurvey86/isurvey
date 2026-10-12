namespace iSurvey;

/// <summary>char.IsAsciiDigit có từ .NET 7, không có trên .NET Framework 4.8.</summary>
internal static class Ascii
{
    public static bool IsDigit(char c) => c is >= '0' and <= '9';
}
