using Spectre.Console;
using Spectre.Console.Rendering;
using SoloForge.Console.Engines.Mythic2e;
using SoloForge.Console.Models;

var app = new SoloForgeApp();
app.Run();

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
        new("NPC Generator", 'N', 4, "green", app => app.ShowNotImplemented("NPC Generator")),
        new("Dice Roller", 'D', 5, "green", app => app.ShowNotImplemented("Dice Roller")),
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
            .Padding(1, 0);
        container.Width = 60;

        return container;
    }

    private Panel BuildSessionPanel()
    {
        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn("Label").PadRight(1))
            .AddColumn(new TableColumn("Value"));

        table.AddRow("[grey]Engine:[/]", $"[white]{Session.Engine}[/]");
        table.AddRow("[grey]Theme:[/]", $"[white]{Session.Theme}[/]");
        table.AddRow("[grey]Chaos:[/]", $"[white]{Session.Chaos}[/]");
        // Padding rows to match menu panel height
        table.AddRow("", "");
        table.AddRow("", "");
        table.AddRow("", "");
        table.AddRow("", "");
        table.AddRow("", "");

        var panel = new Panel(table)
            .Header("[bold cyan]Session[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Cyan1)
            .Padding(1, 0);
        panel.Width = 24;
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

            lines.Add($"{numberPrefix}[[[bold {item.HotkeyColor}]{item.Hotkey}[/]]] {item.Label}");
        }

        var menuContent = new Markup(string.Join("\n", lines));

        var panel = new Panel(menuContent)
            .Header("[bold cyan]Main Menu[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Cyan1)
            .Padding(1, 0);
        panel.Width = 30;
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
        AnsiConsole.WriteLine();

        // Prompt user to select odds
        var selectedOdds = AnsiConsole.Prompt(
            new SelectionPrompt<Odds>()
                .Title("[bold cyan]Select Odds:[/]")
                .AddChoices(Enum.GetValues<Odds>())
                .UseConverter(odds => odds.GetDisplayName())
        );

        // Perform the fate check
        var result = FateCheck.PerformCheck(Session.Chaos, selectedOdds);

        // Display results
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold cyan]Odds:[/] {selectedOdds.GetDisplayName()}");
        AnsiConsole.MarkupLine($"[bold cyan]Chaos Factor:[/] {Session.Chaos}");
        AnsiConsole.MarkupLine($"[bold cyan]Roll:[/] {result.Roll}");
        AnsiConsole.WriteLine();

        // Color-code the result
        string resultColor = result.Result.Contains("Yes") ? "green" : "red";
        AnsiConsole.MarkupLine($"[bold {resultColor}]{result.Result}[/]");

        // Show random event if triggered
        if (result.RandomEventTriggered)
        {
            var randomEvent = RandomEvent.Generate();
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold yellow]Random Event Triggered![/]");
            AnsiConsole.MarkupLine($"[yellow]Event Focus:[/] {randomEvent.EventFocus}");
            AnsiConsole.MarkupLine($"[yellow]Event:[/] {randomEvent.EventAction}");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
        return true;
    }

    public bool ShowRandomEvent()
    {
        AnsiConsole.Clear();
        AnsiConsole.WriteLine();

        var result = RandomEvent.Generate();

        AnsiConsole.MarkupLine("[bold cyan]Random Event[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold yellow]Event Focus:[/] {result.EventFocus}");
        AnsiConsole.MarkupLine($"[bold yellow]Event:[/] {result.EventAction}");

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
        return true;
    }

    public bool ShowSceneCheck()
    {
        AnsiConsole.Clear();
        AnsiConsole.WriteLine();

        var result = SceneCheck.PerformCheck(Session.Chaos);

        AnsiConsole.MarkupLine("[bold cyan]Scene Check[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold cyan]Chaos Factor:[/] {Session.Chaos}");
        AnsiConsole.MarkupLine($"[bold cyan]Roll:[/] {result.Roll}");
        AnsiConsole.WriteLine();

        // Color-code the result
        string resultColor = result.Result switch
        {
            "Normal Scene" => "green",
            "Altered Scene!" => "yellow",
            "Interrupt Scene!" => "red",
            _ => "white"
        };
        AnsiConsole.MarkupLine($"[bold {resultColor}]{result.Result}[/]");

        // Show scene adjustment if present
        if (result.SceneAdjustment != null)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[yellow]Scene Adjustment:[/] {result.SceneAdjustment}");
        }

        // Show random event if present
        if (result.RandomEvent != null)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold yellow]Random Event:[/]");
            AnsiConsole.MarkupLine($"[yellow]Event Focus:[/] {result.RandomEvent.EventFocus}");
            AnsiConsole.MarkupLine($"[yellow]Event:[/] {result.RandomEvent.EventAction}");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
        return true;
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
}
