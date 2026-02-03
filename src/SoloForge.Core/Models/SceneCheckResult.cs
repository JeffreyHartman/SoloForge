namespace SoloForge.Console.Models;

/// <summary>
/// Represents the result of a scene check.
/// </summary>
public record SceneCheckResult(
    int Roll,
    string Result,
    string? SceneAdjustment = null,
    RandomEventResult? RandomEvent = null
);
