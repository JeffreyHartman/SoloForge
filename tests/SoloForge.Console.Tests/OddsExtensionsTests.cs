using SoloForge.Console.Engines.Mythic2e;

namespace SoloForge.Console.Tests;

public class OddsExtensionsTests
{
    [Theory]
    [InlineData(Odds.Impossible, "Impossible")]
    [InlineData(Odds.NearlyImpossible, "Nearly Impossible")]
    [InlineData(Odds.VeryUnlikely, "Very Unlikely")]
    [InlineData(Odds.Unlikely, "Unlikely")]
    [InlineData(Odds.FiftyFifty, "50/50")]
    [InlineData(Odds.Likely, "Likely")]
    [InlineData(Odds.VeryLikely, "Very Likely")]
    [InlineData(Odds.NearlyCertain, "Nearly Certain")]
    [InlineData(Odds.Certain, "Certain")]
    public void GetDisplayName_ReturnsExpectedLabel(Odds odds, string expected)
    {
        odds.GetDisplayName().Should().Be(expected);
    }
}
