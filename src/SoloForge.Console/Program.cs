﻿using Spectre.Console;
using Spectre.Console.Rendering;

// Main application loop
var running = true;
while (running)
{
    AnsiConsole.Clear();
    RenderMainMenu();

    var key = Console.ReadKey(intercept: true);
    running = HandleInput(key);
}

// === UI Building Methods ===

void RenderMainMenu()
{
    // Build the full-screen layout with three rows
    var layout = new Layout("Root")
        .SplitRows(
            new Layout("Title").Size(8),
            new Layout("Content"),
            new Layout("Footer").Size(2)
        );

    // Title section - FigletText centered
    var title = new FigletText("SoloForge")
        .Color(Color.Gold1);
    layout["Title"].Update(Align.Center(title, VerticalAlignment.Middle));

    // Content section - outer container with Session and Menu panels
    var container = BuildContentContainer();
    layout["Content"].Update(Align.Center(container, VerticalAlignment.Middle));

    // Footer section - hint text
    var footer = new Markup("[grey]Press a highlighted key or number to select an option[/]");
    layout["Footer"].Update(Align.Center(footer, VerticalAlignment.Top));

    // Render the full layout
    AnsiConsole.Write(layout);
}

IRenderable BuildContentContainer()
{
    // Session panel (left side)
    var sessionPanel = BuildSessionPanel();

    // Menu panel (right side)
    var menuPanel = BuildMenuPanel();

    // Two-column layout using Columns
    var columns = new Columns(sessionPanel, menuPanel);

    // Outer container panel
    var container = new Panel(columns)
        .Header("[bold yellow]SoloForge[/]")
        .HeaderAlignment(Justify.Center)
        .Border(BoxBorder.Rounded)
        .BorderColor(Color.Gold1)
        .Padding(1, 0);
    container.Width = 60;

    return container;
}

Panel BuildSessionPanel()
{
    var table = new Table()
        .Border(TableBorder.None)
        .HideHeaders()
        .AddColumn(new TableColumn("Label").PadRight(1))
        .AddColumn(new TableColumn("Value"));

    table.AddRow("[grey]Engine:[/]", "[white]Mythic 2e[/]");
    table.AddRow("[grey]Theme:[/]", "[white]Fantasy[/]");
    table.AddRow("[grey]Chaos:[/]", "[white]5[/]");
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

Panel BuildMenuPanel()
{
    var menuItems = new Markup(
        "[grey]1[/] [[[bold green]F[/]]] Fate Check\n" +
        "[grey]2[/] [[[bold green]R[/]]] Random Event\n" +
        "[grey]3[/] [[[bold green]C[/]]] Scene Check\n" +
        "[grey]4[/] [[[bold green]N[/]]] NPC Generator\n" +
        "[grey]5[/] [[[bold green]D[/]]] Dice Roller\n" +
        "[grey]───────────────────────[/]\n" +
        "  [[[bold yellow]S[/]]] Settings\n" +
        "  [[[bold red]Q[/]]] Quit"
    );

    var panel = new Panel(menuItems)
        .Header("[bold cyan]Main Menu[/]")
        .HeaderAlignment(Justify.Center)
        .Border(BoxBorder.Rounded)
        .BorderColor(Color.Cyan1)
        .Padding(1, 0);
    panel.Width = 30;
    return panel;
}

// === Input Handling ===

bool HandleInput(ConsoleKeyInfo key)
{
    var keyChar = char.ToUpperInvariant(key.KeyChar);

    return keyChar switch
    {
        'F' or '1' => ShowNotImplemented("Fate Check"),
        'R' or '2' => ShowNotImplemented("Random Event"),
        'C' or '3' => ShowNotImplemented("Scene Check"),
        'N' or '4' => ShowNotImplemented("NPC Generator"),
        'D' or '5' => ShowNotImplemented("Dice Roller"),
        'S'        => ShowNotImplemented("Settings"),
        'Q'        => !ConfirmQuit(),
        _          => true // Default case
    };
}

bool ShowNotImplemented(string feature)
{
    AnsiConsole.Clear();
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine($"[yellow]{feature}[/] [grey]is not implemented yet[/]");
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
    Console.ReadKey(intercept: true);
    return true;
}

bool ConfirmQuit()
{
    AnsiConsole.Clear();
    AnsiConsole.WriteLine();
    return AnsiConsole.Confirm("[yellow]Are you sure you want to quit?[/]", defaultValue: false);
}
