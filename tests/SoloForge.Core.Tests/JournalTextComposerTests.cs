using SoloForge.Core.Services;

namespace SoloForge.Core.Tests;

public class JournalTextComposerTests
{
    [Fact]
    public void AppendMarkdown_WhenCurrentIsEmpty_JustAppendsMarkdownAndNewline()
    {
        var result = JournalTextComposer.AppendMarkdown("", "Hello");
        result.Should().Be("Hello\n");
    }

    [Fact]
    public void AppendMarkdown_WhenCurrentHasText_AddsBlankLineBetweenEntries()
    {
        var current = "# Title\n\nExisting";

        var result = JournalTextComposer.AppendMarkdown(current, "Next");

        result.Should().Be("# Title\n\nExisting\n\nNext\n");
    }

    [Fact]
    public void AppendMarkdown_WhenCurrentAlreadyEndsWithBlankLine_DoesNotAddExtraBlankLine()
    {
        var current = "Existing\n\n";

        var result = JournalTextComposer.AppendMarkdown(current, "Next");

        result.Should().Be("Existing\n\nNext\n");
    }
}
