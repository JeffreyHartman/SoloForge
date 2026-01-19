using SoloForge.Console.Models;
using SoloForge.Console.Services;

namespace SoloForge.Console.Tests;

public class JournalServiceTests
{
    [Fact]
    public void LoadOrCreate_WhenFileMissing_ReturnsDefault()
    {
        var tempDir = Directory.CreateTempSubdirectory("soloforge-journal-tests");
        try
        {
            string PathResolver(Guid id) => Path.Combine(tempDir.FullName, $"{id}.md");

            var renderer = new Mock<ITemplateRenderer>();
            renderer.Setup(r => r.ToMarkdown(It.IsAny<LogEntry>())).Returns("MD");

            var sut = new JournalService(PathResolver, renderer.Object);

            var text = sut.LoadOrCreate(Guid.NewGuid(), "Camp");

            text.Should().StartWith("# Camp");
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Save_ThenLoadOrCreate_RoundTripsContent()
    {
        var tempDir = Directory.CreateTempSubdirectory("soloforge-journal-tests");
        try
        {
            string PathResolver(Guid id) => Path.Combine(tempDir.FullName, $"{id}.md");

            var renderer = new Mock<ITemplateRenderer>();
            renderer.Setup(r => r.ToMarkdown(It.IsAny<LogEntry>())).Returns("MD");

            var sut = new JournalService(PathResolver, renderer.Object);

            var id = Guid.NewGuid();
            sut.Save(id, "Hello").Should().BeTrue();

            var loaded = sut.LoadOrCreate(id, "Ignored");
            loaded.Should().Be("Hello");
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void AppendEntryToText_UsesTemplateRenderer()
    {
        var renderer = new Mock<ITemplateRenderer>();
        renderer.Setup(r => r.ToMarkdown(It.IsAny<LogEntry>())).Returns("Rendered");

        var sut = new JournalService(_ => "unused", renderer.Object);

        var entry = new LogEntry { Type = LogType.Note, Result = "Hi" };
        var updated = sut.AppendEntryToText("Existing", entry);

        updated.Should().Contain("Rendered");
        renderer.Verify(r => r.ToMarkdown(entry), Times.Once);
    }
}
