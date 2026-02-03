using SoloForge.Console.Models;

namespace SoloForge.Console.Services;

/// <summary>
/// Manages the current adventure state (characters, threads, etc.).
/// Provides a singleton instance for global access.
/// </summary>
public sealed class AdventureStateManager
{
    private static readonly Lazy<AdventureStateManager> _instance = new(() => new AdventureStateManager());
    public static AdventureStateManager Instance => _instance.Value;

    /// <summary>
    /// The current adventure state.
    /// </summary>
    public AdventureState State { get; private set; } = new();

    private AdventureStateManager() { }

    /// <summary>
    /// Resets the adventure state to a new empty state.
    /// </summary>
    public void Reset() => State = new AdventureState();

    /// <summary>
    /// Loads adventure state from saved data (used by CampaignService).
    /// </summary>
    public void LoadState(AdventureState state) => State = state;

    /// <summary>
    /// Adds a new character to the adventure.
    /// </summary>
    public Character AddCharacter(string name, string? description = null)
    {
        var character = new Character { Name = name, Description = description };
        State.Characters.Add(character);
        return character;
    }

    /// <summary>
    /// Removes a character from the adventure.
    /// </summary>
    public bool RemoveCharacter(Character character) =>
        State.Characters.Remove(character);

    /// <summary>
    /// Adds a new plot thread to the adventure.
    /// </summary>
    public PlotThread AddThread(string name, string? description = null)
    {
        var thread = new PlotThread { Name = name, Description = description };
        State.ActiveThreads.Add(thread);
        return thread;
    }

    /// <summary>
    /// Closes a thread (moves it to the closed list).
    /// </summary>
    public void CloseThread(PlotThread thread)
    {
        if (State.ActiveThreads.Remove(thread))
        {
            thread.ClosedAt = DateTime.Now;
            State.ClosedThreads.Add(thread);
        }
    }

    /// <summary>
    /// Reopens a closed thread (moves it back to active).
    /// </summary>
    public void ReopenThread(PlotThread thread)
    {
        if (State.ClosedThreads.Remove(thread))
        {
            thread.ClosedAt = null;
            State.ActiveThreads.Add(thread);
        }
    }

    /// <summary>
    /// Removes a thread completely (from either list).
    /// </summary>
    public bool RemoveThread(PlotThread thread) =>
        State.ActiveThreads.Remove(thread) || State.ClosedThreads.Remove(thread);

    /// <summary>
    /// Gets the count of active characters.
    /// </summary>
    public int CharacterCount => State.Characters.Count;

    /// <summary>
    /// Gets the count of active threads.
    /// </summary>
    public int ActiveThreadCount => State.ActiveThreads.Count;

    /// <summary>
    /// Gets the count of closed threads.
    /// </summary>
    public int ClosedThreadCount => State.ClosedThreads.Count;
}
