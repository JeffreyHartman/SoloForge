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

    internal static void EnsureVaultExists(CampaignService campaignService, NotesService notesService, JournalService journalService)
    {
        var current = campaignService.CurrentCampaign;
        if (current == null) return;

        // Migrate legacy single-file journal to vault if needed
        var legacyPath = campaignService.GetJournalPath(current.Id);
        notesService.MigrateIfNeeded(current.Id, legacyPath, current.SessionLogPath);

        // Ensure vault directory exists
        notesService.EnsureVault(current.Id);

        // Ensure session log note exists
        var sessionLogContent = notesService.ReadNote(current.Id, current.SessionLogPath);
        if (sessionLogContent == null)
        {
            var defaultContent = JournalDefaults.CreateDefault(current.Name);
            notesService.WriteNote(current.Id, current.SessionLogPath, defaultContent);
        }
    }

    internal static void AppendEntryToJournal(LogEntry entry, CampaignService campaignService, JournalService journalService, NotesService notesService)
    {
        var current = campaignService.CurrentCampaign;
        if (current == null) return;

        var sessionLogPath = current.SessionLogPath;
        var currentText = notesService.ReadNote(current.Id, sessionLogPath)
            ?? JournalDefaults.CreateDefault(current.Name);

        var markdown = journalService.ToMarkdown(entry);
        var updated = JournalTextComposer.AppendMarkdown(currentText, markdown);
        notesService.WriteNote(current.Id, sessionLogPath, updated);
    }
}
