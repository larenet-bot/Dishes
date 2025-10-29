using System.Globalization;
using UnityEngine;

/// <summary>
/// Centralized formatting wrapper for UI.
/// - Global defaults from Inspector (Short/Long, Decimals, Currency)
/// - Per-call override via the 'useShort' parameter on FormatNumber/FormatMoney
/// </summary>
public class BigNumberFormatter : MonoBehaviour
{
    public enum Style { Short, Long }

    // ---- Singleton for convenience ----
    private static BigNumberFormatter _instance;

    [Header("Global Defaults (used when no override passed)")]
    [Tooltip("Short = 1.2m | Long = 1.2 million")]
    public Style numberStyle = Style.Short;

    [Tooltip("Max decimal places for scaled numbers (e.g., 1.23m)")]
    [Range(0, 4)] public int decimals = 2;

    [Tooltip("Currency symbol for money fields")]
    public string currencySymbol = "$";

    // Fallbacks used if _instance is not available yet
    private const int DEFAULT_DECIMALS = 2;
    private const string DEFAULT_CURRENCY = "$";

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        // Optional: DontDestroyOnLoad(gameObject);
    }

    // ---------------- Number (no currency) ----------------

    /// <summary>
    /// Uses the Inspector default style (Short/Long).
    /// </summary>
    public static string FormatNumber(double value)
    {
        if (_instance == null)
            return value.ToString("#,0", CultureInfo.InvariantCulture);

        bool useShort = _instance.numberStyle == Style.Short;
        return useShort
            ? ConvertDecimal.ToShort(value, _instance.decimals)
            : ConvertDecimal.ToLong(value, _instance.decimals);
    }

    /// <summary>
    /// Per-call override. useShort = true -> "1.2m"; false -> "1.2 million".
    /// Honors Inspector 'decimals' even when overridden.
    /// </summary>
    public static string FormatNumber(double value, bool useShort)
    {
        if (_instance == null)
        {
            // No instance yet: still give a sensible result
            return useShort
                ? ConvertDecimal.ToShort(value, DEFAULT_DECIMALS)
                : ConvertDecimal.ToLong(value, DEFAULT_DECIMALS);
        }

        return useShort
            ? ConvertDecimal.ToShort(value, _instance.decimals)
            : ConvertDecimal.ToLong(value, _instance.decimals);
    }

    // Overloads
    public static string FormatNumber(long value) => FormatNumber((double)value);
    public static string FormatNumber(long value, bool useShort) => FormatNumber((double)value, useShort);
    public static string FormatNumber(decimal value) => FormatNumber((double)value);
    public static string FormatNumber(decimal value, bool useShort) => FormatNumber((double)value, useShort);

    // ---------------- Money (with currency) ----------------

    /// <summary>
    /// Uses Inspector default style + currency symbol.
    /// </summary>
    public static string FormatMoney(double value)
    {
        if (_instance == null)
        {
            string formattedFallback = ConvertDecimal.ToShort(value, DEFAULT_DECIMALS);
            return DEFAULT_CURRENCY + formattedFallback;
        }

        bool useShort = _instance.numberStyle == Style.Short;
        string formatted = useShort
            ? ConvertDecimal.ToShort(value, _instance.decimals)
            : ConvertDecimal.ToLong(value, _instance.decimals);

        // For scientific notation, just prefix currency as usual.
        return _instance.currencySymbol + formatted;
    }

    /// <summary>
    /// Per-call override. useShort = true -> "$1.2m"; false -> "$1.2 million".
    /// Uses Inspector 'decimals' and 'currencySymbol' even when overridden.
    /// </summary>
    public static string FormatMoney(double value, bool useShort)
    {
        if (_instance == null)
        {
            string formattedFallback = useShort
                ? ConvertDecimal.ToShort(value, DEFAULT_DECIMALS)
                : ConvertDecimal.ToLong(value, DEFAULT_DECIMALS);

            return DEFAULT_CURRENCY + formattedFallback;
        }

        string formatted = useShort
            ? ConvertDecimal.ToShort(value, _instance.decimals)
            : ConvertDecimal.ToLong(value, _instance.decimals);

        return _instance.currencySymbol + formatted;
    }

    // Overloads
    public static string FormatMoney(long value) => FormatMoney((double)value);
    public static string FormatMoney(long value, bool useShort) => FormatMoney((double)value, useShort);
    public static string FormatMoney(decimal value) => FormatMoney((double)value);
    public static string FormatMoney(decimal value, bool useShort) => FormatMoney((double)value, useShort);
}
