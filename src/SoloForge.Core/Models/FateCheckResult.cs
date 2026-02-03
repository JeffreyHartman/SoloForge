namespace SoloForge.Console.Models;

/// <summary>
/// Represents the result of a Mythic 2e Fate Check.
/// </summary>
public record FateCheckResult(
    int Roll,
    string Result,
    bool RandomEventTriggered
);
