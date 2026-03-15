using SoloForge.Core.Models;

using SoloForge.Core.Services;

namespace SoloForge.Core.Engines.Mythic2e;

/// <summary>
/// Implements the Mythic 2e Scene Check system.
/// </summary>
public static class SceneCheck
{
    private static readonly string[] SceneAdjustmentTable =
    [
        "Remove a Character",
        "Add a Character",
        "Reduce/Remove an Activity",
        "Increase an Activity",
        "Remove an Object",
        "Add an Object",
        "Make 2 Adjustments",
        "Make 2 Adjustments",
        "Make 2 Adjustments",
        "Make 2 Adjustments"
    ];

    /// <summary>
    /// Performs a scene check with the given chaos factor.
    /// </summary>
    public static SceneCheckResult PerformCheck(int chaosFactor)
    {
        return PerformCheck(chaosFactor, SharedRng.Instance, null);
    }

    public static SceneCheckResult PerformCheck(int chaosFactor, IRng rng, Func<RandomEventResult>? randomEventGenerator)
    {
        if (chaosFactor < 1 || chaosFactor > 9)
            throw new ArgumentException("Chaos factor must be between 1 and 9", nameof(chaosFactor));

        if (rng == null)
            throw new ArgumentNullException(nameof(rng));

        int roll = rng.Next(1, 11); // d10

        if (roll > chaosFactor)
        {
            return new SceneCheckResult(roll, "Normal Scene");
        }

        // Roll is <= chaos factor
        if (roll % 2 == 0)
        {
            // Even roll = Interrupt Scene with random event
            var randomEvent = randomEventGenerator?.Invoke() ?? RandomEvent.Generate();
            return new SceneCheckResult(roll, "Interrupt Scene!", RandomEvent: randomEvent);
        }

        // Odd roll = Altered Scene with adjustment
        var adjustment = GenerateSceneAdjustment(rng);
        return new SceneCheckResult(roll, "Altered Scene!", SceneAdjustment: adjustment);
    }

    /// <summary>
    /// Generates a scene adjustment by rolling d10.
    /// </summary>
    public static string GenerateSceneAdjustment()
    {
        return GenerateSceneAdjustment(SharedRng.Instance);
    }

    public static string GenerateSceneAdjustment(IRng rng)
    {
        if (rng == null)
            throw new ArgumentNullException(nameof(rng));

        int roll = rng.Next(1, 11); // d10
        return SceneAdjustmentTable[roll - 1];
    }
}
