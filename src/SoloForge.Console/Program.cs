using Microsoft.Extensions.DependencyInjection;
using SoloForge.Console.Core;
using SoloForge.Console.Screens;
using SoloForge.Console.Services;

// Initialize logging first
AppLogger.Initialize();

try
{
    // Configure dependency injection
    var services = new ServiceCollection();

    // Register singletons (state persists across screens)
    services.AddSingleton<Session>();
    services.AddSingleton(AdventureStateManager.Instance);
    services.AddSingleton<HistoryService>();
    services.AddSingleton<CampaignService>();
    services.AddSingleton(ClipboardService.Instance);
    services.AddSingleton(TemplateService.Instance);
    services.AddSingleton<JournalService>();

    // Register screens as transients (new instance each navigation)
    services.AddTransient<MainMenuScreen>();
    services.AddTransient<FateCheckScreen>();
    services.AddTransient<SceneCheckScreen>();
    services.AddTransient<RandomEventScreen>();
    services.AddTransient<MeaningScreen>();
    services.AddTransient<AdventureListScreen>();
    services.AddTransient<DiceRollScreen>();
    services.AddTransient<GameManagerScreen>();
    services.AddTransient<HistoryScreen>();

    var provider = services.BuildServiceProvider();

    // Initialize campaign service (loads last campaign or creates default)
    var campaignService = provider.GetRequiredService<CampaignService>();
    campaignService.Initialize();

    AppLogger.Logger.Information("Services initialized, starting main loop");

    // Main application loop
    IScreen? currentScreen = provider.GetRequiredService<MainMenuScreen>();
    while (currentScreen != null)
    {
        currentScreen = currentScreen.Run();
    }
}
catch (Exception ex)
{
    AppLogger.Logger.Fatal(ex, "Unhandled exception in application");
    throw;
}
finally
{
    AppLogger.Shutdown();
}
