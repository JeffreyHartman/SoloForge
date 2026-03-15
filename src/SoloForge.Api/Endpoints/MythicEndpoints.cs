using SoloForge.Api.Models;
using SoloForge.Core.Core;
using SoloForge.Core.Engines.Mythic2e;
using SoloForge.Core.Models;
using SoloForge.Core.Services;

namespace SoloForge.Api.Endpoints;

internal static class MythicEndpoints
{
    internal static IEndpointRouteBuilder MapMythicEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/tables", () =>
        {
            var tables = TableService.Instance.AvailableTables
                .Select(t => new
                {
                    id = t.Id,
                    displayName = t.DisplayName,
                    isElement = t.IsElement,
                    category = t.Category
                })
                .ToList();

            return Results.Json(tables);
        });

        app.MapGet("/quick-sets", () => Results.Json(QuickSetService.Instance.QuickSets));

        app.MapPost("/quick-sets/generate", async (
            HttpRequest request,
            CancellationToken ct,
            CampaignService campaignService,
            HistoryService historyService,
            JournalService journalService,
            NotesService notesService) =>
        {
            var body = await EndpointHelpers.ReadBodyAsync<GenerateQuickSetRequest>(request, ct);
            var id = body?.Id?.Trim();
            if (string.IsNullOrWhiteSpace(id))
            {
                return Results.Json(new { error = "id is required" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var quickSet = QuickSetService.Instance.QuickSets.FirstOrDefault(q =>
                q.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

            if (quickSet == null)
            {
                return Results.Json(new { error = "quick set not found" }, statusCode: StatusCodes.Status404NotFound);
            }

            var result = QuickSetService.Instance.Generate(quickSet);

            var entry = historyService.AddEntry(
                LogType.Meaning,
                $"{quickSet.Name} Generated",
                string.IsNullOrWhiteSpace(body?.Context) ? null : body!.Context!.Trim(),
                result.ToDisplayDetails()
            );
            EndpointHelpers.AppendEntryToJournal(entry, campaignService, journalService, notesService);
            campaignService.Save();

            return Results.Json(result);
        });

        app.MapPost("/fate-check", async (
            HttpRequest request,
            CancellationToken ct,
            Session session,
            CampaignService campaignService,
            HistoryService historyService,
            JournalService journalService,
            NotesService notesService) =>
        {
            var body = await EndpointHelpers.ReadBodyAsync<FateCheckRequest>(request, ct);
            if (body == null)
            {
                return Results.Json(new { error = "invalid json" }, statusCode: StatusCodes.Status400BadRequest);
            }

            if (!TryParseOdds(body.Odds, out var odds))
            {
                return Results.Json(new { error = "invalid odds" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var chaos = body.Chaos ?? session.Chaos;
            var question = string.IsNullOrWhiteSpace(body.Question) ? null : body.Question.Trim();
            var result = FateCheck.PerformCheck(chaos, odds);

            var fateEntry = historyService.AddEntry(
                LogType.FateCheck,
                result.Result,
                question,
                $"Odds: {odds.GetDisplayName()}, Roll: {result.Roll}, Chaos: {chaos}"
            );
            EndpointHelpers.AppendEntryToJournal(fateEntry, campaignService, journalService, notesService);

            RandomEventResult? randomEvent = null;
            if (result.RandomEventTriggered)
            {
                randomEvent = RandomEvent.Generate();
                var eventEntry = historyService.AddEntry(
                    LogType.RandomEvent,
                    $"{randomEvent.EventFocus}: {randomEvent.EventAction}",
                    "Triggered by Fate Check"
                );
                EndpointHelpers.AppendEntryToJournal(eventEntry, campaignService, journalService, notesService);
            }

            campaignService.Save();

            return Results.Json(new
            {
                chaos,
                odds = odds.GetDisplayName(),
                fate = result,
                randomEvent
            });
        });

        app.MapPost("/scene-check", async (
            HttpRequest request,
            CancellationToken ct,
            Session session,
            CampaignService campaignService,
            HistoryService historyService,
            JournalService journalService,
            NotesService notesService) =>
        {
            var body = await EndpointHelpers.ReadBodyAsync<SceneCheckRequest>(request, ct) ?? new SceneCheckRequest();
            var chaos = body.Chaos ?? session.Chaos;
            var contextText = string.IsNullOrWhiteSpace(body.Context) ? null : body.Context.Trim();

            var result = SceneCheck.PerformCheck(chaos);

            var details = $"Roll: {result.Roll}, Chaos: {chaos}";
            if (result.SceneAdjustment != null)
            {
                details += $", Adjustment: {result.SceneAdjustment}";
            }

            var sceneEntry = historyService.AddEntry(LogType.SceneCheck, result.Result, contextText, details);
            EndpointHelpers.AppendEntryToJournal(sceneEntry, campaignService, journalService, notesService);

            if (result.RandomEvent != null)
            {
                var ev = result.RandomEvent;
                var eventEntry = historyService.AddEntry(
                    LogType.RandomEvent,
                    $"{ev.EventFocus}: {ev.EventAction}",
                    "Triggered by Scene Interrupt"
                );
                EndpointHelpers.AppendEntryToJournal(eventEntry, campaignService, journalService, notesService);
            }

            campaignService.Save();

            return Results.Json(new { chaos, scene = result });
        });

        app.MapPost("/random-event", (
            CampaignService campaignService,
            HistoryService historyService,
            JournalService journalService,
            NotesService notesService) =>
        {
            var ev = RandomEvent.Generate();

            var eventDetails = ev.SelectedCharacter != null
                ? $"Character: {ev.SelectedCharacter}"
                : ev.SelectedThread != null
                    ? $"Thread: {ev.SelectedThread}"
                    : null;

            var entry = historyService.AddEntry(LogType.RandomEvent, $"{ev.EventFocus}: {ev.EventAction}", null, eventDetails);
            EndpointHelpers.AppendEntryToJournal(entry, campaignService, journalService, notesService);
            campaignService.Save();

            return Results.Json(ev);
        });

        app.MapPost("/dice-roll", async (
            HttpRequest request,
            CancellationToken ct,
            CampaignService campaignService,
            HistoryService historyService,
            JournalService journalService,
            NotesService notesService) =>
        {
            var body = await EndpointHelpers.ReadBodyAsync<DiceRollRequest>(request, ct);
            var input = body?.Expression;

            if (string.IsNullOrWhiteSpace(input))
            {
                return Results.Json(new { error = "expression is required" }, statusCode: StatusCodes.Status400BadRequest);
            }

            if (!DiceExpression.TryParse(input, out var expression, out var error))
            {
                return Results.Json(new { error = error ?? "invalid dice expression" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var result = DiceRoller.Instance.Roll(expression!);
            var breakdown = result.BuildBreakdown();
            var entry = historyService.AddEntry(LogType.DiceRoll, result.Total.ToString(), expression!.ToDisplayString(), breakdown);
            EndpointHelpers.AppendEntryToJournal(entry, campaignService, journalService, notesService);
            campaignService.Save();

            return Results.Json(new { roll = result, breakdown });
        });

        app.MapPost("/meaning/action", async (
            HttpRequest request,
            CancellationToken ct,
            CampaignService campaignService,
            HistoryService historyService,
            JournalService journalService,
            NotesService notesService) =>
        {
            var body = await EndpointHelpers.ReadBodyAsync<MeaningRequest>(request, ct);
            var contextText = string.IsNullOrWhiteSpace(body?.Context) ? null : body!.Context!.Trim();

            var result = MeaningEngine.GenerateAction();
            var entry = historyService.AddEntry(LogType.Meaning, result.Combined, contextText, "Table: Action");
            EndpointHelpers.AppendEntryToJournal(entry, campaignService, journalService, notesService);
            campaignService.Save();

            return Results.Json(result);
        });

        app.MapPost("/meaning/description", async (
            HttpRequest request,
            CancellationToken ct,
            CampaignService campaignService,
            HistoryService historyService,
            JournalService journalService,
            NotesService notesService) =>
        {
            var body = await EndpointHelpers.ReadBodyAsync<MeaningRequest>(request, ct);
            var contextText = string.IsNullOrWhiteSpace(body?.Context) ? null : body!.Context!.Trim();

            var result = MeaningEngine.GenerateDescription();
            var entry = historyService.AddEntry(LogType.Meaning, result.Combined, contextText, "Table: Description");
            EndpointHelpers.AppendEntryToJournal(entry, campaignService, journalService, notesService);
            campaignService.Save();

            return Results.Json(result);
        });

        app.MapPost("/meaning/table", async (
            HttpRequest request,
            CancellationToken ct,
            CampaignService campaignService,
            HistoryService historyService,
            JournalService journalService,
            NotesService notesService) =>
        {
            var body = await EndpointHelpers.ReadBodyAsync<MeaningTableRequest>(request, ct);
            var tableId = body?.TableId?.Trim();
            if (string.IsNullOrWhiteSpace(tableId))
            {
                return Results.Json(new { error = "tableId is required" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var contextText = string.IsNullOrWhiteSpace(body?.Context) ? null : body!.Context!.Trim();
            var table = TableService.Instance.FindTable(tableId);
            var displayName = table?.DisplayName ?? tableId;

            var result = MeaningEngine.GenerateFromTable(tableId, displayName);
            var entry = historyService.AddEntry(LogType.Meaning, result.Combined, contextText, $"Table: {displayName}");
            EndpointHelpers.AppendEntryToJournal(entry, campaignService, journalService, notesService);
            campaignService.Save();

            return Results.Json(new
            {
                table = new { id = tableId, displayName },
                meaning = result
            });
        });

        app.MapPost("/meaning/fusion", async (
            HttpRequest request,
            CancellationToken ct,
            CampaignService campaignService,
            HistoryService historyService,
            JournalService journalService,
            NotesService notesService) =>
        {
            var body = await EndpointHelpers.ReadBodyAsync<MeaningFusionRequest>(request, ct);
            var tableId1 = body?.TableId1?.Trim();
            var tableId2 = body?.TableId2?.Trim();

            if (string.IsNullOrWhiteSpace(tableId1) || string.IsNullOrWhiteSpace(tableId2))
            {
                return Results.Json(new { error = "tableId1 and tableId2 are required" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var contextText = string.IsNullOrWhiteSpace(body?.Context) ? null : body!.Context!.Trim();
            var table1 = TableService.Instance.FindTable(tableId1);
            var table2 = TableService.Instance.FindTable(tableId2);
            var name1 = table1?.DisplayName ?? tableId1;
            var name2 = table2?.DisplayName ?? tableId2;

            var result = MeaningEngine.GenerateFusion(tableId1, tableId2);
            var entry = historyService.AddEntry(LogType.Meaning, result.Combined, contextText, $"Fusion: {name1} + {name2}");
            EndpointHelpers.AppendEntryToJournal(entry, campaignService, journalService, notesService);
            campaignService.Save();

            return Results.Json(new
            {
                table1 = new { id = tableId1, displayName = name1 },
                table2 = new { id = tableId2, displayName = name2 },
                meaning = result
            });
        });

        return app;
    }

    private static bool TryParseOdds(string? input, out Odds odds)
    {
        odds = Odds.FiftyFifty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var trimmed = input.Trim();

        if (int.TryParse(trimmed, out var number) && number >= 0 && number <= 8)
        {
            odds = (Odds)number;
            return true;
        }

        if (Enum.TryParse(trimmed, ignoreCase: true, out Odds parsed))
        {
            odds = parsed;
            return true;
        }

        foreach (var candidate in Enum.GetValues<Odds>())
        {
            if (string.Equals(candidate.GetDisplayName(), trimmed, StringComparison.OrdinalIgnoreCase))
            {
                odds = candidate;
                return true;
            }
        }

        return false;
    }
}
