using Banking.NET.Commerzbank.CorporatePayments;
using Shouldly;
using Xunit;

namespace Banking.NET.Tests.CorporatePayments;

public class OrderTypeTests
{
    public static TheoryData<OrderType, string, OrderDirection, string, string[], string> KnownOrderTypeData()
    {
        var data = new TheoryData<OrderType, string, OrderDirection, string, string[], string>
        {
            { OrderType.C52, "C52", OrderDirection.Download, "camt.052", ["camt.052.001.08", "camt.052.001.02"], "Intraday account report" },
            { OrderType.C53, "C53", OrderDirection.Download, "camt.053", ["camt.053.001.08", "camt.053.001.02"], "Account statement" },
            { OrderType.C54, "C54", OrderDirection.Download, "camt.054", ["camt.054.001.08"], "Debit and credit notification" },
            { OrderType.C86, "C86", OrderDirection.Download, "camt.086", ["camt.086.001.02"], "Bank services billing statement" },
            { OrderType.HAC, "HAC", OrderDirection.Download, "pain.002", ["pain.002.001.10", "pain.002.001.03"], "Customer acknowledgement (payment status report)" },
            { OrderType.AXS, "AXS", OrderDirection.Download, "pain.002", ["pain.002.001.10"], "Payment status report for cross-border credit transfers" },
            { OrderType.CUZ, "CUZ", OrderDirection.Download, "pain.002", ["pain.002.001.10"], "Payment status report for urgent credit transfers" },
            { OrderType.XIP, "XIP", OrderDirection.Download, "pain.002", ["pain.002.001.03", "pain.002.001.10"], "Payment status report for XIC and XID orders" },
            { OrderType.CIZ, "CIZ", OrderDirection.Download, "pain.002", ["pain.002.001.10"], "Payment status report for instant credit transfers" },
            { OrderType.CDZ, "CDZ", OrderDirection.Download, "pain.002", ["pain.002.001.10"], "Payment status report for direct debits" },
            { OrderType.CRZ, "CRZ", OrderDirection.Download, "pain.002", ["pain.002.001.10"], "Payment status report for credit transfers" },
            { OrderType.CCT, "CCT", OrderDirection.Upload, "pain.001", ["pain.001.001.09"], "SEPA credit transfer" },
            { OrderType.CTV, "CTV", OrderDirection.Upload, "pain.001", ["pain.001.001.09"], "SEPA credit transfer (CTV)" },
            { OrderType.CIP, "CIP", OrderDirection.Upload, "pain.001", ["pain.001.001.09"], "SEPA instant credit transfer" },
            { OrderType.CIV, "CIV", OrderDirection.Upload, "pain.001", ["pain.001.001.09"], "SEPA instant credit transfer (CIV)" },
            { OrderType.XIC, "XIC", OrderDirection.Upload, "pain.001", ["pain.001.001.03"], "SEPA credit transfer (pain.001.001.03)" },
            { OrderType.CDD, "CDD", OrderDirection.Upload, "pain.008", ["pain.008.001.08"], "SEPA direct debit (CORE)" },
            { OrderType.CDB, "CDB", OrderDirection.Upload, "pain.008", ["pain.008.001.08"], "SEPA direct debit (B2B)" },
            { OrderType.XID, "XID", OrderDirection.Upload, "pain.008", ["pain.008.001.02"], "SEPA direct debit (pain.008.001.02)" },
            { OrderType.AXZ, "AXZ", OrderDirection.Upload, "pain.001", ["pain.001.001.09"], "Cross-border credit transfer" },
            { OrderType.CCU, "CCU", OrderDirection.Upload, "pain.001", ["pain.001.001.09"], "Urgent credit transfer" },
        };
        return data;
    }

    [Theory]
    [MemberData(nameof(KnownOrderTypeData))]
    public void KnownOrderType_HasExpectedMetadata(OrderType orderType, string code, OrderDirection direction, string messageType, string[] schemaVersions, string description)
    {
        orderType.Code.ShouldBe(code);
        orderType.Direction.ShouldBe(direction);
        orderType.MessageType.ShouldBe(messageType);
        orderType.SchemaVersions.ShouldBe(schemaVersions);
        orderType.Description.ShouldBe(description);
        orderType.IsKnown.ShouldBeTrue();
    }

    [Fact]
    public void Known_Has21Entries() => OrderType.Known.Count.ShouldBe(21);

    [Fact]
    public void DownloadTypes_Has11Entries() => OrderType.DownloadTypes.Count.ShouldBe(11);

    [Fact]
    public void UploadTypes_Has10Entries() => OrderType.UploadTypes.Count.ShouldBe(10);

    [Fact]
    public void DownloadTypes_AllHaveDownloadDirection() => OrderType.DownloadTypes.ShouldAllBe(orderType => orderType.Direction == OrderDirection.Download);

    [Fact]
    public void UploadTypes_AllHaveUploadDirection() => OrderType.UploadTypes.ShouldAllBe(orderType => orderType.Direction == OrderDirection.Upload);

    [Fact]
    public void DownloadTypes_And_UploadTypes_CoverAllKnownTypes() => (OrderType.DownloadTypes.Count + OrderType.UploadTypes.Count).ShouldBe(OrderType.Known.Count);

    [Theory]
    [InlineData("c53")]
    [InlineData("C53")]
    [InlineData(" C53 ")]
    [InlineData("\tC53\n")]
    public void Parse_IsCaseInsensitiveAndTrims(string input)
    {
        var orderType = OrderType.Parse(input);
        orderType.ShouldBe(OrderType.C53);
        orderType.Code.ShouldBe("C53");
    }

    [Fact]
    public void Parse_UnknownCode_ReturnsUnknownOrderType()
    {
        var orderType = OrderType.Parse("zzz");

        orderType.IsKnown.ShouldBeFalse();
        orderType.Direction.ShouldBe(OrderDirection.Unknown);
        orderType.Code.ShouldBe("ZZZ");
        orderType.MessageType.ShouldBeNull();
        orderType.Description.ShouldBeNull();
        orderType.SchemaVersions.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NullOrWhiteSpace_ThrowsArgumentException(string? code)
    {
        Should.Throw<ArgumentException>(() => OrderType.Parse(code!));
    }

    [Fact]
    public void TryParse_Null_ReturnsFalseAndDefault()
    {
        var result = OrderType.TryParse(null, out var orderType);

        result.ShouldBeFalse();
        orderType.ShouldBe(default(OrderType));
    }

    [Fact]
    public void TryParse_Empty_ReturnsFalse() => OrderType.TryParse("", out _).ShouldBeFalse();

    [Fact]
    public void TryParse_KnownCode_ReturnsTrueAndOrderType()
    {
        var result = OrderType.TryParse("cct", out var orderType);

        result.ShouldBeTrue();
        orderType.ShouldBe(OrderType.CCT);
    }

    [Fact]
    public void TryParse_UnknownCode_ReturnsTrueAndUnknownOrderType()
    {
        var result = OrderType.TryParse("zzz", out var orderType);

        result.ShouldBeTrue();
        orderType.IsKnown.ShouldBeFalse();
    }

    [Fact]
    public void Equals_SameCodeDifferentCase_AreEqual() => OrderType.Parse("cct").Equals(OrderType.Parse("CCT")).ShouldBeTrue();

    [Fact]
    public void Equals_DifferentCode_AreNotEqual() => OrderType.C52.Equals(OrderType.C53).ShouldBeFalse();

    [Fact]
    public void EqualityOperator_SameCode_ReturnsTrue() => (OrderType.Parse("c53") == OrderType.C53).ShouldBeTrue();

    [Fact]
    public void InequalityOperator_DifferentCode_ReturnsTrue() => (OrderType.C52 != OrderType.C53).ShouldBeTrue();

    [Fact]
    public void GetHashCode_SameCodeDifferentCase_AreEqual() => OrderType.Parse("cct").GetHashCode().ShouldBe(OrderType.Parse("CCT").GetHashCode());

    [Fact]
    public void ToString_ReturnsCode() => OrderType.CCT.ToString().ShouldBe("CCT");

    [Fact]
    public void Default_HasEmptyCodeAndUnknownDirection()
    {
        var orderType = default(OrderType);

        orderType.Code.ShouldBe("");
        orderType.Direction.ShouldBe(OrderDirection.Unknown);
        orderType.IsKnown.ShouldBeFalse();
        orderType.MessageType.ShouldBeNull();
        orderType.Description.ShouldBeNull();
        orderType.SchemaVersions.ShouldBeEmpty();
    }
}
