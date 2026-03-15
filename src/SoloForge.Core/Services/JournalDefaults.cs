namespace SoloForge.Core.Services;

public static class JournalDefaults
{
    public static string CreateDefault(string campaignName)
    {
        if (string.IsNullOrWhiteSpace(campaignName))
        {
            campaignName = "Campaign";
        }

        return $"# {campaignName}\n\nJournal entries will appear here.\n\n";
    }
}
