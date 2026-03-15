using SoloForge.Core.Services;

namespace SoloForge.Core.Engines.Mythic2e;

/// <summary>
/// Implements the Mythic 2e "Discovering Meaning" system for generating
/// word pairs and complex interpretive results.
/// </summary>
public static class MeaningEngine
{
    private static readonly ITableWordSource _defaultWordSource = new TableServiceWordSource();

    /// <summary>
    /// NPC profile attributes and their corresponding element table IDs.
    /// </summary>
    public static readonly IReadOnlyList<(string Attribute, string TableId)> NpcProfileTables =
    [
        ("Identity", "elements/characteridentity"),
        ("Description", "elements/characterdescription"),
        ("Appearance", "elements/characterappearance"),
        ("Background", "elements/characterbackground"),
        ("Personality", "elements/characterpersonality"),
        ("Motivation", "elements/charactermotivation"),
        ("Skills", "elements/characterskills"),
        ("Trait", "elements/charactertrait")
    ];

    /// <summary>
    /// Quick roll presets for common generation tasks.
    /// </summary>
    public static readonly IReadOnlyList<QuickRollPreset> QuickRolls =
    [
        new("Action", "What is happening?", "action1", "action2"),
        new("Description", "What is it like?", "descriptor1", "descriptor2")
    ];

    /// <summary>
    /// Generates an Action meaning (action1 + action2).
    /// </summary>
    public static MeaningResult GenerateAction()
    {
        return GenerateAction(_defaultWordSource);
    }

    public static MeaningResult GenerateAction(ITableWordSource wordSource)
    {
        if (wordSource == null)
            throw new ArgumentNullException(nameof(wordSource));

        var word1 = wordSource.GetRandomWord("action1");
        var word2 = wordSource.GetRandomWord("action2");
        return new MeaningResult("Action", word1, word2);
    }

    /// <summary>
    /// Generates a Description meaning (descriptor1 + descriptor2).
    /// </summary>
    public static MeaningResult GenerateDescription()
    {
        return GenerateDescription(_defaultWordSource);
    }

    public static MeaningResult GenerateDescription(ITableWordSource wordSource)
    {
        if (wordSource == null)
            throw new ArgumentNullException(nameof(wordSource));

        var word1 = wordSource.GetRandomWord("descriptor1");
        var word2 = wordSource.GetRandomWord("descriptor2");
        return new MeaningResult("Description", word1, word2);
    }

    /// <summary>
    /// Generates a word pair from a single element table.
    /// </summary>
    public static MeaningResult GenerateFromTable(string tableId, string? displayName = null)
    {
        return GenerateFromTable(_defaultWordSource, tableId, displayName);
    }

    public static MeaningResult GenerateFromTable(ITableWordSource wordSource, string tableId, string? displayName = null)
    {
        if (wordSource == null)
            throw new ArgumentNullException(nameof(wordSource));

        var table = wordSource.FindTable(tableId);
        var name = displayName ?? table?.DisplayName ?? tableId;
        var word1 = wordSource.GetRandomWord(tableId);
        var word2 = wordSource.GetRandomWord(tableId);
        return new MeaningResult(name, word1, word2);
    }

    /// <summary>
    /// Generates a fusion pair from two different tables.
    /// </summary>
    public static MeaningResult GenerateFusion(string tableId1, string tableId2)
    {
        return GenerateFusion(_defaultWordSource, tableId1, tableId2);
    }

    public static MeaningResult GenerateFusion(ITableWordSource wordSource, string tableId1, string tableId2)
    {
        if (wordSource == null)
            throw new ArgumentNullException(nameof(wordSource));

        var table1 = wordSource.FindTable(tableId1);
        var table2 = wordSource.FindTable(tableId2);
        var name = $"{table1?.DisplayName ?? tableId1} + {table2?.DisplayName ?? tableId2}";
        var word1 = wordSource.GetRandomWord(tableId1);
        var word2 = wordSource.GetRandomWord(tableId2);
        return new MeaningResult(name, word1, word2, IsFusion: true);
    }

    /// <summary>
    /// Generates a complete NPC profile with all attributes.
    /// </summary>
    public static NpcProfile GenerateNpcProfile()
    {
        var attributes = new Dictionary<string, MeaningResult>();

        foreach (var (attribute, tableId) in NpcProfileTables)
        {
            attributes[attribute] = GenerateFromTable(_defaultWordSource, tableId, attribute);
        }

        return new NpcProfile(attributes);
    }
}

/// <summary>
/// Represents a generated meaning result (word pair).
/// </summary>
public record MeaningResult(
    string TableName,
    string Word1,
    string Word2,
    bool IsFusion = false
)
{
    public string Combined => $"{Word1} {Word2}";
}

/// <summary>
/// Represents a quick roll preset configuration.
/// </summary>
public record QuickRollPreset(
    string Name,
    string Description,
    string Table1Id,
    string Table2Id
);

/// <summary>
/// Represents a complete NPC profile with all generated attributes.
/// </summary>
public record NpcProfile(IReadOnlyDictionary<string, MeaningResult> Attributes);
