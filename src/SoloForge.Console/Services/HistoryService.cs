using SoloForge.Console.Models;

namespace SoloForge.Console.Services;

/// <summary>
/// Manages the in-memory history/journal of campaign events.
/// </summary>
public sealed class HistoryService
{
    private readonly List<LogEntry> _entries = [];

    public HistoryService()
    {
    }

    /// <summary>
    /// All history entries in chronological order.
    /// </summary>
    public IReadOnlyList<LogEntry> Entries => _entries;

    /// <summary>
    /// Number of entries in the history.
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Adds a new entry to the history.
    /// </summary>
    public LogEntry AddEntry(LogType type, string result, string? context = null, string? details = null)
    {
        var entry = new LogEntry
        {
            Type = type,
            Result = result,
            Context = context,
            Details = details
        };
        _entries.Add(entry);
        return entry;
    }

    /// <summary>
    /// Gets the most recent entries.
    /// </summary>
    public IEnumerable<LogEntry> GetRecent(int count)
    {
        return _entries
            .OrderByDescending(e => e.Timestamp)
            .Take(count);
    }

    /// <summary>
    /// Gets entries filtered by type.
    /// </summary>
    public IEnumerable<LogEntry> GetByType(LogType type)
    {
        return _entries.Where(e => e.Type == type);
    }

    /// <summary>
    /// Clears all history entries (used when creating a new campaign).
    /// </summary>
    public void Clear()
    {
        _entries.Clear();
    }

    /// <summary>
    /// Loads history entries from a saved campaign.
    /// </summary>
    public void LoadHistory(IEnumerable<LogEntry> entries)
    {
        _entries.Clear();
        _entries.AddRange(entries);
    }

    /// <summary>
    /// Gets all entries for serialization.
    /// </summary>
    public List<LogEntry> GetAllEntries() => [.. _entries];
}
