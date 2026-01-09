using Spectre.Console;
using SoloForge.Console.Core;
using SoloForge.Console.Models;
using SoloForge.Console.Services;
using SoloForge.Console.UI;

namespace SoloForge.Console.Screens;

/// <summary>
/// Screen for viewing the campaign history/journal.
/// </summary>
public class HistoryScreen(
    Session session,
    AdventureStateManager stateManager,
    HistoryService historyService,
    CampaignService campaignService)
    : BaseScreen(session, stateManager, historyService, campaignService)
{
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
                var message = _filter.HasValue
                    ? $"[yellow]No {_filter.Value} entries found.[/]"
                    : "[yellow]No journal entries yet. Make some rolls![/]";
                AnsiConsole.MarkupLine(message);
            }
            else
            {
                RenderHistoryTable(entries);
            }

            AnsiConsole.WriteLine();
            RenderFilterOptions();

            var key = ReadKey();
            switch (GetKeyChar(key))
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
        }
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

    private void RenderHistoryTable(List<LogEntry> entries)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(MythicUi.PrimaryColor)
            .AddColumn(new TableColumn("[bold cyan]Time[/]").Width(12))
            .AddColumn(new TableColumn("[bold cyan]Type[/]").Width(12))
            .AddColumn(new TableColumn("[bold cyan]Context[/]").Width(30))
            .AddColumn(new TableColumn("[bold cyan]Result[/]"));

        var displayCount = Math.Min(entries.Count, 15);

        for (var i = 0; i < displayCount; i++)
        {
            var entry = entries[i];
            var typeColor = GetTypeColor(entry.Type);
            var time = entry.Timestamp.ToString("h:mm tt");
            var context = TruncateText(entry.Context ?? "-", 28);
            var result = TruncateText(entry.Result, 35);

            table.AddRow(
                $"[grey]{time}[/]",
                $"[{typeColor}]{entry.Type}[/]",
                $"[white]{Markup.Escape(context)}[/]",
                $"[gold1]{Markup.Escape(result)}[/]"
            );
        }

        AnsiConsole.Write(table);

        if (entries.Count > displayCount)
        {
            AnsiConsole.MarkupLine($"[grey]... and {entries.Count - displayCount} more entries[/]");
        }

        // Show filter status
        if (_filter.HasValue)
        {
            AnsiConsole.MarkupLine($"\n[grey]Filtered by:[/] [{GetTypeColor(_filter.Value)}]{_filter.Value}[/]");
        }
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

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text.Length <= maxLength
            ? text
            : text[..(maxLength - 3)] + "...";
    }
}
