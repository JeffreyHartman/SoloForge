using SoloForge.Console.Models;

using SoloForge.Console.Services;

namespace SoloForge.Console.Engines.Mythic2e;

/// <summary>
/// Implements the Mythic 2e Fate Check system.
/// </summary>
public static class FateCheck
{
    // Fate Chart: 9 odds rows x 9 chaos columns
    // Each cell contains [exceptionalYes, yes, no] thresholds
    // -1 means impossible to roll (no Exceptional Yes possible)
    // 999 means impossible to roll (no Exceptional No possible)
    private static readonly int[][][] FateChart =
    [
        // Impossible
        [[-1, 1, 81], [-1, 1, 81], [-1, 1, 81], [1, 5, 82], [2, 10, 83], [3, 15, 84], [5, 25, 86], [7, 35, 88], [10, 50, 91]],
        // Nearly Impossible
        [[-1, 1, 81], [-1, 1, 81], [1, 5, 82], [2, 10, 83], [3, 15, 84], [5, 25, 86], [7, 35, 88], [10, 50, 91], [13, 65, 94]],
        // Very Unlikely
        [[-1, 1, 81], [1, 5, 82], [2, 10, 83], [3, 15, 84], [5, 25, 86], [7, 35, 88], [10, 50, 91], [13, 65, 94], [15, 75, 96]],
        // Unlikely
        [[1, 5, 82], [2, 10, 83], [3, 15, 84], [5, 25, 86], [7, 35, 88], [10, 50, 91], [13, 65, 94], [15, 75, 96], [17, 85, 98]],
        // 50/50
        [[2, 10, 83], [3, 15, 84], [5, 25, 86], [7, 35, 88], [10, 50, 91], [13, 65, 94], [15, 75, 96], [17, 85, 98], [18, 90, 99]],
        // Likely
        [[3, 15, 84], [5, 25, 86], [7, 35, 88], [10, 50, 91], [13, 65, 94], [15, 75, 96], [17, 85, 98], [18, 90, 99], [19, 95, 100]],
        // Very Likely
        [[5, 25, 86], [7, 35, 88], [10, 50, 91], [13, 65, 94], [15, 75, 96], [17, 85, 98], [18, 90, 99], [19, 95, 100], [20, 99, 999]],
        // Nearly Certain
        [[7, 35, 88], [10, 50, 91], [13, 65, 94], [15, 75, 96], [17, 85, 98], [18, 90, 99], [19, 95, 100], [20, 99, 999], [20, 99, 999]],
        // Certain
        [[10, 50, 91], [13, 65, 94], [15, 75, 96], [17, 85, 98], [18, 90, 99], [19, 95, 100], [20, 99, 999], [20, 99, 999], [20, 99, 999]]
    ];


    /// <summary>
    /// Performs a Fate Check with the given chaos factor and odds.
    /// </summary>
    public static FateCheckResult PerformCheck(int chaosFactor, Odds odds)
    {
        return PerformCheck(chaosFactor, odds, SharedRng.Instance);
    }

    public static FateCheckResult PerformCheck(int chaosFactor, Odds odds, IRng rng)
    {
        // Validate inputs
        if (chaosFactor < 1 || chaosFactor > 9)
            throw new ArgumentException("Chaos factor must be between 1 and 9", nameof(chaosFactor));

        if (!Enum.IsDefined(typeof(Odds), odds))
            throw new ArgumentException("Invalid odds value", nameof(odds));

        if (rng == null)
            throw new ArgumentNullException(nameof(rng));

        // Roll d100 (1-100)
        int roll = rng.Next(1, 101);

        // Get thresholds from fate chart
        int oddsIndex = (int)odds;
        int chaosIndex = chaosFactor - 1; // Convert 1-9 to 0-8
        int[] thresholds = FateChart[oddsIndex][chaosIndex];

        int exceptionalYesThreshold = thresholds[0];
        int yesThreshold = thresholds[1];
        int noThreshold = thresholds[2];

        // Determine result
        string result = roll <= exceptionalYesThreshold && exceptionalYesThreshold != -1
            ? "Exceptional Yes"
            : roll <= yesThreshold
                ? "Yes"
                : roll <= noThreshold
                    ? "No"
                    : "Exceptional No";

        // Check for random event (doubles and digit <= chaos factor)
        bool randomEventTriggered = IsDoublesAndTriggerEvent(roll, chaosFactor);

        return new FateCheckResult(roll, result, randomEventTriggered);
    }

    /// <summary>
    /// Checks if the roll is doubles and if the digit is <= chaos factor.
    /// </summary>
    private static bool IsDoublesAndTriggerEvent(int roll, int chaosFactor)
    {
        // Check if roll is doubles (11, 22, 33, ..., 99)
        int tens = roll / 10;
        int ones = roll % 10;

        if (tens != ones)
            return false;

        // Check if the digit (1-9) is <= chaos factor
        return tens <= chaosFactor;
    }
}
