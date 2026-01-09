using Spectre.Console;
using SoloForge.Console.Core;
using SoloForge.Console.Engines.Mythic2e;
using SoloForge.Console.Services;
using SoloForge.Console.UI;

namespace SoloForge.Console.Screens;

/// <summary>
/// Screen for generating random events with focus and action.
/// </summary>
public class RandomEventScreen(Session session, AdventureStateManager stateManager)
    : BaseScreen(session, stateManager)
{
    public override IScreen? Run()
    {
        while (true)
        {
            RenderHeader("Random Event");

            var result = RandomEvent.Generate();

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

            AnsiConsole.Write(Align.Center(focusPanel));
            AnsiConsole.WriteLine();

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

            AnsiConsole.Write(Align.Center(actionPanel));
            AnsiConsole.WriteLine();

            // Build options string
            var options = $"[grey]{FormatShortcut("R", "grey")} Re-roll  {FormatShortcut("B", "grey")} Back";
            if (result.IsNewNpc)
            {
                options += $"  {FormatShortcut("A", "grey")} Add NPC";
            }
            options += "[/]";
            AnsiConsole.MarkupLine(options);

            var key = ReadKey();
            switch (GetKeyChar(key))
            {
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
            AnsiConsole.MarkupLine($"[green]Added character:[/] [aqua]{name}[/]");
            Thread.Sleep(800);
        }
    }
}
