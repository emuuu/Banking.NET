using System.Globalization;
using System.Xml.Linq;

namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022.Internal;

/// <summary>
/// XML navigation helpers that match elements by local name, so camt and pain readers use a single code path
/// regardless of the document's schema version (and therefore target namespace).
/// </summary>
/// <remarks>
/// The child-lookup methods are named <see cref="Child"/> and <see cref="Children"/> rather than <c>Element</c>/
/// <c>Elements</c>: <see cref="XElement"/> already declares instance methods <c>Element(XName)</c> and
/// <c>Elements(XName)</c>, and since <c>string</c> converts implicitly to <see cref="XName"/>, an extension method
/// named <c>Element</c>/<c>Elements</c> with a <see cref="string"/> parameter would never be called — overload
/// resolution always prefers the instance method, silently defeating local-name matching (and throwing on a
/// <see langword="null"/> receiver instead of propagating it).
/// </remarks>
internal static class XmlNavigation
{
    /// <summary>Returns the first direct child with the given local name, or <see langword="null"/> when <paramref name="element"/> is <see langword="null"/> or has no such child.</summary>
    public static XElement? Child(this XElement? element, string localName) =>
        element?.Elements().FirstOrDefault(e => e.Name.LocalName == localName);

    /// <summary>Returns the direct children with the given local name, or an empty sequence when <paramref name="element"/> is <see langword="null"/>.</summary>
    public static IEnumerable<XElement> Children(this XElement? element, string localName) =>
        element?.Elements().Where(e => e.Name.LocalName == localName) ?? [];

    /// <summary>Returns the trimmed text content of <paramref name="element"/>, or <see langword="null"/> when <paramref name="element"/> is <see langword="null"/> or its text content is empty.</summary>
    public static string? Value(this XElement? element)
    {
        if (element is null)
            return null;

        var value = element.Value.Trim();
        return value.Length == 0 ? null : value;
    }

    /// <summary>Parses the element's text content as a <see cref="decimal"/> using <see cref="CultureInfo.InvariantCulture"/>, or <see langword="null"/> when <paramref name="element"/> is absent or empty.</summary>
    public static decimal? Decimal(this XElement? element)
    {
        var value = element.Value();
        return value is null ? null : decimal.Parse(value, CultureInfo.InvariantCulture);
    }

    /// <summary>Parses the element's text content as an <see cref="int"/>, or <see langword="null"/> when <paramref name="element"/> is absent or empty.</summary>
    public static int? Int(this XElement? element)
    {
        var value = element.Value();
        return value is null ? null : int.Parse(value, CultureInfo.InvariantCulture);
    }

    /// <summary>Parses the element's text content as a <see cref="long"/>, or <see langword="null"/> when <paramref name="element"/> is absent or empty.</summary>
    public static long? Long(this XElement? element)
    {
        var value = element.Value();
        return value is null ? null : long.Parse(value, CultureInfo.InvariantCulture);
    }

    /// <summary>Parses the element's text content as a boolean (<c>"true"</c>/<c>"false"</c>/<c>"1"</c>/<c>"0"</c>), or <see langword="null"/> when <paramref name="element"/> is absent or empty.</summary>
    /// <exception cref="FormatException">The text content is neither <c>"true"</c>, <c>"false"</c>, <c>"1"</c> nor <c>"0"</c>.</exception>
    public static bool? Bool(this XElement? element)
    {
        var value = element.Value();
        return value switch
        {
            null => null,
            "true" or "1" => true,
            "false" or "0" => false,
            _ => throw new FormatException($"'{value}' is not a valid boolean value."),
        };
    }

    /// <summary>Parses the element's text content as a <see cref="DateOnly"/> (ISO 8601; a value that also carries a time component is truncated to its date part), or <see langword="null"/> when <paramref name="element"/> is absent or empty.</summary>
    public static DateOnly? Date(this XElement? element)
    {
        var value = element.Value();
        if (value is null)
            return null;

        var datePart = value.Length > 10 ? value[..10] : value;
        return DateOnly.Parse(datePart, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Parses the element's text content as a <see cref="DateTimeOffset"/>, or <see langword="null"/> when
    /// <paramref name="element"/> is absent or empty. A value with an explicit offset (including a trailing
    /// <c>Z</c>) keeps that offset. A value without an offset — the common case in camt/pain documents — is read as
    /// offset zero, without converting to local time.
    /// </summary>
    public static DateTimeOffset? DateTime(this XElement? element)
    {
        var value = element.Value();
        if (value is null)
            return null;

        if (HasExplicitOffset(value))
            return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        var dateTime = System.DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.None);
        return new DateTimeOffset(System.DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), TimeSpan.Zero);
    }

    /// <summary>Reads a money amount from an element whose text content is the amount and whose <c>Ccy</c> attribute is the currency, or <see langword="null"/> when <paramref name="element"/> is absent or empty. The currency is an empty string when the <c>Ccy</c> attribute is absent.</summary>
    public static Money? Money(this XElement? element)
    {
        var amount = element.Decimal();
        if (amount is null)
            return null;

        var currency = element?.Attribute("Ccy")?.Value ?? string.Empty;
        return new Money(amount.Value, currency);
    }

    /// <summary>Navigates a chain of local names, following each name as a direct-child lookup, or <see langword="null"/> when any step is missing.</summary>
    public static XElement? Path(this XElement? element, params string[] localNames)
    {
        var current = element;
        foreach (var localName in localNames)
        {
            current = current.Child(localName);
            if (current is null)
                return null;
        }

        return current;
    }

    private static bool HasExplicitOffset(string value)
    {
        if (value.Length > 0 && value[^1] == 'Z')
            return true;

        var timeStart = value.IndexOf('T');
        if (timeStart < 0)
            return false;

        var timePart = value.AsSpan(timeStart + 1);
        return timePart.Contains('+') || timePart.Contains('-');
    }
}
