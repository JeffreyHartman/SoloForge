using SoloForge.Console.Engines.Mythic2e;
using SoloForge.Console.Services;

namespace SoloForge.Console.Tests;

public class MeaningEngineTests
{
    [Fact]
    public void GenerateAction_UsesExpectedTables()
    {
        var wordSource = new Mock<ITableWordSource>();
        wordSource.Setup(w => w.GetRandomWord("action1")).Returns("Explore");
        wordSource.Setup(w => w.GetRandomWord("action2")).Returns("Ruins");

        var result = MeaningEngine.GenerateAction(wordSource.Object);

        result.TableName.Should().Be("Action");
        result.Combined.Should().Be("Explore Ruins");
        result.IsFusion.Should().BeFalse();
    }

    [Fact]
    public void GenerateFusion_UsesTableDisplayNamesWhenAvailable()
    {
        var wordSource = new Mock<ITableWordSource>();
        wordSource.Setup(w => w.FindTable("t1")).Returns(new TableInfo("t1", "Table One", "", false, ""));
        wordSource.Setup(w => w.FindTable("t2")).Returns(new TableInfo("t2", "Table Two", "", false, ""));
        wordSource.Setup(w => w.GetRandomWord("t1")).Returns("Alpha");
        wordSource.Setup(w => w.GetRandomWord("t2")).Returns("Beta");

        var result = MeaningEngine.GenerateFusion(wordSource.Object, "t1", "t2");

        result.TableName.Should().Be("Table One + Table Two");
        result.Combined.Should().Be("Alpha Beta");
        result.IsFusion.Should().BeTrue();
    }
}
