namespace SoloForge.Console.Models;

/// <summary>
/// Types of log entries that can be recorded in the journal.
/// </summary>
public enum LogType
{
    FateCheck,
    SceneCheck,
    RandomEvent,
    Meaning,
    Note
}

/// <summary>
/// A record of a single event in the campaign history/journal.
/// </summary>
public record LogEntry
{
    /// <summary>
    /// Unique identifier for the entry.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// When the event occurred.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;

    /// <summary>
    /// The type of event recorded.
    /// </summary>
    public LogType Type { get; init; }

    /// <summary>
    /// The user's optional question or context (e.g., "Does the guard see me?").
    /// </summary>
    public string? Context { get; init; }

    /// <summary>
    /// The outcome of the event (e.g., "Exceptional Yes", "Altered Scene").
    /// </summary>
    public string Result { get; init; } = "";

    /// <summary>
    /// Additional details about the event (e.g., "Rolled 11 vs Chaos 5").
    /// </summary>
    public string? Details { get; init; }
}
