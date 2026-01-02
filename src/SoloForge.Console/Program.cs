using Spectre.Console;
using Spectre.Console.Rendering;

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
        new("Fate Check", 'F', 1, "green", app => app.ShowNotImplemented("Fate Check")),
        new("Random Event", 'R', 2, "green", app => app.ShowNotImplemented("Random Event")),
        new("Scene Check", 'C', 3, "green", app => app.ShowNotImplemented("Scene Check")),
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

        var footer = new Markup("[grey]Press a highlighted key or number to select an option[/]");
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
        var keyChar = char.ToUpperInvariant(key.KeyChar);

        // Find matching menu item by hotkey or number key
        var menuItem = _menuItems.FirstOrDefault(m =>
            m.Hotkey == keyChar ||
            (m.NumberKey.HasValue && m.NumberKey.Value.ToString()[0] == keyChar));

        return menuItem?.Action(this) ?? true;
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
