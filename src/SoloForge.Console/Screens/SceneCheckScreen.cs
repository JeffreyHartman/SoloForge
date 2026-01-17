using Spectre.Console;
using Spectre.Console.Rendering;
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
    CampaignService campaignService,
    JournalService journalService)
    : BaseScreen(session, stateManager, historyService, campaignService, journalService)
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
        var content = new List<IRenderable>();

        if (!string.IsNullOrEmpty(context))
        {
            content.Add(new Markup($"[italic grey]\"{context}\"[/]"));
            content.Add(new Text(""));
        }

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
        content.Add(resultPanel);
        content.Add(new Text(""));

        var detailsTable = MythicUi.CreateDetailsTable();
        detailsTable.AddRow("[grey]Chaos Factor[/]", $"[white]{Session.Chaos}[/]");
        detailsTable.AddRow("[grey]Roll[/]", $"[white]{result.Roll}[/]");
        content.Add(detailsTable);

        if (result.SceneAdjustment != null)
        {
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

            content.Add(new Text(""));
            content.Add(adjustmentPanel);
        }

        if (result.RandomEvent != null)
        {
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

            content.Add(new Text(""));
            content.Add(eventPanel);
        }

        content.Add(new Text(""));
        content.Add(new Markup($"[grey]{FormatShortcut("C", "grey")} Copy  Press any key to continue...[/]"));

        while (true)
        {
            RenderSplit(new Rows(content), "Scene Check");
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
