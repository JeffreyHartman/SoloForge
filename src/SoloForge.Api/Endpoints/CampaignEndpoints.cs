using SoloForge.Api.Models;
using SoloForge.Core.Core;
using SoloForge.Core.Services;

namespace SoloForge.Api.Endpoints;

internal static class CampaignEndpoints
{
    internal static IEndpointRouteBuilder MapCampaignEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/campaigns", (CampaignService campaignService) =>
        {
            var campaigns = campaignService.ListCampaigns()
                .Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    createdAt = c.CreatedAt,
                    lastPlayed = c.LastPlayed,
                    chaos = c.Chaos,
                    engine = c.Engine,
                    theme = c.Theme,
                    characterCount = c.Characters.Count,
                    activeThreadCount = c.ActiveThreads.Count,
                    closedThreadCount = c.ClosedThreads.Count,
                    historyCount = c.History.Count
                })
                .OrderByDescending(c => c.lastPlayed)
                .ToList();

            return Results.Json(campaigns);
        });

        app.MapPost("/campaigns", async (
            HttpRequest request,
            CancellationToken ct,
            Session session,
            CampaignService campaignService,
            AdventureStateManager stateManager,
            HistoryService historyService,
            JournalService journalService,
            NotesService notesService) =>
        {
            var body = await EndpointHelpers.ReadBodyAsync<CreateCampaignRequest>(request, ct);
            var name = body?.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return Results.Json(new { error = "name is required" }, statusCode: StatusCodes.Status400BadRequest);
            }

            campaignService.CreateNew(name);
            EndpointHelpers.EnsureVaultExists(campaignService, notesService, journalService);

            return Results.Json(
                EndpointHelpers.BuildStateResponse(session, campaignService, stateManager, historyService),
                statusCode: StatusCodes.Status201Created);
        });

        app.MapPost("/campaigns/{campaignIdText}/load", (
            string campaignIdText,
            Session session,
            CampaignService campaignService,
            AdventureStateManager stateManager,
            HistoryService historyService,
            JournalService journalService,
            NotesService notesService) =>
        {
            if (!Guid.TryParse(campaignIdText, out var campaignId))
            {
                return Results.Json(new { error = "invalid campaign id" }, statusCode: StatusCodes.Status400BadRequest);
            }

            try
            {
                campaignService.Load(campaignId);
            }
            catch (FileNotFoundException)
            {
                return Results.Json(new { error = "campaign not found" }, statusCode: StatusCodes.Status404NotFound);
            }

            EndpointHelpers.EnsureVaultExists(campaignService, notesService, journalService);
            return Results.Json(EndpointHelpers.BuildStateResponse(session, campaignService, stateManager, historyService));
        });

        app.MapPut("/campaigns/journal-prefs", async (
            HttpRequest request,
            CancellationToken ct,
            CampaignService campaignService) =>
        {
            var current = campaignService.CurrentCampaign;
            if (current == null)
            {
                return Results.Json(new { error = "no campaign loaded" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var body = await EndpointHelpers.ReadBodyAsync<UpdateJournalPrefsRequest>(request, ct);
            if (body == null)
            {
                return Results.Json(new { error = "invalid request body" }, statusCode: StatusCodes.Status400BadRequest);
            }

            if (body.AutoJournalEvents.HasValue)
                current.AutoJournalEvents = body.AutoJournalEvents.Value;
            if (body.AutoJournalDiceRolls.HasValue)
                current.AutoJournalDiceRolls = body.AutoJournalDiceRolls.Value;

            campaignService.Save();

            return Results.Json(new
            {
                autoJournalEvents = current.AutoJournalEvents,
                autoJournalDiceRolls = current.AutoJournalDiceRolls
            });
        });

        app.MapDelete("/campaigns/{deleteCampaignIdText}", (
            string deleteCampaignIdText,
            CampaignService campaignService,
            JournalService journalService,
            NotesService notesService) =>
        {
            if (!Guid.TryParse(deleteCampaignIdText, out var deleteCampaignId))
            {
                return Results.Json(new { error = "invalid campaign id" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var wasCurrent = campaignService.CurrentCampaign?.Id == deleteCampaignId;
            var deleted = campaignService.Delete(deleteCampaignId);

            if (deleted && wasCurrent)
            {
                var remaining = campaignService.ListCampaigns().FirstOrDefault(c => c.Id != deleteCampaignId);
                if (remaining != null)
                {
                    campaignService.Load(remaining.Id);
                }
                else
                {
                    campaignService.CreateNew("Default Campaign");
                }
                EndpointHelpers.EnsureVaultExists(campaignService, notesService, journalService);
            }

            return Results.Json(new { deleted }, statusCode: deleted ? StatusCodes.Status200OK : StatusCodes.Status404NotFound);
        });

        return app;
    }
}
