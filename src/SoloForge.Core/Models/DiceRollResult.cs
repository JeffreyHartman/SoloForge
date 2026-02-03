using System.Linq;

namespace SoloForge.Console.Models;

public sealed record DiceRollResult(
    DiceExpression Expression,
    IReadOnlyList<DiceTermResult> Terms,
    int Modifier,
    int Total,
    int DiceTotal
)
{
    public string Summary => $"{Expression.ToDisplayString()} = {Total}";

    public string BuildBreakdown()
    {
        var parts = new List<string>();

        foreach (var term in Terms)
        {
            var faces = term.Faces == 100 ? "%" : term.Faces.ToString();
            var prefix = term.Sign < 0 ? "-" : "";
            var count = term.Count == 1 ? "" : term.Count.ToString();
            parts.Add($"{prefix}{count}d{faces}: {string.Join(",", term.Rolls)}");
        }

        if (Modifier != 0)
        {
            var sign = Modifier > 0 ? "+" : "-";
            parts.Add($"{sign}{Math.Abs(Modifier)}");
        }

        if (parts.Count == 0)
            return "";

        var diceTotalText = DiceTotal == Total ? $"Total {Total}" : $"Dice {DiceTotal}, Total {Total}";
        return $"{string.Join(" | ", parts)}  ({diceTotalText})";
    }
}

public sealed record DiceTermResult(int Count, int Faces, int Sign, IReadOnlyList<int> Rolls)
{
    public int Total => Rolls.Sum() * Sign;
}
