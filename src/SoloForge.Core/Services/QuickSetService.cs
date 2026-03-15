using System.Text.Json;
using Serilog;
using SoloForge.Core.Models;

namespace SoloForge.Core.Services;

/// <summary>
/// Service for loading and managing Quick Sets - configurable table roll collections.
/// </summary>
public sealed class QuickSetService
{
    private static readonly Lazy<QuickSetService> _instance = new(() => new QuickSetService());
    public static QuickSetService Instance => _instance.Value;

    private readonly ILogger _log = AppLogger.ForContext<QuickSetService>();
    private readonly List<QuickSet> _quickSets = [];
    private readonly string _configPath;
    private bool _initialized;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private QuickSetService()
    {
        _configPath = FindConfigPath();
        _log.Debug("QuickSetService initialized with config path: {Path}", _configPath);
    }

    /// <summary>
    /// Gets all available quick sets.
    /// </summary>
    public IReadOnlyList<QuickSet> QuickSets
    {
        get
        {
            EnsureInitialized();
            return _quickSets;
        }
    }

    /// <summary>
    /// Generates results for a quick set by rolling on all configured tables.
    /// </summary>
    public QuickSetResult Generate(QuickSet quickSet)
    {
        _log.Information("Generating quick set: {Name}", quickSet.Name);

        var results = new List<QuickSetStepResult>();

        foreach (var step in quickSet.Steps)
        {
            var tableId = ResolveTableId(step.Table);
            if (tableId == null)
            {
                _log.Warning("Could not resolve table '{Table}' for step '{Label}'", step.Table, step.Label);
                results.Add(new QuickSetStepResult
                {
                    Label = step.Label,
                    Words = [$"(Table '{step.Table}' not found)"],
                    TableId = step.Table
                });
                continue;
            }

            var words = new List<string>();
            for (var i = 0; i < step.Count; i++)
            {
                words.Add(TableService.Instance.GetRandomWord(tableId));
            }

            results.Add(new QuickSetStepResult
            {
                Label = step.Label,
                Words = words,
                TableId = tableId
            });

            _log.Debug("Generated {Label}: {Result}", step.Label, string.Join(" ", words));
        }

        return new QuickSetResult
        {
            QuickSet = quickSet,
            Results = results
        };
    }

    /// <summary>
    /// Reloads quick sets from the configuration file.
    /// </summary>
    public void Reload()
    {
        _quickSets.Clear();
        _initialized = false;
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;

        LoadQuickSets();
        _initialized = true;
    }

    private void LoadQuickSets()
    {
        _log.Information("Loading quick sets from {Path}", _configPath);

        if (!File.Exists(_configPath))
        {
            _log.Information("Quick sets config not found, creating default");
            CreateDefaultConfig();
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            var sets = JsonSerializer.Deserialize<List<QuickSet>>(json, JsonOptions);

            if (sets != null)
            {
                foreach (var set in sets)
                {
                    ValidateQuickSet(set);
                    _quickSets.Add(set);
                }
            }

            _log.Information("Loaded {Count} quick sets", _quickSets.Count);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to load quick sets config");

            // Create default if loading failed
            CreateDefaultConfig();
            try
            {
                var json = File.ReadAllText(_configPath);
                var sets = JsonSerializer.Deserialize<List<QuickSet>>(json, JsonOptions);
                if (sets != null) _quickSets.AddRange(sets);
            }
            catch
            {
                // Last resort - add hardcoded default
                _quickSets.Add(GetDefaultNpcProfile());
            }
        }
    }

    private void ValidateQuickSet(QuickSet set)
    {
        foreach (var step in set.Steps)
        {
            var tableId = ResolveTableId(step.Table);
            if (tableId == null)
            {
                _log.Warning("Quick set '{Name}' references unknown table '{Table}' in step '{Label}'",
                    set.Name, step.Table, step.Label);
            }
        }
    }

    /// <summary>
    /// Resolves a table name/ID using fuzzy matching.
    /// Supports: exact ID, display name, partial matches.
    /// </summary>
    private string? ResolveTableId(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return null;

        var tables = TableService.Instance.AvailableTables;

        // Try exact ID match first
        var exact = tables.FirstOrDefault(t =>
            t.Id.Equals(tableName, StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact.Id;

        // Try display name match
        var displayMatch = tables.FirstOrDefault(t =>
            t.DisplayName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
        if (displayMatch != null) return displayMatch.Id;

        // Try partial ID match (e.g., "characteridentity" matches "elements/characteridentity")
        var partialId = tables.FirstOrDefault(t =>
            t.Id.EndsWith($"/{tableName}", StringComparison.OrdinalIgnoreCase) ||
            t.Id.Equals(tableName, StringComparison.OrdinalIgnoreCase));
        if (partialId != null) return partialId.Id;

        // Try contains match on display name
        var containsMatch = tables.FirstOrDefault(t =>
            t.DisplayName.Replace(" ", "").Contains(tableName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
        if (containsMatch != null) return containsMatch.Id;

        // Try normalizing: remove spaces, lowercase
        var normalized = tableName.Replace(" ", "").ToLowerInvariant();
        var normalizedMatch = tables.FirstOrDefault(t =>
            t.Id.Replace("/", "").Replace(" ", "").ToLowerInvariant().Contains(normalized) ||
            t.DisplayName.Replace(" ", "").ToLowerInvariant().Contains(normalized));
        if (normalizedMatch != null) return normalizedMatch.Id;

        return null;
    }

    private void CreateDefaultConfig()
    {
        var defaults = new List<QuickSet>
        {
            GetDefaultNpcProfile(),
            GetDefaultLocationProfile(),
            GetDefaultCreatureProfile()
        };

        try
        {
            var directory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(defaults, JsonOptions);
            File.WriteAllText(_configPath, json);
            _log.Information("Created default quick sets config at {Path}", _configPath);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to create default quick sets config");
        }
    }

    private static QuickSet GetDefaultNpcProfile() => new()
    {
        Id = "npc_profile",
        Name = "NPC Profile",
        Description = "Generates a complete NPC dossier with identity, personality, and background",
        Steps =
        [
            new() { Label = "Identity", Table = "Character Identity" },
            new() { Label = "Description", Table = "Character Description" },
            new() { Label = "Appearance", Table = "Character Appearance" },
            new() { Label = "Background", Table = "Character Background" },
            new() { Label = "Personality", Table = "Character Personality" },
            new() { Label = "Motivation", Table = "Character Motivation" },
            new() { Label = "Skills", Table = "Character Skills" },
            new() { Label = "Trait", Table = "Character Trait" }
        ]
    };

    private static QuickSet GetDefaultLocationProfile() => new()
    {
        Id = "location_profile",
        Name = "Location Profile",
        Description = "Generates details for a location or place of interest",
        Steps =
        [
            new() { Label = "Type", Table = "Location" },
            new() { Label = "Description", Table = "Location Description" },
            new() { Label = "Atmosphere", Table = "Adventure Tone" },
            new() { Label = "Feature", Table = "Descriptor 1", Count = 1 },
            new() { Label = "Detail", Table = "Descriptor 2", Count = 1 }
        ]
    };

    private static QuickSet GetDefaultCreatureProfile() => new()
    {
        Id = "creature_profile",
        Name = "Creature Profile",
        Description = "Generates details for a creature or monster",
        Steps =
        [
            new() { Label = "Type", Table = "Creature" },
            new() { Label = "Abilities", Table = "Creature Abilities" },
            new() { Label = "Description", Table = "Creature Description" },
            new() { Label = "Behavior", Table = "Animal Act" }
        ]
    };

    private static string FindConfigPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var currentDir = new DirectoryInfo(baseDir);

        while (currentDir != null)
        {
            var dataPath = Path.Combine(currentDir.FullName, "data", "quicksets.json");
            if (File.Exists(dataPath) || Directory.Exists(Path.GetDirectoryName(dataPath)))
                return dataPath;

            currentDir = currentDir.Parent;
        }

        // Default to data directory relative to base
        return Path.Combine(baseDir, "data", "quicksets.json");
    }
}
