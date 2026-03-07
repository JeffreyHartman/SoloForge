using System.Text.Json;

using Microsoft.AspNetCore.Http.Json;

using SoloForge.Console.Core;
using SoloForge.Console.Engines.Mythic2e;
using SoloForge.Console.Models;
using SoloForge.Console.Services;

AppLogger.Initialize();

var builder = WebApplication.CreateBuilder(args);

ConfigureUrls(builder);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.WriteIndented = true;
});

builder.Services.AddSingleton<Session>();
builder.Services.AddSingleton(_ => AdventureStateManager.Instance);
builder.Services.AddSingleton<HistoryService>();
builder.Services.AddSingleton<CampaignService>();
builder.Services.AddSingleton<JournalService>(sp =>
{
    var campaignService = sp.GetRequiredService<CampaignService>();
    return new JournalService(campaignService.GetJournalPath, new TemplateServiceRenderer());
});

var app = builder.Build();

app.Lifetime.ApplicationStopping.Register(AppLogger.Shutdown);

app.UseCors();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        if (context.Response.HasStarted)
        {
            throw;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { error = "Unhandled server error", detail = ex.Message });
    }
});

app.MapMethods("/{**path}", ["OPTIONS"], () => Results.NoContent());

var campaignService = app.Services.GetRequiredService<CampaignService>();
var journalService = app.Services.GetRequiredService<JournalService>();
var historyService = app.Services.GetRequiredService<HistoryService>();
var session = app.Services.GetRequiredService<Session>();
var stateManager = app.Services.GetRequiredService<AdventureStateManager>();

campaignService.Initialize();

app.Lifetime.ApplicationStarted.Register(() =>
{
    var urls = app.Urls.Count > 0 ? string.Join(", ", app.Urls) : "http://localhost:5137";
    Console.WriteLine($"SoloForge API listening on {urls}");
    Console.WriteLine("Press Ctrl+C to stop.");
});

app.MapGet("/", () => Results.Json(new
{
    name = "SoloForge API",
    version = "0.1",
    endpoints = new[]
    {
        "/api/health",
        "/api/state",
        "/api/campaigns",
        "/api/campaigns/{id}/load",
        "/api/fate-check",
        "/api/scene-check",
        "/api/random-event",
        "/api/dice-roll",
        "/api/meaning/action",
        "/api/meaning/description",
        "/api/meaning/table",
        "/api/meaning/fusion",
        "/api/tables",
        "/api/quick-sets",
        "/api/quick-sets/generate",
        "/api/journal",
        "/api/history",
        "/api/adventure"
    }
}));

app.MapGet("/api/health", () => Results.Json(new { status = "ok" }));

app.MapGet("/api/state", () => Results.Json(BuildStateResponse(session, campaignService, stateManager, historyService)));

app.MapGet("/api/tables", () =>
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

app.MapGet("/api/quick-sets", () => Results.Json(QuickSetService.Instance.QuickSets));

app.MapPost("/api/quick-sets/generate", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    var body = await ReadBodyAsync<GenerateQuickSetRequest>(request, cancellationToken);
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
    AppendEntryToJournal(entry, campaignService, journalService);
    campaignService.Save();

    return Results.Json(result);
});

app.MapGet("/api/themes", () =>
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

app.MapGet("/api/campaigns", () =>
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

app.MapPost("/api/campaigns", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    var body = await ReadBodyAsync<CreateCampaignRequest>(request, cancellationToken);
    var name = body?.Name?.Trim();
    if (string.IsNullOrWhiteSpace(name))
    {
        return Results.Json(new { error = "name is required" }, statusCode: StatusCodes.Status400BadRequest);
    }

    campaignService.CreateNew(name);
    EnsureJournalExistsForCurrentCampaign(campaignService, journalService);

    return Results.Json(
        BuildStateResponse(session, campaignService, stateManager, historyService),
        statusCode: StatusCodes.Status201Created);
});

app.MapPost("/api/campaigns/{campaignIdText}/load", (string campaignIdText) =>
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

    EnsureJournalExistsForCurrentCampaign(campaignService, journalService);
    return Results.Json(BuildStateResponse(session, campaignService, stateManager, historyService));
});

app.MapDelete("/api/campaigns/{deleteCampaignIdText}", (string deleteCampaignIdText) =>
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
            EnsureJournalExistsForCurrentCampaign(campaignService, journalService);
        }
        else
        {
            campaignService.CreateNew("Default Campaign");
            EnsureJournalExistsForCurrentCampaign(campaignService, journalService);
        }
    }

    return Results.Json(new { deleted }, statusCode: deleted ? StatusCodes.Status200OK : StatusCodes.Status404NotFound);
});

app.MapPut("/api/session", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    var body = await ReadBodyAsync<UpdateSessionRequest>(request, cancellationToken);
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
    return Results.Json(BuildStateResponse(session, campaignService, stateManager, historyService));
});

app.MapGet("/api/history", () => Results.Json(historyService.Entries));

app.MapGet("/api/journal", () =>
{
    var current = campaignService.CurrentCampaign;
    if (current == null)
    {
        return Results.Json(new { error = "no campaign loaded" }, statusCode: StatusCodes.Status404NotFound);
    }

    var content = journalService.LoadOrCreate(current.Id, current.Name);
    return Results.Json(new { campaignId = current.Id, content });
});

app.MapPut("/api/journal", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    var current = campaignService.CurrentCampaign;
    if (current == null)
    {
        return Results.Json(new { error = "no campaign loaded" }, statusCode: StatusCodes.Status404NotFound);
    }

    var body = await ReadBodyAsync<JournalUpdateRequest>(request, cancellationToken);
    var content = body?.Content ?? string.Empty;

    var saved = journalService.Save(current.Id, content);
    return Results.Json(new { saved }, statusCode: saved ? StatusCodes.Status200OK : StatusCodes.Status500InternalServerError);
});

app.MapPost("/api/fate-check", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    var body = await ReadBodyAsync<FateCheckRequest>(request, cancellationToken);
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
    AppendEntryToJournal(fateEntry, campaignService, journalService);

    RandomEventResult? randomEvent = null;
    if (result.RandomEventTriggered)
    {
        randomEvent = RandomEvent.Generate();
        var eventEntry = historyService.AddEntry(
            LogType.RandomEvent,
            $"{randomEvent.EventFocus}: {randomEvent.EventAction}",
            "Triggered by Fate Check"
        );
        AppendEntryToJournal(eventEntry, campaignService, journalService);
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

app.MapPost("/api/scene-check", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    var body = await ReadBodyAsync<SceneCheckRequest>(request, cancellationToken) ?? new SceneCheckRequest();
    var chaos = body.Chaos ?? session.Chaos;
    var contextText = string.IsNullOrWhiteSpace(body.Context) ? null : body.Context.Trim();

    var result = SceneCheck.PerformCheck(chaos);

    var details = $"Roll: {result.Roll}, Chaos: {chaos}";
    if (result.SceneAdjustment != null)
    {
        details += $", Adjustment: {result.SceneAdjustment}";
    }

    var sceneEntry = historyService.AddEntry(LogType.SceneCheck, result.Result, contextText, details);
    AppendEntryToJournal(sceneEntry, campaignService, journalService);

    if (result.RandomEvent != null)
    {
        var ev = result.RandomEvent;
        var eventEntry = historyService.AddEntry(
            LogType.RandomEvent,
            $"{ev.EventFocus}: {ev.EventAction}",
            "Triggered by Scene Interrupt"
        );
        AppendEntryToJournal(eventEntry, campaignService, journalService);
    }

    campaignService.Save();

    return Results.Json(new { chaos, scene = result });
});

app.MapPost("/api/random-event", () =>
{
    var ev = RandomEvent.Generate();

    var eventDetails = ev.SelectedCharacter != null
        ? $"Character: {ev.SelectedCharacter}"
        : ev.SelectedThread != null
            ? $"Thread: {ev.SelectedThread}"
            : null;

    var entry = historyService.AddEntry(LogType.RandomEvent, $"{ev.EventFocus}: {ev.EventAction}", null, eventDetails);
    AppendEntryToJournal(entry, campaignService, journalService);
    campaignService.Save();

    return Results.Json(ev);
});

app.MapPost("/api/dice-roll", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    var body = await ReadBodyAsync<DiceRollRequest>(request, cancellationToken);
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
    AppendEntryToJournal(entry, campaignService, journalService);
    campaignService.Save();

    return Results.Json(new { roll = result, breakdown });
});

app.MapPost("/api/meaning/action", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    var body = await ReadBodyAsync<MeaningRequest>(request, cancellationToken);
    var contextText = string.IsNullOrWhiteSpace(body?.Context) ? null : body!.Context!.Trim();

    var result = MeaningEngine.GenerateAction();
    var entry = historyService.AddEntry(LogType.Meaning, result.Combined, contextText, "Table: Action");
    AppendEntryToJournal(entry, campaignService, journalService);
    campaignService.Save();

    return Results.Json(result);
});

app.MapPost("/api/meaning/description", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    var body = await ReadBodyAsync<MeaningRequest>(request, cancellationToken);
    var contextText = string.IsNullOrWhiteSpace(body?.Context) ? null : body!.Context!.Trim();

    var result = MeaningEngine.GenerateDescription();
    var entry = historyService.AddEntry(LogType.Meaning, result.Combined, contextText, "Table: Description");
    AppendEntryToJournal(entry, campaignService, journalService);
    campaignService.Save();

    return Results.Json(result);
});

app.MapPost("/api/meaning/table", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    var body = await ReadBodyAsync<MeaningTableRequest>(request, cancellationToken);
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
    AppendEntryToJournal(entry, campaignService, journalService);
    campaignService.Save();

    return Results.Json(new
    {
        table = new { id = tableId, displayName },
        meaning = result
    });
});

app.MapPost("/api/meaning/fusion", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    var body = await ReadBodyAsync<MeaningFusionRequest>(request, cancellationToken);
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
    AppendEntryToJournal(entry, campaignService, journalService);
    campaignService.Save();

    return Results.Json(new
    {
        table1 = new { id = tableId1, displayName = name1 },
        table2 = new { id = tableId2, displayName = name2 },
        meaning = result
    });
});

app.MapGet("/api/adventure", () => Results.Json(new
{
    characters = stateManager.State.Characters,
    activeThreads = stateManager.State.ActiveThreads,
    closedThreads = stateManager.State.ClosedThreads
}));

app.MapPost("/api/adventure/characters", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    var body = await ReadBodyAsync<AddCharacterRequest>(request, cancellationToken);
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

app.MapDelete("/api/adventure/characters", (string? name) =>
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

app.MapPost("/api/adventure/threads", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    var body = await ReadBodyAsync<AddThreadRequest>(request, cancellationToken);
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

app.MapPost("/api/adventure/threads/close", (string? name) =>
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

app.MapPost("/api/adventure/threads/reopen", (string? name) =>
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

app.MapFallback(() => Results.Json(new { error = "not found" }, statusCode: StatusCodes.Status404NotFound));

app.Run();

static void ConfigureUrls(WebApplicationBuilder builder)
{
    var soloForgeUrl = Environment.GetEnvironmentVariable("SOLOFORGE_API_URL");
    if (!string.IsNullOrWhiteSpace(soloForgeUrl))
    {
        builder.WebHost.UseUrls(NormalizeUrl(soloForgeUrl));
        return;
    }

    var aspNetUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
    if (string.IsNullOrWhiteSpace(aspNetUrls))
    {
        builder.WebHost.UseUrls("http://localhost:5137");
    }
}

static string NormalizeUrl(string url)
{
    var trimmed = url.Trim();
    return trimmed.EndsWith("/", StringComparison.Ordinal)
        ? trimmed.TrimEnd('/')
        : trimmed;
}

static object BuildStateResponse(
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

static void EnsureJournalExistsForCurrentCampaign(CampaignService campaignService, JournalService journalService)
{
    var current = campaignService.CurrentCampaign;
    if (current == null)
    {
        return;
    }

    var content = journalService.LoadOrCreate(current.Id, current.Name);
    journalService.Save(current.Id, content);
}

static void AppendEntryToJournal(LogEntry entry, CampaignService campaignService, JournalService journalService)
{
    var current = campaignService.CurrentCampaign;
    if (current == null)
    {
        return;
    }

    var currentText = journalService.LoadOrCreate(current.Id, current.Name);
    var updated = journalService.AppendEntryToText(currentText, entry);
    journalService.Save(current.Id, updated);
}

static string? FindThemesJsonPath()
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

static async Task<T?> ReadBodyAsync<T>(HttpRequest request, CancellationToken cancellationToken)
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

static bool TryParseOdds(string? input, out Odds odds)
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

public sealed record CreateCampaignRequest(string? Name);

public sealed record UpdateSessionRequest(int? Chaos, string? Engine, string? Theme);

public sealed record FateCheckRequest(string? Odds, string? Question, int? Chaos);

public sealed record SceneCheckRequest(string? Context = null, int? Chaos = null);

public sealed record DiceRollRequest(string? Expression);

public sealed record JournalUpdateRequest(string? Content);

public sealed record AddCharacterRequest(string? Name, string? Description);

public sealed record AddThreadRequest(string? Name, string? Description);

public sealed record MeaningRequest(string? Context);

public sealed record MeaningTableRequest(string? TableId, string? Context);

public sealed record MeaningFusionRequest(string? TableId1, string? TableId2, string? Context);

public sealed record GenerateQuickSetRequest(string? Id, string? Context);
