using System.Globalization;
using System.Text;

namespace Commerzbank.NET.CorporatePayments.Iso20022;

/// <summary>Validates and sanitizes text against the SEPA character set used in pain.001 and pain.008 messages.</summary>
public static class SepaCharacterSet
{
    private const string AllowedSpecialCharacters = "/-?:().,'+ ";

    /// <summary>Determines whether every character in <paramref name="value"/> is part of the SEPA character set (<c>a-z A-Z 0-9 / - ? : ( ) . , ' + Space</c>).</summary>
    /// <param name="value">The text to validate.</param>
    /// <returns><see langword="true"/> when every character is allowed; otherwise <see langword="false"/>.</returns>
    public static bool IsValid(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        foreach (var c in value)
        {
            if (!IsAllowed(c))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Converts text into the SEPA character set: the German umlauted vowels and the sharp s are each expanded to
    /// their two-letter equivalent (<c>ae</c>, <c>oe</c>, <c>ue</c>, and their capitalized forms, plus <c>ss</c>),
    /// other accented Latin letters are reduced to their base letter (e.g. an e with an acute accent becomes a
    /// plain <c>e</c>) by Unicode normalization, and any remaining disallowed character becomes a space.
    /// Consecutive spaces are collapsed to one and the result is trimmed.
    /// </summary>
    /// <param name="value">The text to sanitize.</param>
    /// <returns>The sanitized text, containing only characters from the SEPA character set.</returns>
    public static string Sanitize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var expanded = ExpandGermanCharacters(value);
        var normalized = expanded.Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;

            builder.Append(IsAllowed(c) ? c : ' ');
        }

        return CollapseSpaces(builder.ToString());
    }

    private static bool IsAllowed(char c) =>
        (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || AllowedSpecialCharacters.IndexOf(c) >= 0;

    private static string ExpandGermanCharacters(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            builder.Append(c switch
            {
                '\u00E4' => "ae",
                '\u00F6' => "oe",
                '\u00FC' => "ue",
                '\u00C4' => "Ae",
                '\u00D6' => "Oe",
                '\u00DC' => "Ue",
                '\u00DF' => "ss",
                _ => c.ToString(),
            });
        }

        return builder.ToString();
    }

    private static string CollapseSpaces(string value)
    {
        var builder = new StringBuilder(value.Length);
        var lastWasSpace = false;

        foreach (var c in value)
        {
            if (c == ' ')
            {
                if (lastWasSpace)
                    continue;

                lastWasSpace = true;
            }
            else
            {
                lastWasSpace = false;
            }

            builder.Append(c);
        }

        return builder.ToString().Trim();
    }
}
