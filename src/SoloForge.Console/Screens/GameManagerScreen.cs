using Spectre.Console;
using Spectre.Console.Rendering;
using SoloForge.Console.Core;
using SoloForge.Console.Models;
using SoloForge.Console.Services;
using SoloForge.Console.UI;

namespace SoloForge.Console.Screens;

/// <summary>
/// Screen for managing campaigns (create, switch, delete).
/// </summary>
public class GameManagerScreen(
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
            RenderHeader("Game Manager");

            var content = new List<IRenderable>();
            var current = CampaignService.CurrentCampaign;
            if (current != null)
            {
                var infoTable = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderColor(MythicUi.AccentColor)
                    .Title("[bold gold1]Current Campaign[/]")
                    .AddColumn(new TableColumn("[grey]Property[/]").Width(14))
                    .AddColumn(new TableColumn("[grey]Value[/]"));

                infoTable.AddRow("[grey]Name[/]", $"[gold1]{current.Name}[/]");
                infoTable.AddRow("[grey]Created[/]", $"[white]{current.CreatedAt:MMM d, yyyy}[/]");
                infoTable.AddRow("[grey]Last Played[/]", $"[white]{current.LastPlayed:MMM d, yyyy h:mm tt}[/]");
                infoTable.AddRow("[grey]Entries[/]", $"[aqua]{current.History.Count}[/]");

                content.Add(infoTable);
                content.Add(new Text(""));
            }

            var menuPanel = new Panel(
                new Markup(string.Join("\n", [
                    $"{FormatShortcut("N")} New Campaign",
                    $"{FormatShortcut("S")} Switch Campaign",
                    $"{FormatShortcut("D")} Delete Campaign",
                    "[grey]───────────────────────[/]",
                    $"{FormatShortcut("B", "bold yellow")} Back to Main Menu"
                ]))
            )
            .Header("[bold cyan]Options[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Rounded)
            .BorderColor(MythicUi.PrimaryColor)
            .Padding(1, 0);

            content.Add(menuPanel);

            RenderSplit(new Rows(content), "Game Manager");

            var key = ReadKey();
            if (JournalService.Focus == JournalFocus.Journal)
                continue;
            switch (GetKeyChar(key))
            {
                case 'N':
                    CreateNewCampaign();
                    break;
                case 'S':
                    SwitchCampaign();
                    break;
                case 'D':
                    DeleteCampaign();
                    break;
                case 'B':
                case 'Q':
                    return null;
            }
        }
    }

    private void CreateNewCampaign()
    {
        RenderHeader("New Campaign");

        var name = AnsiConsole.Prompt(
            new TextPrompt<string>("[bold cyan]Enter campaign name:[/]")
                .PromptStyle("white")
                .Validate(n => !string.IsNullOrWhiteSpace(n)
                    ? ValidationResult.Success()
                    : ValidationResult.Error("[red]Name cannot be empty[/]"))
        );

        CampaignService.CreateNew(name);

        AnsiConsole.MarkupLine($"\n[green]Created campaign:[/] [gold1]{name}[/]");
        Thread.Sleep(1000);
    }

    private void SwitchCampaign()
    {
        var campaigns = CampaignService.ListCampaigns().ToList();

        if (campaigns.Count == 0)
        {
            RenderHeader("Switch Campaign");
            AnsiConsole.MarkupLine("[yellow]No campaigns found.[/]");
            WaitForKey();
            return;
        }

        if (campaigns.Count == 1)
        {
            RenderHeader("Switch Campaign");
            AnsiConsole.MarkupLine("[yellow]Only one campaign exists. Create another to switch.[/]");
            WaitForKey();
            return;
        }

        RenderHeader("Switch Campaign");

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<CampaignData>()
                .Title("[bold cyan]Select a campaign:[/]")
                .HighlightStyle(new Style(MythicUi.AccentColor))
                .PageSize(10)
                .AddChoices(campaigns)
                .UseConverter(c =>
                {
                    var current = CampaignService.CurrentCampaign?.Id == c.Id ? " [green](current)[/]" : "";
                    return $"[gold1]{c.Name}[/]{current} [grey]- {c.LastPlayed:MMM d}[/]";
                })
        );

        if (selected.Id != CampaignService.CurrentCampaign?.Id)
        {
            CampaignService.Load(selected.Id);
            AnsiConsole.MarkupLine($"\n[green]Switched to:[/] [gold1]{selected.Name}[/]");
            Thread.Sleep(1000);
        }
    }

    private void DeleteCampaign()
    {
        var campaigns = CampaignService.ListCampaigns().ToList();

        if (campaigns.Count == 0)
        {
            RenderHeader("Delete Campaign");
            AnsiConsole.MarkupLine("[yellow]No campaigns found.[/]");
            WaitForKey();
            return;
        }

        if (campaigns.Count == 1)
        {
            RenderHeader("Delete Campaign");
            AnsiConsole.MarkupLine("[yellow]Cannot delete the only campaign. Create another first.[/]");
            WaitForKey();
            return;
        }

        RenderHeader("Delete Campaign");

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<CampaignData>()
                .Title("[bold cyan]Select a campaign to delete:[/]")
                .HighlightStyle(new Style(MythicUi.ErrorColor))
                .PageSize(10)
                .AddChoices(campaigns)
                .UseConverter(c =>
                {
                    var current = CampaignService.CurrentCampaign?.Id == c.Id ? " [red](current)[/]" : "";
                    return $"[gold1]{c.Name}[/]{current} [grey]- {c.LastPlayed:MMM d}[/]";
                })
        );

        var confirm = AnsiConsole.Confirm(
            $"[red]Delete campaign \"{selected.Name}\"? This cannot be undone.[/]",
            defaultValue: false
        );

        if (confirm)
        {
            var wasCurrentCampaign = selected.Id == CampaignService.CurrentCampaign?.Id;
            CampaignService.Delete(selected.Id);

            AnsiConsole.MarkupLine($"\n[red]Deleted campaign:[/] [grey]{selected.Name}[/]");

            if (wasCurrentCampaign)
            {
                // Load another campaign or create default
                var remaining = CampaignService.ListCampaigns().FirstOrDefault();
                if (remaining != null)
                {
                    CampaignService.Load(remaining.Id);
                    AnsiConsole.MarkupLine($"[green]Switched to:[/] [gold1]{remaining.Name}[/]");
                }
                else
                {
                    CampaignService.CreateNew("Default Campaign");
                    AnsiConsole.MarkupLine("[green]Created new default campaign[/]");
                }
            }

            Thread.Sleep(1000);
        }
    }
}
