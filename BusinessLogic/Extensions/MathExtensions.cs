namespace BusinessLogic.Extensions;

public static class MathExtensions
{
    public static double PercentOf(this double value, double percent)
    {
        return value * (percent / 100);
    }

    public static decimal PercentOf(this decimal value, decimal percent)
    {
        return value * (percent / 100);
    }

    public static decimal Percentage(this decimal part, decimal total)
    {
        if (total == 0)
            throw new ArgumentException("Total cannot be 0");

        return (part / total) * 100;
    }

    public static decimal RoundDecimals(this decimal value, int decimals, MidpointRounding rounding = MidpointRounding.ToEven)
    {
        return Math.Round(value, decimals, rounding);
    }
}

