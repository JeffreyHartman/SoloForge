using SoloForge.Console.Engines.Mythic2e;
using SoloForge.Console.Services;

namespace SoloForge.Console.Tests;

public class FateCheckTests
{
    [Theory]
    [InlineData(1, Odds.Impossible, 1, "Yes")]
    [InlineData(1, Odds.Impossible, 82, "Exceptional No")]
    [InlineData(9, Odds.Certain, 20, "Exceptional Yes")]
    [InlineData(9, Odds.Certain, 100, "No")]
    public void PerformCheck_WithDeterministicRoll_ProducesExpectedResult(
        int chaos,
        Odds odds,
        int roll,
        string expectedResult)
    {
        var rng = new Mock<IRng>();
        rng.Setup(r => r.Next(1, 101)).Returns(roll);

        var result = FateCheck.PerformCheck(chaos, odds, rng.Object);

        result.Roll.Should().Be(roll);
        result.Result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(5, 11, true)]
    [InlineData(5, 55, true)]
    [InlineData(5, 66, false)] // digit 6 > chaos 5
    [InlineData(9, 99, true)]
    [InlineData(1, 11, true)]
    [InlineData(5, 12, false)] // not doubles
    public void PerformCheck_RandomEventTriggerMatchesRules(int chaos, int roll, bool expected)
    {
        var rng = new Mock<IRng>();
        rng.Setup(r => r.Next(1, 101)).Returns(roll);

        var result = FateCheck.PerformCheck(chaos, Odds.FiftyFifty, rng.Object);

        result.RandomEventTriggered.Should().Be(expected);
    }
}
