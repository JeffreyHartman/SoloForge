using Spectre.Console;
using Spectre.Console.Rendering;
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
    CampaignService campaignService,
    JournalService journalService)
    : BaseScreen(session, stateManager, historyService, campaignService, journalService)
{
    public override IScreen? Run()
    {
        while (true)
        {
            RenderSplit(new Markup("[bold cyan]Enter dice expression (e.g., 2d6+1 or 1d%):[/]"), "Dice Roller");

            if (!TryPromptDiceExpression("[bold cyan]Enter dice expression (e.g., 2d6+1 or 1d%):[/]", out var expression))
            {
                return null;
            }

            var result = DiceRoller.Instance.Roll(expression!);
            var summary = result.Summary;
            var breakdown = result.BuildBreakdown();

            HistoryService.AddEntry(LogType.DiceRoll, result.Total.ToString(), expression!.ToDisplayString(), breakdown);
            CampaignService.Save();

            var content = new List<IRenderable>();

            var panel = new Panel(new Markup($"[bold gold1]{Markup.Escape(summary)}[/]"))
                .Header("[bold cyan]Result[/]")
                .HeaderAlignment(Justify.Center)
                .Border(BoxBorder.Rounded)
                .BorderColor(MythicUi.AccentColor)
                .Padding(1, 0);
            panel.Width = Math.Min(MythicUi.ResultPanelWidth, 38);

            content.Add(panel);

            if (!string.IsNullOrWhiteSpace(breakdown))
            {
                content.Add(new Text(""));
                content.Add(new Markup($"[grey]{Markup.Escape(breakdown)}[/]"));
            }

            content.Add(new Text(""));
            content.Add(new Markup($"[grey]{FormatShortcut("R", "grey")} Roll Again  {FormatShortcut("B", "grey")} Back[/]"));

            RenderSplit(new Rows(content), "Dice Roller");

            var key = ReadKey();
            if (JournalService.Focus == JournalFocus.Journal)
                continue;

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
