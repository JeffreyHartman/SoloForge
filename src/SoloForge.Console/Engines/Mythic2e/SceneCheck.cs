using SoloForge.Console.Models;

namespace SoloForge.Console.Engines.Mythic2e;

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
        if (chaosFactor < 1 || chaosFactor > 9)
            throw new ArgumentException("Chaos factor must be between 1 and 9", nameof(chaosFactor));

        int roll = Random.Shared.Next(1, 11); // d10

        if (roll > chaosFactor)
        {
            return new SceneCheckResult(roll, "Normal Scene");
        }

        // Roll is <= chaos factor
        if (roll % 2 == 0)
        {
            // Even roll = Interrupt Scene with random event
            var randomEvent = RandomEvent.Generate();
            return new SceneCheckResult(roll, "Interrupt Scene!", RandomEvent: randomEvent);
        }
        else
        {
            // Odd roll = Altered Scene with adjustment
            var adjustment = GenerateSceneAdjustment();
            return new SceneCheckResult(roll, "Altered Scene!", SceneAdjustment: adjustment);
        }
    }

    /// <summary>
    /// Generates a scene adjustment by rolling d10.
    /// </summary>
    public static string GenerateSceneAdjustment()
    {
        int roll = Random.Shared.Next(1, 11); // d10
        return SceneAdjustmentTable[roll - 1];
    }
}
