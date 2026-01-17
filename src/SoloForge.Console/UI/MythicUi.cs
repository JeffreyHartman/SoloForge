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
    public const int MainMenuContainerWidth = 80;
    public const int SessionPanelWidth = 32;
    public const int MenuPanelWidth = 38;
    public const int ListColumnWidth = 30;
    public const int JournalMinWidth = 42;
    public const int JournalMaxWidth = 80;
    public const int SplitGapWidth = 2;
    public const int FooterHeight = 2;

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

    public static Layout BuildSplitLayout(IRenderable leftContent, JournalService journal, string title, int chaos, int characters, int threads, string? campaignName, string? footerText = null)
    {
        var windowWidth = Math.Max(System.Console.WindowWidth, 80);
        var windowHeight = Math.Max(System.Console.WindowHeight, 24);
        var leftWidth = Math.Max(40, windowWidth / 2 - SplitGapWidth - 1);
        var rightWidth = Math.Clamp(windowWidth - leftWidth - SplitGapWidth, JournalMinWidth, JournalMaxWidth);
        leftWidth = Math.Max(leftWidth, windowWidth - rightWidth - SplitGapWidth);

        var headerHeight = SettingsService.Instance.Features.ShowSubpageTitles ? 9 : 4;
        var contentHeight = Math.Max(6, windowHeight - headerHeight - FooterHeight);

        journal.SetViewportSize(rightWidth - 4, contentHeight - 4);

        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Header").Size(headerHeight),
                new Layout("Content"),
                new Layout("Footer").Size(FooterHeight)
            );

        var header = new Rows(
            new IRenderable[]
            {
                new Align(new FigletText(title).Color(AccentColor), HorizontalAlignment.Center),
                CreateSessionTable(title, chaos, characters, threads, campaignName)
            }
        );
        layout["Header"].Update(Align.Center(header));

        var journalPanel = BuildJournalPanel(journal, rightWidth, contentHeight);
        var grid = new Grid();
        grid.AddColumn(new GridColumn().Width(leftWidth));
        grid.AddColumn(new GridColumn().Width(SplitGapWidth));
        grid.AddColumn(new GridColumn().Width(rightWidth));
        grid.AddRow(leftContent, new Text(""), journalPanel);

        layout["Content"].Update(grid);

        var footerLine = footerText ?? string.Empty;
        layout["Footer"].Update(new Markup(footerLine));

        return layout;
    }

    public static Panel BuildJournalPanel(JournalService journal, int width, int height)
    {
        var lines = journal.GetVisibleLines();
        var content = new Markup(string.Join("\n", lines.Select(Markup.Escape)));

        var panel = new Panel(content)
            .Header($"[bold cyan]{Markup.Escape(journal.GetHeaderLabel())}[/]")
            .HeaderAlignment(Justify.Left)
            .Border(BoxBorder.Rounded)
            .BorderColor(journal.Focus == JournalFocus.Journal ? AccentColor : MutedColor)
            .Padding(1, 0);
        panel.Width = width;
        panel.Height = height;
        return panel;
    }

    public static void RenderSplitScreen(IRenderable leftContent, JournalService journal, string title, int chaos, int characters, int threads, string? campaignName, string? footerText = null)
    {
        Clear();

        var layout = BuildSplitLayout(leftContent, journal, title, chaos, characters, threads, campaignName, footerText);
        AnsiConsole.Write(layout);
        RenderQuickRollLine(null);
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
    /// Clears the console and prepares for new content.
    /// </summary>
    public static void Clear() => AnsiConsole.Clear();

    /// <summary>
    /// Renders the last quick roll summary in a fixed bottom line.
    /// </summary>
    public static void RenderQuickRollLine(string? summary)
    {
        RenderBottomMessage(string.IsNullOrWhiteSpace(summary)
            ? string.Empty
            : $"Last quick roll: {Markup.Escape(summary)}", isError: false);
    }

    /// <summary>
    /// Prompts for inline input on the bottom prompt line.
    /// </summary>
    public static string? PromptInlineAtBottom(string prompt)
    {
        var promptLine = Math.Max(System.Console.WindowHeight - 1, 0);
        var maxWidth = Math.Max(System.Console.WindowWidth - 1, 0);
        var (left, top) = System.Console.GetCursorPosition();
        var input = new List<char>();

        void Render()
        {
            ClearConsoleLine(promptLine);
            System.Console.SetCursorPosition(0, promptLine);

            var promptText = prompt;
            var inputText = new string(input.ToArray());
            var fullText = promptText + inputText;

            if (fullText.Length > maxWidth)
            {
                fullText = fullText[^maxWidth..];
            }

            System.Console.Write(fullText);
        }

        Render();

        while (true)
        {
            var key = System.Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                break;
            }

            if (key.Key == ConsoleKey.Escape)
            {
                input.Clear();
                break;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (input.Count > 0)
                    input.RemoveAt(input.Count - 1);
                Render();
                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                input.Add(key.KeyChar);
                Render();
            }
        }

        ClearConsoleLine(promptLine);
        System.Console.SetCursorPosition(left, top);

        return input.Count == 0 ? null : new string(input.ToArray());
    }

    /// <summary>
    /// Renders a bottom status line without moving the cursor.
    /// </summary>
    public static void RenderBottomMessage(string message, bool isError = false)
    {
        var line = Math.Max(System.Console.WindowHeight - 2, 0);
        var (left, top) = System.Console.GetCursorPosition();

        ClearConsoleLine(line);
        System.Console.SetCursorPosition(0, line);

        if (!string.IsNullOrWhiteSpace(message))
        {
            var color = isError ? "red" : "grey";
            AnsiConsole.Markup($"[{color}]{Markup.Escape(message)}[/]");
        }

        System.Console.SetCursorPosition(left, top);
    }

    /// <summary>
    /// Clears a specific console line without moving the cursor.
    /// </summary>
    public static void ClearConsoleLine(int line)
    {
        var windowHeight = System.Console.WindowHeight;
        var windowWidth = System.Console.WindowWidth;
        if (line < 0 || line >= windowHeight || windowWidth <= 0)
            return;

        var (left, top) = System.Console.GetCursorPosition();
        System.Console.SetCursorPosition(0, line);
        AnsiConsole.Write(new string(' ', Math.Max(windowWidth - 1, 0)));
        System.Console.SetCursorPosition(left, top);
    }

    /// <summary>
    /// Shows a brief feedback message for clipboard operations.
    /// The message appears momentarily then disappears.
    /// </summary>
    public static void ShowClipboardFeedback(bool success, string message)
    {
        var (left, top) = System.Console.GetCursorPosition();

        // Move to bottom of visible area or use current position
        var feedbackLine = Math.Min(top + 1, System.Console.WindowHeight - 1);

        System.Console.SetCursorPosition(0, feedbackLine);

        if (success)
        {
            AnsiConsole.Markup($"[green][[Copied]][/] [grey]{message}[/]");
        }
        else
        {
            AnsiConsole.Markup($"[red][[Failed]][/] [grey]{message}[/]");
        }

        // Brief pause so user can see the feedback
        Thread.Sleep(800);

        // Clear the feedback line
        System.Console.SetCursorPosition(0, feedbackLine);
        AnsiConsole.Write(new string(' ', System.Console.WindowWidth - 1));

        // Restore cursor position
        System.Console.SetCursorPosition(left, top);
    }

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
