namespace SoloForge.Console.Engines.Mythic2e;

/// <summary>
/// Represents the nine odds levels in Mythic 2e Fate Check system.
/// </summary>
public enum Odds
{
    Impossible = 0,
    NearlyImpossible = 1,
    VeryUnlikely = 2,
    Unlikely = 3,
    FiftyFifty = 4,
    Likely = 5,
    VeryLikely = 6,
    NearlyCertain = 7,
    Certain = 8
}

/// <summary>
/// Extension methods for Odds enum to provide display names.
/// </summary>
public static class OddsExtensions
{
    public static string GetDisplayName(this Odds odds) => odds switch
    {
        Odds.Impossible => "Impossible",
        Odds.NearlyImpossible => "Nearly Impossible",
        Odds.VeryUnlikely => "Very Unlikely",
        Odds.Unlikely => "Unlikely",
        Odds.FiftyFifty => "50/50",
        Odds.Likely => "Likely",
        Odds.VeryLikely => "Very Likely",
        Odds.NearlyCertain => "Nearly Certain",
        Odds.Certain => "Certain",
        _ => "Unknown"
    };
}

