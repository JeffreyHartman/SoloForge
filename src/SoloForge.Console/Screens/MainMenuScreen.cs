using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Rendering;
using SoloForge.Console.Core;
using SoloForge.Console.Services;
using SoloForge.Console.UI;

namespace SoloForge.Console.Screens;

/// <summary>
/// Main menu screen with navigation to all features.
/// </summary>
public class MainMenuScreen(
    Session session,
    AdventureStateManager stateManager,
    HistoryService historyService,
    CampaignService campaignService,
    JournalService journalService,
    IServiceProvider serviceProvider)
    : BaseScreen(session, stateManager, historyService, campaignService, journalService)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public override IScreen? Run()
    {
        while (true)
        {
            RenderMainMenu();

            var key = ReadKey();
            if (JournalService.Focus == JournalFocus.Journal)
                continue;
            var shouldExit = HandleInput(key);

            if (shouldExit)
                return null;
        }
    }

    private void RenderMainMenu()
    {
        var footer = "[grey]Press a highlighted key or number to select an option | [yellow]+[/]/[yellow]-[/] Chaos[/]";
        var container = Align.Center(BuildContentContainer(), VerticalAlignment.Middle);

        RenderSplit(container, "SoloForge", footer);
    }

    private IRenderable BuildContentContainer()
    {
        var sessionPanel = BuildSessionPanel();
        var menuPanel = BuildMenuPanel();
        var columns = new Columns(sessionPanel, menuPanel);

        var container = new Panel(columns)
            .Header("[bold yellow]SoloForge[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Rounded)
            .BorderColor(MythicUi.AccentColor)
            .Padding(2, 0);
        container.Width = MythicUi.MainMenuContainerWidth;

        return container;
    }

    private Panel BuildSessionPanel()
    {
        var campaignName = CampaignService.CurrentCampaign?.Name ?? "No Campaign";

        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn("Label").PadRight(1))
            .AddColumn(new TableColumn("Value").PadLeft(1));

        table.AddRow("[grey]Campaign:[/]", $"[gold1]{campaignName}[/]");
        table.AddRow("[grey]Engine:[/]", $"[white]{Session.Engine}[/]");
        table.AddRow("[grey]Theme:[/]", $"[white]{Session.Theme}[/]");
        table.AddRow("[grey]Chaos:[/]", $"[white]{Session.Chaos}[/]");
        table.AddRow("[grey]───────────[/]", "");
        table.AddRow("", "");
        table.AddRow("[grey]Characters:[/]", $"[aqua]{StateManager.CharacterCount}[/]");
        table.AddRow("[grey]Threads:[/]", $"[aqua]{StateManager.ActiveThreadCount}[/]");
        table.AddRow("", "");
        table.AddRow("", "");
        table.AddRow("", "");
        table.AddRow("", "");

        var panel = new Panel(table)
            .Header("[bold cyan]Session[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Rounded)
            .BorderColor(MythicUi.PrimaryColor)
            .Padding(1, 0);
        panel.Width = MythicUi.SessionPanelWidth;
        return panel;
    }

    private Panel BuildMenuPanel()
    {
        var lines = new List<string>
        {
            $"[grey]1[/] {FormatShortcut("F", "bold green")} Fate Check",
            $"[grey]2[/] {FormatShortcut("R", "bold green")} Random Event",
            $"[grey]3[/] {FormatShortcut("C", "bold green")} Scene Check",
            $"[grey]4[/] {FormatShortcut("M", "bold green")} Discovering Meaning",
            $"[grey]5[/] {FormatShortcut("L", "bold green")} Adventure Lists",
            $"[grey]6[/] {FormatShortcut("D", "bold green")} Dice Roller",
            "[grey]───────────────────────[/]",
            $"[grey]7[/] {FormatShortcut("G", "bold cyan")} Game Manager",
            $"[grey]8[/] {FormatShortcut("J", "bold cyan")} Journal",
            $"  {FormatShortcut("S", "bold yellow")} Settings",
            $"  {FormatShortcut("Q", "bold red")} Quit"
        };

        var menuContent = new Markup(string.Join("\n", lines));

        var panel = new Panel(menuContent)
            .Header("[bold cyan]Main Menu[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Rounded)
            .BorderColor(MythicUi.PrimaryColor)
            .Padding(1, 0);
        panel.Width = MythicUi.MenuPanelWidth;
        return panel;
    }

    /// <summary>
    /// Handles input and runs subscreens. Returns true if the app should exit.
    /// </summary>
    private bool HandleInput(ConsoleKeyInfo key)
    {
        // Handle chaos factor adjustments
        if (key.Key == ConsoleKey.UpArrow || key.KeyChar == '+' || key.KeyChar == '=')
        {
            Session.Chaos++;
            return false;
        }

        if (key.Key == ConsoleKey.DownArrow || key.KeyChar == '-' || key.KeyChar == '_')
        {
            Session.Chaos--;
            return false;
        }

        var keyChar = GetKeyChar(key);

        switch (keyChar)
        {
            case 'F' or '1':
                _serviceProvider.GetRequiredService<FateCheckScreen>().Run();
                return false;
            case 'R' or '2':
                _serviceProvider.GetRequiredService<RandomEventScreen>().Run();
                return false;
            case 'C' or '3':
                _serviceProvider.GetRequiredService<SceneCheckScreen>().Run();
                return false;
            case 'M' or '4':
                _serviceProvider.GetRequiredService<MeaningScreen>().Run();
                return false;
            case 'L' or '5':
                _serviceProvider.GetRequiredService<AdventureListScreen>().Run();
                return false;
            case 'D' or '6':
                _serviceProvider.GetRequiredService<DiceRollScreen>().Run();
                return false;
            case 'G' or '7':
                _serviceProvider.GetRequiredService<GameManagerScreen>().Run();
                return false;
            case 'J' or '8':
                _serviceProvider.GetRequiredService<HistoryScreen>().Run();
                return false;
            case 'S':
                ShowNotImplemented("Settings");
                return false;
            case 'Q':
                return ConfirmQuit();
            default:
                return false;
        }
    }

    private void ShowNotImplemented(string feature)
    {
        MythicUi.Clear();
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[yellow]{feature}[/] [grey]is not implemented yet[/]");
        WaitForKey();
    }

    private bool ConfirmQuit()
    {
        MythicUi.Clear();
        AnsiConsole.WriteLine();
        return AnsiConsole.Confirm("[yellow]Are you sure you want to quit?[/]", defaultValue: false);
    }
}
