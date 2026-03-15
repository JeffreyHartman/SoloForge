using SoloForge.Api.Models;
using SoloForge.Core.Services;

namespace SoloForge.Api.Endpoints;

internal static class AdventureEndpoints
{
    internal static IEndpointRouteBuilder MapAdventureEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/adventure", (AdventureStateManager stateManager) =>
        {
            return Results.Json(new
            {
                characters = stateManager.State.Characters,
                activeThreads = stateManager.State.ActiveThreads,
                closedThreads = stateManager.State.ClosedThreads
            });
        });

        app.MapPost("/adventure/characters", async (
            HttpRequest request,
            CancellationToken ct,
            AdventureStateManager stateManager,
            CampaignService campaignService) =>
        {
            var body = await EndpointHelpers.ReadBodyAsync<AddCharacterRequest>(request, ct);
            var name = body?.Name?.Trim();
            var desc = string.IsNullOrWhiteSpace(body?.Description) ? null : body!.Description!.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                return Results.Json(new { error = "name is required" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var character = stateManager.AddCharacter(name, desc);
            campaignService.Save();
            return Results.Json(character, statusCode: StatusCodes.Status201Created);
        });

        app.MapDelete("/adventure/characters", (
            string? name,
            AdventureStateManager stateManager,
            CampaignService campaignService) =>
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Results.Json(new { error = "name is required" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var character = stateManager.State.Characters.FirstOrDefault(c =>
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (character == null)
            {
                return Results.Json(new { error = "character not found" }, statusCode: StatusCodes.Status404NotFound);
            }

            var removed = stateManager.RemoveCharacter(character);
            if (removed)
            {
                campaignService.Save();
            }

            return Results.Json(new { removed });
        });

        app.MapPost("/adventure/threads", async (
            HttpRequest request,
            CancellationToken ct,
            AdventureStateManager stateManager,
            CampaignService campaignService) =>
        {
            var body = await EndpointHelpers.ReadBodyAsync<AddThreadRequest>(request, ct);
            var name = body?.Name?.Trim();
            var desc = string.IsNullOrWhiteSpace(body?.Description) ? null : body!.Description!.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                return Results.Json(new { error = "name is required" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var thread = stateManager.AddThread(name, desc);
            campaignService.Save();
            return Results.Json(thread, statusCode: StatusCodes.Status201Created);
        });

        app.MapPost("/adventure/threads/close", (
            string? name,
            AdventureStateManager stateManager,
            CampaignService campaignService) =>
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Results.Json(new { error = "name is required" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var thread = stateManager.State.ActiveThreads.FirstOrDefault(t =>
                t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (thread == null)
            {
                return Results.Json(new { error = "thread not found" }, statusCode: StatusCodes.Status404NotFound);
            }

            stateManager.CloseThread(thread);
            campaignService.Save();
            return Results.Json(thread);
        });

        app.MapPost("/adventure/threads/reopen", (
            string? name,
            AdventureStateManager stateManager,
            CampaignService campaignService) =>
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Results.Json(new { error = "name is required" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var thread = stateManager.State.ClosedThreads.FirstOrDefault(t =>
                t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (thread == null)
            {
                return Results.Json(new { error = "thread not found" }, statusCode: StatusCodes.Status404NotFound);
            }

            stateManager.ReopenThread(thread);
            campaignService.Save();
            return Results.Json(thread);
        });

        return app;
    }
}
