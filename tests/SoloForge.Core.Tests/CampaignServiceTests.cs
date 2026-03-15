using SoloForge.Core.Core;
using SoloForge.Core.Services;

namespace SoloForge.Core.Tests;

public class CampaignServiceTests
{
    [Fact]
    public void ListCampaigns_WhenJsonIdDoesNotMatchFilename_UsesFilenameId()
    {
        var tempDir = Directory.CreateTempSubdirectory("soloforge-campaign-tests");
        try
        {
            // Arrange: file name ID is the authoritative one.
            var fileId = Guid.NewGuid();
            var wrongId = Guid.NewGuid();

            var jsonPath = Path.Combine(tempDir.FullName, $"{fileId}.json");
            var json = $$"""
            {
              "id": "{{wrongId}}",
              "name": "Campaign",
              "createdAt": "2026-01-01T00:00:00",
              "lastPlayed": "2026-01-01T00:00:00",
              "chaos": 5,
              "engine": "Mythic 2e",
              "theme": "Fantasy",
              "characters": [],
              "activeThreads": [],
              "closedThreads": [],
              "history": []
            }
            """;

            File.WriteAllText(jsonPath, json);

            var session = new Session();
            var state = AdventureStateManager.Instance;
            var history = new HistoryService();

            var sut = new CampaignService(session, state, history, tempDir.FullName);

            // Act
            var campaigns = sut.ListCampaigns().ToList();

            // Assert
            campaigns.Should().ContainSingle();
            campaigns[0].Id.Should().Be(fileId);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }
}
