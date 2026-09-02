using Commerzbank.NET.CorporatePayments.Iso20022;
using Shouldly;
using Xunit;

namespace Commerzbank.NET.Tests.Iso20022.Common;

public class SepaCharacterSetTests
{
    [Theory]
    [InlineData("ABC abc 123 /-?:().,'+")]
    [InlineData("")]
    public void IsValid_AllowedCharacters_ReturnsTrue(string value)
    {
        SepaCharacterSet.IsValid(value).ShouldBeTrue();
    }

    [Theory]
    [InlineData("M\u00FCller")]
    [InlineData("café")]
    [InlineData("50% off")]
    [InlineData("a@b.com")]
    public void IsValid_DisallowedCharacter_ReturnsFalse(string value)
    {
        SepaCharacterSet.IsValid(value).ShouldBeFalse();
    }

    [Theory]
    [InlineData("\u00E4", "ae")]
    [InlineData("\u00F6", "oe")]
    [InlineData("\u00FC", "ue")]
    [InlineData("\u00C4", "Ae")]
    [InlineData("\u00D6", "Oe")]
    [InlineData("\u00DC", "Ue")]
    [InlineData("\u00DF", "ss")]
    public void Sanitize_GermanCharacters_Expands(string input, string expected)
    {
        SepaCharacterSet.Sanitize(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData("café", "cafe")]
    [InlineData("M\u00FCller", "Mueller")]
    [InlineData("Stra\u00DFe", "Strasse")]
    public void Sanitize_MixedText_ProducesSepaCompliantResult(string input, string expected)
    {
        var result = SepaCharacterSet.Sanitize(input);
        result.ShouldBe(expected);
        SepaCharacterSet.IsValid(result).ShouldBeTrue();
    }

    [Fact]
    public void Sanitize_DisallowedSymbol_BecomesSpace()
    {
        SepaCharacterSet.Sanitize("50% off").ShouldBe("50 off");
    }

    [Fact]
    public void Sanitize_MultipleSpaces_CollapsesToOne()
    {
        SepaCharacterSet.Sanitize("a    b").ShouldBe("a b");
    }

    [Fact]
    public void Sanitize_LeadingAndTrailingWhitespace_Trims()
    {
        SepaCharacterSet.Sanitize("  hello  ").ShouldBe("hello");
    }

    [Fact]
    public void Sanitize_AlreadyValidText_Unchanged()
    {
        SepaCharacterSet.Sanitize("Example Debtor GmbH").ShouldBe("Example Debtor GmbH");
    }

    [Fact]
    public void Sanitize_ResultIsAlwaysValid()
    {
        var result = SepaCharacterSet.Sanitize("M\u00FCller & Co. <Test> #123 100%");
        SepaCharacterSet.IsValid(result).ShouldBeTrue();
    }
}
