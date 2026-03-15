using System.Text.Json.Serialization;

namespace SoloForge.Core.Models;

/// <summary>
/// Represents the current adventure state including characters and plot threads.
/// Designed to be easily serializable to JSON for future save/load functionality.
/// </summary>
public class AdventureState
{
    /// <summary>
    /// Active characters (NPCs and important entities) in the adventure.
    /// </summary>
    public List<Character> Characters { get; init; } = [];

    /// <summary>
    /// Active plot threads, goals, and mysteries.
    /// </summary>
    public List<PlotThread> ActiveThreads { get; init; } = [];

    /// <summary>
    /// Closed/resolved plot threads for reference.
    /// </summary>
    public List<PlotThread> ClosedThreads { get; init; } = [];

    /// <summary>
    /// Gets a random active character, or null if none exist.
    /// </summary>
    public Character? GetRandomCharacter() =>
        Characters.Count > 0
            ? Characters[Random.Shared.Next(Characters.Count)]
            : null;

    /// <summary>
    /// Gets a random active thread, or null if none exist.
    /// </summary>
    public PlotThread? GetRandomThread() =>
        ActiveThreads.Count > 0
            ? ActiveThreads[Random.Shared.Next(ActiveThreads.Count)]
            : null;
}

/// <summary>
/// Represents a character or NPC in the adventure.
/// </summary>
public class Character
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    [JsonIgnore]
    public string DisplayName => string.IsNullOrEmpty(Description)
        ? Name
        : $"{Name} - {Description}";

    public override string ToString() => Name;
}

/// <summary>
/// Represents a plot thread, goal, or mystery in the adventure.
/// </summary>
public class PlotThread
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public DateTime? ClosedAt { get; set; }

    [JsonIgnore]
    public bool IsClosed => ClosedAt.HasValue;

    [JsonIgnore]
    public string DisplayName => string.IsNullOrEmpty(Description)
        ? Name
        : $"{Name} - {Description}";

    public override string ToString() => Name;
}
