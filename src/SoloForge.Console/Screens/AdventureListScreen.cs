using Spectre.Console;
using Spectre.Console.Rendering;
using SoloForge.Console.Core;
using SoloForge.Console.Models;
using SoloForge.Console.Services;
using SoloForge.Console.UI;

namespace SoloForge.Console.Screens;

/// <summary>
/// Screen for managing adventure lists (characters and threads).
/// </summary>
public class AdventureListScreen(
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
            RenderHeader("Adventure Lists");

            // Build character list panel
            var characterLines = new List<string>();
            if (StateManager.CharacterCount == 0)
            {
                characterLines.Add("[grey italic]No characters yet[/]");
            }
            else
            {
                for (var i = 0; i < StateManager.State.Characters.Count; i++)
                {
                    var character = StateManager.State.Characters[i];
                    characterLines.Add($"[aqua]{i + 1}.[/] [white]{character.Name}[/]");
                }
            }

            var charactersPanel = new Panel(new Markup(string.Join("\n", characterLines)))
                .Header("[bold aqua]Characters[/]")
                .HeaderAlignment(Justify.Center)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Aqua)
                .Padding(1, 0);
            charactersPanel.Width = MythicUi.ListColumnWidth;

            // Build threads list panel
            var threadLines = new List<string>();
            if (StateManager.ActiveThreadCount == 0)
            {
                threadLines.Add("[grey italic]No active threads[/]");
            }
            else
            {
                for (var i = 0; i < StateManager.State.ActiveThreads.Count; i++)
                {
                    var thread = StateManager.State.ActiveThreads[i];
                    threadLines.Add($"[aqua]{i + 1}.[/] [white]{thread.Name}[/]");
                }
            }

            var threadsPanel = new Panel(new Markup(string.Join("\n", threadLines)))
                .Header("[bold aqua]Active Threads[/]")
                .HeaderAlignment(Justify.Center)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Aqua)
                .Padding(1, 0);
            threadsPanel.Width = MythicUi.ListColumnWidth;

            // Use Grid for stable side-by-side layout
            var listsGrid = new Grid()
                .AddColumn(new GridColumn().Width(MythicUi.ListColumnWidth))
                .AddColumn(new GridColumn().Width(2))
                .AddColumn(new GridColumn().Width(MythicUi.ListColumnWidth));
            listsGrid.AddRow(charactersPanel, new Text(""), threadsPanel);

            var content = new List<IRenderable>
            {
                listsGrid
            };

            if (StateManager.ClosedThreadCount > 0)
            {
                content.Add(new Text(""));
                content.Add(new Markup($"[grey]Closed threads: {StateManager.ClosedThreadCount}[/]"));
            }

            var menuPanel = new Panel(
                new Markup(string.Join("\n", [
                    $"{FormatShortcut("C")} Add Character      {FormatShortcut("1-9")} View Character",
                    $"{FormatShortcut("T")} Add Thread         {FormatShortcut("!", "bold cyan")}{FormatShortcut("1-9", "bold cyan")} View Thread",
                    $"{FormatShortcut("E")} Edit Character",
                    $"{FormatShortcut("H")} Edit Thread",
                    $"{FormatShortcut("V")} View Closed Threads",
                    "[grey]─────────────────────────────────────────[/]",
                    $"{FormatShortcut("B", "bold yellow")} Back to Main Menu"
                ]))
            )
            .Header("[bold cyan]Options[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Rounded)
            .BorderColor(MythicUi.PrimaryColor)
            .Padding(1, 0);

            content.Add(new Text(""));
            content.Add(menuPanel);

            RenderSplit(new Rows(content), "Adventure Lists");

            var key = ReadKey();
            if (JournalService.Focus == JournalFocus.Journal)
                continue;
            var keyChar = GetKeyChar(key);

            // Handle number keys for viewing characters (1-9)
            if (char.IsDigit(key.KeyChar) && key.KeyChar != '0')
            {
                var index = key.KeyChar - '1';
                if (index < StateManager.CharacterCount)
                {
                    ShowCharacterDetail(StateManager.State.Characters[index]);
                }
                continue;
            }

            // Handle !1-!9 (Shift+Number) for viewing threads
            const string symbols = "!@#$%^&*(";
            if (symbols.Contains(key.KeyChar))
            {
                int index = symbols.IndexOf(key.KeyChar);
                if (index < StateManager.State.ActiveThreads.Count)
                {
                    ShowThreadDetail(StateManager.State.ActiveThreads[index]);
                }
                continue;
            }

            switch (keyChar)
            {
                case 'C':
                    AddCharacterPrompt();
                    break;
                case 'T':
                    AddThreadPrompt();
                    break;
                case 'E':
                    EditCharacterPrompt();
                    break;
                case 'H':
                    EditThreadPrompt();
                    break;
                case 'V':
                    ViewClosedThreads();
                    break;
                case 'B':
                case 'Q':
                    return null;
            }
        }
    }

    private void ShowCharacterDetail(Character character)
    {
        RenderHeader("Character Detail");

        var detailTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Aqua)
            .Title($"[bold aqua]{character.Name}[/]")
            .AddColumn(new TableColumn("[cyan]Field[/]").Width(14))
            .AddColumn(new TableColumn("[cyan]Value[/]").Width(30));

        detailTable.AddRow("[grey]Name[/]", $"[white]{character.Name}[/]");
        detailTable.AddRow("[grey]Description[/]", string.IsNullOrEmpty(character.Description)
            ? "[grey italic]None[/]"
            : $"[white]{character.Description}[/]");
        detailTable.AddRow("[grey]Created[/]", $"[grey]{character.CreatedAt:MMM dd, yyyy}[/]");

        AnsiConsole.Write(Align.Center(detailTable));
        AnsiConsole.WriteLine();

        AnsiConsole.Write(Align.Center(new Markup($"[grey]{FormatShortcut("E", "grey")} Edit  {FormatShortcut("D", "grey")} Delete  {FormatShortcut("B", "grey")} Back[/]")));

        var key = ReadKey();
        switch (GetKeyChar(key))
        {
            case 'E':
                EditCharacterInline(character);
                break;
            case 'D':
                if (AnsiConsole.Confirm($"[red]Delete {character.Name}?[/]", defaultValue: false))
                {
                    StateManager.RemoveCharacter(character);
                    CampaignService.Save();
                    AnsiConsole.MarkupLine($"[red]Deleted:[/] {character.Name}");
                    Thread.Sleep(600);
                }
                break;
        }
    }

    private void EditCharacterInline(Character character)
    {
        AnsiConsole.WriteLine();
        var newName = AnsiConsole.Prompt(
            new TextPrompt<string>("[bold cyan]Name:[/]")
                .DefaultValue(character.Name)
                .PromptStyle("white")
        );
        character.Name = newName;

        var newDesc = AnsiConsole.Prompt(
            new TextPrompt<string>("[bold cyan]Description:[/]")
                .DefaultValue(character.Description ?? "")
                .PromptStyle("white")
                .AllowEmpty()
        );
        character.Description = string.IsNullOrWhiteSpace(newDesc) ? null : newDesc;

        CampaignService.Save();
        AnsiConsole.MarkupLine("[green]Character updated.[/]");
        Thread.Sleep(600);
    }

    private void ShowThreadDetail(PlotThread thread)
    {
        RenderHeader("Thread Detail");

        var detailTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Aqua)
            .Title($"[bold aqua]{thread.Name}[/]")
            .AddColumn(new TableColumn("[cyan]Field[/]").Width(14))
            .AddColumn(new TableColumn("[cyan]Value[/]").Width(30));

        detailTable.AddRow("[grey]Name[/]", $"[white]{thread.Name}[/]");
        detailTable.AddRow("[grey]Description[/]", string.IsNullOrEmpty(thread.Description)
            ? "[grey italic]None[/]"
            : $"[white]{thread.Description}[/]");
        detailTable.AddRow("[grey]Created[/]", $"[grey]{thread.CreatedAt:MMM dd, yyyy}[/]");
        detailTable.AddRow("[grey]Status[/]", thread.IsClosed
            ? $"[yellow]Closed {thread.ClosedAt:MMM dd, yyyy}[/]"
            : "[green]Active[/]");

        AnsiConsole.Write(Align.Center(detailTable));
        AnsiConsole.WriteLine();

        AnsiConsole.Write(Align.Center(new Markup($"[grey]{FormatShortcut("E", "grey")} Edit  {FormatShortcut("X", "grey")} Close Thread  {FormatShortcut("D", "grey")} Delete  {FormatShortcut("B", "grey")} Back[/]")));

        var key = ReadKey();
        switch (GetKeyChar(key))
        {
            case 'E':
                EditThreadInline(thread);
                break;
            case 'X':
                StateManager.CloseThread(thread);
                CampaignService.Save();
                AnsiConsole.MarkupLine($"[yellow]Closed:[/] {thread.Name}");
                Thread.Sleep(600);
                break;
            case 'D':
                if (AnsiConsole.Confirm($"[red]Delete {thread.Name}?[/]", defaultValue: false))
                {
                    StateManager.RemoveThread(thread);
                    CampaignService.Save();
                    AnsiConsole.MarkupLine($"[red]Deleted:[/] {thread.Name}");
                    Thread.Sleep(600);
                }
                break;
        }
    }

    private void EditThreadInline(PlotThread thread)
    {
        AnsiConsole.WriteLine();
        var newName = AnsiConsole.Prompt(
            new TextPrompt<string>("[bold cyan]Name:[/]")
                .DefaultValue(thread.Name)
                .PromptStyle("white")
        );
        thread.Name = newName;

        var newDesc = AnsiConsole.Prompt(
            new TextPrompt<string>("[bold cyan]Description:[/]")
                .DefaultValue(thread.Description ?? "")
                .PromptStyle("white")
                .AllowEmpty()
        );
        thread.Description = string.IsNullOrWhiteSpace(newDesc) ? null : newDesc;

        CampaignService.Save();
        AnsiConsole.MarkupLine("[green]Thread updated.[/]");
        Thread.Sleep(600);
    }

    private void AddCharacterPrompt()
    {
        AnsiConsole.WriteLine();
        var name = AnsiConsole.Prompt(
            new TextPrompt<string>("[bold cyan]Character name:[/]")
                .PromptStyle("white")
        );

        if (string.IsNullOrWhiteSpace(name)) return;

        var description = AnsiConsole.Prompt(
            new TextPrompt<string>("[grey]Description (optional):[/]")
                .PromptStyle("white")
                .AllowEmpty()
        );

        StateManager.AddCharacter(name, string.IsNullOrWhiteSpace(description) ? null : description);
        CampaignService.Save();
        AnsiConsole.MarkupLine($"[green]Added:[/] [aqua]{name}[/]");
        Thread.Sleep(600);
    }

    private void AddThreadPrompt()
    {
        AnsiConsole.WriteLine();
        var name = AnsiConsole.Prompt(
            new TextPrompt<string>("[bold cyan]Thread name:[/]")
                .PromptStyle("white")
        );

        if (string.IsNullOrWhiteSpace(name)) return;

        var description = AnsiConsole.Prompt(
            new TextPrompt<string>("[grey]Description (optional):[/]")
                .PromptStyle("white")
                .AllowEmpty()
        );

        StateManager.AddThread(name, string.IsNullOrWhiteSpace(description) ? null : description);
        CampaignService.Save();
        AnsiConsole.MarkupLine($"[green]Added:[/] [aqua]{name}[/]");
        Thread.Sleep(600);
    }

    private void EditCharacterPrompt()
    {
        if (StateManager.CharacterCount == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No characters to edit.[/]");
            Thread.Sleep(800);
            return;
        }

        AnsiConsole.WriteLine();
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<Character>()
                .Title("[bold cyan]Select character:[/]")
                .HighlightStyle(new Style(MythicUi.AccentColor))
                .AddChoices(StateManager.State.Characters)
                .UseConverter(c => c.DisplayName)
        );

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold cyan]Action for[/] [aqua]{selected.Name}[/][bold cyan]:[/]")
                .HighlightStyle(new Style(MythicUi.AccentColor))
                .AddChoices(["Rename", "Edit Description", "Delete", "Cancel"])
        );

        switch (action)
        {
            case "Rename":
                var newName = AnsiConsole.Prompt(
                    new TextPrompt<string>("[bold cyan]New name:[/]")
                        .DefaultValue(selected.Name)
                        .PromptStyle("white")
                );
                selected.Name = newName;
                CampaignService.Save();
                AnsiConsole.MarkupLine($"[green]Renamed to:[/] [aqua]{newName}[/]");
                break;
            case "Edit Description":
                var newDesc = AnsiConsole.Prompt(
                    new TextPrompt<string>("[bold cyan]Description:[/]")
                        .DefaultValue(selected.Description ?? "")
                        .PromptStyle("white")
                        .AllowEmpty()
                );
                selected.Description = string.IsNullOrWhiteSpace(newDesc) ? null : newDesc;
                CampaignService.Save();
                AnsiConsole.MarkupLine("[green]Description updated.[/]");
                break;
            case "Delete":
                if (AnsiConsole.Confirm($"[red]Delete {selected.Name}?[/]", defaultValue: false))
                {
                    StateManager.RemoveCharacter(selected);
                    CampaignService.Save();
                    AnsiConsole.MarkupLine($"[red]Deleted:[/] {selected.Name}");
                }
                break;
        }
        Thread.Sleep(600);
    }

    private void EditThreadPrompt()
    {
        if (StateManager.ActiveThreadCount == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No active threads to edit.[/]");
            Thread.Sleep(800);
            return;
        }

        AnsiConsole.WriteLine();
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<PlotThread>()
                .Title("[bold cyan]Select thread:[/]")
                .HighlightStyle(new Style(MythicUi.AccentColor))
                .AddChoices(StateManager.State.ActiveThreads)
                .UseConverter(t => t.DisplayName)
        );

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold cyan]Action for[/] [aqua]{selected.Name}[/][bold cyan]:[/]")
                .HighlightStyle(new Style(MythicUi.AccentColor))
                .AddChoices(["Rename", "Edit Description", "Close Thread", "Delete", "Cancel"])
        );

        switch (action)
        {
            case "Rename":
                var newName = AnsiConsole.Prompt(
                    new TextPrompt<string>("[bold cyan]New name:[/]")
                        .DefaultValue(selected.Name)
                        .PromptStyle("white")
                );
                selected.Name = newName;
                CampaignService.Save();
                AnsiConsole.MarkupLine($"[green]Renamed to:[/] [aqua]{newName}[/]");
                break;
            case "Edit Description":
                var newDesc = AnsiConsole.Prompt(
                    new TextPrompt<string>("[bold cyan]Description:[/]")
                        .DefaultValue(selected.Description ?? "")
                        .PromptStyle("white")
                        .AllowEmpty()
                );
                selected.Description = string.IsNullOrWhiteSpace(newDesc) ? null : newDesc;
                CampaignService.Save();
                AnsiConsole.MarkupLine("[green]Description updated.[/]");
                break;
            case "Close Thread":
                StateManager.CloseThread(selected);
                CampaignService.Save();
                AnsiConsole.MarkupLine($"[yellow]Closed:[/] {selected.Name}");
                break;
            case "Delete":
                if (AnsiConsole.Confirm($"[red]Delete {selected.Name}?[/]", defaultValue: false))
                {
                    StateManager.RemoveThread(selected);
                    CampaignService.Save();
                    AnsiConsole.MarkupLine($"[red]Deleted:[/] {selected.Name}");
                }
                break;
        }
        Thread.Sleep(600);
    }

    private void ViewClosedThreads()
    {
        RenderHeader("Closed Threads");

        if (StateManager.ClosedThreadCount == 0)
        {
            AnsiConsole.MarkupLine("[grey]No closed threads.[/]");
            WaitForKey();
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(MythicUi.MutedColor)
            .Title("[bold grey]Closed Threads[/]")
            .AddColumn(new TableColumn("[grey]Thread[/]").Width(30))
            .AddColumn(new TableColumn("[grey]Closed[/]").Width(20));

        foreach (var thread in StateManager.State.ClosedThreads)
        {
            var closedDate = thread.ClosedAt?.ToString("MMM dd, yyyy") ?? "Unknown";
            table.AddRow(
                $"[grey]{thread.Name}[/]",
                $"[grey]{closedDate}[/]"
            );
        }

        AnsiConsole.Write(Align.Center(table));
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine($"[grey]{FormatShortcut("R", "grey")} Reopen a Thread  {FormatShortcut("B", "grey")} Back[/]");

        var key = ReadKey();
        if (GetKeyChar(key) == 'R' && StateManager.ClosedThreadCount > 0)
        {
            var selected = AnsiConsole.Prompt(
                new SelectionPrompt<PlotThread>()
                    .Title("[bold cyan]Select thread to reopen:[/]")
                    .HighlightStyle(new Style(MythicUi.AccentColor))
                    .AddChoices(StateManager.State.ClosedThreads)
                    .UseConverter(t => t.Name)
            );

            StateManager.ReopenThread(selected);
            CampaignService.Save();
            AnsiConsole.MarkupLine($"[green]Reopened:[/] [aqua]{selected.Name}[/]");
            Thread.Sleep(600);
        }
    }
}
