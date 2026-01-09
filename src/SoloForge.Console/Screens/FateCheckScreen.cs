using Spectre.Console;
using SoloForge.Console.Core;
using SoloForge.Console.Engines.Mythic2e;
using SoloForge.Console.Services;
using SoloForge.Console.UI;

namespace SoloForge.Console.Screens;

/// <summary>
/// Screen for performing Fate Checks against the Mythic 2e fate chart.
/// </summary>
public class FateCheckScreen(Session session, AdventureStateManager stateManager)
    : BaseScreen(session, stateManager)
{
    public override IScreen? Run()
    {
        RenderHeader("Fate Check");

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

        // Display results
        RenderHeader("Fate Check");

        // Color-code the result
        string resultColor = result.Result.Contains("Yes") ? "green" : "red";
        var borderColor = resultColor == "green" ? MythicUi.SuccessColor : MythicUi.ErrorColor;

        var resultPanel = MythicUi.CreateResultPanel(
            $"[bold {resultColor}]{result.Result}[/]",
            "Result",
            borderColor
        );
        AnsiConsole.Write(Align.Center(resultPanel));
        AnsiConsole.WriteLine();

        var detailsTable = MythicUi.CreateDetailsTable();
        detailsTable.AddRow("[grey]Odds[/]", $"[white]{selectedOdds.GetDisplayName()}[/]");
        detailsTable.AddRow("[grey]Chaos Factor[/]", $"[white]{Session.Chaos}[/]");
        detailsTable.AddRow("[grey]Roll[/]", $"[white]{result.Roll}[/]");

        AnsiConsole.Write(Align.Center(detailsTable));

        // Show random event if triggered
        if (result.RandomEventTriggered)
        {
            var randomEvent = RandomEvent.Generate();
            AnsiConsole.WriteLine();

            var eventPanel = new Panel(
                new Markup($"[bold gold1]{randomEvent.EventAction}[/]")
            )
            .Header($"[bold yellow]Random Event: {randomEvent.EventFocus}[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Double)
            .BorderColor(MythicUi.WarningColor)
            .Padding(2, 1);
            eventPanel.Width = MythicUi.ResultPanelWidth;

            AnsiConsole.Write(Align.Center(eventPanel));
        }

        WaitForKey();
        return null;
    }
}
