using System.Xml.Linq;
using Banking.NET.Commerzbank.CorporatePayments.Iso20022.Internal;

namespace Banking.NET.Commerzbank.CorporatePayments.Iso20022;

/// <summary>Reads camt.086 (bank services billing statement) messages. Covers the base structure; not every field of the schema is mapped (see <see cref="BillingStatement.Source"/> and related <c>Source</c> properties for the rest).</summary>
public static class Camt086Reader
{
    /// <summary>Reads a bank services billing message from an already-loaded document.</summary>
    /// <param name="document">The document to read.</param>
    /// <returns>The message.</returns>
    /// <exception cref="Iso20022ValidationException">The document's root element is not named <c>Document</c>, or has no <c>BkSvcsBllgStmt</c> child.</exception>
    public static BankServicesBillingMessage Read(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var root = document.Root;
        if (root is null || root.Name.LocalName != "Document")
            throw new Iso20022ValidationException(
                $"Expected root element 'Document', but found '{root?.Name.LocalName ?? "none"}'.", "Document");

        var message = root.Child("BkSvcsBllgStmt")
            ?? throw new Iso20022ValidationException(
                $"Expected root child 'BkSvcsBllgStmt', but found '{root.Elements().FirstOrDefault()?.Name.LocalName ?? "none"}'.",
                "Document/BkSvcsBllgStmt");

        var reportHeader = message.Child("RptHdr");

        return new BankServicesBillingMessage
        {
            Identifier = Iso20022Document.Identify(document),
            ReportId = reportHeader.Child("RptId").Value(),
            PageNumber = reportHeader.Path("MsgPgntn", "PgNb").Int(),
            LastPageIndicator = reportHeader.Path("MsgPgntn", "LastPgInd").Bool(),
            Groups = [.. message.Children("BllgStmtGrp").Select(ReadGroup)],
            Source = message,
        };
    }

    /// <summary>Reads a bank services billing message from a stream.</summary>
    /// <param name="xml">The stream to read the document from.</param>
    /// <returns>The message.</returns>
    public static BankServicesBillingMessage Read(Stream xml) => Read(Iso20022Document.Load(xml));

    /// <summary>Reads a bank services billing message from a string.</summary>
    /// <param name="xml">The XML content to parse.</param>
    /// <returns>The message.</returns>
    public static BankServicesBillingMessage Read(string xml) => Read(Iso20022Document.Load(xml));

    /// <summary>Reads a bank services billing message from a downloaded Corporate Payments message.</summary>
    /// <param name="message">The downloaded message.</param>
    /// <returns>The message.</returns>
    public static BankServicesBillingMessage Read(CorporatePaymentsMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return Read(message.GetContentAsString());
    }

    private static BillingStatementGroup ReadGroup(XElement element) =>
        new()
        {
            GroupId = element.Child("GrpId").Value(),
            Sender = CommonReaders.ReadParty(element.Child("Sndr")),
            Receiver = CommonReaders.ReadParty(element.Child("Rcvr")),
            Statements = [.. element.Children("BllgStmt").Select(ReadStatement)],
            Source = element,
        };

    private static BillingStatement ReadStatement(XElement element)
    {
        var accountCharacteristics = element.Child("AcctChrtcs");

        return new BillingStatement
        {
            StatementId = element.Child("StmtId").Value(),
            FromDate = element.Path("FrToDt", "FrDt").Date(),
            ToDate = element.Path("FrToDt", "ToDt").Date(),
            CreationDateTime = element.Child("CreDtTm").DateTime(),
            Status = element.Child("Sts").Value(),
            AccountLevel = accountCharacteristics.Child("AcctLvl").Value(),
            Account = CommonReaders.ReadAccount(accountCharacteristics.Child("CshAcct")),
            AccountServicer = CommonReaders.ReadFinancialInstitution(accountCharacteristics.Child("AcctSvcr")),
            CompensationMethod = accountCharacteristics.Child("CompstnMtd").Value(),
            AccountBalanceCurrency = accountCharacteristics.Child("AcctBalCcyCd").Value(),
            SettlementCurrency = accountCharacteristics.Child("SttlmCcyCd").Value(),
            HostCurrency = accountCharacteristics.Child("HstCcyCd").Value(),
            Balances = [.. element.Children("Bal").Select(ReadBalance)],
            Services = [.. element.Children("Svc").Select(ReadService)],
            TaxRegions = [.. element.Children("TaxRgn").Select(ReadTaxRegion)],
            Source = element,
        };
    }

    private static BillingBalance ReadBalance(XElement element)
    {
        var value = element.Child("Val");

        return new BillingBalance
        {
            TypeCode = element.Path("Tp", "Cd").Value() ?? element.Path("Tp", "Prtry").Value(),
            Amount = value.Child("Amt").Money()
                ?? throw new Iso20022ValidationException("Missing 'Amt' element on balance.", "BllgStmt/Bal/Val/Amt"),
            CreditDebit = (value.Child("Sgn").Bool() ?? false) ? CreditDebitIndicator.Debit : CreditDebitIndicator.Credit,
            Source = element,
        };
    }

    private static BillingService ReadService(XElement element)
    {
        var serviceDetail = element.Child("SvcDtl");
        var bankService = serviceDetail.Child("BkSvc");
        var subService = bankService.Child("SubSvc");
        var pricing = element.Child("Pric");

        return new BillingService
        {
            ServiceId = bankService.Child("Id").Value(),
            SubServiceCode = subService.Child("Id").Value(),
            SubServiceIssuer = subService.Path("Issr", "Cd").Value(),
            Description = bankService.Child("Desc").Value(),
            CommonCode = bankService.Path("CmonCd", "Id").Value(),
            CommonCodeIssuer = bankService.Path("CmonCd", "Issr").Value(),
            ServiceType = bankService.Child("SvcTp").Value(),
            BankTransactionCode = CommonReaders.ReadBankTransactionCode(bankService.Child("BkTxCd")),
            Volume = serviceDetail.Child("Vol").Decimal(),
            PriceCurrency = pricing.Child("Ccy").Value(),
            UnitPrice = SignedMoney(pricing.Child("UnitPric")),
            PriceMethod = pricing.Child("Mtd").Value(),
            PriceRule = pricing.Child("Rule").Value(),
            PaymentMethod = element.Child("PmtMtd").Value(),
            OriginalChargePrice = SignedMoney(element.Child("OrgnlChrgPric")),
            OriginalChargeSettlementAmount = SignedMoney(element.Child("OrgnlChrgSttlmAmt")),
            BalanceRequiredAmount = SignedMoney(element.Child("BalReqrdAcctAmt")),
            TaxDesignation = element.Path("TaxDsgnt", "Cd").Value(),
            Source = element,
        };
    }

    private static BillingTaxRegion ReadTaxRegion(XElement element) =>
        new()
        {
            RegionNumber = element.Child("RgnNb").Value(),
            RegionName = element.Child("RgnNm").Value(),
            CustomerTaxId = element.Child("CstmrTaxId").Value(),
            SettlementAmount = SignedMoney(element.Child("SttlmAmt")),
            TaxDueToRegion = SignedMoney(element.Child("TaxDueToRgn")),
            Source = element,
        };

    /// <summary>Reads an <c>AmountAndDirection34</c> element (<c>Amt</c> + <c>Sgn</c>), negating the amount when <c>Sgn</c> is <see langword="true"/>.</summary>
    private static Money? SignedMoney(XElement? element)
    {
        var money = element.Child("Amt").Money();
        if (money is null)
            return null;

        return (element.Child("Sgn").Bool() ?? false) ? money.Value with { Amount = -money.Value.Amount } : money;
    }
}
