using Spectre.Console;
using Spectre.Console.Rendering;
using SoloForge.Console.Services;

namespace SoloForge.Console.UI;

/// <summary>
/// Static factory class for standardized UI components and theming.
/// </summary>
public static class MythicUi
{
    // === UI Constants ===
    public const int MinPanelWidth = 36;
    public const int ResultPanelWidth = 40;
    public const int MainMenuContainerWidth = 72;
    public const int SessionPanelWidth = 28;
    public const int MenuPanelWidth = 36;
    public const int ListColumnWidth = 30;

    // === Theme Colors ===
    public static Color PrimaryColor => Color.Cyan1;
    public static Color AccentColor => Color.Gold1;
    public static Color SuccessColor => Color.Green;
    public static Color WarningColor => Color.Yellow;
    public static Color ErrorColor => Color.Red;
    public static Color MutedColor => Color.Grey;

    /// <summary>
    /// Creates a standard menu panel with cyan border.
    /// </summary>
    public static Panel CreateMenuPanel(IRenderable content, string header, int? width = null)
    {
        var panel = new Panel(content)
            .Header($"[bold cyan]{header}[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Rounded)
            .BorderColor(PrimaryColor)
            .Padding(1, 0);

        if (width.HasValue)
            panel.Width = width.Value;

        return panel;
    }

    /// <summary>
    /// Creates a result panel with double border and custom color.
    /// </summary>
    public static Panel CreateResultPanel(string text, string header, Color borderColor, int? width = null)
    {
        var panel = new Panel(
            new Align(
                new Markup(text),
                HorizontalAlignment.Center
            )
        )
        .Header($"[bold cyan]{header}[/]")
        .HeaderAlignment(Justify.Center)
        .Border(BoxBorder.Double)
        .BorderColor(borderColor)
        .Padding(2, 1);

        panel.Width = width ?? ResultPanelWidth;
        return panel;
    }

    /// <summary>
    /// Creates a panel with double border for emphasis.
    /// </summary>
    public static Panel CreateDoubleBorderPanel(IRenderable content, string header, Color borderColor, int? width = null)
    {
        var panel = new Panel(content)
            .Header($"[bold {GetColorName(borderColor)}]{header}[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Double)
            .BorderColor(borderColor)
            .Padding(2, 1);

        if (width.HasValue)
            panel.Width = width.Value;

        return panel;
    }

    /// <summary>
    /// Creates a panel with rounded border for standard display.
    /// </summary>
    public static Panel CreateRoundedPanel(IRenderable content, string header, Color borderColor, int? width = null)
    {
        var panel = new Panel(content)
            .Header($"[bold {GetColorName(borderColor)}]{header}[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Rounded)
            .BorderColor(borderColor)
            .Padding(2, 1);

        if (width.HasValue)
            panel.Width = width.Value;

        return panel;
    }

    /// <summary>
    /// Creates a session info table showing chaos, characters, threads, and optionally campaign name.
    /// </summary>
    public static Table CreateSessionTable(string title, int chaos, int characterCount, int threadCount, string? campaignName = null)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(PrimaryColor)
            .AddColumn(new TableColumn($"[bold cyan]{title}[/]").Centered())
            .AddColumn(new TableColumn($"[grey]Chaos:[/] [white]{chaos}[/]").Centered())
            .AddColumn(new TableColumn($"[grey]Characters:[/] [aqua]{characterCount}[/] [grey]|[/] [grey]Threads:[/] [aqua]{threadCount}[/]").Centered());

        // Add campaign name row if provided
        if (!string.IsNullOrEmpty(campaignName))
        {
            table.AddEmptyRow();
            table.AddRow(
                new Markup($"[grey]Campaign:[/] [gold1]{campaignName}[/]"),
                new Text(""),
                new Text("")
            );
        }

        return table;
    }

    /// <summary>
    /// Creates a details table with two columns.
    /// </summary>
    public static Table CreateDetailsTable(string col1Header = "Detail", string col2Header = "Value", int col1Width = 16, int col2Width = 18)
    {
        return new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(PrimaryColor)
            .AddColumn(new TableColumn($"[bold cyan]{col1Header}[/]").Centered().Width(col1Width))
            .AddColumn(new TableColumn($"[bold cyan]{col2Header}[/]").Centered().Width(col2Width));
    }

    /// <summary>
    /// Creates a key-value table with no headers.
    /// </summary>
    public static Table CreateKeyValueTable()
    {
        return new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(MutedColor)
            .HideHeaders()
            .AddColumn(new TableColumn("Label").Width(10))
            .AddColumn(new TableColumn("Value").Width(20));
    }

    /// <summary>
    /// Renders a figlet title in accent color.
    /// </summary>
    public static void RenderFigletTitle(string title)
    {
        if (!SettingsService.Instance.Features.ShowSubpageTitles)
            return;

        var figlet = new FigletText(title).Color(AccentColor);
        AnsiConsole.Write(Align.Center(figlet));
    }

    /// <summary>
    /// Renders the standard session header with title, stats, and campaign name.
    /// </summary>
    public static void RenderSessionHeader(string title, int chaos, int characterCount, int threadCount, string? campaignName = null)
    {
        RenderFigletTitle(title);

        var table = CreateSessionTable(title, chaos, characterCount, threadCount, campaignName);
        AnsiConsole.Write(Align.Center(table));
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Formats a keyboard shortcut with proper bracket escaping for Spectre.Console.
    /// Uses triple-bracket technique to display [Key] with colored text inside.
    /// </summary>
    public static string FormatShortcut(string key, string color = "bold green")
    {
        return $"[[[{color}]{key}[/]]]";
    }

    /// <summary>
    /// Renders a standard "press any key" prompt.
    /// </summary>
    public static void WaitForKey(string message = "Press any key to continue...")
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[grey]{message}[/]");
        System.Console.ReadKey(intercept: true);
    }

    /// <summary>
    /// Clears the console and prepares for new content.
    /// </summary>
    public static void Clear() => AnsiConsole.Clear();

    private static string GetColorName(Color color)
    {
        if (color == Color.Cyan1) return "cyan";
        if (color == Color.Gold1) return "gold1";
        if (color == Color.Green) return "green";
        if (color == Color.Yellow) return "yellow";
        if (color == Color.Red) return "red";
        if (color == Color.Grey) return "grey";
        if (color == Color.Aqua) return "aqua";
        if (color == Color.White) return "white";
        return "white";
    }
}
