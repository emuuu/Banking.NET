using System.Xml.Linq;

namespace Commerzbank.NET.CorporatePayments.Iso20022.Internal;

/// <summary>Readers for the ISO 20022 building blocks shared across camt and pain messages.</summary>
internal static class CommonReaders
{
    /// <summary>
    /// Reads a party (e.g. debtor, creditor, originator). Resolves the <c>Pty</c> child transparently when the
    /// element carries a party-choice wrapper (<c>Party40Choice</c>, used in schema versions .08/.10).
    /// </summary>
    public static PartyIdentification? ReadParty(XElement? element)
    {
        if (element is null)
            return null;

        var party = element.Child("Pty") ?? element;

        return new PartyIdentification
        {
            Name = party.Child("Nm").Value(),
            PostalAddress = ReadPostalAddress(party.Child("PstlAdr")),
            OrganisationId = ReadOrganisationId(party.Path("Id", "OrgId")),
            PrivateId = ReadPrivateId(party.Path("Id", "PrvtId")),
            CountryOfResidence = party.Child("CtryOfRes").Value(),
            Source = element,
        };
    }

    /// <summary>Reads a postal address.</summary>
    public static PostalAddress? ReadPostalAddress(XElement? element)
    {
        if (element is null)
            return null;

        return new PostalAddress
        {
            Country = element.Child("Ctry").Value(),
            AddressLines = [.. element.Children("AdrLine").Select(e => e.Value()).OfType<string>()],
            StreetName = element.Child("StrtNm").Value(),
            BuildingNumber = element.Child("BldgNb").Value(),
            PostCode = element.Child("PstCd").Value(),
            TownName = element.Child("TwnNm").Value(),
            Department = element.Child("Dept").Value(),
            CountrySubDivision = element.Child("CtrySubDvsn").Value(),
            Source = element,
        };
    }

    /// <summary>Reads a cash account identification.</summary>
    public static AccountIdentification? ReadAccount(XElement? element)
    {
        if (element is null)
            return null;

        return new AccountIdentification
        {
            Iban = element.Path("Id", "IBAN").Value(),
            OtherId = element.Path("Id", "Othr", "Id").Value(),
            OtherSchemeCode = element.Path("Id", "Othr", "SchmeNm", "Cd").Value(),
            Currency = element.Child("Ccy").Value(),
            Name = element.Child("Nm").Value(),
            TypeCode = element.Path("Tp", "Cd").Value(),
            Source = element,
        };
    }

    /// <summary>
    /// Reads a financial institution identification from a wrapper element (e.g. <c>DbtrAgt</c>, <c>CdtrAgt</c>,
    /// <c>Svcr</c>) that carries a <c>FinInstnId</c> child, resolved transparently the same way <see cref="ReadParty"/>
    /// resolves <c>Pty</c>. The BIC is read from either <c>BIC</c> (older schema versions) or <c>BICFI</c> (newer ones).
    /// </summary>
    public static FinancialInstitution? ReadFinancialInstitution(XElement? element)
    {
        if (element is null)
            return null;

        var finInstnId = element.Child("FinInstnId") ?? element;

        return new FinancialInstitution
        {
            Bic = finInstnId.Child("BIC").Value() ?? finInstnId.Child("BICFI").Value(),
            Name = finInstnId.Child("Nm").Value(),
            ClearingSystemMemberId = finInstnId.Path("ClrSysMmbId", "MmbId").Value(),
            OtherId = finInstnId.Path("Othr", "Id").Value(),
            PostalAddress = ReadPostalAddress(finInstnId.Child("PstlAdr")),
            Source = element,
        };
    }

    /// <summary>Reads remittance information. All <c>Strd</c> elements are scanned; the creditor reference is read from the first one that carries a <c>CdtrRefInf</c> child.</summary>
    public static RemittanceInformation? ReadRemittance(XElement? element)
    {
        if (element is null)
            return null;

        var creditorRefInfo = element.Children("Strd")
            .Select(structured => structured.Child("CdtrRefInf"))
            .FirstOrDefault(cdtrRefInf => cdtrRefInf is not null);
        var creditorRefType = creditorRefInfo.Child("Tp");

        return new RemittanceInformation
        {
            Unstructured = [.. element.Children("Ustrd").Select(e => e.Value()).OfType<string>()],
            CreditorReference = creditorRefInfo.Child("Ref").Value(),
            CreditorReferenceTypeCode = creditorRefType.Path("CdOrPrtry", "Cd").Value(),
            CreditorReferenceTypeProprietary = creditorRefType.Path("CdOrPrtry", "Prtry").Value(),
            CreditorReferenceIssuer = creditorRefType.Child("Issr").Value(),
            Source = element,
        };
    }

    /// <summary>Reads status reason information entries.</summary>
    public static List<StatusReason> ReadStatusReasons(IEnumerable<XElement> elements) =>
        [.. elements.Select(ReadStatusReason)];

    /// <summary>Parses a credit/debit indicator element (<c>"CRDT"</c>/<c>"DBIT"</c>), or <see langword="null"/> when <paramref name="element"/> is absent or empty.</summary>
    /// <exception cref="FormatException">The text content is neither <c>"CRDT"</c> nor <c>"DBIT"</c>.</exception>
    public static CreditDebitIndicator? ReadCreditDebit(XElement? element)
    {
        var value = element.Value();
        return value switch
        {
            null => null,
            "CRDT" => CreditDebitIndicator.Credit,
            "DBIT" => CreditDebitIndicator.Debit,
            _ => throw new FormatException($"'{value}' is not a valid credit/debit indicator."),
        };
    }

    /// <summary>Reads a bank transaction code. The element passed in is the <c>BkTxCd</c> element itself.</summary>
    public static BankTransactionCode? ReadBankTransactionCode(XElement? element)
    {
        if (element is null)
            return null;

        return new BankTransactionCode
        {
            Domain = element.Path("Domn", "Cd").Value(),
            Family = element.Path("Domn", "Fmly", "Cd").Value(),
            SubFamily = element.Path("Domn", "Fmly", "SubFmlyCd").Value(),
            ProprietaryCode = element.Path("Prtry", "Cd").Value(),
            ProprietaryIssuer = element.Path("Prtry", "Issr").Value(),
            Source = element,
        };
    }

    /// <summary>Reads payment type information. The element passed in is the <c>PmtTpInf</c> element itself.</summary>
    public static PaymentTypeInformation? ReadPaymentTypeInformation(XElement? element)
    {
        if (element is null)
            return null;

        return new PaymentTypeInformation
        {
            InstructionPriority = element.Child("InstrPrty").Value(),
            ServiceLevelCode = element.Path("SvcLvl", "Cd").Value(),
            ServiceLevelProprietary = element.Path("SvcLvl", "Prtry").Value(),
            LocalInstrumentCode = element.Path("LclInstrm", "Cd").Value(),
            LocalInstrumentProprietary = element.Path("LclInstrm", "Prtry").Value(),
            SequenceType = element.Child("SeqTp").Value(),
            CategoryPurposeCode = element.Path("CtgyPurp", "Cd").Value(),
            CategoryPurposeProprietary = element.Path("CtgyPurp", "Prtry").Value(),
        };
    }

    /// <summary>Reads mandate-related information. The element passed in is the <c>MndtRltdInf</c> element itself.</summary>
    public static MandateInformation? ReadMandate(XElement? element)
    {
        if (element is null)
            return null;

        var amendmentDetails = element.Child("AmdmntInfDtls");
        var originalCreditorSchemeId = amendmentDetails.Child("OrgnlCdtrSchmeId");

        return new MandateInformation
        {
            MandateId = element.Child("MndtId").Value(),
            DateOfSignature = element.Child("DtOfSgntr").Date(),
            AmendmentIndicator = element.Child("AmdmntInd").Bool(),
            OriginalMandateId = amendmentDetails.Child("OrgnlMndtId").Value(),
            OriginalCreditorSchemeId = originalCreditorSchemeId.Path("Id", "PrvtId", "Othr", "Id").Value(),
            OriginalCreditorName = originalCreditorSchemeId.Child("Nm").Value(),
            OriginalDebtorAccount = ReadAccount(amendmentDetails.Child("OrgnlDbtrAcct")),
            OriginalDebtorAgent = ReadFinancialInstitution(amendmentDetails.Child("OrgnlDbtrAgt")),
            Source = element,
        };
    }

    private static StatusReason ReadStatusReason(XElement element) =>
        new()
        {
            Code = element.Path("Rsn", "Cd").Value(),
            Proprietary = element.Path("Rsn", "Prtry").Value(),
            Originator = ReadParty(element.Child("Orgtr")),
            AdditionalInformation = [.. element.Children("AddtlInf").Select(e => e.Value()).OfType<string>()],
            Source = element,
        };

    private static OrganisationIdentification? ReadOrganisationId(XElement? element)
    {
        if (element is null)
            return null;

        return new OrganisationIdentification
        {
            Bic = element.Child("AnyBIC").Value() ?? element.Child("BICOrBEI").Value(),
            OtherId = element.Path("Othr", "Id").Value(),
            OtherSchemeCode = element.Path("Othr", "SchmeNm", "Cd").Value(),
            OtherSchemeProprietary = element.Path("Othr", "SchmeNm", "Prtry").Value(),
            OtherIssuer = element.Path("Othr", "Issr").Value(),
        };
    }

    private static PrivateIdentification? ReadPrivateId(XElement? element)
    {
        if (element is null)
            return null;

        var birth = element.Child("DtAndPlcOfBirth");

        return new PrivateIdentification
        {
            BirthDate = birth.Child("BirthDt").Date(),
            CityOfBirth = birth.Child("CityOfBirth").Value(),
            CountryOfBirth = birth.Child("CtryOfBirth").Value(),
            OtherId = element.Path("Othr", "Id").Value(),
            OtherSchemeCode = element.Path("Othr", "SchmeNm", "Cd").Value(),
            OtherSchemeProprietary = element.Path("Othr", "SchmeNm", "Prtry").Value(),
            OtherIssuer = element.Path("Othr", "Issr").Value(),
        };
    }
}
