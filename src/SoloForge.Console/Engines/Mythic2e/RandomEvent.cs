using SoloForge.Console.Models;
using SoloForge.Console.Services;

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
        var eventFocus = GenerateEventFocus();
        var eventAction = GenerateAction();
        var state = AdventureStateManager.Instance;

        string? selectedCharacter = null;
        string? selectedThread = null;
        bool isNewNpc = eventFocus == "New NPC";
        bool listWasEmpty = false;

        // Handle NPC-related focus types
        if (NpcFocusTypes.Contains(eventFocus))
        {
            var character = state.State.GetRandomCharacter();
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
            var thread = state.State.GetRandomThread();
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
        return TableService.Instance.GetFusionPair("action1", "action2");
    }

    /// <summary>
    /// Checks if the given focus type is NPC-related.
    /// </summary>
    public static bool IsNpcFocus(string focus) => NpcFocusTypes.Contains(focus) || focus == "New NPC";

    /// <summary>
    /// Checks if the given focus type is Thread-related.
    /// </summary>
    public static bool IsThreadFocus(string focus) => ThreadFocusTypes.Contains(focus);
}
