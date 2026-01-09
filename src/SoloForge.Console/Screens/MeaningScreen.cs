using Spectre.Console;
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
    CampaignService campaignService)
    : BaseScreen(session, stateManager, historyService, campaignService)
{
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
                    $"{FormatShortcut("N")} NPC Profile",
                    "[grey]───────────────────────[/]",
                    $"{FormatShortcut("B", "bold yellow")} Back to Main Menu"
                ]))
            )
            .Header("[bold cyan]Select an Option[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Rounded)
            .BorderColor(MythicUi.PrimaryColor)
            .Padding(1, 0);

            AnsiConsole.Write(Align.Center(menuPanel));

            var key = ReadKey();
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
                case 'N':
                    ShowNpcProfile();
                    break;
                case 'B':
                case 'Q':
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
            RenderHeader("Meaning Result");

            // Show context if provided
            if (!string.IsNullOrEmpty(context))
            {
                AnsiConsole.Write(Align.Center(new Markup($"[italic grey]\"{context}\"[/]")));
                AnsiConsole.WriteLine();
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

            AnsiConsole.Write(Align.Center(panel));
            AnsiConsole.WriteLine();

            var table = MythicUi.CreateKeyValueTable();
            table.AddRow("[grey]Word 1:[/]", $"[white]{result.Word1}[/]");
            table.AddRow("[grey]Word 2:[/]", $"[white]{result.Word2}[/]");

            AnsiConsole.Write(Align.Center(table));
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine($"[grey]{FormatShortcut("R", "grey")} Re-roll  {FormatShortcut("N", "grey")} New Roll  {FormatShortcut("B", "grey")} Back[/]");

            var key = ReadKey();
            switch (GetKeyChar(key))
            {
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
            AnsiConsole.MarkupLine("[red]No element tables found in data/elements/[/]");
            WaitForKey();
            return;
        }

        RenderHeader("Element Tables");

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

        AnsiConsole.MarkupLine("[bold cyan]Select first table:[/]");
        var table1 = AnsiConsole.Prompt(
            new SelectionPrompt<TableInfo>()
                .HighlightStyle(new Style(MythicUi.AccentColor))
                .PageSize(12)
                .EnableSearch()
                .SearchPlaceholderText("[grey]Type to search...[/]")
                .AddChoices(allTables)
                .UseConverter(t => t.IsElement ? $"[cyan]{t.Category}[/] > {t.DisplayName}" : $"[yellow]Core[/] > {t.DisplayName}")
        );

        RenderHeader("Fusion Roll");

        AnsiConsole.MarkupLine($"[grey]First table:[/] [gold1]{table1.DisplayName}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold cyan]Select second table:[/]");

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

    private void ShowNpcProfile()
    {
        while (true)
        {
            RenderHeader("NPC Profile Generator");

            var profile = MeaningEngine.GenerateNpcProfile();

            // Log NPC profile generation
            var profileSummary = string.Join(", ", profile.Attributes.Select(a => $"{a.Key}: {a.Value.Combined}"));
            HistoryService.AddEntry(
                LogType.Meaning,
                "NPC Profile Generated",
                null,
                profileSummary
            );
            CampaignService.Save();

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(MythicUi.AccentColor)
                .Title("[bold gold1]Complete NPC Profile[/]")
                .AddColumn(new TableColumn("[bold cyan]Attribute[/]").Width(14))
                .AddColumn(new TableColumn("[bold cyan]Result[/]"));

            foreach (var (attribute, meaning) in profile.Attributes)
            {
                table.AddRow(
                    $"[yellow]{attribute}[/]",
                    $"[white]{meaning.Combined}[/]"
                );
            }

            AnsiConsole.Write(Align.Center(table));
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine($"[grey]{FormatShortcut("R", "grey")} Generate New NPC  {FormatShortcut("B", "grey")} Back[/]");

            var key = ReadKey();
            switch (GetKeyChar(key))
            {
                case 'R':
                    continue;
                default:
                    return;
            }
        }
    }
}
