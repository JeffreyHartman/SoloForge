using SoloForge.Console.Models;

namespace SoloForge.Console.Engines.Mythic2e;

/// <summary>
/// Implements the Mythic 2e Random Event system.
/// </summary>
public static class RandomEvent
{
    /// <summary>
    /// Event Focus table mapping d100 rolls to event types.
    /// </summary>
    private static readonly (Range Range, string Focus)[] EventFocusTable =
    [
        (1..5, "Remote Event"),
        (6..10, "Ambiguous Event"),
        (11..20, "New NPC"),
        (21..40, "NPC Action"),
        (41..45, "NPC Negative"),
        (46..50, "NPC Positive"),
        (51..55, "Move Toward a Thread"),
        (56..65, "Move Away from a Thread"),
        (66..70, "Close a Thread"),
        (71..80, "PC Negative"),
        (81..85, "PC Positive"),
        (86..100, "Current Context")
    ];

    private static string[]? _action1Words;
    private static string[]? _action2Words;

    /// <summary>
    /// Generates a complete random event with focus and action.
    /// </summary>
    public static RandomEventResult Generate()
    {
        var eventFocus = GenerateEventFocus();
        var eventAction = GenerateAction();
        return new RandomEventResult(eventFocus, eventAction);
    }

    /// <summary>
    /// Generates an event focus by rolling d100 and consulting the table.
    /// </summary>
    public static string GenerateEventFocus()
    {
        int roll = Random.Shared.Next(1, 101);

        foreach (var (range, focus) in EventFocusTable)
        {
            if (roll >= range.Start.Value && roll <= range.End.Value)
            {
                return focus;
            }
        }

        return "Invalid Event Focus";
    }

    /// <summary>
    /// Generates an action by combining random words from action1.txt and action2.txt.
    /// </summary>
    public static string GenerateAction()
    {
        var action1 = GetRandomWord(ref _action1Words, "action1.txt");
        var action2 = GetRandomWord(ref _action2Words, "action2.txt");
        return $"{action1} {action2}";
    }

    private static string GetRandomWord(ref string[]? cachedWords, string filename)
    {
        if (cachedWords == null)
        {
            var filePath = GetDataFilePath(filename);
            if (!File.Exists(filePath))
            {
                return $"[{filename} not found]";
            }
            cachedWords = File.ReadAllLines(filePath)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrEmpty(line))
                .ToArray();
        }

        if (cachedWords.Length == 0)
        {
            return $"[{filename} is empty]";
        }

        return cachedWords[Random.Shared.Next(cachedWords.Length)];
    }

    private static string GetDataFilePath(string filename)
    {
        // Try to find data directory relative to executable
        var baseDir = AppContext.BaseDirectory;

        // Walk up directories looking for 'data' folder at solution root
        var currentDir = new DirectoryInfo(baseDir);
        while (currentDir != null)
        {
            var dataPath = Path.Combine(currentDir.FullName, "data", filename);
            if (File.Exists(dataPath))
            {
                return dataPath;
            }
            currentDir = currentDir.Parent;
        }

        // Fallback to relative path from current directory
        return Path.Combine("data", filename);
    }
}
