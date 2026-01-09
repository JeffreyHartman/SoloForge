using Spectre.Console;
using SoloForge.Console.Core;
using SoloForge.Console.Engines.Mythic2e;
using SoloForge.Console.Models;
using SoloForge.Console.Services;
using SoloForge.Console.UI;

namespace SoloForge.Console.Screens;

/// <summary>
/// Screen for performing Scene Checks to determine scene alterations.
/// </summary>
public class SceneCheckScreen(
    Session session,
    AdventureStateManager stateManager,
    HistoryService historyService,
    CampaignService campaignService)
    : BaseScreen(session, stateManager, historyService, campaignService)
{
    public override IScreen? Run()
    {
        RenderHeader("Scene Check");

        // Prompt for optional scene context
        var context = PromptForContext("Enter scene setup/context (optional):");

        // Perform the check
        var result = SceneCheck.PerformCheck(Session.Chaos);

        // Build result details
        var details = $"Roll: {result.Roll}, Chaos: {Session.Chaos}";
        if (result.SceneAdjustment != null)
            details += $", Adjustment: {result.SceneAdjustment}";

        // Log the result
        HistoryService.AddEntry(
            LogType.SceneCheck,
            result.Result,
            context,
            details
        );

        // Auto-save
        CampaignService.Save();

        // Display results
        RenderHeader("Scene Check");

        // Show context if provided
        if (!string.IsNullOrEmpty(context))
        {
            AnsiConsole.Write(Align.Center(new Markup($"[italic grey]\"{context}\"[/]")));
            AnsiConsole.WriteLine();
        }

        // Color-code the result
        var (resultColor, borderColor) = result.Result switch
        {
            "Normal Scene" => ("green", MythicUi.SuccessColor),
            "Altered Scene!" => ("yellow", MythicUi.WarningColor),
            "Interrupt Scene!" => ("red", MythicUi.ErrorColor),
            _ => ("white", Color.White)
        };

        var resultPanel = MythicUi.CreateResultPanel(
            $"[bold {resultColor}]{result.Result}[/]",
            "Result",
            borderColor
        );
        AnsiConsole.Write(Align.Center(resultPanel));
        AnsiConsole.WriteLine();

        var detailsTable = MythicUi.CreateDetailsTable();
        detailsTable.AddRow("[grey]Chaos Factor[/]", $"[white]{Session.Chaos}[/]");
        detailsTable.AddRow("[grey]Roll[/]", $"[white]{result.Roll}[/]");

        AnsiConsole.Write(Align.Center(detailsTable));

        // Show scene adjustment if present
        if (result.SceneAdjustment != null)
        {
            AnsiConsole.WriteLine();

            var adjustmentPanel = new Panel(
                new Align(
                    new Markup($"[bold gold1]{result.SceneAdjustment}[/]"),
                    HorizontalAlignment.Center
                )
            )
            .Header("[bold yellow]Scene Adjustment[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Rounded)
            .BorderColor(MythicUi.WarningColor)
            .Padding(2, 1);
            adjustmentPanel.Width = MythicUi.ResultPanelWidth;

            AnsiConsole.Write(Align.Center(adjustmentPanel));
        }

        // Show random event if present
        if (result.RandomEvent != null)
        {
            AnsiConsole.WriteLine();

            // Log the random event
            HistoryService.AddEntry(
                LogType.RandomEvent,
                $"{result.RandomEvent.EventFocus}: {result.RandomEvent.EventAction}",
                "Triggered by Scene Interrupt"
            );
            CampaignService.Save();

            var eventPanel = new Panel(
                new Align(
                    new Markup($"[bold gold1]{result.RandomEvent.EventAction}[/]"),
                    HorizontalAlignment.Center
                )
            )
            .Header($"[bold yellow]Random Event: {result.RandomEvent.EventFocus}[/]")
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
