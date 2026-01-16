using Serilog;
using Spectre.Console;
using SoloForge.Console.Models;
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

        MythicUi.RenderQuickRollLine(Session.LastQuickRoll);
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
    protected void WaitForKey(string message = "Press any key to continue...")
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[grey]{message}[/]");

        ReadKey();
    }

    /// <summary>
    /// Waits for a key press, showing copy shortcut hint.
    /// Handles C to copy the most recent entry.
    /// </summary>
    protected void WaitForKeyWithCopyHint()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[grey]{FormatShortcut("C", "grey")} Copy to clipboard  Press any key to continue...[/]");

        while (true)
        {
            var key = ReadKey();

            // Handle C to copy last entry
            if (char.ToUpperInvariant(key.KeyChar) == 'C')
            {
                CopyLastEntryToClipboard();
                continue; // Stay on screen after copying
            }

            // Any other key exits
            return;
        }
    }

    /// <summary>
    /// Reads a key without echoing to console.
    /// Intercepts global shortcuts like Alt+C for clipboard operations.
    /// </summary>
    protected ConsoleKeyInfo ReadKey()
    {
        while (true)
        {
            var key = System.Console.ReadKey(intercept: true);

            // Check for Alt+C (copy last entry to clipboard)
            if (key.Modifiers.HasFlag(ConsoleModifiers.Alt) &&
                char.ToUpperInvariant(key.KeyChar) == 'C')
            {
                CopyLastEntryToClipboard();
                continue; // Swallow the input and wait for next key
            }

            if (key.Modifiers.HasFlag(ConsoleModifiers.Alt) &&
                char.ToUpperInvariant(key.KeyChar) == 'R')
            {
                TriggerQuickRoll();
                continue; // Swallow the input and wait for next key
            }

            return key;
        }
    }

    /// <summary>
    /// Copies the most recent history entry to the clipboard as markdown.
    /// </summary>
    protected void CopyLastEntryToClipboard()
    {
        var log = AppLogger.ForContext<BaseScreen>();
        log.Debug("CopyLastEntryToClipboard triggered via Alt+C");

        var lastEntry = HistoryService.Entries.LastOrDefault();
        if (lastEntry == null)
        {
            log.Debug("No entries to copy");
            MythicUi.ShowClipboardFeedback(false, "No entries to copy");
            return;
        }

        log.Debug("Copying last entry: {Type} from {Timestamp}", lastEntry.Type, lastEntry.Timestamp);
        CopyEntryToClipboard(lastEntry);
    }

    /// <summary>
    /// Copies a specific log entry to the clipboard as markdown.
    /// </summary>
    protected static void CopyEntryToClipboard(LogEntry entry)
    {
        var log = AppLogger.ForContext<BaseScreen>();
        log.Debug("CopyEntryToClipboard: {Type}, Result: {Result}", entry.Type, entry.Result);

        var markdown = TemplateService.Instance.ToMarkdown(entry);
        log.Debug("Generated markdown ({Length} chars)", markdown.Length);

        var success = ClipboardService.Instance.CopyToClipboard(markdown);
        MythicUi.ShowClipboardFeedback(success, success ? "Copied to clipboard" : "Failed to copy");
    }

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

    protected void TriggerQuickRoll()
    {
        if (!TryPromptDiceExpressionInline("Quick roll: ", out var expression))
        {
            return;
        }

        if (expression == null)
        {
            return;
        }

        var result = DiceRoller.Instance.Roll(expression);
        var summary = result.Summary;
        var details = result.BuildBreakdown();

        Session.LastQuickRoll = summary;
        MythicUi.RenderQuickRollLine(summary);

        HistoryService.AddEntry(LogType.DiceRoll, result.Total.ToString(), expression.ToDisplayString(), details);
        CampaignService.Save();
    }

    protected static bool TryPromptDiceExpressionInline(string prompt, out DiceExpression? expression)
    {
        expression = null;
        var input = MythicUi.PromptInlineAtBottom(prompt);

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        if (!DiceExpression.TryParse(input, out expression, out var error))
        {
            MythicUi.RenderBottomMessage(error, isError: true);
            return false;
        }

        MythicUi.RenderBottomMessage(string.Empty, isError: false);
        return true;
    }

    protected static bool TryPromptDiceExpression(string prompt, out DiceExpression? expression)
    {
        expression = null;
        var input = AnsiConsole.Prompt(
            new TextPrompt<string>(prompt)
                .PromptStyle("white")
                .AllowEmpty()
        );

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        if (!DiceExpression.TryParse(input, out expression, out var error))
        {
            AnsiConsole.MarkupLine($"[red]{error}[/]");
            return false;
        }

        return true;
    }
}

/// <summary>
/// Session state container for chaos factor, engine, and theme.
/// </summary>
public class Session
{
    public string Engine { get; set; } = "Mythic 2e";
    public string Theme { get; set; } = "Fantasy";
    public string? LastQuickRoll { get; set; }

    public int Chaos
    {
        get;
        set => field = Math.Clamp(value, 1, 9);
    } = 5;
}

