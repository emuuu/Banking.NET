using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022.Internal;

/// <summary>Shared XML-building helpers for <see cref="Pain001Writer"/> and <see cref="Pain008Writer"/>.</summary>
internal static class PainWriterHelpers
{
    private const int LegacyNameMaxLength = 70;
    private const int CurrentNameMaxLength = 140;

    /// <summary>
    /// Returns whether <paramref name="version"/> selects the current (rather than legacy) pain.001 schema, i.e.
    /// whether it writes into the <c>.09</c> namespace with a 140-character name limit instead of <c>.03</c> with 70.
    /// This is the single place both <see cref="Pain001Writer"/> and <see cref="PainValidator"/> derive the
    /// version-dependent namespace suffix and name length limit from.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a defined <see cref="Pain001Version"/> value.</exception>
    public static bool IsCurrentVersion(Pain001Version version)
    {
        if (!Enum.IsDefined(version))
            throw new ArgumentOutOfRangeException(nameof(version), version, "Unknown pain.001 schema version.");
        return version == Pain001Version.V09;
    }

    /// <summary>
    /// Returns whether <paramref name="version"/> selects the current (rather than legacy) pain.008 schema, i.e.
    /// whether it writes into the <c>.08</c> namespace with a 140-character name limit instead of <c>.02</c> with 70.
    /// This is the single place both <see cref="Pain008Writer"/> and <see cref="PainValidator"/> derive the
    /// version-dependent namespace suffix and name length limit from.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a defined <see cref="Pain008Version"/> value.</exception>
    public static bool IsCurrentVersion(Pain008Version version)
    {
        if (!Enum.IsDefined(version))
            throw new ArgumentOutOfRangeException(nameof(version), version, "Unknown pain.008 schema version.");
        return version == Pain008Version.V08;
    }

    /// <summary>The maximum length of a party name for <paramref name="version"/> (70 for the legacy schema, 140 for the current one).</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a defined <see cref="Pain001Version"/> value.</exception>
    public static int MaxNameLength(Pain001Version version) => IsCurrentVersion(version) ? CurrentNameMaxLength : LegacyNameMaxLength;

    /// <summary>The maximum length of a party name for <paramref name="version"/> (70 for the legacy schema, 140 for the current one).</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a defined <see cref="Pain008Version"/> value.</exception>
    public static int MaxNameLength(Pain008Version version) => IsCurrentVersion(version) ? CurrentNameMaxLength : LegacyNameMaxLength;

    /// <summary>Serializes a document to a stream, UTF-8 encoded without a byte order mark, honoring <see cref="Pain00xWriterOptions.OmitXmlDeclaration"/>.</summary>
    public static void Save(XDocument document, Stream destination, Pain00xWriterOptions options)
    {
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = options.OmitXmlDeclaration,
        };

        using var writer = XmlWriter.Create(destination, settings);
        document.Save(writer);
    }

    /// <summary>Writes a party element (<c>Nm</c>, <c>PstlAdr</c>, <c>Id</c>).</summary>
    public static XElement WriteParty(XNamespace ns, string elementName, PartyIdentification party, bool useAnyBic)
    {
        var element = new XElement(ns + elementName);

        if (party.Name is { } name)
            element.Add(new XElement(ns + "Nm", name));

        if (party.PostalAddress is { } address)
            element.Add(WritePostalAddress(ns, "PstlAdr", address));

        var id = WritePartyId(ns, party, useAnyBic);
        if (id is not null)
            element.Add(new XElement(ns + "Id", id));

        return element;
    }

    /// <summary>Writes a postal address element in schema order (<c>Dept, StrtNm, BldgNb, PstCd, TwnNm, CtrySubDvsn, Ctry, AdrLine</c>).</summary>
    public static XElement WritePostalAddress(XNamespace ns, string elementName, PostalAddress address)
    {
        var element = new XElement(ns + elementName);

        if (address.Department is { } department)
            element.Add(new XElement(ns + "Dept", department));

        if (address.StreetName is { } streetName)
            element.Add(new XElement(ns + "StrtNm", streetName));

        if (address.BuildingNumber is { } buildingNumber)
            element.Add(new XElement(ns + "BldgNb", buildingNumber));

        if (address.PostCode is { } postCode)
            element.Add(new XElement(ns + "PstCd", postCode));

        if (address.TownName is { } townName)
            element.Add(new XElement(ns + "TwnNm", townName));

        if (address.CountrySubDivision is { } countrySubDivision)
            element.Add(new XElement(ns + "CtrySubDvsn", countrySubDivision));

        if (address.Country is { } country)
            element.Add(new XElement(ns + "Ctry", country));

        foreach (var line in address.AddressLines)
            element.Add(new XElement(ns + "AdrLine", line));

        return element;
    }

    /// <summary>Writes a cash account element. The IBAN is normalized (whitespace removed, upper-cased) before being written.</summary>
    public static XElement WriteAccount(XNamespace ns, string elementName, AccountIdentification account, bool includeCurrency)
    {
        var id = new XElement(ns + "Id");
        if (account.Iban is { } iban)
            id.Add(new XElement(ns + "IBAN", NormalizeIban(iban)));
        else if (account.OtherId is { } otherId)
            id.Add(new XElement(ns + "Othr", new XElement(ns + "Id", otherId)));

        var element = new XElement(ns + elementName, id);

        if (includeCurrency && account.Currency is { } currency)
            element.Add(new XElement(ns + "Ccy", currency));

        return element;
    }

    /// <summary>
    /// Writes a <c>FinInstnId</c> element carrying a BIC, or an <c>Othr/Id</c> fallback with <paramref name="fallbackId"/>
    /// when <paramref name="agent"/> is <see langword="null"/> or has no BIC.
    /// </summary>
    public static XElement WriteAgent(XNamespace ns, string bicElementName, FinancialInstitution? agent, string fallbackId)
    {
        var finInstnId = new XElement(ns + "FinInstnId");

        if (agent?.Bic is { } bic)
            finInstnId.Add(new XElement(ns + bicElementName, bic));
        else
            finInstnId.Add(new XElement(ns + "Othr", new XElement(ns + "Id", fallbackId)));

        return finInstnId;
    }

    /// <summary>
    /// Writes an <c>RmtInf</c> element for unstructured (<c>Ustrd</c>) or structured creditor reference
    /// (<c>Strd/CdtrRefInf</c>) remittance information, or <see langword="null"/> when neither is set.
    /// </summary>
    public static XElement? WriteRemittance(XNamespace ns, RemittanceInformation remittance)
    {
        if (remittance.Unstructured.Count > 0)
        {
            var element = new XElement(ns + "RmtInf");
            foreach (var line in remittance.Unstructured)
                element.Add(new XElement(ns + "Ustrd", line));
            return element;
        }

        if (remittance.CreditorReference is { } reference && !string.IsNullOrWhiteSpace(reference))
        {
            var type = new XElement(ns + "Tp",
                new XElement(ns + "CdOrPrtry", new XElement(ns + "Cd", remittance.CreditorReferenceTypeCode ?? "SCOR")));
            if (remittance.CreditorReferenceIssuer is { } issuer)
                type.Add(new XElement(ns + "Issr", issuer));

            return new XElement(ns + "RmtInf",
                new XElement(ns + "Strd",
                    new XElement(ns + "CdtrRefInf", type, new XElement(ns + "Ref", reference))));
        }

        return null;
    }

    /// <summary>Formats an amount with exactly two decimal places, using <see cref="CultureInfo.InvariantCulture"/>.</summary>
    public static string FormatAmount(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);

    /// <summary>Formats a date as <c>yyyy-MM-dd</c>.</summary>
    public static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Formats a date and time as <c>yyyy-MM-ddTHH:mm:ss</c>, taking the wall-clock value of the offset carried by <paramref name="dateTime"/> without converting it to another time zone, and without an offset suffix.</summary>
    public static string FormatDateTime(DateTimeOffset dateTime) =>
        dateTime.DateTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

    /// <summary>Normalizes an IBAN by removing whitespace and converting to upper case.</summary>
    public static string NormalizeIban(string iban) =>
        new string([.. iban.Where(c => !char.IsWhiteSpace(c))]).ToUpperInvariant();

    private static XElement? WritePartyId(XNamespace ns, PartyIdentification party, bool useAnyBic)
    {
        if (party.OrganisationId is { } organisation)
        {
            var orgId = new XElement(ns + "OrgId");
            if (organisation.Bic is { } bic)
                orgId.Add(new XElement(ns + (useAnyBic ? "AnyBIC" : "BICOrBEI"), bic));

            var other = WriteOtherIdentification(ns, organisation.OtherId, organisation.OtherSchemeCode, organisation.OtherSchemeProprietary, organisation.OtherIssuer);
            if (other is not null)
                orgId.Add(other);

            return orgId;
        }

        if (party.PrivateId is { } privateId)
        {
            var prvtId = new XElement(ns + "PrvtId");
            if (privateId.BirthDate is not null || privateId.CityOfBirth is not null || privateId.CountryOfBirth is not null)
            {
                var birth = new XElement(ns + "DtAndPlcOfBirth");
                if (privateId.BirthDate is { } birthDate)
                    birth.Add(new XElement(ns + "BirthDt", FormatDate(birthDate)));
                if (privateId.CityOfBirth is { } cityOfBirth)
                    birth.Add(new XElement(ns + "CityOfBirth", cityOfBirth));
                if (privateId.CountryOfBirth is { } countryOfBirth)
                    birth.Add(new XElement(ns + "CtryOfBirth", countryOfBirth));
                prvtId.Add(birth);
            }

            var other = WriteOtherIdentification(ns, privateId.OtherId, privateId.OtherSchemeCode, privateId.OtherSchemeProprietary, privateId.OtherIssuer);
            if (other is not null)
                prvtId.Add(other);

            return prvtId;
        }

        return null;
    }

    private static XElement? WriteOtherIdentification(XNamespace ns, string? id, string? schemeCode, string? schemeProprietary, string? issuer)
    {
        if (id is null)
            return null;

        var other = new XElement(ns + "Othr", new XElement(ns + "Id", id));

        if (schemeCode is not null || schemeProprietary is not null)
        {
            var schemeName = new XElement(ns + "SchmeNm");
            if (schemeCode is { } code)
                schemeName.Add(new XElement(ns + "Cd", code));
            else if (schemeProprietary is { } proprietary)
                schemeName.Add(new XElement(ns + "Prtry", proprietary));
            other.Add(schemeName);
        }

        if (issuer is { } issuerValue)
            other.Add(new XElement(ns + "Issr", issuerValue));

        return other;
    }
}
