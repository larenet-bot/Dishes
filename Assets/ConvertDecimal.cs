using System;
using System.Globalization;

/// <summary>
/// ConvertDecimal: utilities to turn large numeric values into human-readable strings.
/// - Long form: 1,000,000 -> "1 million"
/// - Short form: 1,000,000 -> "1m"
/// Supports up to decillion (10^33). Beyond that, falls back to scientific notation (e.g., "1.23E+36").
/// Works with double; overloads accept decimal and long for convenience.
/// </summary>
public static class ConvertDecimal
{
    private static readonly string[] LongNames = new[]
    {
        "", " thousand", " million", " billion", " trillion",
        " quadrillion", " quintillion", " sextillion", " septillion",
        " octillion", " nonillion", " decillion"
    };

    private static readonly string[] ShortSuffixes = new[]
    {
        "", "k", "m", "b", "t", "q", "Q", "s", "S", "o", "n", "d"
    };

    private static readonly double[] ThousandPowers = BuildThousandPowers(11); // up to decillion (10^33)
    private const double ScientificThreshold = 1e33;

    public static string ToLong(double value, int decimals = 2)
    {
        if (!IsFinite(value))
            return value.ToString(CultureInfo.InvariantCulture);
        if (value == 0) return "0";

        double abs = Math.Abs(value);
        if (abs >= ScientificThreshold)
            return value.ToString(BuildSciFormat(decimals), CultureInfo.InvariantCulture);

        int order = GetOrder(abs);
        if (order <= 0)
            return FormatWithSeparators(value, 0);

        double scaled = value / ThousandPowers[order];
        return $"{FormatScaled(scaled, decimals)}{LongNames[order]}";
    }

    public static string ToShort(double value, int decimals = 2)
    {
        if (!IsFinite(value))
            return value.ToString(CultureInfo.InvariantCulture);
        if (value == 0) return "0";

        double abs = Math.Abs(value);
        if (abs >= ScientificThreshold)
            return value.ToString(BuildSciFormat(decimals), CultureInfo.InvariantCulture);

        int order = GetOrder(abs);
        if (order <= 0)
            return FormatWithSeparators(value, ValueHasFraction(value) ? decimals : 0);

        double scaled = value / ThousandPowers[order];
        return $"{FormatScaled(scaled, decimals)}{ShortSuffixes[order]}";
    }

    // Convenience overloads
    public static string ToLong(decimal value, int decimals = 2) => ToLong((double)value, decimals);
    public static string ToShort(decimal value, int decimals = 2) => ToShort((double)value, decimals);
    public static string ToLong(long value, int decimals = 2) => ToLong((double)value, decimals);
    public static string ToShort(long value, int decimals = 2) => ToShort((double)value, decimals);

    // Helpers
    private static int GetOrder(double absValue)
    {
        int order = (int)Math.Floor(Math.Log10(absValue) / 3.0);
        if (order < 0) order = 0;
        int maxOrder = 11; // decillion index
        if (order > maxOrder) order = maxOrder;
        return order;
    }

    private static string FormatScaled(double scaled, int decimals)
    {
        string pattern = "0." + new string('#', Math.Max(0, decimals));
        return scaled.ToString(pattern, CultureInfo.InvariantCulture);
    }

    private static string FormatWithSeparators(double value, int decimals)
    {
        if (decimals <= 0 || Math.Abs(value % 1) < double.Epsilon)
            return value.ToString("#,0", CultureInfo.InvariantCulture);

        string pattern = "#,0." + new string('#', Math.Max(0, decimals));
        return value.ToString(pattern, CultureInfo.InvariantCulture);
    }

    private static string BuildSciFormat(int decimals) => "0." + new string('0', Math.Max(0, decimals)) + "E+0";
    private static bool IsFinite(double v) => !(double.IsNaN(v) || double.IsInfinity(v));
    private static bool ValueHasFraction(double v) => Math.Abs(v % 1) > 1e-12;

    private static double[] BuildThousandPowers(int maxOrder)
    {
        var arr = new double[maxOrder + 1];
        arr[0] = 1d;
        for (int i = 1; i <= maxOrder; i++) arr[i] = arr[i - 1] * 1000d;
        return arr;
    }
}

/// <summary>Small facade for shorthand.</summary>
public static class NumberAbbreviator
{
    public static string Format(double value, int decimals = 2) => ConvertDecimal.ToShort(value, decimals);
    public static string Format(decimal value, int decimals = 2) => ConvertDecimal.ToShort(value, decimals);
    public static string Format(long value, int decimals = 2) => ConvertDecimal.ToShort(value, decimals);
}
