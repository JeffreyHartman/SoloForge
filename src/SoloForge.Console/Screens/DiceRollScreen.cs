using Spectre.Console;
using SoloForge.Console.Core;
using SoloForge.Console.Models;
using SoloForge.Console.Services;
using SoloForge.Console.UI;

namespace SoloForge.Console.Screens;

/// <summary>
/// Screen for rolling arbitrary dice expressions.
/// </summary>
public class DiceRollScreen(
    Session session,
    AdventureStateManager stateManager,
    HistoryService historyService,
    CampaignService campaignService)
    : BaseScreen(session, stateManager, historyService, campaignService)
{
    public override IScreen? Run()
    {
        while (true)
        {
            RenderHeader("Dice Roller");

            if (!TryPromptDiceExpression("[bold cyan]Enter dice expression (e.g., 2d6+1 or 1d%):[/]", out var expression))
            {
                return null;
            }

            var result = DiceRoller.Instance.Roll(expression!);
            var summary = result.Summary;
            var breakdown = result.BuildBreakdown();

            HistoryService.AddEntry(LogType.DiceRoll, result.Total.ToString(), expression!.ToDisplayString(), breakdown);
            CampaignService.Save();

            RenderHeader("Dice Roller");

            var panel = new Panel(new Markup($"[bold gold1]{Markup.Escape(summary)}[/]"))
                .Header("[bold cyan]Result[/]")
                .HeaderAlignment(Justify.Center)
                .Border(BoxBorder.Rounded)
                .BorderColor(MythicUi.AccentColor)
                .Padding(1, 0);
            panel.Width = Math.Min(MythicUi.ResultPanelWidth, 38);

            AnsiConsole.Write(Align.Center(panel));
            AnsiConsole.WriteLine();

            if (!string.IsNullOrWhiteSpace(breakdown))
            {
                AnsiConsole.Write(Align.Center(new Markup($"[grey]{Markup.Escape(breakdown)}[/]")));
                AnsiConsole.WriteLine();
            }

            AnsiConsole.MarkupLine($"[grey]{FormatShortcut("R", "grey")} Roll Again  {FormatShortcut("B", "grey")} Back[/]");

            var key = ReadKey();
            switch (GetKeyChar(key))
            {
                case 'R':
                    continue;
                case 'B':
                case 'Q':
                    return null;
            }
        }
    }
}
