using System.Globalization;
using System.Text.RegularExpressions;

namespace Lragrz.Enova.Core.Xml;

/// <summary>
/// Parses date/time values from XJustiz and foreign formats and formats them
/// for German display (dd.MM.yyyy / dd.MM.yyyy HH:mm:ss).
/// </summary>
public static class EnovaDateTime
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    private static readonly string[] ExactFormats =
    {
        // ISO date / date-time
        "yyyy-MM-dd",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
        "yyyy-MM-ddTHH:mm:ssK",
        "yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd HH:mm",
        "yyyy/MM/dd",
        "yyyy/MM/dd HH:mm:ss",
        // German
        "dd.MM.yyyy",
        "d.M.yyyy",
        "dd.MM.yyyy HH:mm:ss",
        "dd.MM.yyyy HH:mm",
        "d.M.yyyy HH:mm:ss",
        "d.M.yyyy HH:mm",
        // US / UK ambiguous styles (parsed with explicit formats only)
        "MM/dd/yyyy",
        "M/d/yyyy",
        "MM/dd/yyyy HH:mm:ss",
        "M/d/yyyy h:mm:ss tt",
        "M/d/yyyy h:mm tt",
        "dd/MM/yyyy",
        "d/M/yyyy",
        "dd/MM/yyyy HH:mm:ss",
        // Compact
        "yyyyMMdd",
        "yyyyMMddHHmmss",
        "yyyyMMddHHmm",
        // RFC-ish
        "r",
        "R"
    };

    /// <summary>
    /// Try parse any common XML/notary date or date-time string.
    /// Returns UTC-normalized DateTimeOffset when timezone is present; otherwise unspecified local calendar value.
    /// </summary>
    public static bool TryParse(string? input, out DateTimeOffset value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var s = input.Trim();

        // Normalize trailing Z / space-separated timezone
        if (s.EndsWith(" UTC", StringComparison.OrdinalIgnoreCase))
            s = s[..^3].Trim() + "Z";

        // Pure date yyyy-MM-dd etc.
        if (DateTimeOffset.TryParseExact(s, ExactFormats, Invariant, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out value))
            return true;

        if (DateTimeOffset.TryParseExact(s, ExactFormats, German, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out value))
            return true;

        // Flexible ISO / RFC
        if (DateTimeOffset.TryParse(s, Invariant, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind, out value))
            return true;

        if (DateTimeOffset.TryParse(s, German, DateTimeStyles.AllowWhiteSpaces, out value))
            return true;

        // US culture as last resort for month names etc.
        if (DateTimeOffset.TryParse(s, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.AllowWhiteSpaces, out value))
            return true;

        // Slash dates: prefer ISO yyyy/MM/dd already covered; try reorder dd-MM-yyyy
        var m = Regex.Match(s, @"^(\d{1,4})[.\-/](\d{1,2})[.\-/](\d{1,4})(?:[ T](\d{1,2}):(\d{2})(?::(\d{2}))?)?");
        if (m.Success)
        {
            var a = int.Parse(m.Groups[1].Value, Invariant);
            var b = int.Parse(m.Groups[2].Value, Invariant);
            var c = int.Parse(m.Groups[3].Value, Invariant);
            int year, month, day;
            if (a > 31) { year = a; month = b; day = c; }           // yyyy-M-d
            else if (c > 31) { day = a; month = b; year = c; }     // d-M-yyyy (German-like)
            else if (a > 12) { day = a; month = b; year = c < 100 ? 2000 + c : c; }
            else if (b > 12) { month = a; day = b; year = c < 100 ? 2000 + c : c; } // US M/d/y when day>12
            else { day = a; month = b; year = c < 100 ? 2000 + c : c; } // default European

            if (year is >= 1 and <= 9999 && month is >= 1 and <= 12 && day is >= 1 and <= 31)
            {
                var hh = m.Groups[4].Success ? int.Parse(m.Groups[4].Value, Invariant) : 0;
                var mm = m.Groups[5].Success ? int.Parse(m.Groups[5].Value, Invariant) : 0;
                var ss = m.Groups[6].Success ? int.Parse(m.Groups[6].Value, Invariant) : 0;
                try
                {
                    value = new DateTimeOffset(year, month, day, hh, mm, ss, TimeSpan.Zero);
                    return true;
                }
                catch { /* invalid calendar day */ }
            }
        }

        return false;
    }

    /// <summary>
    /// Format for UI/PDF: date-only → dd.MM.yyyy; date-time → dd.MM.yyyy HH:mm:ss.
    /// Unparseable input is returned trimmed unchanged.
    /// </summary>
    public static string FormatDisplay(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "";

        if (!TryParse(input, out var dto))
            return input.Trim();

        var hasTime = HasTimeComponent(input.Trim(), dto);
        if (!hasTime)
            return dto.ToString("dd.MM.yyyy", German);

        return dto.ToString("dd.MM.yyyy HH:mm:ss", German);
    }

    /// <summary>Always date-only German.</summary>
    public static string FormatDate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "";
        if (!TryParse(input, out var dto))
            return input.Trim();
        return dto.ToString("dd.MM.yyyy", German);
    }

    /// <summary>Always date-time German (seconds).</summary>
    public static string FormatDateTime(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "";
        if (!TryParse(input, out var dto))
            return input.Trim();
        return dto.ToString("dd.MM.yyyy HH:mm:ss", German);
    }

    /// <summary>XJustiz / XML friendly UTC: yyyy-MM-ddTHH:mm:ssZ</summary>
    public static string FormatXmlUtc(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", Invariant);

    /// <summary>XJustiz date-only: yyyy-MM-dd</summary>
    public static string FormatXmlDate(DateTimeOffset value) =>
        value.ToString("yyyy-MM-dd", Invariant);

    private static bool HasTimeComponent(string original, DateTimeOffset dto)
    {
        if (original.Contains('T', StringComparison.OrdinalIgnoreCase)) return true;
        if (original.Contains(':')) return true;
        if (Regex.IsMatch(original, @"\d{8}\d{4,}")) return true; // compact yyyyMMddHHmm
        // Parsed midnight with no time tokens → date only
        return dto.TimeOfDay != TimeSpan.Zero && !original.Contains('T')
            ? true
            : original.Contains(':') || original.Contains('T', StringComparison.OrdinalIgnoreCase);
    }
}
