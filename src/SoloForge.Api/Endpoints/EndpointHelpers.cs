using SoloForge.Console.Core;
using SoloForge.Console.Models;
using SoloForge.Console.Services;

namespace SoloForge.Api.Endpoints;

internal static class EndpointHelpers
{
    internal static async Task<T?> ReadBodyAsync<T>(HttpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await request.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        }
        catch
        {
            return default;
        }
    }

    internal static object BuildStateResponse(
        Session session,
        CampaignService campaignService,
        AdventureStateManager stateManager,
        HistoryService historyService)
    {
        var current = campaignService.CurrentCampaign;

        return new
        {
            session = new
            {
                chaos = session.Chaos,
                engine = session.Engine,
                theme = session.Theme,
                lastQuickRoll = session.LastQuickRoll
            },
            currentCampaign = current == null
                ? null
                : new
                {
                    id = current.Id,
                    name = current.Name,
                    createdAt = current.CreatedAt,
                    lastPlayed = current.LastPlayed,
                    historyCount = current.History.Count
                },
            adventure = new
            {
                characters = stateManager.State.Characters,
                activeThreads = stateManager.State.ActiveThreads,
                closedThreads = stateManager.State.ClosedThreads
            },
            historyCount = historyService.Count
        };
    }

    internal static void EnsureJournalExists(CampaignService campaignService, JournalService journalService)
    {
        var current = campaignService.CurrentCampaign;
        if (current == null) return;

        var content = journalService.LoadOrCreate(current.Id, current.Name);
        journalService.Save(current.Id, content);
    }

    internal static void AppendEntryToJournal(LogEntry entry, CampaignService campaignService, JournalService journalService)
    {
        var current = campaignService.CurrentCampaign;
        if (current == null) return;

        var currentText = journalService.LoadOrCreate(current.Id, current.Name);
        var updated = journalService.AppendEntryToText(currentText, entry);
        journalService.Save(current.Id, updated);
    }
}
