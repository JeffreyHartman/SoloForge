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

        // Legacy journal endpoints now redirect to the session log note in the vault
        app.MapGet("/journal", (CampaignService campaignService, NotesService notesService) =>
        {
            var current = campaignService.CurrentCampaign;
            if (current == null)
                return Results.Json(new { error = "no campaign loaded" }, statusCode: StatusCodes.Status404NotFound);

            var content = notesService.ReadNote(current.Id, current.SessionLogPath)
                ?? JournalDefaults.CreateDefault(current.Name);

            return Results.Json(new { campaignId = current.Id, content });
        });

        app.MapPut("/journal", async (
            HttpRequest request,
            CancellationToken ct,
            CampaignService campaignService,
            NotesService notesService) =>
        {
            var current = campaignService.CurrentCampaign;
            if (current == null)
                return Results.Json(new { error = "no campaign loaded" }, statusCode: StatusCodes.Status404NotFound);

            var body = await EndpointHelpers.ReadBodyAsync<JournalUpdateRequest>(request, ct);
            var content = body?.Content ?? string.Empty;

            var saved = notesService.WriteNote(current.Id, current.SessionLogPath, content);
            return Results.Json(new { saved }, statusCode: saved ? StatusCodes.Status200OK : StatusCodes.Status500InternalServerError);
        });

        return app;
    }
}
