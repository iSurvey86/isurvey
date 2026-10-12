namespace iSurvey.Modules.Plan;

/// <summary>Math.Clamp có từ .NET Core 2.0, không có trên .NET Framework 4.8.</summary>
internal static class Mathx
{
    public static double Clamp(double value, double min, double max)
    {
        if (value < min)
            return min;
        if (value > max)
            return max;
        return value;
    }
}
