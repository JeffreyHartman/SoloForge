namespace SoloForge.Console.Models;

/// <summary>
/// Application-level settings that persist across campaigns.
/// </summary>
public record GlobalSettings
{
    /// <summary>
    /// The ID of the last played campaign, used to auto-load on startup.
    /// </summary>
    public Guid? LastPlayedCampaignId { get; set; }
}
