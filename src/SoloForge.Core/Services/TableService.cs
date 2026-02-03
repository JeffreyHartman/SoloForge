using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace SoloForge.Console.Services;

/// <summary>
/// Provides dynamic discovery, caching, and access to word tables from the data directory.
/// </summary>
public sealed class TableService
{
    private static readonly Lazy<TableService> _instance = new(() => new TableService());
    public static TableService Instance => _instance.Value;

    private readonly ConcurrentDictionary<string, string[]> _tableCache = new();
    private readonly Lazy<string?> _dataDirectory;
    private readonly Lazy<List<TableInfo>> _availableTables;

    private TableService()
    {
        _dataDirectory = new Lazy<string?>(DiscoverDataDirectory);
        _availableTables = new Lazy<List<TableInfo>>(DiscoverTables);
    }

    /// <summary>
    /// Gets all available tables discovered from the data directory.
    /// </summary>
    public IReadOnlyList<TableInfo> AvailableTables => _availableTables.Value;

    /// <summary>
    /// Gets tables in the root data directory (action1, action2, descriptor1, descriptor2).
    /// </summary>
    public IEnumerable<TableInfo> CoreTables => AvailableTables.Where(t => !t.IsElement);

    /// <summary>
    /// Gets tables in the elements subdirectory.
    /// </summary>
    public IEnumerable<TableInfo> ElementTables => AvailableTables.Where(t => t.IsElement);

    /// <summary>
    /// Gets a random word from the specified table.
    /// </summary>
    public string GetRandomWord(string tableId)
    {
        var words = GetTableWords(tableId);
        if (words.Length == 0)
            return $"[{tableId} is empty]";

        return words[Random.Shared.Next(words.Length)];
    }

    /// <summary>
    /// Gets a word pair (two random words) from the specified table.
    /// </summary>
    public string GetWordPair(string tableId)
    {
        var words = GetTableWords(tableId);
        if (words.Length == 0)
            return $"[{tableId} is empty]";

        var word1 = words[Random.Shared.Next(words.Length)];
        var word2 = words[Random.Shared.Next(words.Length)];
        return $"{word1} {word2}";
    }

    /// <summary>
    /// Gets a fusion pair - one word from each of two different tables.
    /// </summary>
    public string GetFusionPair(string tableId1, string tableId2)
    {
        var word1 = GetRandomWord(tableId1);
        var word2 = GetRandomWord(tableId2);
        return $"{word1} {word2}";
    }

    /// <summary>
    /// Gets all words from the specified table.
    /// </summary>
    public string[] GetTableWords(string tableId)
    {
        return _tableCache.GetOrAdd(tableId, LoadTable);
    }

    /// <summary>
    /// Finds a table by its ID.
    /// </summary>
    public TableInfo? FindTable(string tableId)
    {
        return AvailableTables.FirstOrDefault(t =>
            t.Id.Equals(tableId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Refreshes the table cache and re-discovers available tables.
    /// </summary>
    public void Refresh()
    {
        _tableCache.Clear();
        // Note: _availableTables is Lazy and won't refresh automatically
        // For a full refresh, a new instance would be needed
    }

    private string[] LoadTable(string tableId)
    {
        var table = FindTable(tableId);
        if (table == null || !File.Exists(table.FilePath))
            return [];

        return File.ReadAllLines(table.FilePath)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith('#'))
            .ToArray();
    }

    private string? DiscoverDataDirectory()
    {
        var baseDir = AppContext.BaseDirectory;
        var currentDir = new DirectoryInfo(baseDir);

        while (currentDir != null)
        {
            var dataPath = Path.Combine(currentDir.FullName, "data");
            if (Directory.Exists(dataPath))
                return dataPath;
            currentDir = currentDir.Parent;
        }

        // Fallback to relative path
        var relativePath = Path.Combine(Directory.GetCurrentDirectory(), "data");
        return Directory.Exists(relativePath) ? relativePath : null;
    }

    private List<TableInfo> DiscoverTables()
    {
        var tables = new List<TableInfo>();
        var dataDir = _dataDirectory.Value;

        if (dataDir == null || !Directory.Exists(dataDir))
            return tables;

        // Discover root-level tables
        foreach (var file in Directory.GetFiles(dataDir, "*.txt"))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            tables.Add(new TableInfo(
                Id: fileName,
                DisplayName: FormatDisplayName(fileName),
                FilePath: file,
                IsElement: false,
                Category: "Core"
            ));
        }

        // Discover element tables
        var elementsDir = Path.Combine(dataDir, "elements");
        if (Directory.Exists(elementsDir))
        {
            foreach (var file in Directory.GetFiles(elementsDir, "*.txt"))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var category = CategorizeElement(fileName);
                tables.Add(new TableInfo(
                    Id: $"elements/{fileName}",
                    DisplayName: FormatDisplayName(fileName),
                    FilePath: file,
                    IsElement: true,
                    Category: category
                ));
            }
        }

        return tables.OrderBy(t => t.Category).ThenBy(t => t.DisplayName).ToList();
    }

    private static string FormatDisplayName(string fileName)
    {
        // Convert camelCase/lowercase to Title Case with spaces
        var spaced = Regex.Replace(fileName, "([a-z])([A-Z])", "$1 $2");
        spaced = Regex.Replace(spaced, "([0-9]+)", " $1");

        // Capitalize first letter of each word
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo
            .ToTitleCase(spaced.ToLower());
    }

    private static string CategorizeElement(string fileName)
    {
        return fileName.ToLower() switch
        {
            var n when n.StartsWith("character") => "Character",
            var n when n.Contains("creature") || n.Contains("animal") || n.Contains("undead") || n.Contains("alien") => "Creatures",
            var n when n.Contains("location") || n.Contains("terrain") || n.Contains("city") ||
                       n.Contains("forest") || n.Contains("cavern") || n.Contains("dungeon") ||
                       n.Contains("domicile") => "Locations",
            var n when n.Contains("magic") || n.Contains("spell") || n.Contains("curse") ||
                       n.Contains("power") => "Magic",
            var n when n.Contains("object") || n.Contains("starship") || n.Contains("army") => "Objects",
            _ => "General"
        };
    }
}

/// <summary>
/// Represents metadata about a word table.
/// </summary>
public record TableInfo(
    string Id,
    string DisplayName,
    string FilePath,
    bool IsElement,
    string Category
);
