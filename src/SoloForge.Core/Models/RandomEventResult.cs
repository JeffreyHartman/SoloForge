namespace SoloForge.Core.Models;

/// <summary>
/// Represents the result of a random event generation.
/// </summary>
public record RandomEventResult(
    string EventFocus,
    string EventAction,
    string? SelectedCharacter = null,
    string? SelectedThread = null,
    bool IsNewNpc = false,
    bool ListWasEmpty = false
);
