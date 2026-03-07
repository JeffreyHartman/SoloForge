using System.Text.Json;

using Microsoft.AspNetCore.Http.Json;

using SoloForge.Api.Endpoints;
using SoloForge.Console.Core;
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

var apiLog = AppLogger.ForContext("ApiMiddleware");

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        apiLog.Error(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

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

var api = app.MapGroup("/api");
api.MapGet("/health", () => Results.Json(new { status = "ok" }));
api.MapSessionEndpoints();
api.MapCampaignEndpoints();
api.MapMythicEndpoints();
api.MapAdventureEndpoints();
api.MapJournalEndpoints();

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
