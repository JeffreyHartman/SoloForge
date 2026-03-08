namespace SoloForge.Console.Models;

/// <summary>
/// Root DTO for campaign JSON serialization.
/// Contains all persistent state for a single campaign.
/// </summary>
public record CampaignData
{
    /// <summary>
    /// Unique identifier for the campaign.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// User-provided name for the campaign.
    /// </summary>
    public string Name { get; set; } = "New Campaign";

    /// <summary>
    /// When the campaign was first created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    /// <summary>
    /// When the campaign was last played/saved.
    /// </summary>
    public DateTime LastPlayed { get; set; } = DateTime.Now;

    // === Session State ===

    /// <summary>
    /// Current chaos factor (1-9).
    /// </summary>
    public int Chaos { get; set; } = 5;

    /// <summary>
    /// The game engine being used (e.g., "Mythic 2e").
    /// </summary>
    public string Engine { get; set; } = "Mythic 2e";

    /// <summary>
    /// The campaign theme (e.g., "Fantasy").
    /// </summary>
    public string Theme { get; set; } = "Fantasy";

    // === Adventure State ===

    /// <summary>
    /// Active characters/NPCs in the campaign.
    /// </summary>
    public List<Character> Characters { get; init; } = [];

    /// <summary>
    /// Active plot threads.
    /// </summary>
    public List<PlotThread> ActiveThreads { get; init; } = [];

    /// <summary>
    /// Closed/resolved plot threads.
    /// </summary>
    public List<PlotThread> ClosedThreads { get; init; } = [];

    // === Notes ===

    /// <summary>
    /// Relative path within the vault to the session log note (e.g. "Session Log.md").
    /// Roll results are automatically appended here.
    /// </summary>
    public string SessionLogPath { get; set; } = "Session Log.md";

    // === History ===

    /// <summary>
    /// Chronological journal of all events in the campaign.
    /// </summary>
    public List<LogEntry> History { get; init; } = [];
}
