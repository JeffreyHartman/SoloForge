using SoloForge.Console.Services;

namespace SoloForge.Console.Tests;

public class JournalDefaultsTests
{
    [Fact]
    public void CreateDefault_UsesCampaignNameInHeader()
    {
        var result = JournalDefaults.CreateDefault("My Campaign");

        result.Should().StartWith("# My Campaign\n\n");
        result.Should().Contain("Journal entries will appear here.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CreateDefault_WithMissingCampaignName_UsesFallback(string? campaignName)
    {
        var result = JournalDefaults.CreateDefault(campaignName ?? "");

        result.Should().StartWith("# Campaign\n\n");
    }
}
