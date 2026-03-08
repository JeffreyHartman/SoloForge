namespace SoloForge.Console.Models;

/// <summary>
/// Represents a file or folder in a campaign's notes vault.
/// </summary>
public record NoteNode
{
    /// <summary>
    /// Display name (filename without extension, or folder name).
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Relative path within the vault (forward slashes, e.g. "Locations/Fort Skonz.md").
    /// </summary>
    public string Path { get; init; } = "";

    /// <summary>
    /// True if this node is a folder, false if it's a note file.
    /// </summary>
    public bool IsFolder { get; init; }

    /// <summary>
    /// Child nodes (only populated for folders).
    /// </summary>
    public List<NoteNode> Children { get; init; } = [];
}
