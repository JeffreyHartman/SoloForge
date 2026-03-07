using SoloForge.Api.Models;
using SoloForge.Console.Services;

namespace SoloForge.Api.Endpoints;

internal static class JournalEndpoints
{
    internal static IEndpointRouteBuilder MapJournalEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/history", (HistoryService historyService) =>
        {
            return Results.Json(historyService.Entries);
        });

        app.MapGet("/journal", (CampaignService campaignService, JournalService journalService) =>
        {
            var current = campaignService.CurrentCampaign;
            if (current == null)
            {
                return Results.Json(new { error = "no campaign loaded" }, statusCode: StatusCodes.Status404NotFound);
            }

            var content = journalService.LoadOrCreate(current.Id, current.Name);
            return Results.Json(new { campaignId = current.Id, content });
        });

        app.MapPut("/journal", async (
            HttpRequest request,
            CancellationToken ct,
            CampaignService campaignService,
            JournalService journalService) =>
        {
            var current = campaignService.CurrentCampaign;
            if (current == null)
            {
                return Results.Json(new { error = "no campaign loaded" }, statusCode: StatusCodes.Status404NotFound);
            }

            var body = await EndpointHelpers.ReadBodyAsync<JournalUpdateRequest>(request, ct);
            var content = body?.Content ?? string.Empty;

            var saved = journalService.Save(current.Id, content);
            return Results.Json(new { saved }, statusCode: saved ? StatusCodes.Status200OK : StatusCodes.Status500InternalServerError);
        });

        return app;
    }
}
