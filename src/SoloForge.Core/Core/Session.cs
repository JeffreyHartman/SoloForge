namespace SoloForge.Console.Core;

/// <summary>
/// Session state container for chaos factor, engine, and theme.
/// </summary>
public class Session
{
    public string Engine { get; set; } = "Mythic 2e";
    public string Theme { get; set; } = "Fantasy";
    public string? LastQuickRoll { get; set; }

    public int Chaos
    {
        get;
        set => field = Math.Clamp(value, 1, 9);
    } = 5;
}
