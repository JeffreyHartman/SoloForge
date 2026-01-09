using Spectre.Console;
using SoloForge.Console.Services;
using SoloForge.Console.UI;

namespace SoloForge.Console.Core;

/// <summary>
/// Abstract base class for all screens, providing common functionality.
/// </summary>
/// <param name="session">The current session state (chaos factor, engine, theme).</param>
/// <param name="stateManager">The adventure state manager (characters, threads).</param>
/// <param name="historyService">The history/journal service.</param>
/// <param name="campaignService">The campaign persistence service.</param>
public abstract class BaseScreen(
    Session session,
    AdventureStateManager stateManager,
    HistoryService historyService,
    CampaignService campaignService) : IScreen
{
    protected Session Session { get; } = session;
    protected AdventureStateManager StateManager { get; } = stateManager;
    protected HistoryService HistoryService { get; } = historyService;
    protected CampaignService CampaignService { get; } = campaignService;

    /// <summary>
    /// Runs the screen logic. Must be implemented by derived classes.
    /// </summary>
    public abstract IScreen? Run();

    /// <summary>
    /// Clears the console and renders the session header with campaign name.
    /// </summary>
    protected void RenderHeader(string title)
    {
        MythicUi.Clear();
        MythicUi.RenderSessionHeader(
            title,
            Session.Chaos,
            StateManager.CharacterCount,
            StateManager.ActiveThreadCount,
            CampaignService.CurrentCampaign?.Name
        );
    }

    /// <summary>
    /// Formats a keyboard shortcut with proper bracket escaping.
    /// Delegates to MythicUi.FormatShortcut.
    /// </summary>
    protected static string FormatShortcut(string key, string color = "bold green")
        => MythicUi.FormatShortcut(key, color);

    /// <summary>
    /// Waits for a key press with optional custom message.
    /// </summary>
    protected static void WaitForKey(string message = "Press any key to continue...")
        => MythicUi.WaitForKey(message);

    /// <summary>
    /// Reads a key without echoing to console.
    /// </summary>
    protected static ConsoleKeyInfo ReadKey() => System.Console.ReadKey(intercept: true);

    /// <summary>
    /// Gets the uppercase character from a key press.
    /// </summary>
    protected static char GetKeyChar(ConsoleKeyInfo key) => char.ToUpperInvariant(key.KeyChar);

    /// <summary>
    /// Prompts for optional context/question before a roll.
    /// </summary>
    protected static string? PromptForContext(string prompt = "Enter question/context (optional):")
    {
        var input = AnsiConsole.Prompt(
            new TextPrompt<string>($"[grey]{prompt}[/]")
                .PromptStyle("white")
                .AllowEmpty()
        );
        return string.IsNullOrWhiteSpace(input) ? null : input;
    }
}

/// <summary>
/// Session state container for chaos factor, engine, and theme.
/// </summary>
public class Session
{
    public string Engine { get; set; } = "Mythic 2e";
    public string Theme { get; set; } = "Fantasy";

    public int Chaos
    {
        get;
        set => field = Math.Clamp(value, 1, 9);
    } = 5;
}
