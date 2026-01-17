using Terminal.Gui;
using SoloForge.Console.App;
using SoloForge.Console.Core;
using SoloForge.Console.Services;

// Initialize logging first
AppLogger.Initialize();

try
{
    AppLogger.Logger.Information("Starting SoloForge with Terminal.Gui");

    // Initialize services
    var session = new Session();
    var stateManager = AdventureStateManager.Instance;
    var historyService = new HistoryService();
    var campaignService = new CampaignService(session, stateManager, historyService);

    // Initialize campaign service (loads last campaign or creates default)
    campaignService.Initialize();

    AppLogger.Logger.Information("Services initialized, starting Terminal.Gui application");

    // Initialize Terminal.Gui
    Application.Init();

    try
    {
        // Create and run the main application
        var app = new SoloForgeApp(session, stateManager, historyService, campaignService);
        Application.Run(app);
    }
    finally
    {
        Application.Shutdown();
    }

    AppLogger.Logger.Information("Application shutdown complete");
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
