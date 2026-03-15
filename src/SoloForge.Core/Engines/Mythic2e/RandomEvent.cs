using SoloForge.Core.Models;
using SoloForge.Core.Services;

namespace SoloForge.Core.Engines.Mythic2e;

/// <summary>
/// Implements the Mythic 2e Random Event system.
/// </summary>
public static class RandomEvent
{
    private static readonly ITableWordSource _defaultWordSource = new TableServiceWordSource();

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

    /// <summary>
    /// Focus types that involve NPCs/Characters.
    /// </summary>
    private static readonly HashSet<string> NpcFocusTypes =
    [
        "NPC Action",
        "NPC Negative",
        "NPC Positive"
    ];

    /// <summary>
    /// Focus types that involve Threads.
    /// </summary>
    private static readonly HashSet<string> ThreadFocusTypes =
    [
        "Move Toward a Thread",
        "Move Away from a Thread",
        "Close a Thread"
    ];

    /// <summary>
    /// Generates a complete random event with focus and action.
    /// Integrates with adventure lists for NPC and Thread selection.
    /// </summary>
    public static RandomEventResult Generate()
    {
        return Generate(AdventureStateManager.Instance.State, SharedRng.Instance, _defaultWordSource);
    }

    public static RandomEventResult Generate(AdventureState state, IRng rng, ITableWordSource wordSource)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));

        if (rng == null)
            throw new ArgumentNullException(nameof(rng));

        if (wordSource == null)
            throw new ArgumentNullException(nameof(wordSource));

        var eventFocus = GenerateEventFocus(rng);
        var eventAction = GenerateAction(wordSource);

        string? selectedCharacter = null;
        string? selectedThread = null;
        bool isNewNpc = eventFocus == "New NPC";
        bool listWasEmpty = false;

        // Handle NPC-related focus types
        if (NpcFocusTypes.Contains(eventFocus))
        {
            var character = GetRandomCharacter(state, rng);
            if (character != null)
            {
                selectedCharacter = character.Name;
            }
            else
            {
                listWasEmpty = true;
            }
        }

        // Handle Thread-related focus types
        if (ThreadFocusTypes.Contains(eventFocus))
        {
            var thread = GetRandomThread(state, rng);
            if (thread != null)
            {
                selectedThread = thread.Name;
            }
            else
            {
                listWasEmpty = true;
            }
        }

        return new RandomEventResult(
            eventFocus,
            eventAction,
            selectedCharacter,
            selectedThread,
            isNewNpc,
            listWasEmpty
        );
    }

    /// <summary>
    /// Generates an event focus by rolling d100 and consulting the table.
    /// </summary>
    public static string GenerateEventFocus()
    {
        return GenerateEventFocus(SharedRng.Instance);
    }

    public static string GenerateEventFocus(IRng rng)
    {
        if (rng == null)
            throw new ArgumentNullException(nameof(rng));

        int roll = rng.Next(1, 101);

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
        return GenerateAction(_defaultWordSource);
    }

    public static string GenerateAction(ITableWordSource wordSource)
    {
        if (wordSource == null)
            throw new ArgumentNullException(nameof(wordSource));

        return wordSource.GetFusionPair("action1", "action2");
    }

    /// <summary>
    /// Checks if the given focus type is NPC-related.
    /// </summary>
    public static bool IsNpcFocus(string focus) => NpcFocusTypes.Contains(focus) || focus == "New NPC";

    /// <summary>
    /// Checks if the given focus type is Thread-related.
    /// </summary>
    public static bool IsThreadFocus(string focus) => ThreadFocusTypes.Contains(focus);

    private static Character? GetRandomCharacter(AdventureState state, IRng rng)
    {
        if (state.Characters.Count == 0)
        {
            return null;
        }

        var index = rng.Next(0, state.Characters.Count);
        return state.Characters[index];
    }

    private static PlotThread? GetRandomThread(AdventureState state, IRng rng)
    {
        if (state.ActiveThreads.Count == 0)
        {
            return null;
        }

        var index = rng.Next(0, state.ActiveThreads.Count);
        return state.ActiveThreads[index];
    }
}
