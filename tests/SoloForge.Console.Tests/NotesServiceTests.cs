using SoloForge.Console.Services;

namespace SoloForge.Console.Tests;

public class NotesServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly NotesService _service;
    private readonly Guid _campaignId = Guid.NewGuid();

    public NotesServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"soloforge-notes-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _service = new NotesService(id => Path.Combine(_tempDir, id.ToString()));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void WriteNote_CreatesFileAndReturnsTrue()
    {
        _service.EnsureVault(_campaignId);

        var result = _service.WriteNote(_campaignId, "test.md", "Hello world");

        result.Should().BeTrue();
        _service.ReadNote(_campaignId, "test.md").Should().Be("Hello world");
    }

    [Fact]
    public void ReadNote_NonexistentFile_ReturnsNull()
    {
        _service.EnsureVault(_campaignId);

        _service.ReadNote(_campaignId, "nope.md").Should().BeNull();
    }

    [Fact]
    public void CreateNote_NewFile_ReturnsTrue()
    {
        _service.EnsureVault(_campaignId);

        _service.CreateNote(_campaignId, "notes.md", "content").Should().BeTrue();
        _service.ReadNote(_campaignId, "notes.md").Should().Be("content");
    }

    [Fact]
    public void CreateNote_ExistingFile_ReturnsFalse()
    {
        _service.EnsureVault(_campaignId);
        _service.CreateNote(_campaignId, "dup.md", "first");

        _service.CreateNote(_campaignId, "dup.md", "second").Should().BeFalse();
        _service.ReadNote(_campaignId, "dup.md").Should().Be("first");
    }

    [Fact]
    public void WriteNote_CreatesNestedDirectories()
    {
        _service.EnsureVault(_campaignId);

        _service.WriteNote(_campaignId, "deep/nested/note.md", "nested content").Should().BeTrue();
        _service.ReadNote(_campaignId, "deep/nested/note.md").Should().Be("nested content");
    }

    [Fact]
    public void DeleteNote_ExistingFile_ReturnsTrue()
    {
        _service.EnsureVault(_campaignId);
        _service.WriteNote(_campaignId, "delete-me.md", "bye");

        _service.DeleteNote(_campaignId, "delete-me.md").Should().BeTrue();
        _service.ReadNote(_campaignId, "delete-me.md").Should().BeNull();
    }

    [Fact]
    public void DeleteNote_NonexistentFile_ReturnsFalse()
    {
        _service.EnsureVault(_campaignId);

        _service.DeleteNote(_campaignId, "ghost.md").Should().BeFalse();
    }

    [Fact]
    public void CreateFolder_And_DeleteFolder()
    {
        _service.EnsureVault(_campaignId);

        _service.CreateFolder(_campaignId, "subfolder").Should().BeTrue();
        _service.CreateFolder(_campaignId, "subfolder").Should().BeFalse(); // already exists

        _service.DeleteFolder(_campaignId, "subfolder").Should().BeTrue();
        _service.DeleteFolder(_campaignId, "subfolder").Should().BeFalse(); // already deleted
    }

    [Fact]
    public void Move_RenamesFile()
    {
        _service.EnsureVault(_campaignId);
        _service.WriteNote(_campaignId, "old.md", "content");

        _service.Move(_campaignId, "old.md", "new.md").Should().BeTrue();
        _service.ReadNote(_campaignId, "old.md").Should().BeNull();
        _service.ReadNote(_campaignId, "new.md").Should().Be("content");
    }

    [Fact]
    public void Move_ToExistingTarget_ReturnsFalse()
    {
        _service.EnsureVault(_campaignId);
        _service.WriteNote(_campaignId, "a.md", "alpha");
        _service.WriteNote(_campaignId, "b.md", "beta");

        _service.Move(_campaignId, "a.md", "b.md").Should().BeFalse();
    }

    [Fact]
    public void ListTree_ReturnsCorrectStructure()
    {
        _service.EnsureVault(_campaignId);
        _service.WriteNote(_campaignId, "Session Log.md", "log");
        _service.CreateFolder(_campaignId, "Locations");
        _service.WriteNote(_campaignId, "Locations/Town.md", "a town");

        var tree = _service.ListTree(_campaignId);

        tree.Should().HaveCount(2);
        var folder = tree.First(n => n.IsFolder);
        folder.Name.Should().Be("Locations");
        folder.Children.Should().HaveCount(1);
        folder.Children[0].Name.Should().Be("Town");

        var file = tree.First(n => !n.IsFolder);
        file.Name.Should().Be("Session Log");
        file.Path.Should().Be("Session Log.md");
    }

    [Fact]
    public void ListAllNotePaths_ReturnsFlatList()
    {
        _service.EnsureVault(_campaignId);
        _service.WriteNote(_campaignId, "A.md", "");
        _service.WriteNote(_campaignId, "folder/B.md", "");

        var paths = _service.ListAllNotePaths(_campaignId);

        paths.Should().BeEquivalentTo(["A.md", "folder/B.md"]);
    }

    [Fact]
    public void ResolveSafePath_RejectsTraversal()
    {
        _service.EnsureVault(_campaignId);

        var act = () => _service.ReadNote(_campaignId, "../../../etc/passwd");
        act.Should().Throw<InvalidOperationException>().WithMessage("*traversal*");
    }

    [Fact]
    public void MigrateIfNeeded_RejectsTraversalInSessionLogPath()
    {
        _service.EnsureVault(_campaignId);
        var legacyPath = Path.Combine(_tempDir, $"{_campaignId}.md");
        File.WriteAllText(legacyPath, "content");

        // Remove vault files so migration proceeds
        var vault = _service.GetVaultPath(_campaignId);
        foreach (var f in Directory.GetFiles(vault, "*.md", SearchOption.AllDirectories))
            File.Delete(f);

        // Path traversal in session log path should fail gracefully
        var result = _service.MigrateIfNeeded(_campaignId, legacyPath, "../../etc/evil.md");
        result.Should().BeFalse();
        // Legacy file should remain untouched
        File.Exists(legacyPath).Should().BeTrue();
    }

    [Fact]
    public void MigrateIfNeeded_MovesLegacyFile()
    {
        _service.EnsureVault(_campaignId);
        var legacyPath = Path.Combine(_tempDir, $"{_campaignId}.md");
        File.WriteAllText(legacyPath, "legacy journal content");

        // Remove vault files so migration proceeds
        var vault = _service.GetVaultPath(_campaignId);
        foreach (var f in Directory.GetFiles(vault, "*.md", SearchOption.AllDirectories))
            File.Delete(f);

        _service.MigrateIfNeeded(_campaignId, legacyPath, "Session Log.md").Should().BeTrue();
        _service.ReadNote(_campaignId, "Session Log.md").Should().Be("legacy journal content");
        File.Exists(legacyPath).Should().BeFalse();
    }

    [Fact]
    public void MigrateIfNeeded_SkipsWhenVaultHasFiles()
    {
        _service.EnsureVault(_campaignId);
        _service.WriteNote(_campaignId, "existing.md", "already here");

        var legacyPath = Path.Combine(_tempDir, $"{_campaignId}.md");
        File.WriteAllText(legacyPath, "legacy content");

        _service.MigrateIfNeeded(_campaignId, legacyPath, "Session Log.md").Should().BeFalse();
        File.Exists(legacyPath).Should().BeTrue(); // legacy file not touched
    }

    [Fact]
    public void WriteNote_ThenAppend_UpdatesSessionLog()
    {
        // Simulates the flow: set session log to a different note, then append roll results
        _service.EnsureVault(_campaignId);
        _service.WriteNote(_campaignId, "Session Log.md", "# Session Log\n");
        _service.WriteNote(_campaignId, "Adventure Notes.md", "# Adventure Notes\n");

        // Simulate changing session log to "Adventure Notes.md" and appending
        var sessionLogPath = "Adventure Notes.md";
        var currentText = _service.ReadNote(_campaignId, sessionLogPath);
        currentText.Should().NotBeNull();

        var updated = currentText + "\n## Roll Result\nFate Check: Yes\n";
        _service.WriteNote(_campaignId, sessionLogPath, updated);

        // Verify the append went to the correct file
        _service.ReadNote(_campaignId, "Adventure Notes.md").Should().Contain("Fate Check: Yes");
        _service.ReadNote(_campaignId, "Session Log.md").Should().NotContain("Fate Check");
    }
}
