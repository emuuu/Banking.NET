using System.Xml.Linq;
using Commerzbank.NET.CorporatePayments.Iso20022;
using Commerzbank.NET.CorporatePayments.Iso20022.Internal;
using Shouldly;
using Xunit;

namespace Commerzbank.NET.Tests.Iso20022.Common;

public class XmlNavigationTests
{
    private static XElement Parse(string xml) => XElement.Parse(xml);

    [Fact]
    public void Child_ElementExists_ReturnsFirstMatch()
    {
        var element = Parse("<a><b>1</b><b>2</b></a>");
        element.Child("b")!.Value.ShouldBe("1");
    }

    [Fact]
    public void Child_ElementMissing_ReturnsNull()
    {
        var element = Parse("<a><b>1</b></a>");
        element.Child("c").ShouldBeNull();
    }

    [Fact]
    public void Child_ReceiverNull_ReturnsNull()
    {
        XElement? element = null;
        element.Child("b").ShouldBeNull();
    }

    [Fact]
    public void Child_NamespacedDocument_MatchesByLocalNameIgnoringNamespace()
    {
        var element = Parse("<a xmlns='urn:test:ns'><b>value</b></a>");
        element.Child("b")!.Value.ShouldBe("value");
    }

    [Fact]
    public void Children_MultipleMatches_ReturnsAllInOrder()
    {
        var element = Parse("<a><b>1</b><c/><b>2</b></a>");
        element.Children("b").Select(e => e.Value).ShouldBe(["1", "2"]);
    }

    [Fact]
    public void Children_ReceiverNull_ReturnsEmpty()
    {
        XElement? element = null;
        element.Children("b").ShouldBeEmpty();
    }

    [Fact]
    public void Value_TrimsWhitespace()
    {
        var element = Parse("<a>  hello  </a>");
        element.Value().ShouldBe("hello");
    }

    [Fact]
    public void Value_EmptyElement_ReturnsNull()
    {
        var element = Parse("<a></a>");
        element.Value().ShouldBeNull();
    }

    [Fact]
    public void Value_ReceiverNull_ReturnsNull()
    {
        XElement? element = null;
        element.Value().ShouldBeNull();
    }

    [Fact]
    public void Decimal_ValidValue_ParsesInvariantCulture()
    {
        var element = Parse("<a>1234.56</a>");
        element.Decimal().ShouldBe(1234.56m);
    }

    [Fact]
    public void Decimal_ReceiverNull_ReturnsNull()
    {
        XElement? element = null;
        element.Decimal().ShouldBeNull();
    }

    [Fact]
    public void Int_ValidValue_Parses()
    {
        Parse("<a>42</a>").Int().ShouldBe(42);
    }

    [Fact]
    public void Long_ValidValue_Parses()
    {
        Parse("<a>9000000000</a>").Long().ShouldBe(9000000000L);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("1")]
    public void Bool_TrueValues_ReturnsTrue(string value)
    {
        Parse($"<a>{value}</a>").Bool().ShouldBe(true);
    }

    [Theory]
    [InlineData("false")]
    [InlineData("0")]
    public void Bool_FalseValues_ReturnsFalse(string value)
    {
        Parse($"<a>{value}</a>").Bool().ShouldBe(false);
    }

    [Fact]
    public void Bool_InvalidValue_ThrowsFormatException()
    {
        var element = Parse("<a>maybe</a>");
        Should.Throw<FormatException>(() => element.Bool());
    }

    [Fact]
    public void Bool_ReceiverNull_ReturnsNull()
    {
        XElement? element = null;
        element.Bool().ShouldBeNull();
    }

    [Fact]
    public void Date_DateOnlyValue_Parses()
    {
        Parse("<a>2026-09-02</a>").Date().ShouldBe(new DateOnly(2026, 9, 2));
    }

    [Fact]
    public void Date_DateTimeValue_TruncatesToDatePart()
    {
        Parse("<a>2026-09-02T10:00:00</a>").Date().ShouldBe(new DateOnly(2026, 9, 2));
    }

    [Fact]
    public void Date_ReceiverNull_ReturnsNull()
    {
        XElement? element = null;
        element.Date().ShouldBeNull();
    }

    [Fact]
    public void DateTime_WithOffset_KeepsOffset()
    {
        var result = Parse("<a>2026-09-02T10:00:00+02:00</a>").DateTime();
        result.ShouldBe(new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.FromHours(2)));
        result!.Value.Offset.ShouldBe(TimeSpan.FromHours(2));
    }

    [Fact]
    public void DateTime_WithZSuffix_ReadsAsUtc()
    {
        var result = Parse("<a>2026-09-02T10:00:00Z</a>").DateTime();
        result!.Value.Offset.ShouldBe(TimeSpan.Zero);
        result.ShouldBe(new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void DateTime_WithoutOffset_UsesOffsetZeroWithoutConversion()
    {
        var result = Parse("<a>2026-09-02T10:00:00</a>").DateTime();
        result!.Value.Offset.ShouldBe(TimeSpan.Zero);
        result.Value.Hour.ShouldBe(10);
    }

    [Fact]
    public void DateTime_ReceiverNull_ReturnsNull()
    {
        XElement? element = null;
        element.DateTime().ShouldBeNull();
    }

    [Fact]
    public void Money_WithCurrency_ReadsAmountAndCurrency()
    {
        var element = Parse("<a Ccy=\"EUR\">42.50</a>");
        element.Money().ShouldBe(new Money(42.50m, "EUR"));
    }

    [Fact]
    public void Money_WithoutCcyAttribute_ReturnsEmptyCurrency()
    {
        var element = Parse("<a>42.50</a>");
        element.Money().ShouldBe(new Money(42.50m, string.Empty));
    }

    [Fact]
    public void Money_ReceiverNull_ReturnsNull()
    {
        XElement? element = null;
        element.Money().ShouldBeNull();
    }

    [Fact]
    public void Path_AllStepsPresent_ReturnsDeepestElement()
    {
        var element = Parse("<a><b><c>value</c></b></a>");
        element.Path("b", "c")!.Value.ShouldBe("value");
    }

    [Fact]
    public void Path_StepMissing_ReturnsNull()
    {
        var element = Parse("<a><b/></a>");
        element.Path("b", "c").ShouldBeNull();
    }
}
