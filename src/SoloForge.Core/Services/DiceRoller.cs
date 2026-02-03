using SoloForge.Console.Models;

namespace SoloForge.Console.Services;

public sealed class DiceRoller
{
    private static readonly Lazy<DiceRoller> _instance = new(() => new DiceRoller());
    public static DiceRoller Instance => _instance.Value;

    private DiceRoller() { }

    public DiceRollResult Roll(DiceExpression expression)
    {
        var termResults = new List<DiceTermResult>();
        var diceTotal = 0;

        foreach (var term in expression.Terms)
        {
            var rolls = new List<int>();
            for (var i = 0; i < term.Count; i++)
            {
                rolls.Add(Random.Shared.Next(1, term.Faces + 1));
            }

            var termResult = new DiceTermResult(term.Count, term.Faces, term.Sign, rolls);
            termResults.Add(termResult);
            diceTotal += termResult.Total;
        }

        var total = diceTotal + expression.Modifier;
        return new DiceRollResult(expression, termResults, expression.Modifier, total, diceTotal);
    }
}
