namespace SoloForge.Console.Models;

/// <summary>
/// Represents a configurable "Quick Set" - a collection of table rolls
/// that generate a complete entity (NPC, location, object, etc.).
/// </summary>
public record QuickSet
{
    /// <summary>
    /// Unique identifier for this quick set.
    /// </summary>
    public string Id { get; init; } = "";

    /// <summary>
    /// Display name shown in the UI.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Description of what this quick set generates.
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// The steps (table rolls) that make up this quick set.
    /// </summary>
    public List<QuickSetStep> Steps { get; init; } = [];
}

/// <summary>
/// Represents a single step/roll in a quick set.
/// </summary>
public record QuickSetStep
{
    /// <summary>
    /// Label for this attribute (e.g., "Identity", "Appearance").
    /// </summary>
    public string Label { get; init; } = "";

    /// <summary>
    /// The table name or ID to roll on. Supports fuzzy matching.
    /// </summary>
    public string Table { get; init; } = "";

    /// <summary>
    /// Number of words to generate (default 2 for a word pair).
    /// </summary>
    public int Count { get; init; } = 2;
}

/// <summary>
/// Represents the result of generating a quick set.
/// </summary>
public record QuickSetResult
{
    /// <summary>
    /// The quick set that was generated.
    /// </summary>
    public QuickSet QuickSet { get; init; } = null!;

    /// <summary>
    /// The generated results for each step.
    /// </summary>
    public List<QuickSetStepResult> Results { get; init; } = [];

    /// <summary>
    /// Gets the result as plain text with newlines for history storage.
    /// </summary>
    public string ToDisplayDetails()
    {
        return string.Join("\n", Results.Select(r => $"{r.Label}: {r.Combined}"));
    }

    /// <summary>
    /// Gets the result as a plain text summary (single line).
    /// </summary>
    public string ToPlainSummary()
    {
        return string.Join(", ", Results.Select(r => $"{r.Label}: {r.Combined}"));
    }
}

/// <summary>
/// Represents the result of a single quick set step.
/// </summary>
public record QuickSetStepResult
{
    /// <summary>
    /// The label from the step configuration.
    /// </summary>
    public string Label { get; init; } = "";

    /// <summary>
    /// The generated words.
    /// </summary>
    public List<string> Words { get; init; } = [];

    /// <summary>
    /// The combined result string.
    /// </summary>
    public string Combined => string.Join(" ", Words);

    /// <summary>
    /// The table ID that was actually used (after resolution).
    /// </summary>
    public string TableId { get; init; } = "";
}
