using Microsoft.Extensions.DependencyInjection;
using SoloForge.Console.Core;
using SoloForge.Console.Screens;
using SoloForge.Console.Services;

// Configure dependency injection
var services = new ServiceCollection();

// Register singletons (state persists across screens)
services.AddSingleton<Session>();
services.AddSingleton(AdventureStateManager.Instance);

// Register screens as transients (new instance each navigation)
services.AddTransient<MainMenuScreen>();
services.AddTransient<FateCheckScreen>();
services.AddTransient<SceneCheckScreen>();
services.AddTransient<RandomEventScreen>();
services.AddTransient<MeaningScreen>();
services.AddTransient<AdventureListScreen>();

var provider = services.BuildServiceProvider();

// Main application loop
IScreen? currentScreen = provider.GetRequiredService<MainMenuScreen>();
while (currentScreen != null)
{
    currentScreen = currentScreen.Run();
}
