using System.Text.Json;
using SoloForge.Api.Models;
using SoloForge.Console.Core;
using SoloForge.Console.Models;
using SoloForge.Console.Services;

namespace SoloForge.Api.Endpoints;

internal static class SessionEndpoints
{
    internal static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/state", (
            Session session,
            CampaignService campaignService,
            AdventureStateManager stateManager,
            HistoryService historyService) =>
        {
            return Results.Json(EndpointHelpers.BuildStateResponse(session, campaignService, stateManager, historyService));
        });

        app.MapPut("/session", async (
            HttpRequest request,
            CancellationToken ct,
            Session session,
            CampaignService campaignService,
            AdventureStateManager stateManager,
            HistoryService historyService) =>
        {
            var body = await EndpointHelpers.ReadBodyAsync<UpdateSessionRequest>(request, ct);
            if (body == null)
            {
                return Results.Json(new { error = "invalid json" }, statusCode: StatusCodes.Status400BadRequest);
            }

            if (body.Chaos.HasValue)
            {
                session.Chaos = body.Chaos.Value;
            }

            if (!string.IsNullOrWhiteSpace(body.Engine))
            {
                session.Engine = body.Engine.Trim();
            }

            if (!string.IsNullOrWhiteSpace(body.Theme))
            {
                session.Theme = body.Theme.Trim();
            }

            campaignService.Save();
            return Results.Json(EndpointHelpers.BuildStateResponse(session, campaignService, stateManager, historyService));
        });

        app.MapGet("/themes", () =>
        {
            var themesPath = FindThemesJsonPath();
            if (themesPath == null || !File.Exists(themesPath))
            {
                return Results.Json(new { error = "themes.json not found" }, statusCode: StatusCodes.Status404NotFound);
            }

            ThemeCollection? collection = null;
            try
            {
                var json = File.ReadAllText(themesPath);
                collection = JsonSerializer.Deserialize<ThemeCollection>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                // Ignore.
            }

            var themes = collection?.Themes
                .Select(t => new { name = t.Name, description = t.Description })
                .OrderBy(t => t.name)
                .ToList() ?? [];

            return Results.Json(themes);
        });

        return app;
    }

    private static string? FindThemesJsonPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "data", "themes.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        var relative = Path.Combine(Directory.GetCurrentDirectory(), "data", "themes.json");
        return File.Exists(relative) ? relative : null;
    }
}
