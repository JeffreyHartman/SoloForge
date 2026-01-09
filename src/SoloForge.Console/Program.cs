using Spectre.Console;
using Spectre.Console.Rendering;
using SoloForge.Console.Engines.Mythic2e;
using SoloForge.Console.Models;
using SoloForge.Console.Services;

var app = new SoloForgeApp();
app.Run();

// === UI Constants ===
static class UiConstants
{
    public const int MinPanelWidth = 36;
    public const int ResultPanelWidth = 40;
    public const int MainMenuContainerWidth = 72;
    public const int SessionPanelWidth = 28;
    public const int MenuPanelWidth = 36;
    public const int ListColumnWidth = 30;
}

// === Data Structures ===

record MenuItem(
    string Label,
    char Hotkey,
    int? NumberKey,
    string HotkeyColor,
    Func<SoloForgeApp, bool> Action,
    bool ShowSeparatorBefore = false
);

class Session
{
    public string Engine { get; set; } = "Mythic 2e";
    public string Theme { get; set; } = "Fantasy";

    public int Chaos
    {
        get;
        set => field = Math.Clamp(value, 1, 9);
    } = 5;
}

// === Main Application ===

class SoloForgeApp
{
    public Session Session { get; } = new();

    private readonly List<MenuItem> _menuItems =
    [
        new("Fate Check", 'F', 1, "green", app => app.ShowFateCheck()),
        new("Random Event", 'R', 2, "green", app => app.ShowRandomEvent()),
        new("Scene Check", 'C', 3, "green", app => app.ShowSceneCheck()),
        new("Discovering Meaning", 'M', 4, "green", app => app.ShowDiscoveringMeaning()),
        new("Adventure Lists", 'L', 5, "green", app => app.ShowAdventureLists()),
        new("Dice Roller", 'D', 6, "green", app => app.ShowNotImplemented("Dice Roller")),
        new("Settings", 'S', null, "yellow", app => app.ShowNotImplemented("Settings"), ShowSeparatorBefore: true),
        new("Quit", 'Q', null, "red", app => !app.ConfirmQuit())
    ];

    public void Run()
    {
        var running = true;
        while (running)
        {
            AnsiConsole.Clear();
            RenderMainMenu();

            var key = Console.ReadKey(intercept: true);
            running = HandleInput(key);
        }
    }

    // === UI Building Methods ===

    private void RenderMainMenu()
    {
        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Title").Size(8),
                new Layout("Content"),
                new Layout("Footer").Size(2)
            );

        var title = new FigletText("SoloForge")
            .Color(Color.Gold1);
        layout["Title"].Update(Align.Center(title, VerticalAlignment.Middle));

        var container = BuildContentContainer();
        layout["Content"].Update(Align.Center(container, VerticalAlignment.Middle));

        var footer = new Markup("[grey]Press a highlighted key or number to select an option | [yellow]+[/]/[yellow]-[/] Chaos[/]");
        layout["Footer"].Update(Align.Center(footer, VerticalAlignment.Top));

        AnsiConsole.Write(layout);
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
            .BorderColor(Color.Gold1)
            .Padding(2, 0);
        container.Width = UiConstants.MainMenuContainerWidth;

        return container;
    }

    private Panel BuildSessionPanel()
    {
        var state = AdventureStateManager.Instance;
        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn("Label").PadRight(1))
            .AddColumn(new TableColumn("Value"));

        table.AddRow("[grey]Engine:[/]", $"[white]{Session.Engine}[/]");
        table.AddRow("[grey]Theme:[/]", $"[white]{Session.Theme}[/]");
        table.AddRow("[grey]Chaos:[/]", $"[white]{Session.Chaos}[/]");
        table.AddRow("[grey]───────────[/]", "");
        table.AddRow("[grey]Characters:[/]", $"[aqua]{state.CharacterCount}[/]");
        table.AddRow("[grey]Threads:[/]", $"[aqua]{state.ActiveThreadCount}[/]");
        // Padding rows to match menu panel height
        table.AddRow("", "");
        table.AddRow("", "");
        table.AddRow("", "");

        var panel = new Panel(table)
            .Header("[bold cyan]Session[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Cyan1)
            .Padding(1, 0);
        panel.Width = UiConstants.SessionPanelWidth;
        return panel;
    }

    private Panel BuildMenuPanel()
    {
        var lines = new List<string>();

        foreach (var item in _menuItems)
        {
            if (item.ShowSeparatorBefore)
            {
                lines.Add("[grey]───────────────────────[/]");
            }

            var numberPrefix = item.NumberKey.HasValue
                ? $"[grey]{item.NumberKey}[/] "
                : "  ";

            lines.Add($"{numberPrefix}{FormatShortcut(item.Hotkey.ToString(), $"bold {item.HotkeyColor}")} {item.Label}");
        }

        var menuContent = new Markup(string.Join("\n", lines));

        var panel = new Panel(menuContent)
            .Header("[bold cyan]Main Menu[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Cyan1)
            .Padding(1, 0);
        panel.Width = UiConstants.MenuPanelWidth;
        return panel;
    }

    // === Input Handling ===

    private bool HandleInput(ConsoleKeyInfo key)
    {
        // Handle chaos factor adjustments
        if (key.Key == ConsoleKey.UpArrow || key.KeyChar == '+' || key.KeyChar == '=')
        {
            Session.Chaos++;
            return true;
        }

        if (key.Key == ConsoleKey.DownArrow || key.KeyChar == '-' || key.KeyChar == '_')
        {
            Session.Chaos--;
            return true;
        }

        var keyChar = char.ToUpperInvariant(key.KeyChar);

        // Find matching menu item by hotkey or number key
        var menuItem = _menuItems.FirstOrDefault(m =>
            m.Hotkey == keyChar ||
            (m.NumberKey.HasValue && m.NumberKey.Value.ToString()[0] == keyChar));

        return menuItem?.Action(this) ?? true;
    }

    public bool ShowFateCheck()
    {
        AnsiConsole.Clear();
        RenderSessionHeader("Fate Check");

        // Prompt user to select odds
        var selectedOdds = AnsiConsole.Prompt(
            new SelectionPrompt<Odds>()
                .Title("[bold cyan]Select Odds:[/]")
                .HighlightStyle(new Style(Color.Gold1))
                .AddChoices(Enum.GetValues<Odds>())
                .UseConverter(odds => odds.GetDisplayName())
        );

        // Perform the fate check
        var result = FateCheck.PerformCheck(Session.Chaos, selectedOdds);

        // Display results
        AnsiConsole.Clear();
        RenderSessionHeader("Fate Check");

        // Color-code the result
        string resultColor = result.Result.Contains("Yes") ? "green" : "red";

        var resultPanel = new Panel(
            new Align(
                new Markup($"[bold {resultColor}]{result.Result}[/]"), 
                HorizontalAlignment.Center
            )
        )
        .Header("[bold cyan]Result[/]")
        .HeaderAlignment(Justify.Center)
        .Border(BoxBorder.Double)
        .BorderColor(resultColor == "green" ? Color.Green : Color.Red)
        .Padding(2, 1);
        resultPanel.Width = UiConstants.ResultPanelWidth;

        AnsiConsole.Write(Align.Center(resultPanel));
        AnsiConsole.WriteLine();

        var detailsTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Cyan1)
            .AddColumn(new TableColumn("[bold cyan]Detail[/]").Centered().Width(16))
            .AddColumn(new TableColumn("[bold cyan]Value[/]").Centered().Width(18));

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
            .BorderColor(Color.Yellow)
            .Padding(2, 1);
            eventPanel.Width = UiConstants.ResultPanelWidth;

            AnsiConsole.Write(Align.Center(eventPanel));
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
        return true;
    }

    public bool ShowRandomEvent()
    {
        while (true)
        {
            AnsiConsole.Clear();
            RenderSessionHeader("Random Event");

            var result = RandomEvent.Generate();

            // Build focus text with optional character/thread
            var focusText = result.EventFocus;
            if (result.SelectedCharacter != null)
            {
                focusText += $"\n[aqua]{result.SelectedCharacter}[/]";
            }
            else if (result.SelectedThread != null)
            {
                focusText += $"\n[aqua]{result.SelectedThread}[/]";
            }
            else if (result.ListWasEmpty)
            {
                var listType = RandomEvent.IsNpcFocus(result.EventFocus) ? "No characters" : "No threads";
                focusText += $"\n[grey italic]({listType} in list)[/]";
            }

            var focusPanel = new Panel(
                new Align(
                    new Markup($"[bold cyan]{focusText}[/]"),
                    HorizontalAlignment.Center
                )
            )
            .Header("[bold yellow]Event Focus[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Yellow)
            .Padding(2, 1);
            focusPanel.Width = UiConstants.ResultPanelWidth;

            AnsiConsole.Write(Align.Center(focusPanel));
            AnsiConsole.WriteLine();

            var actionPanel = new Panel(
                new Align(
                    new Markup($"[bold gold1]{result.EventAction}[/]"),
                    HorizontalAlignment.Center
                )
            )
            .Header("[bold yellow]Event Action[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Double)
            .BorderColor(Color.Gold1)
            .Padding(2, 1);
            actionPanel.Width = UiConstants.ResultPanelWidth;

            AnsiConsole.Write(Align.Center(actionPanel));
            AnsiConsole.WriteLine();

            // Build options string
            var options = $"[grey]{FormatShortcut("R", "grey")} Re-roll  {FormatShortcut("B", "grey")} Back";
            if (result.IsNewNpc)
            {
                options += $"  {FormatShortcut("A", "grey")} Add NPC";
            }
            options += "[/]";
            AnsiConsole.MarkupLine(options);

            var key = Console.ReadKey(intercept: true);
            switch (char.ToUpperInvariant(key.KeyChar))
            {
                case 'R':
                    continue;
                case 'A' when result.IsNewNpc:
                    PromptAddCharacter();
                    continue;
                default:
                    return true;
            }
        }
    }

    private void PromptAddCharacter()
    {
        AnsiConsole.WriteLine();
        var name = AnsiConsole.Prompt(
            new TextPrompt<string>("[bold cyan]Enter character name:[/]")
                .PromptStyle("white")
        );

        if (!string.IsNullOrWhiteSpace(name))
        {
            var description = AnsiConsole.Prompt(
                new TextPrompt<string>("[grey]Description (optional):[/]")
                    .PromptStyle("white")
                    .AllowEmpty()
            );

            AdventureStateManager.Instance.AddCharacter(name, string.IsNullOrWhiteSpace(description) ? null : description);
            AnsiConsole.MarkupLine($"[green]Added character:[/] [aqua]{name}[/]");
            Thread.Sleep(800);
        }
    }

    public bool ShowSceneCheck()
    {
        AnsiConsole.Clear();
        RenderSessionHeader("Scene Check");

        var result = SceneCheck.PerformCheck(Session.Chaos);

        // Color-code the result
        var (resultColor, borderColor) = result.Result switch
        {
            "Normal Scene" => ("green", Color.Green),
            "Altered Scene!" => ("yellow", Color.Yellow),
            "Interrupt Scene!" => ("red", Color.Red),
            _ => ("white", Color.White)
        };

        var resultPanel = new Panel(
            new Align(
                new Markup($"[bold {resultColor}]{result.Result}[/]"),
                HorizontalAlignment.Center
            )
        )
        .Header("[bold cyan]Result[/]")
        .HeaderAlignment(Justify.Center)
        .Border(BoxBorder.Double)
        .BorderColor(borderColor)
        .Padding(2, 1);
        resultPanel.Width = UiConstants.ResultPanelWidth;

        AnsiConsole.Write(Align.Center(resultPanel));
        AnsiConsole.WriteLine();

        var detailsTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Cyan1)
            .AddColumn(new TableColumn("[bold cyan]Detail[/]").Centered().Width(16))
            .AddColumn(new TableColumn("[bold cyan]Value[/]").Centered().Width(18));

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
            .BorderColor(Color.Yellow)
            .Padding(2, 1);
            adjustmentPanel.Width = UiConstants.ResultPanelWidth;

            AnsiConsole.Write(Align.Center(adjustmentPanel));
        }

        // Show random event if present
        if (result.RandomEvent != null)
        {
            AnsiConsole.WriteLine();

            var eventPanel = new Panel(
                new Align(
                    new Markup($"[bold gold1]{result.RandomEvent.EventAction}[/]"),
                    HorizontalAlignment.Center
                )
            )
            .Header($"[bold yellow]Random Event: {result.RandomEvent.EventFocus}[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Double)
            .BorderColor(Color.Yellow)
            .Padding(2, 1);
            eventPanel.Width = UiConstants.ResultPanelWidth;

            AnsiConsole.Write(Align.Center(eventPanel));
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
        return true;
    }

    public bool ShowDiscoveringMeaning()
    {
        while (true)
        {
            AnsiConsole.Clear();
            RenderSessionHeader("Discovering Meaning");

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
            .BorderColor(Color.Cyan1)
            .Padding(1, 0);

            AnsiConsole.Write(Align.Center(menuPanel));

            var key = Console.ReadKey(intercept: true);
            switch (char.ToUpperInvariant(key.KeyChar))
            {
                case 'A':
                    ShowMeaningResult(MeaningEngine.GenerateAction());
                    break;
                case 'D':
                    ShowMeaningResult(MeaningEngine.GenerateDescription());
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
                    return true;
            }
        }
    }

    private void RenderSessionHeader(string title)
    {
        // Optional figlet title for subpages
        if (SettingsService.Instance.Features.ShowSubpageTitles)
        {
            var figlet = new FigletText(title)
                .Color(Color.Gold1);
            AnsiConsole.Write(Align.Center(figlet));
        }

        var state = AdventureStateManager.Instance;
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Cyan1)
            .AddColumn(new TableColumn($"[bold cyan]{title}[/]").Centered())
            .AddColumn(new TableColumn($"[grey]Chaos:[/] [white]{Session.Chaos}[/]").Centered())
            .AddColumn(new TableColumn($"[grey]Characters:[/] [aqua]{state.CharacterCount}[/] [grey]|[/] [grey]Threads:[/] [aqua]{state.ActiveThreadCount}[/]").Centered());

        AnsiConsole.Write(Align.Center(table));
        AnsiConsole.WriteLine();
    }

    private void ShowMeaningResult(MeaningResult result, string? tableId1 = null, string? tableId2 = null)
    {
        while (true)
        {
            AnsiConsole.Clear();
            RenderSessionHeader("Meaning Result");

            var panel = new Panel(
                new Align(
                    new Markup($"[bold gold1]{result.Combined}[/]"),
                    HorizontalAlignment.Center
                )
            )
            .Header($"[bold cyan]{result.TableName}[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Double)
            .BorderColor(Color.Gold1)
            .Padding(2, 1);
            panel.Width = UiConstants.ResultPanelWidth;

            AnsiConsole.Write(Align.Center(panel));
            AnsiConsole.WriteLine();

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey)
                .HideHeaders()
                .AddColumn(new TableColumn("Label").Width(10))
                .AddColumn(new TableColumn("Value").Width(20));
            table.AddRow("[grey]Word 1:[/]", $"[white]{result.Word1}[/]");
            table.AddRow("[grey]Word 2:[/]", $"[white]{result.Word2}[/]");

            AnsiConsole.Write(Align.Center(table));
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine($"[grey]{FormatShortcut("R", "grey")} Re-roll  {FormatShortcut("N", "grey")} New Roll  {FormatShortcut("B", "grey")} Back[/]");

            var key = Console.ReadKey(intercept: true);
            switch (char.ToUpperInvariant(key.KeyChar))
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
            AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
            Console.ReadKey(intercept: true);
            return;
        }

        AnsiConsole.Clear();
        RenderSessionHeader("Element Tables");

        var selectedTable = AnsiConsole.Prompt(
            new SelectionPrompt<TableInfo>()
                .Title("[bold cyan]Select an element table:[/]")
                .HighlightStyle(new Style(Color.Gold1))
                .PageSize(15)
                .EnableSearch()
                .SearchPlaceholderText("[grey]Type to search...[/]")
                .AddChoices(tables)
                .UseConverter(t => $"[cyan]{t.Category}[/] > {t.DisplayName}")
        );

        var result = MeaningEngine.GenerateFromTable(selectedTable.Id, selectedTable.DisplayName);
        ShowMeaningResult(result, selectedTable.Id);
    }

    private void ShowFusionRoll()
    {
        var allTables = TableService.Instance.AvailableTables.ToList();

        AnsiConsole.Clear();
        RenderSessionHeader("Fusion Roll");

        AnsiConsole.MarkupLine("[bold cyan]Select first table:[/]");
        var table1 = AnsiConsole.Prompt(
            new SelectionPrompt<TableInfo>()
                .HighlightStyle(new Style(Color.Gold1))
                .PageSize(12)
                .EnableSearch()
                .SearchPlaceholderText("[grey]Type to search...[/]")
                .AddChoices(allTables)
                .UseConverter(t => t.IsElement ? $"[cyan]{t.Category}[/] > {t.DisplayName}" : $"[yellow]Core[/] > {t.DisplayName}")
        );

        AnsiConsole.Clear();
        RenderSessionHeader("Fusion Roll");

        AnsiConsole.MarkupLine($"[grey]First table:[/] [gold1]{table1.DisplayName}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold cyan]Select second table:[/]");

        var table2 = AnsiConsole.Prompt(
            new SelectionPrompt<TableInfo>()
                .HighlightStyle(new Style(Color.Gold1))
                .PageSize(12)
                .EnableSearch()
                .SearchPlaceholderText("[grey]Type to search...[/]")
                .AddChoices(allTables)
                .UseConverter(t => t.IsElement ? $"[cyan]{t.Category}[/] > {t.DisplayName}" : $"[yellow]Core[/] > {t.DisplayName}")
        );

        var result = MeaningEngine.GenerateFusion(table1.Id, table2.Id);
        ShowMeaningResult(result, table1.Id, table2.Id);
    }

    private void ShowNpcProfile()
    {
        while (true)
        {
            AnsiConsole.Clear();
            RenderSessionHeader("NPC Profile Generator");

            var profile = MeaningEngine.GenerateNpcProfile();

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Gold1)
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

            var key = Console.ReadKey(intercept: true);
            switch (char.ToUpperInvariant(key.KeyChar))
            {
                case 'R':
                    continue;
                default:
                    return;
            }
        }
    }

    public bool ShowAdventureLists()
    {
        while (true)
        {
            AnsiConsole.Clear();
            RenderSessionHeader("Adventure Lists");

            var state = AdventureStateManager.Instance;

            // Build character list panel
            var characterLines = new List<string>();
            if (state.CharacterCount == 0)
            {
                characterLines.Add("[grey italic]No characters yet[/]");
            }
            else
            {
                for (var i = 0; i < state.State.Characters.Count; i++)
                {
                    var character = state.State.Characters[i];
                    characterLines.Add($"[aqua]{i + 1}.[/] [white]{character.Name}[/]");
                }
            }

            var charactersPanel = new Panel(new Markup(string.Join("\n", characterLines)))
                .Header("[bold aqua]Characters[/]")
                .HeaderAlignment(Justify.Center)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Aqua)
                .Padding(1, 0);
            charactersPanel.Width = UiConstants.ListColumnWidth;

            // Build threads list panel
            var threadLines = new List<string>();
            if (state.ActiveThreadCount == 0)
            {
                threadLines.Add("[grey italic]No active threads[/]");
            }
            else
            {
                for (var i = 0; i < state.State.ActiveThreads.Count; i++)
                {
                    var thread = state.State.ActiveThreads[i];
                    threadLines.Add($"[aqua]{i + 1}.[/] [white]{thread.Name}[/]");
                }
            }

            var threadsPanel = new Panel(new Markup(string.Join("\n", threadLines)))
                .Header("[bold aqua]Active Threads[/]")
                .HeaderAlignment(Justify.Center)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Aqua)
                .Padding(1, 0);
            threadsPanel.Width = UiConstants.ListColumnWidth;

            // Use Grid for stable side-by-side layout
            var listsGrid = new Grid()
                .AddColumn(new GridColumn().Width(UiConstants.ListColumnWidth))
                .AddColumn(new GridColumn().Width(2)) // Spacer
                .AddColumn(new GridColumn().Width(UiConstants.ListColumnWidth));
            listsGrid.AddRow(charactersPanel, new Text(""), threadsPanel);

            AnsiConsole.Write(Align.Center(listsGrid));
            AnsiConsole.WriteLine();

            // Show closed threads count if any
            if (state.ClosedThreadCount > 0)
            {
                AnsiConsole.Write(Align.Center(new Markup($"[grey]Closed threads: {state.ClosedThreadCount}[/]")));
                AnsiConsole.WriteLine();
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
            .BorderColor(Color.Cyan1)
            .Padding(1, 0);

            AnsiConsole.Write(Align.Center(menuPanel));

            var key = Console.ReadKey(intercept: true);
            var keyChar = char.ToUpperInvariant(key.KeyChar);

            // Handle number keys for viewing characters (1-9)
            if (char.IsDigit(key.KeyChar) && key.KeyChar != '0')
            {
                var index = key.KeyChar - '1';
                if (index < state.CharacterCount)
                {
                    ShowCharacterDetail(state.State.Characters[index]);
                }
                continue;
            }

            // Handle !1-!9 for viewing threads
            if (key.KeyChar == '!')
            {
                var nextKey = Console.ReadKey(intercept: true);
                if (char.IsDigit(nextKey.KeyChar) && nextKey.KeyChar != '0')
                {
                    var index = nextKey.KeyChar - '1';
                    if (index < state.ActiveThreadCount)
                    {
                        ShowThreadDetail(state.State.ActiveThreads[index]);
                    }
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
                    return true;
            }
        }
    }

    private void ShowCharacterDetail(Character character)
    {
        AnsiConsole.Clear();
        RenderSessionHeader("Character Detail");

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

        var key = Console.ReadKey(intercept: true);
        switch (char.ToUpperInvariant(key.KeyChar))
        {
            case 'E':
                EditCharacterInline(character);
                break;
            case 'D':
                if (AnsiConsole.Confirm($"[red]Delete {character.Name}?[/]", defaultValue: false))
                {
                    AdventureStateManager.Instance.RemoveCharacter(character);
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

        AnsiConsole.MarkupLine("[green]Character updated.[/]");
        Thread.Sleep(600);
    }

    private void ShowThreadDetail(PlotThread thread)
    {
        AnsiConsole.Clear();
        RenderSessionHeader("Thread Detail");

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

        var key = Console.ReadKey(intercept: true);
        switch (char.ToUpperInvariant(key.KeyChar))
        {
            case 'E':
                EditThreadInline(thread);
                break;
            case 'X':
                AdventureStateManager.Instance.CloseThread(thread);
                AnsiConsole.MarkupLine($"[yellow]Closed:[/] {thread.Name}");
                Thread.Sleep(600);
                break;
            case 'D':
                if (AnsiConsole.Confirm($"[red]Delete {thread.Name}?[/]", defaultValue: false))
                {
                    AdventureStateManager.Instance.RemoveThread(thread);
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

        AdventureStateManager.Instance.AddCharacter(name, string.IsNullOrWhiteSpace(description) ? null : description);
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

        AdventureStateManager.Instance.AddThread(name, string.IsNullOrWhiteSpace(description) ? null : description);
        AnsiConsole.MarkupLine($"[green]Added:[/] [aqua]{name}[/]");
        Thread.Sleep(600);
    }

    private void EditCharacterPrompt()
    {
        var state = AdventureStateManager.Instance;
        if (state.CharacterCount == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No characters to edit.[/]");
            Thread.Sleep(800);
            return;
        }

        AnsiConsole.WriteLine();
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<Character>()
                .Title("[bold cyan]Select character:[/]")
                .HighlightStyle(new Style(Color.Gold1))
                .AddChoices(state.State.Characters)
                .UseConverter(c => c.DisplayName)
        );

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold cyan]Action for[/] [aqua]{selected.Name}[/][bold cyan]:[/]")
                .HighlightStyle(new Style(Color.Gold1))
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
                AnsiConsole.MarkupLine("[green]Description updated.[/]");
                break;
            case "Delete":
                if (AnsiConsole.Confirm($"[red]Delete {selected.Name}?[/]", defaultValue: false))
                {
                    state.RemoveCharacter(selected);
                    AnsiConsole.MarkupLine($"[red]Deleted:[/] {selected.Name}");
                }
                break;
        }
        Thread.Sleep(600);
    }

    private void EditThreadPrompt()
    {
        var state = AdventureStateManager.Instance;
        if (state.ActiveThreadCount == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No active threads to edit.[/]");
            Thread.Sleep(800);
            return;
        }

        AnsiConsole.WriteLine();
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<PlotThread>()
                .Title("[bold cyan]Select thread:[/]")
                .HighlightStyle(new Style(Color.Gold1))
                .AddChoices(state.State.ActiveThreads)
                .UseConverter(t => t.DisplayName)
        );

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold cyan]Action for[/] [aqua]{selected.Name}[/][bold cyan]:[/]")
                .HighlightStyle(new Style(Color.Gold1))
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
                AnsiConsole.MarkupLine("[green]Description updated.[/]");
                break;
            case "Close Thread":
                state.CloseThread(selected);
                AnsiConsole.MarkupLine($"[yellow]Closed:[/] {selected.Name}");
                break;
            case "Delete":
                if (AnsiConsole.Confirm($"[red]Delete {selected.Name}?[/]", defaultValue: false))
                {
                    state.RemoveThread(selected);
                    AnsiConsole.MarkupLine($"[red]Deleted:[/] {selected.Name}");
                }
                break;
        }
        Thread.Sleep(600);
    }

    private void ViewClosedThreads()
    {
        var state = AdventureStateManager.Instance;

        AnsiConsole.Clear();
        RenderSessionHeader("Closed Threads");

        if (state.ClosedThreadCount == 0)
        {
            AnsiConsole.MarkupLine("[grey]No closed threads.[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
            Console.ReadKey(intercept: true);
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .Title("[bold grey]Closed Threads[/]")
            .AddColumn(new TableColumn("[grey]Thread[/]").Width(30))
            .AddColumn(new TableColumn("[grey]Closed[/]").Width(20));

        foreach (var thread in state.State.ClosedThreads)
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

        var key = Console.ReadKey(intercept: true);
        if (char.ToUpperInvariant(key.KeyChar) == 'R' && state.ClosedThreadCount > 0)
        {
            var selected = AnsiConsole.Prompt(
                new SelectionPrompt<PlotThread>()
                    .Title("[bold cyan]Select thread to reopen:[/]")
                    .HighlightStyle(new Style(Color.Gold1))
                    .AddChoices(state.State.ClosedThreads)
                    .UseConverter(t => t.Name)
            );

            state.ReopenThread(selected);
            AnsiConsole.MarkupLine($"[green]Reopened:[/] [aqua]{selected.Name}[/]");
            Thread.Sleep(600);
        }
    }

    public bool ShowNotImplemented(string feature)
    {
        AnsiConsole.Clear();
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[yellow]{feature}[/] [grey]is not implemented yet[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
        return true;
    }

    public bool ConfirmQuit()
    {
        AnsiConsole.Clear();
        AnsiConsole.WriteLine();
        return AnsiConsole.Confirm("[yellow]Are you sure you want to quit?[/]", defaultValue: false);
        }
    
        private static string FormatShortcut(string key, string color = "bold green")
        {
            // Correctly escapes brackets for Spectre.Console.
            // Result looks like [Key] with the specified color inside.
            return $"[[[{color}]{key}[/]]]";
        }
    }
