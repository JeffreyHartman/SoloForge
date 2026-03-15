using SoloForge.Core.Engines.Mythic2e;
using SoloForge.Core.Services;

namespace SoloForge.Core.Tests;

public class SceneCheckTests
{
    [Fact]
    public void PerformCheck_WhenRollAboveChaos_IsNormalScene()
    {
        var rng = new Mock<IRng>();
        rng.Setup(r => r.Next(1, 11)).Returns(9);

        var result = SceneCheck.PerformCheck(chaosFactor: 5, rng: rng.Object, randomEventGenerator: null);

        result.Result.Should().Be("Normal Scene");
        result.RandomEvent.Should().BeNull();
        result.SceneAdjustment.Should().BeNull();
    }

    [Fact]
    public void PerformCheck_WhenRollIsEvenAndUnderChaos_IsInterruptSceneWithRandomEvent()
    {
        var rng = new Mock<IRng>();
        rng.Setup(r => r.Next(1, 11)).Returns(4);

        var eventResult = new SoloForge.Core.Models.RandomEventResult("Focus", "Action");
        var result = SceneCheck.PerformCheck(chaosFactor: 5, rng: rng.Object, randomEventGenerator: () => eventResult);

        result.Result.Should().Be("Interrupt Scene!");
        result.RandomEvent.Should().Be(eventResult);
        result.SceneAdjustment.Should().BeNull();
    }

    [Fact]
    public void PerformCheck_WhenRollIsOddAndUnderChaos_IsAlteredSceneWithAdjustment()
    {
        var rng = new Mock<IRng>();
        rng.SetupSequence(r => r.Next(1, 11))
            .Returns(3) // perform check roll
            .Returns(2); // adjustment roll

        var result = SceneCheck.PerformCheck(chaosFactor: 5, rng: rng.Object, randomEventGenerator: null);

        result.Result.Should().Be("Altered Scene!");
        result.SceneAdjustment.Should().NotBeNullOrWhiteSpace();
        result.RandomEvent.Should().BeNull();
    }
}
