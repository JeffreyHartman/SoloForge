using System.Net;
using System.Text;
using System.Text.Json;

using SoloForge.Console.Core;
using SoloForge.Console.Engines.Mythic2e;
using SoloForge.Console.Models;
using SoloForge.Console.Services;

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true
};

AppLogger.Initialize();

var session = new Session();
var stateManager = AdventureStateManager.Instance;
var historyService = new HistoryService();
var campaignService = new CampaignService(session, stateManager, historyService);
var journalService = new JournalService(campaignService.GetJournalPath, new TemplateServiceRenderer());

campaignService.Initialize();

var urlPrefix = Environment.GetEnvironmentVariable("SOLOFORGE_API_URL") ?? "http://localhost:5137/";
if (!urlPrefix.EndsWith("/", StringComparison.Ordinal))
{
    urlPrefix += "/";
}

using var listener = new HttpListener();
listener.Prefixes.Add(urlPrefix);
listener.Start();

Console.WriteLine($"SoloForge API listening on {urlPrefix}");
Console.WriteLine("Press Ctrl+C to stop.");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

await RunAsync(listener, cts.Token);

async Task RunAsync(HttpListener httpListener, CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        HttpListenerContext context;
        try
        {
            context = await httpListener.GetContextAsync();
        }
        catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            await HandleRequestAsync(context);
        }
        catch (Exception ex)
        {
            try
            {
                await WriteJsonAsync(context.Response, 500, new { error = "Unhandled server error", detail = ex.Message });
            }
            catch
            {
                // Best effort.
            }
        }
        finally
        {
            try
            {
                context.Response.OutputStream.Close();
            }
            catch
            {
                // Ignore.
            }
        }
    }
}

async Task HandleRequestAsync(HttpListenerContext context)
{
    var req = context.Request;
    var res = context.Response;

    AddCorsHeaders(req, res);
    if (req.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
    {
        res.StatusCode = 204;
        return;
    }

    var path = (req.Url?.AbsolutePath ?? "/").TrimEnd('/');
    if (path.Length == 0)
    {
        path = "/";
    }

    var method = req.HttpMethod.ToUpperInvariant();
    var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

    // GET /
    if (method == "GET" && path == "/")
    {
        await WriteJsonAsync(res, 200, new
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
        });
        return;
    }

    // GET /api/health
    if (method == "GET" && path == "/api/health")
    {
        await WriteJsonAsync(res, 200, new { status = "ok" });
        return;
    }

    // GET /api/state
    if (method == "GET" && path == "/api/state")
    {
        await WriteJsonAsync(res, 200, BuildStateResponse());
        return;
    }

    // GET /api/tables
    if (segments is ["api", "tables"] && method == "GET")
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

        await WriteJsonAsync(res, 200, tables);
        return;
    }

    // GET /api/quick-sets
    if (segments is ["api", "quick-sets"] && method == "GET")
    {
        await WriteJsonAsync(res, 200, QuickSetService.Instance.QuickSets);
        return;
    }

    // POST /api/quick-sets/generate
    if (segments is ["api", "quick-sets", "generate"] && method == "POST")
    {
        var body = await ReadJsonAsync<GenerateQuickSetRequest>(req);
        var id = body?.Id?.Trim();
        if (string.IsNullOrWhiteSpace(id))
        {
            await WriteJsonAsync(res, 400, new { error = "id is required" });
            return;
        }

        var quickSet = QuickSetService.Instance.QuickSets.FirstOrDefault(q =>
            q.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        if (quickSet == null)
        {
            await WriteJsonAsync(res, 404, new { error = "quick set not found" });
            return;
        }

        var result = QuickSetService.Instance.Generate(quickSet);

        var entry = historyService.AddEntry(
            LogType.Meaning,
            $"{quickSet.Name} Generated",
            string.IsNullOrWhiteSpace(body?.Context) ? null : body!.Context!.Trim(),
            result.ToDisplayDetails()
        );
        AppendEntryToJournal(entry);
        campaignService.Save();

        await WriteJsonAsync(res, 200, result);
        return;
    }

    // GET /api/themes
    if (segments is ["api", "themes"] && method == "GET")
    {
        var themesPath = FindThemesJsonPath();
        if (themesPath == null || !File.Exists(themesPath))
        {
            await WriteJsonAsync(res, 404, new { error = "themes.json not found" });
            return;
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

        await WriteJsonAsync(res, 200, themes);
        return;
    }

    // Campaigns
    if (segments is ["api", "campaigns"])
    {
        if (method == "GET")
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

            await WriteJsonAsync(res, 200, campaigns);
            return;
        }

        if (method == "POST")
        {
            var body = await ReadJsonAsync<CreateCampaignRequest>(req);
            var name = body?.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await WriteJsonAsync(res, 400, new { error = "name is required" });
                return;
            }

            campaignService.CreateNew(name);
            EnsureJournalExistsForCurrentCampaign();

            await WriteJsonAsync(res, 201, BuildStateResponse());
            return;
        }
    }

    // POST /api/campaigns/{id}/load
    if (segments is ["api", "campaigns", var campaignIdText, "load"] && method == "POST")
    {
        if (!Guid.TryParse(campaignIdText, out var campaignId))
        {
            await WriteJsonAsync(res, 400, new { error = "invalid campaign id" });
            return;
        }

        try
        {
            campaignService.Load(campaignId);
        }
        catch (FileNotFoundException)
        {
            await WriteJsonAsync(res, 404, new { error = "campaign not found" });
            return;
        }

        EnsureJournalExistsForCurrentCampaign();
        await WriteJsonAsync(res, 200, BuildStateResponse());
        return;
    }

    // DELETE /api/campaigns/{id}
    if (segments is ["api", "campaigns", var deleteCampaignIdText] && method == "DELETE")
    {
        if (!Guid.TryParse(deleteCampaignIdText, out var deleteCampaignId))
        {
            await WriteJsonAsync(res, 400, new { error = "invalid campaign id" });
            return;
        }

        var wasCurrent = campaignService.CurrentCampaign?.Id == deleteCampaignId;
        var deleted = campaignService.Delete(deleteCampaignId);

        if (deleted && wasCurrent)
        {
            var remaining = campaignService.ListCampaigns().FirstOrDefault(c => c.Id != deleteCampaignId);
            if (remaining != null)
            {
                campaignService.Load(remaining.Id);
                EnsureJournalExistsForCurrentCampaign();
            }
            else
            {
                campaignService.CreateNew("Default Campaign");
                EnsureJournalExistsForCurrentCampaign();
            }
        }

        await WriteJsonAsync(res, deleted ? 200 : 404, new { deleted });
        return;
    }

    // PUT /api/session
    if (segments is ["api", "session"] && method == "PUT")
    {
        var body = await ReadJsonAsync<UpdateSessionRequest>(req);
        if (body == null)
        {
            await WriteJsonAsync(res, 400, new { error = "invalid json" });
            return;
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
        await WriteJsonAsync(res, 200, BuildStateResponse());
        return;
    }

    // GET /api/history
    if (segments is ["api", "history"] && method == "GET")
    {
        await WriteJsonAsync(res, 200, historyService.Entries);
        return;
    }

    // Journal
    if (segments is ["api", "journal"])
    {
        var current = campaignService.CurrentCampaign;
        if (current == null)
        {
            await WriteJsonAsync(res, 404, new { error = "no campaign loaded" });
            return;
        }

        if (method == "GET")
        {
            var content = journalService.LoadOrCreate(current.Id, current.Name);
            await WriteJsonAsync(res, 200, new { campaignId = current.Id, content });
            return;
        }

        if (method == "PUT")
        {
            var body = await ReadJsonAsync<JournalUpdateRequest>(req);
            var content = body?.Content ?? string.Empty;

            var saved = journalService.Save(current.Id, content);
            await WriteJsonAsync(res, saved ? 200 : 500, new { saved });
            return;
        }
    }

    // POST /api/fate-check
    if (segments is ["api", "fate-check"] && method == "POST")
    {
        var body = await ReadJsonAsync<FateCheckRequest>(req);
        if (body == null)
        {
            await WriteJsonAsync(res, 400, new { error = "invalid json" });
            return;
        }

        if (!TryParseOdds(body.Odds, out var odds))
        {
            await WriteJsonAsync(res, 400, new { error = "invalid odds" });
            return;
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
        AppendEntryToJournal(fateEntry);

        RandomEventResult? randomEvent = null;
        if (result.RandomEventTriggered)
        {
            randomEvent = RandomEvent.Generate();
            var eventEntry = historyService.AddEntry(
                LogType.RandomEvent,
                $"{randomEvent.EventFocus}: {randomEvent.EventAction}",
                "Triggered by Fate Check"
            );
            AppendEntryToJournal(eventEntry);
        }

        campaignService.Save();

        await WriteJsonAsync(res, 200, new
        {
            chaos,
            odds = odds.GetDisplayName(),
            fate = result,
            randomEvent
        });
        return;
    }

    // POST /api/scene-check
    if (segments is ["api", "scene-check"] && method == "POST")
    {
        var body = await ReadJsonAsync<SceneCheckRequest>(req) ?? new SceneCheckRequest();
        var chaos = body.Chaos ?? session.Chaos;
        var contextText = string.IsNullOrWhiteSpace(body.Context) ? null : body.Context.Trim();

        var result = SceneCheck.PerformCheck(chaos);

        var details = $"Roll: {result.Roll}, Chaos: {chaos}";
        if (result.SceneAdjustment != null)
        {
            details += $", Adjustment: {result.SceneAdjustment}";
        }

        var sceneEntry = historyService.AddEntry(LogType.SceneCheck, result.Result, contextText, details);
        AppendEntryToJournal(sceneEntry);

        if (result.RandomEvent != null)
        {
            var ev = result.RandomEvent;
            var eventEntry = historyService.AddEntry(
                LogType.RandomEvent,
                $"{ev.EventFocus}: {ev.EventAction}",
                "Triggered by Scene Interrupt"
            );
            AppendEntryToJournal(eventEntry);
        }

        campaignService.Save();

        await WriteJsonAsync(res, 200, new { chaos, scene = result });
        return;
    }

    // POST /api/random-event
    if (segments is ["api", "random-event"] && method == "POST")
    {
        var ev = RandomEvent.Generate();

        var eventDetails = ev.SelectedCharacter != null
            ? $"Character: {ev.SelectedCharacter}"
            : ev.SelectedThread != null
                ? $"Thread: {ev.SelectedThread}"
                : null;

        var entry = historyService.AddEntry(LogType.RandomEvent, $"{ev.EventFocus}: {ev.EventAction}", null, eventDetails);
        AppendEntryToJournal(entry);
        campaignService.Save();

        await WriteJsonAsync(res, 200, ev);
        return;
    }

    // POST /api/dice-roll
    if (segments is ["api", "dice-roll"] && method == "POST")
    {
        var body = await ReadJsonAsync<DiceRollRequest>(req);
        var input = body?.Expression;

        if (string.IsNullOrWhiteSpace(input))
        {
            await WriteJsonAsync(res, 400, new { error = "expression is required" });
            return;
        }

        if (!DiceExpression.TryParse(input, out var expression, out var error))
        {
            await WriteJsonAsync(res, 400, new { error = error ?? "invalid dice expression" });
            return;
        }

        var result = DiceRoller.Instance.Roll(expression!);
        var breakdown = result.BuildBreakdown();
        var entry = historyService.AddEntry(LogType.DiceRoll, result.Total.ToString(), expression!.ToDisplayString(), breakdown);
        AppendEntryToJournal(entry);
        campaignService.Save();

        await WriteJsonAsync(res, 200, new { roll = result, breakdown });
        return;
    }

    // Meaning
    // POST /api/meaning/action
    if (segments is ["api", "meaning", "action"] && method == "POST")
    {
        var body = await ReadJsonAsync<MeaningRequest>(req);
        var contextText = string.IsNullOrWhiteSpace(body?.Context) ? null : body!.Context!.Trim();

        var result = MeaningEngine.GenerateAction();
        var entry = historyService.AddEntry(LogType.Meaning, result.Combined, contextText, "Table: Action");
        AppendEntryToJournal(entry);
        campaignService.Save();

        await WriteJsonAsync(res, 200, result);
        return;
    }

    // POST /api/meaning/description
    if (segments is ["api", "meaning", "description"] && method == "POST")
    {
        var body = await ReadJsonAsync<MeaningRequest>(req);
        var contextText = string.IsNullOrWhiteSpace(body?.Context) ? null : body!.Context!.Trim();

        var result = MeaningEngine.GenerateDescription();
        var entry = historyService.AddEntry(LogType.Meaning, result.Combined, contextText, "Table: Description");
        AppendEntryToJournal(entry);
        campaignService.Save();

        await WriteJsonAsync(res, 200, result);
        return;
    }

    // POST /api/meaning/table
    if (segments is ["api", "meaning", "table"] && method == "POST")
    {
        var body = await ReadJsonAsync<MeaningTableRequest>(req);
        var tableId = body?.TableId?.Trim();
        if (string.IsNullOrWhiteSpace(tableId))
        {
            await WriteJsonAsync(res, 400, new { error = "tableId is required" });
            return;
        }

        var contextText = string.IsNullOrWhiteSpace(body?.Context) ? null : body!.Context!.Trim();
        var table = TableService.Instance.FindTable(tableId);
        var displayName = table?.DisplayName ?? tableId;

        var result = MeaningEngine.GenerateFromTable(tableId, displayName);
        var entry = historyService.AddEntry(LogType.Meaning, result.Combined, contextText, $"Table: {displayName}");
        AppendEntryToJournal(entry);
        campaignService.Save();

        await WriteJsonAsync(res, 200, new
        {
            table = new { id = tableId, displayName },
            meaning = result
        });
        return;
    }

    // POST /api/meaning/fusion
    if (segments is ["api", "meaning", "fusion"] && method == "POST")
    {
        var body = await ReadJsonAsync<MeaningFusionRequest>(req);
        var tableId1 = body?.TableId1?.Trim();
        var tableId2 = body?.TableId2?.Trim();

        if (string.IsNullOrWhiteSpace(tableId1) || string.IsNullOrWhiteSpace(tableId2))
        {
            await WriteJsonAsync(res, 400, new { error = "tableId1 and tableId2 are required" });
            return;
        }

        var contextText = string.IsNullOrWhiteSpace(body?.Context) ? null : body!.Context!.Trim();
        var table1 = TableService.Instance.FindTable(tableId1);
        var table2 = TableService.Instance.FindTable(tableId2);
        var name1 = table1?.DisplayName ?? tableId1;
        var name2 = table2?.DisplayName ?? tableId2;

        var result = MeaningEngine.GenerateFusion(tableId1, tableId2);
        var entry = historyService.AddEntry(LogType.Meaning, result.Combined, contextText, $"Fusion: {name1} + {name2}");
        AppendEntryToJournal(entry);
        campaignService.Save();

        await WriteJsonAsync(res, 200, new
        {
            table1 = new { id = tableId1, displayName = name1 },
            table2 = new { id = tableId2, displayName = name2 },
            meaning = result
        });
        return;
    }

    // GET /api/adventure
    if (segments is ["api", "adventure"] && method == "GET")
    {
        await WriteJsonAsync(res, 200, new
        {
            characters = stateManager.State.Characters,
            activeThreads = stateManager.State.ActiveThreads,
            closedThreads = stateManager.State.ClosedThreads
        });
        return;
    }

    // POST /api/adventure/characters
    if (segments is ["api", "adventure", "characters"] && method == "POST")
    {
        var body = await ReadJsonAsync<AddCharacterRequest>(req);
        var name = body?.Name?.Trim();
        var desc = string.IsNullOrWhiteSpace(body?.Description) ? null : body!.Description!.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            await WriteJsonAsync(res, 400, new { error = "name is required" });
            return;
        }

        var character = stateManager.AddCharacter(name, desc);
        campaignService.Save();
        await WriteJsonAsync(res, 201, character);
        return;
    }

    // DELETE /api/adventure/characters?name=...
    if (segments is ["api", "adventure", "characters"] && method == "DELETE")
    {
        var name = req.QueryString["name"];
        if (string.IsNullOrWhiteSpace(name))
        {
            await WriteJsonAsync(res, 400, new { error = "name is required" });
            return;
        }

        var character = stateManager.State.Characters.FirstOrDefault(c =>
            c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (character == null)
        {
            await WriteJsonAsync(res, 404, new { error = "character not found" });
            return;
        }

        var removed = stateManager.RemoveCharacter(character);
        if (removed)
        {
            campaignService.Save();
        }

        await WriteJsonAsync(res, 200, new { removed });
        return;
    }

    // POST /api/adventure/threads
    if (segments is ["api", "adventure", "threads"] && method == "POST")
    {
        var body = await ReadJsonAsync<AddThreadRequest>(req);
        var name = body?.Name?.Trim();
        var desc = string.IsNullOrWhiteSpace(body?.Description) ? null : body!.Description!.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            await WriteJsonAsync(res, 400, new { error = "name is required" });
            return;
        }

        var thread = stateManager.AddThread(name, desc);
        campaignService.Save();
        await WriteJsonAsync(res, 201, thread);
        return;
    }

    // POST /api/adventure/threads/close?name=...
    if (segments is ["api", "adventure", "threads", "close"] && method == "POST")
    {
        var name = req.QueryString["name"];
        if (string.IsNullOrWhiteSpace(name))
        {
            await WriteJsonAsync(res, 400, new { error = "name is required" });
            return;
        }

        var thread = stateManager.State.ActiveThreads.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (thread == null)
        {
            await WriteJsonAsync(res, 404, new { error = "thread not found" });
            return;
        }

        stateManager.CloseThread(thread);
        campaignService.Save();
        await WriteJsonAsync(res, 200, thread);
        return;
    }

    // POST /api/adventure/threads/reopen?name=...
    if (segments is ["api", "adventure", "threads", "reopen"] && method == "POST")
    {
        var name = req.QueryString["name"];
        if (string.IsNullOrWhiteSpace(name))
        {
            await WriteJsonAsync(res, 400, new { error = "name is required" });
            return;
        }

        var thread = stateManager.State.ClosedThreads.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (thread == null)
        {
            await WriteJsonAsync(res, 404, new { error = "thread not found" });
            return;
        }

        stateManager.ReopenThread(thread);
        campaignService.Save();
        await WriteJsonAsync(res, 200, thread);
        return;
    }

    await WriteJsonAsync(res, 404, new { error = "not found" });
}

object BuildStateResponse()
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

void EnsureJournalExistsForCurrentCampaign()
{
    var current = campaignService.CurrentCampaign;
    if (current == null)
    {
        return;
    }

    var content = journalService.LoadOrCreate(current.Id, current.Name);
    journalService.Save(current.Id, content);
}

void AppendEntryToJournal(LogEntry entry)
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

string? FindThemesJsonPath()
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

void AddCorsHeaders(HttpListenerRequest req, HttpListenerResponse res)
{
    var origin = req.Headers["Origin"];
    res.Headers["Access-Control-Allow-Origin"] = string.IsNullOrWhiteSpace(origin) ? "*" : origin;
    res.Headers["Vary"] = "Origin";
    res.Headers["Access-Control-Allow-Methods"] = "GET,POST,PUT,DELETE,OPTIONS";
    res.Headers["Access-Control-Allow-Headers"] = "Content-Type";
}

async Task<T?> ReadJsonAsync<T>(HttpListenerRequest request)
{
    if (!request.HasEntityBody)
    {
        return default;
    }

    try
    {
        return await JsonSerializer.DeserializeAsync<T>(request.InputStream, jsonOptions);
    }
    catch
    {
        return default;
    }
}

async Task WriteJsonAsync(HttpListenerResponse response, int statusCode, object payload)
{
    response.StatusCode = statusCode;
    response.ContentType = "application/json; charset=utf-8";

    var json = JsonSerializer.Serialize(payload, jsonOptions);
    var bytes = Encoding.UTF8.GetBytes(json);
    response.ContentEncoding = Encoding.UTF8;
    response.ContentLength64 = bytes.LongLength;
    await response.OutputStream.WriteAsync(bytes);
}

bool TryParseOdds(string? input, out Odds odds)
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
