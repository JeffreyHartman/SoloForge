using Serilog;
using Spectre.Console;
using Spectre.Console.Rendering;
using SoloForge.Console.Core;
using SoloForge.Console.Engines.Mythic2e;
using SoloForge.Console.Models;
using SoloForge.Console.Services;
using SoloForge.Console.UI;

namespace SoloForge.Console.Screens;

/// <summary>
/// Screen for the Discovering Meaning submenu with quick rolls, element browser, and fusion rolls.
/// </summary>
public class MeaningScreen(
    Session session,
    AdventureStateManager stateManager,
    HistoryService historyService,
    CampaignService campaignService,
    JournalService journalService)
    : BaseScreen(session, stateManager, historyService, campaignService, journalService)
{
    private readonly ILogger _log = AppLogger.ForContext<MeaningScreen>();
    public override IScreen? Run()
    {
        while (true)
        {
            RenderHeader("Discovering Meaning");

            var menuPanel = new Panel(
                new Markup(string.Join("\n", [
                    $"{FormatShortcut("A")} Action (Quick Roll)",
                    $"{FormatShortcut("D")} Description (Quick Roll)",
                    $"{FormatShortcut("E")} Element Tables",
                    $"{FormatShortcut("F")} Fusion Roll",
                    $"{FormatShortcut("Q")} Quick Sets",
                    "[grey]───────────────────────[/]",
                    $"{FormatShortcut("B", "bold yellow")} Back to Main Menu"
                ]))
            )
            .Header("[bold cyan]Select an Option[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Rounded)
            .BorderColor(MythicUi.PrimaryColor)
            .Padding(1, 0);

            RenderSplit(Align.Center(menuPanel), "Discovering Meaning");

            var key = ReadKey();
            if (JournalService.Focus == JournalFocus.Journal)
                continue;

            switch (GetKeyChar(key))
            {
                case 'A':
                    ShowMeaningWithContext("Action");
                    break;
                case 'D':
                    ShowMeaningWithContext("Description");
                    break;
                case 'E':
                    ShowElementBrowser();
                    break;
                case 'F':
                    ShowFusionRoll();
                    break;
                case 'Q':
                    ShowQuickSets();
                    break;
                case 'B':
                    return null;
            }
        }
    }

    private void ShowMeaningWithContext(string type)
    {
        RenderHeader($"{type} Roll");

        // Prompt for context
        var context = PromptForContext("What are you trying to understand? (optional):");

        var result = type == "Action"
            ? MeaningEngine.GenerateAction()
            : MeaningEngine.GenerateDescription();

        // Log and save
        HistoryService.AddEntry(
            LogType.Meaning,
            result.Combined,
            context,
            $"Table: {type}"
        );
        CampaignService.Save();

        ShowMeaningResult(result, context: context);
    }

    private void ShowMeaningResult(MeaningResult result, string? tableId1 = null, string? tableId2 = null, string? context = null)
    {
        while (true)
        {
            var content = new List<IRenderable>();

            if (!string.IsNullOrEmpty(context))
            {
                content.Add(new Markup($"[italic grey]\"{context}\"[/]"));
                content.Add(new Text(""));
            }

            var panel = new Panel(
                new Align(
                    new Markup($"[bold gold1]{result.Combined}[/]"),
                    HorizontalAlignment.Center
                )
            )
            .Header($"[bold cyan]{result.TableName}[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Double)
            .BorderColor(MythicUi.AccentColor)
            .Padding(2, 1);
            panel.Width = MythicUi.ResultPanelWidth;

            content.Add(panel);
            content.Add(new Text(""));

            var table = MythicUi.CreateKeyValueTable();
            table.AddRow("[grey]Word 1:[/]", $"[white]{result.Word1}[/]");
            table.AddRow("[grey]Word 2:[/]", $"[white]{result.Word2}[/]");

            content.Add(table);
            content.Add(new Text(""));
            content.Add(new Markup($"[grey]{FormatShortcut("C", "grey")} Copy  {FormatShortcut("R", "grey")} Re-roll  {FormatShortcut("N", "grey")} New Roll  {FormatShortcut("B", "grey")} Back[/]"));

            RenderSplit(new Rows(content), "Meaning Result");

            var key = ReadKey();
            if (JournalService.Focus == JournalFocus.Journal)
                continue;

            switch (GetKeyChar(key))
            {
                case 'C':
                    CopyLastEntryToClipboard();
                    break;
                case 'R':
                    // Re-roll with same tables
                    if (result.IsFusion && tableId1 != null && tableId2 != null)
                        result = MeaningEngine.GenerateFusion(tableId1, tableId2);
                    else if (tableId1 != null)
                        result = MeaningEngine.GenerateFromTable(tableId1, result.TableName);
                    else if (result.TableName == "Action")
                        result = MeaningEngine.GenerateAction();
                    else if (result.TableName == "Description")
                        result = MeaningEngine.GenerateDescription();

                    // Log re-roll
                    HistoryService.AddEntry(
                        LogType.Meaning,
                        result.Combined,
                        context,
                        $"Table: {result.TableName} (Re-roll)"
                    );
                    CampaignService.Save();
                    break;
                case 'N':
                case 'B':
                    return;
            }
        }
    }

    private void ShowElementBrowser()
    {
        var tables = TableService.Instance.ElementTables.ToList();

        if (tables.Count == 0)
        {
            RenderSplit(new Markup("[red]No element tables found in data/elements/[/]"), "Element Tables");
            WaitForKey();
            return;
        }

        RenderSplit(new Markup("[bold cyan]Select an element table:[/]"), "Element Tables");

        // Prompt for context
        var context = PromptForContext("What are you looking for? (optional):");

        var selectedTable = AnsiConsole.Prompt(
            new SelectionPrompt<TableInfo>()
                .Title("[bold cyan]Select an element table:[/]")
                .HighlightStyle(new Style(MythicUi.AccentColor))
                .PageSize(15)
                .EnableSearch()
                .SearchPlaceholderText("[grey]Type to search...[/]")
                .AddChoices(tables)
                .UseConverter(t => $"[cyan]{t.Category}[/] > {t.DisplayName}")
        );

        var result = MeaningEngine.GenerateFromTable(selectedTable.Id, selectedTable.DisplayName);

        // Log and save
        HistoryService.AddEntry(
            LogType.Meaning,
            result.Combined,
            context,
            $"Table: {selectedTable.DisplayName}"
        );
        CampaignService.Save();

        ShowMeaningResult(result, selectedTable.Id, context: context);
    }

    private void ShowFusionRoll()
    {
        var allTables = TableService.Instance.AvailableTables.ToList();

        RenderHeader("Fusion Roll");

        // Prompt for context
        var context = PromptForContext("What are you combining meanings for? (optional):");

        RenderSplit(new Markup("[bold cyan]Select first table:[/]"), "Fusion Roll");
        var table1 = AnsiConsole.Prompt(
            new SelectionPrompt<TableInfo>()
                .HighlightStyle(new Style(MythicUi.AccentColor))
                .PageSize(12)
                .EnableSearch()
                .SearchPlaceholderText("[grey]Type to search...[/]")
                .AddChoices(allTables)
                .UseConverter(t => t.IsElement ? $"[cyan]{t.Category}[/] > {t.DisplayName}" : $"[yellow]Core[/] > {t.DisplayName}")
        );

        var fusionHeader = new Rows(
            new IRenderable[]
            {
                new Markup($"[grey]First table:[/] [gold1]{table1.DisplayName}[/]"),
                new Text(""),
                new Markup("[bold cyan]Select second table:[/]")
            }
        );
        RenderSplit(fusionHeader, "Fusion Roll");

        var table2 = AnsiConsole.Prompt(
            new SelectionPrompt<TableInfo>()
                .HighlightStyle(new Style(MythicUi.AccentColor))
                .PageSize(12)
                .EnableSearch()
                .SearchPlaceholderText("[grey]Type to search...[/]")
                .AddChoices(allTables)
                .UseConverter(t => t.IsElement ? $"[cyan]{t.Category}[/] > {t.DisplayName}" : $"[yellow]Core[/] > {t.DisplayName}")
        );

        var result = MeaningEngine.GenerateFusion(table1.Id, table2.Id);

        // Log and save
        HistoryService.AddEntry(
            LogType.Meaning,
            result.Combined,
            context,
            $"Fusion: {table1.DisplayName} + {table2.DisplayName}"
        );
        CampaignService.Save();

        ShowMeaningResult(result, table1.Id, table2.Id, context);
    }

    private void ShowQuickSets()
    {
        var quickSets = QuickSetService.Instance.QuickSets;

        if (quickSets.Count == 0)
        {
            RenderSplit(new Markup("[red]No quick sets found. Check data/quicksets.json[/]"), "Quick Sets");
            WaitForKey();
            return;
        }

        RenderSplit(new Markup("[bold cyan]Select a Quick Set:[/]"), "Quick Sets");

        var selectedSet = AnsiConsole.Prompt(
            new SelectionPrompt<QuickSet>()
                .Title("[bold cyan]Select a Quick Set:[/]")
                .HighlightStyle(new Style(MythicUi.AccentColor))
                .PageSize(10)
                .AddChoices(quickSets)
                .UseConverter(q => $"[gold1]{q.Name}[/] - [grey]{q.Description}[/]")
        );

        ShowQuickSetResult(selectedSet);
    }

    private void ShowQuickSetResult(QuickSet quickSet)
    {
        var needsGenerate = true;
        QuickSetResult? result = null;
        var historyWritten = false;

        while (true)
        {
                if (needsGenerate)
                {
                    RenderHeader(quickSet.Name);
                    historyWritten = false;

                    try
                    {
                        result = QuickSetService.Instance.Generate(quickSet);

                        var table = new Table()
                            .Border(TableBorder.Rounded)
                            .BorderColor(MythicUi.AccentColor)
                            .Title($"[bold gold1]{Markup.Escape(quickSet.Name)}[/]")
                            .AddColumn(new TableColumn("[bold cyan]Attribute[/]").Width(14))
                            .AddColumn(new TableColumn("[bold cyan]Result[/]"));

                        foreach (var stepResult in result.Results)
                        {
                            table.AddRow(
                                $"[yellow]{Markup.Escape(stepResult.Label)}[/]",
                                $"[white]{Markup.Escape(stepResult.Combined)}[/]"
                            );
                        }

                        var content = new List<IRenderable>
                        {
                            table,
                            new Text(""),
                            new Markup($"[grey]{FormatShortcut("C", "grey")} Copy  {FormatShortcut("R", "grey")} Generate New  {FormatShortcut("B", "grey")} Back[/]")
                        };

                        // Only log after successful display
                        HistoryService.AddEntry(
                            LogType.Meaning,
                            $"{quickSet.Name} Generated",
                            null,
                            result.ToDisplayDetails()
                        );
                        CampaignService.Save();
                        historyWritten = true;

                        RenderSplit(new Rows(content), quickSet.Name);
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Failed to generate or display quick set '{Name}'", quickSet.Name);
                        var content = new List<IRenderable>
                        {
                            new Markup($"[red]Error generating quick set: {Markup.Escape(ex.Message)}[/]"),
                            new Text(""),
                            new Markup($"[grey]{FormatShortcut("R", "grey")} Try Again  {FormatShortcut("B", "grey")} Back[/]")
                        };
                        RenderSplit(new Rows(content), quickSet.Name);
                    }

                    needsGenerate = false;
                }

            var key = ReadKey();
            if (JournalService.Focus == JournalFocus.Journal)
                continue;

            switch (GetKeyChar(key))
            {
                case 'C':
                    if (historyWritten)
                        CopyLastEntryToClipboard();
                    break;
                case 'R':
                    needsGenerate = true;
                    break;
                default:
                    return;
            }

        }
    }
}
