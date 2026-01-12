using Spectre.Console;
using SoloForge.Console.Core;
using SoloForge.Console.Models;
using SoloForge.Console.Services;
using SoloForge.Console.UI;

namespace SoloForge.Console.Screens;

/// <summary>
/// Screen for viewing the campaign history/journal with interactive browsing.
/// </summary>
public class HistoryScreen(
    Session session,
    AdventureStateManager stateManager,
    HistoryService historyService,
    CampaignService campaignService)
    : BaseScreen(session, stateManager, historyService, campaignService)
{
    private const int PageSize = 15;
    private const int NarrativePageSize = 10;
    private const int ContextTruncateLength = 50;

    private LogType? _filter;

    public override IScreen? Run()
    {
        _filter = null;

        while (true)
        {
            RenderHeader("Journal");

            var entries = GetFilteredEntries();

            if (entries.Count == 0)
            {
                RenderEmptyState();
                var emptyKey = ReadKey();
                switch (GetKeyChar(emptyKey))
                {
                    case 'F':
                        PromptFilter();
                        break;
                    case 'C':
                        _filter = null;
                        break;
                    case 'B':
                    case 'Q':
                        return null;
                }
                continue;
            }

            // Show filter status if active
            if (_filter.HasValue)
            {
                AnsiConsole.MarkupLine($"[grey]Filtered by:[/] [{GetTypeColor(_filter.Value)}]{_filter.Value}[/]\n");
            }

            // Build selection choices with navigation options
            var choices = new List<JournalChoice>
            {
                new(JournalChoiceType.ReadMode, "📖 Narrative Read Mode"),
                new(JournalChoiceType.Filter, _filter.HasValue ? "🔍 Change Filter" : "🔍 Filter by Type"),
            };

            if (_filter.HasValue)
            {
                choices.Add(new JournalChoice(JournalChoiceType.ClearFilter, "✕ Clear Filter"));
            }

            choices.Add(new JournalChoice(JournalChoiceType.Back, "← Back to Menu"));
            choices.Add(new JournalChoice(JournalChoiceType.Separator, "──────────────────────────────────────────────"));

            // Add all entries
            foreach (var entry in entries)
            {
                choices.Add(new JournalChoice(JournalChoiceType.Entry, entry));
            }

            var selected = AnsiConsole.Prompt(
                new SelectionPrompt<JournalChoice>()
                    .Title($"[bold cyan]Journal Entries[/] [grey]({entries.Count} entries)[/]")
                    .HighlightStyle(new Style(MythicUi.AccentColor))
                    .PageSize(PageSize + 6) // Extra for menu options
                    .EnableSearch()
                    .SearchPlaceholderText("[grey]Type to search...[/]")
                    .AddChoices(choices)
                    .UseConverter(FormatJournalChoice)
            );

            switch (selected.Type)
            {
                case JournalChoiceType.ReadMode:
                    ShowNarrativeMode(entries);
                    break;
                case JournalChoiceType.Filter:
                    PromptFilter();
                    break;
                case JournalChoiceType.ClearFilter:
                    _filter = null;
                    break;
                case JournalChoiceType.Back:
                    return null;
                case JournalChoiceType.Entry when selected.Entry != null:
                    ShowEntryDetail(entries, selected.Entry);
                    break;
            }
        }
    }

    private void RenderEmptyState()
    {
        var message = _filter.HasValue
            ? $"[yellow]No {_filter.Value} entries found.[/]"
            : "[yellow]No journal entries yet. Make some rolls![/]";
        AnsiConsole.MarkupLine(message);
        AnsiConsole.WriteLine();
        RenderFilterOptions();
    }

    private List<LogEntry> GetFilteredEntries()
    {
        var entries = HistoryService.Entries.ToList();

        if (_filter.HasValue)
            entries = entries.Where(e => e.Type == _filter.Value).ToList();

        // Most recent first
        entries.Reverse();
        return entries;
    }

    private string FormatJournalChoice(JournalChoice choice)
    {
        return choice.Type switch
        {
            JournalChoiceType.Separator => "[grey]──────────────────────────────────────────────[/]",
            JournalChoiceType.Entry when choice.Entry != null => FormatEntryLine(choice.Entry),
            _ => choice.Label ?? ""
        };
    }

    private string FormatEntryLine(LogEntry entry)
    {
        var time = entry.Timestamp.ToString("h:mm tt");
        var typeColor = GetTypeColor(entry.Type);
        var context = TruncateText(entry.Context ?? entry.Result, ContextTruncateLength);
        return $"[grey]{time}[/] [bold {typeColor}]{entry.Type,-12}[/] {Markup.Escape(context)}";
    }

    /// <summary>
    /// Shows a detailed card view for a single journal entry with navigation.
    /// </summary>
    private void ShowEntryDetail(List<LogEntry> entries, LogEntry entry)
    {
        var currentIndex = entries.IndexOf(entry);

        while (true)
        {
            var current = entries[currentIndex];
            RenderHeader("Journal Entry");

            var typeColor = GetTypeColor(current.Type);

            // Header with type and time
            var headerText = $"[bold {typeColor}]{current.Type}[/] [grey]@[/] [white]{current.Timestamp:h:mm tt}[/] [grey]on[/] [white]{current.Timestamp:MMM dd, yyyy}[/]";
            AnsiConsole.Write(Align.Center(new Markup(headerText)));
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine();

            // Context Panel (the user's question/prompt)
            if (!string.IsNullOrEmpty(current.Context))
            {
                var contextPanel = new Panel(new Markup($"[white]{Markup.Escape(current.Context)}[/]"))
                    .Header("[bold cyan]Question / Context[/]")
                    .HeaderAlignment(Justify.Left)
                    .Border(BoxBorder.Rounded)
                    .BorderColor(MythicUi.PrimaryColor)
                    .Padding(1, 1)
                    .Expand();

                AnsiConsole.Write(contextPanel);
                AnsiConsole.WriteLine();
            }

            // Result Panel (the outcome)
            var resultPanel = new Panel(new Markup($"[bold gold1]{Markup.Escape(current.Result)}[/]"))
                .Header($"[bold {typeColor}]Result[/]")
                .HeaderAlignment(Justify.Left)
                .Border(BoxBorder.Double)
                .BorderColor(GetTypeColorValue(current.Type))
                .Padding(1, 1)
                .Expand();

            AnsiConsole.Write(resultPanel);
            AnsiConsole.WriteLine();

            // Details (mechanics)
            if (!string.IsNullOrEmpty(current.Details))
            {
                AnsiConsole.MarkupLine($"[grey]Details:[/] [dim]{Markup.Escape(current.Details)}[/]");
                AnsiConsole.WriteLine();
            }

            // Navigation bar
            var navParts = new List<string>
            {
                $"{FormatShortcut("C", "grey")} Copy"
            };

            if (currentIndex < entries.Count - 1)
                navParts.Add($"{FormatShortcut("P", "grey")} Previous (Older)");

            if (currentIndex > 0)
                navParts.Add($"{FormatShortcut("N", "grey")} Next (Newer)");

            navParts.Add($"{FormatShortcut("B", "grey")} Back to List");

            var positionInfo = $"[grey]Entry {currentIndex + 1} of {entries.Count}[/]";
            AnsiConsole.MarkupLine(positionInfo);
            AnsiConsole.MarkupLine($"[grey]{string.Join("  ", navParts)}[/]");

            var key = ReadKey();
            switch (GetKeyChar(key))
            {
                case 'C':
                    CopyEntryToClipboard(current);
                    break;
                case 'P':
                    // Previous = older = higher index
                    if (currentIndex < entries.Count - 1)
                        currentIndex++;
                    break;
                case 'N':
                    // Next = newer = lower index
                    if (currentIndex > 0)
                        currentIndex--;
                    break;
                case 'B':
                case 'Q':
                    return;
            }
        }
    }

    /// <summary>
    /// Shows entries as a linear narrative/chat log for easy reading.
    /// </summary>
    private void ShowNarrativeMode(List<LogEntry> allEntries)
    {
        // For narrative mode, show oldest first (chronological order)
        var chronological = allEntries.AsEnumerable().Reverse().ToList();
        var totalPages = (int)Math.Ceiling(chronological.Count / (double)NarrativePageSize);
        var currentPage = totalPages - 1; // Start at the last page (most recent)

        while (true)
        {
            RenderHeader("Journal - Narrative Mode");

            if (_filter.HasValue)
            {
                AnsiConsole.MarkupLine($"[grey]Filtered by:[/] [{GetTypeColor(_filter.Value)}]{_filter.Value}[/]\n");
            }

            var pageEntries = chronological
                .Skip(currentPage * NarrativePageSize)
                .Take(NarrativePageSize)
                .ToList();

            DateTime? lastDate = null;

            foreach (var entry in pageEntries)
            {
                // Date separator
                if (lastDate == null || entry.Timestamp.Date != lastDate.Value.Date)
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.Write(new Rule($"[grey]{entry.Timestamp:dddd, MMMM dd, yyyy}[/]").RuleStyle("grey dim").LeftJustified());
                    AnsiConsole.WriteLine();
                    lastDate = entry.Timestamp.Date;
                }

                RenderNarrativeEntry(entry);
            }

            AnsiConsole.WriteLine();

            // Navigation
            var navParts = new List<string>();

            if (currentPage > 0)
                navParts.Add($"{FormatShortcut("P", "grey")} Previous Page");

            if (currentPage < totalPages - 1)
                navParts.Add($"{FormatShortcut("N", "grey")} Next Page");

            navParts.Add($"{FormatShortcut("B", "grey")} Back to List");

            var pageInfo = $"[grey]Page {currentPage + 1} of {totalPages}[/]";
            AnsiConsole.MarkupLine(pageInfo);
            AnsiConsole.MarkupLine($"[grey]{string.Join("  ", navParts)}[/]");

            var key = ReadKey();
            switch (GetKeyChar(key))
            {
                case 'P':
                    if (currentPage > 0)
                        currentPage--;
                    break;
                case 'N':
                    if (currentPage < totalPages - 1)
                        currentPage++;
                    break;
                case 'B':
                case 'Q':
                    return;
            }
        }
    }

    private void RenderNarrativeEntry(LogEntry entry)
    {
        var typeColor = GetTypeColor(entry.Type);
        var time = entry.Timestamp.ToString("h:mm tt");

        // Question/Context (user's "message")
        if (!string.IsNullOrEmpty(entry.Context))
        {
            var contextGrid = new Grid()
                .AddColumn(new GridColumn().Width(System.Console.WindowWidth - 20).PadRight(2))
                .AddColumn(new GridColumn().Width(12));

            var questionPanel = new Panel(new Markup($"[white]{Markup.Escape(entry.Context)}[/]"))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Grey)
                .Padding(1, 0);

            contextGrid.AddRow(questionPanel, new Markup($"[grey]{time}[/]"));
            AnsiConsole.Write(contextGrid);
        }

        // Answer/Result (system's "response")
        var answerGrid = new Grid()
            .AddColumn(new GridColumn().Width(4))
            .AddColumn(new GridColumn().Width(System.Console.WindowWidth - 16));

        var typeTag = $"[{typeColor}]{entry.Type}[/]";
        var resultText = $"[bold gold1]{Markup.Escape(entry.Result)}[/]";

        if (!string.IsNullOrEmpty(entry.Details))
        {
            resultText += $"\n[dim grey]{Markup.Escape(entry.Details)}[/]";
        }

        var resultPanel = new Panel(new Markup(resultText))
            .Border(BoxBorder.Rounded)
            .BorderColor(GetTypeColorValue(entry.Type))
            .Header($"[{typeColor}]{entry.Type}[/]")
            .HeaderAlignment(Justify.Left)
            .Padding(1, 0);

        answerGrid.AddRow(new Text(""), resultPanel);
        AnsiConsole.Write(answerGrid);
        AnsiConsole.WriteLine();
    }

    private void RenderFilterOptions()
    {
        var options = $"[grey]{FormatShortcut("F", "grey")} Filter by Type  ";
        if (_filter.HasValue)
            options += $"{FormatShortcut("C", "grey")} Clear Filter  ";
        options += $"{FormatShortcut("B", "grey")} Back[/]";

        AnsiConsole.MarkupLine(options);
    }

    private void PromptFilter()
    {
        RenderHeader("Filter Journal");

        var choices = new List<string> { "All Types" };
        choices.AddRange(Enum.GetNames<LogType>());

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold cyan]Filter by entry type:[/]")
                .HighlightStyle(new Style(MythicUi.AccentColor))
                .AddChoices(choices)
                .UseConverter(s =>
                {
                    if (s == "All Types")
                        return "[grey]All Types[/]";
                    var logType = Enum.Parse<LogType>(s);
                    return $"[{GetTypeColor(logType)}]{s}[/]";
                })
        );

        _filter = selected == "All Types" ? null : Enum.Parse<LogType>(selected);
    }

    private static string GetTypeColor(LogType type) => type switch
    {
        LogType.FateCheck => "green",
        LogType.SceneCheck => "yellow",
        LogType.RandomEvent => "red",
        LogType.Meaning => "cyan",
        LogType.Note => "grey",
        _ => "white"
    };

    private static Color GetTypeColorValue(LogType type) => type switch
    {
        LogType.FateCheck => Color.Green,
        LogType.SceneCheck => Color.Yellow,
        LogType.RandomEvent => Color.Red,
        LogType.Meaning => Color.Cyan1,
        LogType.Note => Color.Grey,
        _ => Color.White
    };

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Normalize whitespace
        text = string.Join(" ", text.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries));

        return text.Length <= maxLength
            ? text
            : text[..(maxLength - 3)] + "...";
    }

    /// <summary>
    /// Represents a choice in the journal browser.
    /// </summary>
    private record JournalChoice(JournalChoiceType Type, string? Label = null, LogEntry? Entry = null)
    {
        public JournalChoice(JournalChoiceType type, LogEntry entry)
            : this(type, null, entry) { }
    }

    private enum JournalChoiceType
    {
        Entry,
        ReadMode,
        Filter,
        ClearFilter,
        Back,
        Separator
    }
}
