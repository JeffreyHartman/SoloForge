using SoloForge.Api.Models;
using SoloForge.Console.Services;

namespace SoloForge.Api.Endpoints;

internal static class NotesEndpoints
{
    internal static IEndpointRouteBuilder MapNotesEndpoints(this IEndpointRouteBuilder app)
    {
        // List file tree
        app.MapGet("/notes/tree", (CampaignService campaignService, NotesService notesService) =>
        {
            var current = campaignService.CurrentCampaign;
            if (current == null)
                return Results.Json(new { error = "no campaign loaded" }, statusCode: StatusCodes.Status404NotFound);

            var tree = notesService.ListTree(current.Id);
            return Results.Json(new { campaignId = current.Id, sessionLogPath = current.SessionLogPath, tree });
        });

        // List all note paths (flat list for link autocomplete)
        app.MapGet("/notes/list", (CampaignService campaignService, NotesService notesService) =>
        {
            var current = campaignService.CurrentCampaign;
            if (current == null)
                return Results.Json(new { error = "no campaign loaded" }, statusCode: StatusCodes.Status404NotFound);

            var paths = notesService.ListAllNotePaths(current.Id);
            return Results.Json(new { campaignId = current.Id, paths });
        });

        // Read a note
        app.MapGet("/notes", (string? path, CampaignService campaignService, NotesService notesService) =>
        {
            var current = campaignService.CurrentCampaign;
            if (current == null)
                return Results.Json(new { error = "no campaign loaded" }, statusCode: StatusCodes.Status404NotFound);

            if (string.IsNullOrWhiteSpace(path))
                return Results.Json(new { error = "path is required" }, statusCode: StatusCodes.Status400BadRequest);

            var content = notesService.ReadNote(current.Id, path);
            if (content == null)
                return Results.Json(new { error = "note not found" }, statusCode: StatusCodes.Status404NotFound);

            return Results.Json(new { path, content });
        });

        // Create a note
        app.MapPost("/notes", async (HttpRequest request, CancellationToken ct,
            CampaignService campaignService, NotesService notesService) =>
        {
            var current = campaignService.CurrentCampaign;
            if (current == null)
                return Results.Json(new { error = "no campaign loaded" }, statusCode: StatusCodes.Status404NotFound);

            var body = await EndpointHelpers.ReadBodyAsync<NoteCreateRequest>(request, ct);
            if (string.IsNullOrWhiteSpace(body?.Path))
                return Results.Json(new { error = "path is required" }, statusCode: StatusCodes.Status400BadRequest);

            var created = notesService.CreateNote(current.Id, body.Path, body.Content);
            if (!created)
                return Results.Json(new { error = "note already exists or could not be created" }, statusCode: StatusCodes.Status409Conflict);

            return Results.Json(new { created = true, path = body.Path }, statusCode: StatusCodes.Status201Created);
        });

        // Update a note
        app.MapPut("/notes", async (HttpRequest request, CancellationToken ct,
            CampaignService campaignService, NotesService notesService) =>
        {
            var current = campaignService.CurrentCampaign;
            if (current == null)
                return Results.Json(new { error = "no campaign loaded" }, statusCode: StatusCodes.Status404NotFound);

            var body = await EndpointHelpers.ReadBodyAsync<NoteUpdateRequest>(request, ct);
            if (string.IsNullOrWhiteSpace(body?.Path))
                return Results.Json(new { error = "path is required" }, statusCode: StatusCodes.Status400BadRequest);

            var saved = notesService.WriteNote(current.Id, body.Path, body.Content ?? string.Empty);
            return Results.Json(new { saved }, statusCode: saved ? StatusCodes.Status200OK : StatusCodes.Status500InternalServerError);
        });

        // Delete a note
        app.MapDelete("/notes", (string? path, CampaignService campaignService, NotesService notesService) =>
        {
            var current = campaignService.CurrentCampaign;
            if (current == null)
                return Results.Json(new { error = "no campaign loaded" }, statusCode: StatusCodes.Status404NotFound);

            if (string.IsNullOrWhiteSpace(path))
                return Results.Json(new { error = "path is required" }, statusCode: StatusCodes.Status400BadRequest);

            var deleted = notesService.DeleteNote(current.Id, path);
            return Results.Json(new { deleted });
        });

        // Create a folder
        app.MapPost("/notes/folder", async (HttpRequest request, CancellationToken ct,
            CampaignService campaignService, NotesService notesService) =>
        {
            var current = campaignService.CurrentCampaign;
            if (current == null)
                return Results.Json(new { error = "no campaign loaded" }, statusCode: StatusCodes.Status404NotFound);

            var body = await EndpointHelpers.ReadBodyAsync<FolderCreateRequest>(request, ct);
            if (string.IsNullOrWhiteSpace(body?.Path))
                return Results.Json(new { error = "path is required" }, statusCode: StatusCodes.Status400BadRequest);

            var created = notesService.CreateFolder(current.Id, body.Path);
            if (!created)
                return Results.Json(new { error = "folder already exists or could not be created" }, statusCode: StatusCodes.Status409Conflict);

            return Results.Json(new { created = true, path = body.Path }, statusCode: StatusCodes.Status201Created);
        });

        // Delete a folder
        app.MapDelete("/notes/folder", (string? path, CampaignService campaignService, NotesService notesService) =>
        {
            var current = campaignService.CurrentCampaign;
            if (current == null)
                return Results.Json(new { error = "no campaign loaded" }, statusCode: StatusCodes.Status404NotFound);

            if (string.IsNullOrWhiteSpace(path))
                return Results.Json(new { error = "path is required" }, statusCode: StatusCodes.Status400BadRequest);

            var deleted = notesService.DeleteFolder(current.Id, path);
            return Results.Json(new { deleted });
        });

        // Move / rename a note or folder
        app.MapPost("/notes/move", async (HttpRequest request, CancellationToken ct,
            CampaignService campaignService, NotesService notesService) =>
        {
            var current = campaignService.CurrentCampaign;
            if (current == null)
                return Results.Json(new { error = "no campaign loaded" }, statusCode: StatusCodes.Status404NotFound);

            var body = await EndpointHelpers.ReadBodyAsync<NoteMoveRequest>(request, ct);
            if (string.IsNullOrWhiteSpace(body?.OldPath) || string.IsNullOrWhiteSpace(body?.NewPath))
                return Results.Json(new { error = "oldPath and newPath are required" }, statusCode: StatusCodes.Status400BadRequest);

            var moved = notesService.Move(current.Id, body.OldPath, body.NewPath);
            if (!moved)
                return Results.Json(new { error = "move failed (source not found or target exists)" }, statusCode: StatusCodes.Status409Conflict);

            // If the moved item was or contains the session log, update the path
            if (string.Equals(current.SessionLogPath, body.OldPath, StringComparison.OrdinalIgnoreCase))
            {
                current.SessionLogPath = body.NewPath;
                campaignService.Save();
            }
            else if (current.SessionLogPath.StartsWith(body.OldPath + "/", StringComparison.OrdinalIgnoreCase))
            {
                current.SessionLogPath = body.NewPath + current.SessionLogPath[body.OldPath.Length..];
                campaignService.Save();
            }

            return Results.Json(new { moved = true, oldPath = body.OldPath, newPath = body.NewPath });
        });

        // Set session log
        app.MapPut("/notes/session-log", async (HttpRequest request, CancellationToken ct,
            CampaignService campaignService) =>
        {
            var current = campaignService.CurrentCampaign;
            if (current == null)
                return Results.Json(new { error = "no campaign loaded" }, statusCode: StatusCodes.Status404NotFound);

            var body = await EndpointHelpers.ReadBodyAsync<SetSessionLogRequest>(request, ct);
            if (string.IsNullOrWhiteSpace(body?.Path))
                return Results.Json(new { error = "path is required" }, statusCode: StatusCodes.Status400BadRequest);

            current.SessionLogPath = body.Path;
            campaignService.Save();

            return Results.Json(new { sessionLogPath = current.SessionLogPath });
        });

        return app;
    }
}
