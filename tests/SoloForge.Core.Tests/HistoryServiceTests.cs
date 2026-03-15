using SoloForge.Core.Models;
using SoloForge.Core.Services;

namespace SoloForge.Core.Tests;

public class HistoryServiceTests
{
    [Fact]
    public void AddEntry_AddsEntryToCollection()
    {
        var sut = new HistoryService();

        var entry = sut.AddEntry(LogType.Note, "Hello", context: "Ctx", details: "Details");

        sut.Entries.Should().ContainSingle();
        sut.Count.Should().Be(1);

        entry.Type.Should().Be(LogType.Note);
        entry.Result.Should().Be("Hello");
        entry.Context.Should().Be("Ctx");
        entry.Details.Should().Be("Details");
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var sut = new HistoryService();
        sut.AddEntry(LogType.Note, "A");
        sut.AddEntry(LogType.Note, "B");

        sut.Clear();

        sut.Count.Should().Be(0);
        sut.Entries.Should().BeEmpty();
    }

    [Fact]
    public void GetByType_FiltersEntries()
    {
        var sut = new HistoryService();
        sut.AddEntry(LogType.Note, "A");
        sut.AddEntry(LogType.Meaning, "B");
        sut.AddEntry(LogType.Note, "C");

        var notes = sut.GetByType(LogType.Note).ToList();

        notes.Should().HaveCount(2);
        notes.Select(n => n.Result).Should().BeEquivalentTo(["A", "C"]);
    }
}
