using SoloForge.Core.Engines.Mythic2e;
using SoloForge.Core.Models;
using SoloForge.Core.Services;

namespace SoloForge.Core.Tests;

public class RandomEventTests
{
    [Theory]
    [InlineData(1, "Remote Event")]
    [InlineData(6, "Ambiguous Event")]
    [InlineData(11, "New NPC")]
    [InlineData(21, "NPC Action")]
    [InlineData(86, "Current Context")]
    public void GenerateEventFocus_WithDeterministicRoll_MapsToExpectedFocus(int roll, string expected)
    {
        var rng = new Mock<IRng>();
        rng.Setup(r => r.Next(1, 101)).Returns(roll);

        RandomEvent.GenerateEventFocus(rng.Object).Should().Be(expected);
    }

    [Fact]
    public void Generate_WhenNpcFocusAndNoCharacters_SetsListWasEmpty()
    {
        var state = new AdventureState();

        var rng = new Mock<IRng>();
        rng.Setup(r => r.Next(1, 101)).Returns(21); // NPC Action

        var wordSource = new Mock<ITableWordSource>();
        wordSource.Setup(w => w.GetFusionPair("action1", "action2")).Returns("Do Thing");

        var result = RandomEvent.Generate(state, rng.Object, wordSource.Object);

        result.EventFocus.Should().Be("NPC Action");
        result.ListWasEmpty.Should().BeTrue();
        result.SelectedCharacter.Should().BeNull();
    }

    [Fact]
    public void Generate_WhenThreadFocusAndThreadsExist_SelectsDeterministicThread()
    {
        var state = new AdventureState
        {
            ActiveThreads =
            [
                new PlotThread { Name = "Thread A" },
                new PlotThread { Name = "Thread B" }
            ]
        };

        var rng = new Mock<IRng>();
        rng.SetupSequence(r => r.Next(1, 101))
            .Returns(51); // Move Toward a Thread

        rng.Setup(r => r.Next(0, 2)).Returns(1); // select index

        var wordSource = new Mock<ITableWordSource>();
        wordSource.Setup(w => w.GetFusionPair("action1", "action2")).Returns("Do Thing");

        var result = RandomEvent.Generate(state, rng.Object, wordSource.Object);

        result.EventFocus.Should().Be("Move Toward a Thread");
        result.SelectedThread.Should().Be("Thread B");
        result.ListWasEmpty.Should().BeFalse();
    }
}
