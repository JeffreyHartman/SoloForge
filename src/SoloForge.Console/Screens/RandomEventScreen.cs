using Spectre.Console;
using Spectre.Console.Rendering;
using SoloForge.Console.Core;
using SoloForge.Console.Engines.Mythic2e;
using SoloForge.Console.Models;
using SoloForge.Console.Services;
using SoloForge.Console.UI;

namespace SoloForge.Console.Screens;

/// <summary>
/// Screen for generating random events with focus and action.
/// </summary>
public class RandomEventScreen(
    Session session,
    AdventureStateManager stateManager,
    HistoryService historyService,
    CampaignService campaignService,
    JournalService journalService)
    : BaseScreen(session, stateManager, historyService, campaignService, journalService)
{
    public override IScreen? Run()
    {
        while (true)
        {
            RenderHeader("Random Event");

            var result = RandomEvent.Generate();

            // Log the event
            var eventDetails = result.SelectedCharacter != null
                ? $"Character: {result.SelectedCharacter}"
                : result.SelectedThread != null
                    ? $"Thread: {result.SelectedThread}"
                    : null;

            HistoryService.AddEntry(
                LogType.RandomEvent,
                $"{result.EventFocus}: {result.EventAction}",
                null,
                eventDetails
            );
            CampaignService.Save();

            // Build focus text with optional character/thread
            var focusText = result.EventFocus;
            if (result.SelectedCharacter != null)
            {
                focusText += $"\n[aqua]{result.SelectedCharacter}[/]";
            }
            else if (result.SelectedThread != null)
            {
                focusText += $"\n[aqua]{result.SelectedThread}[/]";
            }
            else if (result.ListWasEmpty)
            {
                var listType = RandomEvent.IsNpcFocus(result.EventFocus) ? "No characters" : "No threads";
                focusText += $"\n[grey italic]({listType} in list)[/]";
            }

            var focusPanel = new Panel(
                new Align(
                    new Markup($"[bold cyan]{focusText}[/]"),
                    HorizontalAlignment.Center
                )
            )
            .Header("[bold yellow]Event Focus[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Rounded)
            .BorderColor(MythicUi.WarningColor)
            .Padding(2, 1);
            focusPanel.Width = MythicUi.ResultPanelWidth;

            var actionPanel = new Panel(
                new Align(
                    new Markup($"[bold gold1]{result.EventAction}[/]"),
                    HorizontalAlignment.Center
                )
            )
            .Header("[bold yellow]Event Action[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Double)
            .BorderColor(MythicUi.AccentColor)
            .Padding(2, 1);
            actionPanel.Width = MythicUi.ResultPanelWidth;

            var options = $"[grey]{FormatShortcut("C", "grey")} Copy  {FormatShortcut("R", "grey")} Re-roll  {FormatShortcut("B", "grey")} Back";
            if (result.IsNewNpc)
            {
                options += $"  {FormatShortcut("A", "grey")} Add NPC";
            }
            options += "[/]";

            var content = new List<IRenderable>
            {
                focusPanel,
                new Text(""),
                actionPanel,
                new Text(""),
                new Markup(options)
            };

            RenderSplit(new Rows(content), "Random Event");

            var key = ReadKey();
            if (JournalService.Focus == JournalFocus.Journal)
                continue;

            switch (GetKeyChar(key))
            {
                case 'C':
                    CopyLastEntryToClipboard();
                    continue;
                case 'R':
                    continue;
                case 'A' when result.IsNewNpc:
                    PromptAddCharacter();
                    continue;
                default:
                    return null;
            }
        }
    }

    private void PromptAddCharacter()
    {
        AnsiConsole.WriteLine();
        var name = AnsiConsole.Prompt(
            new TextPrompt<string>("[bold cyan]Enter character name:[/]")
                .PromptStyle("white")
        );

        if (!string.IsNullOrWhiteSpace(name))
        {
            var description = AnsiConsole.Prompt(
                new TextPrompt<string>("[grey]Description (optional):[/]")
                    .PromptStyle("white")
                    .AllowEmpty()
            );

            StateManager.AddCharacter(name, string.IsNullOrWhiteSpace(description) ? null : description);
            CampaignService.Save();
            AnsiConsole.MarkupLine($"[green]Added character:[/] [aqua]{name}[/]");
            Thread.Sleep(800);
        }
    }
}
