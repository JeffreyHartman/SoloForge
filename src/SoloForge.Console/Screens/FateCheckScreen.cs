using Spectre.Console;
using Spectre.Console.Rendering;
using SoloForge.Console.Core;
using SoloForge.Console.Engines.Mythic2e;
using SoloForge.Console.Models;
using SoloForge.Console.Services;
using SoloForge.Console.UI;

namespace SoloForge.Console.Screens;

/// <summary>
/// Screen for performing Fate Checks against the Mythic 2e fate chart.
/// </summary>
public class FateCheckScreen(
    Session session,
    AdventureStateManager stateManager,
    HistoryService historyService,
    CampaignService campaignService,
    JournalService journalService)
    : BaseScreen(session, stateManager, historyService, campaignService, journalService)
{
    public override IScreen? Run()
    {
        RenderHeader("Fate Check");

        // Prompt for optional question/context
        var context = PromptForContext("Enter question (optional):");

        // Prompt user to select odds
        var selectedOdds = AnsiConsole.Prompt(
            new SelectionPrompt<Odds>()
                .Title("[bold cyan]Select Odds:[/]")
                .HighlightStyle(new Style(MythicUi.AccentColor))
                .AddChoices(Enum.GetValues<Odds>())
                .UseConverter(odds => odds.GetDisplayName())
        );

        // Perform the fate check
        var result = FateCheck.PerformCheck(Session.Chaos, selectedOdds);

        // Log the result
        HistoryService.AddEntry(
            LogType.FateCheck,
            result.Result,
            context,
            $"Odds: {selectedOdds.GetDisplayName()}, Roll: {result.Roll}, Chaos: {Session.Chaos}"
        );

        // Auto-save
        CampaignService.Save();

        // Display results
        var content = new List<IRenderable>();

        if (!string.IsNullOrEmpty(context))
        {
            content.Add(new Markup($"[italic grey]\"{context}\"[/]"));
            content.Add(new Text(""));
        }

        string resultColor = result.Result.Contains("Yes") ? "green" : "red";
        var borderColor = resultColor == "green" ? MythicUi.SuccessColor : MythicUi.ErrorColor;

        var resultPanel = MythicUi.CreateResultPanel(
            $"[bold {resultColor}]{result.Result}[/]",
            "Result",
            borderColor
        );
        content.Add(resultPanel);
        content.Add(new Text(""));

        var detailsTable = MythicUi.CreateDetailsTable();
        detailsTable.AddRow("[grey]Odds[/]", $"[white]{selectedOdds.GetDisplayName()}[/]");
        detailsTable.AddRow("[grey]Chaos Factor[/]", $"[white]{Session.Chaos}[/]");
        detailsTable.AddRow("[grey]Roll[/]", $"[white]{result.Roll}[/]");
        content.Add(detailsTable);

        if (result.RandomEventTriggered)
        {
            var randomEvent = RandomEvent.Generate();

            // Log the random event too
            HistoryService.AddEntry(
                LogType.RandomEvent,
                $"{randomEvent.EventFocus}: {randomEvent.EventAction}",
                "Triggered by Fate Check"
            );
            CampaignService.Save();

            var eventPanel = new Panel(
                new Markup($"[bold gold1]{randomEvent.EventAction}[/]")
            )
            .Header($"[bold yellow]Random Event: {randomEvent.EventFocus}[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Double)
            .BorderColor(MythicUi.WarningColor)
            .Padding(2, 1);
            eventPanel.Width = MythicUi.ResultPanelWidth;

            content.Add(new Text(""));
            content.Add(eventPanel);
        }

        content.Add(new Text(""));
        content.Add(new Markup($"[grey]{FormatShortcut("C", "grey")} Copy  Press any key to continue...[/]"));

        while (true)
        {
            RenderSplit(new Rows(content), "Fate Check");
            var key = ReadKey();

            if (JournalService.Focus == JournalFocus.Journal)
                continue;

            if (GetKeyChar(key) == 'C')
            {
                CopyLastEntryToClipboard();
                continue;
            }

            return null;
        }
    }
}
