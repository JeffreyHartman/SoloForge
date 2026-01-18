using System.Collections.ObjectModel;
using Terminal.Gui;
using SoloForge.Console.App;
using SoloForge.Console.Models;
using SoloForge.Console.Services;

namespace SoloForge.Console.Views.Components;

/// <summary>
/// Right panel displaying the last 15 history entries.
/// Auto-scrolls to latest entry on refresh.
/// </summary>
public class LiveLogPanel : FrameView
{
    private readonly HistoryService _historyService;
    private readonly ListView _listView;
    private readonly ObservableCollection<string> _entries;
    private const int MaxEntries = 15;

    public LiveLogPanel(HistoryService historyService)
    {
        _historyService = historyService;

        Title = "Live Log";
        BorderStyle = LineStyle.Double;
        ColorScheme = UiThemes.Instance.ActiveDefault;

        _entries = new ObservableCollection<string>();

        _listView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = false,
            ColorScheme = UiThemes.Instance.ActiveDefault
        };
        _listView.SetSource(_entries);

        Add(_listView);
        Refresh();
    }

    public void Refresh()
    {
        _entries.Clear();

        var recentEntries = _historyService.GetRecent(MaxEntries).Reverse().ToList();

        foreach (var entry in recentEntries)
        {
            var formatted = FormatEntry(entry);
            _entries.Add(formatted);
        }

        // Auto-scroll to bottom if there are entries
        if (_entries.Count > 0)
        {
            _listView.SelectedItem = _entries.Count - 1;
            _listView.TopItem = Math.Max(0, _entries.Count - _listView.Frame.Height);
        }

        SetNeedsLayout();
    }

    private static string FormatEntry(LogEntry entry)
    {
        var timeStr = entry.Timestamp.ToString("HH:mm");
        var typeCode = GetTypeCode(entry.Type);
        var result = TruncateResult(entry.Result, 20);

        return $"{timeStr} [{typeCode}] {result}";
    }

    private static string GetTypeCode(LogType type) => type switch
    {
        LogType.FateCheck => "F",
        LogType.SceneCheck => "S",
        LogType.RandomEvent => "R",
        LogType.Meaning => "M",
        LogType.DiceRoll => "D",
        LogType.Note => "N",
        _ => "?"
    };

    private static string TruncateResult(string result, int maxLength)
    {
        if (string.IsNullOrEmpty(result))
            return "";

        if (result.Length <= maxLength)
            return result;

        return result[..(maxLength - 3)] + "...";
    }
}
