using SoloForge.Console.Models;

namespace SoloForge.Console.Tests;

public class DiceExpressionTests
{
    [Theory]
    [InlineData("2d6+1", 1, 1, 2, 6)]
    [InlineData("d20", 0, 1, 1, 20)]
    [InlineData("1d8-2", -2, 1, 1, 8)]
    public void TryParse_WithValidExpression_ParsesTermsAndModifier(
        string input,
        int expectedModifier,
        int expectedTermCount,
        int expectedDiceCount,
        int expectedFaces)
    {
        var ok = DiceExpression.TryParse(input, out var expression, out var error);

        ok.Should().BeTrue(error);
        expression.Should().NotBeNull();
        expression!.Modifier.Should().Be(expectedModifier);
        expression.Terms.Should().HaveCount(expectedTermCount);

        var first = expression.Terms[0];
        first.Count.Should().Be(expectedDiceCount);
        first.Faces.Should().Be(expectedFaces);
    }

    [Theory]
    [InlineData("")]
    [InlineData("+")]
    [InlineData("2d")]
    [InlineData("2d%")]
    [InlineData("0d6")]
    [InlineData("2x6")]
    public void TryParse_WithInvalidExpression_ReturnsFalse(string input)
    {
        var ok = DiceExpression.TryParse(input, out var expression, out var error);

        ok.Should().BeFalse();
        expression.Should().BeNull();
        error.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("2d6+1", "2d6+1")]
    [InlineData("d20", "d20")]
    [InlineData("1d8-2", "d8-2")]
    public void ToDisplayString_ProducesStableDisplayFormat(string input, string expected)
    {
        DiceExpression.TryParse(input, out var expression, out _).Should().BeTrue();

        expression!.ToDisplayString().Should().Be(expected);
    }
}
