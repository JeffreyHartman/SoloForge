using System.Linq;

namespace SoloForge.Console.Models;

public sealed record DiceExpression(IReadOnlyList<DiceTerm> Terms, int Modifier)
{
    public static bool TryParse(string input, out DiceExpression? expression, out string error)
    {
        expression = null;
        error = "";

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "Enter a dice expression like 2d6+1.";
            return false;
        }

        var cleaned = new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
        if (cleaned.Length == 0)
        {
            error = "Enter a dice expression like 2d6+1.";
            return false;
        }

        var terms = new List<DiceTerm>();
        var modifier = 0;
        var index = 0;

        while (index < cleaned.Length)
        {
            var sign = 1;
            if (cleaned[index] == '+' || cleaned[index] == '-')
            {
                sign = cleaned[index] == '-' ? -1 : 1;
                index++;
            }

            if (index >= cleaned.Length)
            {
                error = "Expression cannot end with a sign.";
                return false;
            }

            if (cleaned[index] == 'd')
            {
                if (!TryParseFaces(cleaned, ref index, out var faces, out error))
                    return false;

                if (!ValidateDice(1, faces, out error))
                    return false;

                terms.Add(new DiceTerm(1, faces, sign));
                continue;
            }

            if (!char.IsDigit(cleaned[index]))
            {
                error = $"Unexpected character '{cleaned[index]}' in expression.";
                return false;
            }

            var number = ParseNumber(cleaned, ref index);
            if (number <= 0)
            {
                error = "Dice counts and modifiers must be greater than zero.";
                return false;
            }

            if (index < cleaned.Length && cleaned[index] == 'd')
            {
                if (!TryParseFaces(cleaned, ref index, out var faces, out error))
                    return false;

                if (!ValidateDice(number, faces, out error))
                    return false;

                terms.Add(new DiceTerm(number, faces, sign));
                continue;
            }

            modifier += sign * number;
        }

        if (terms.Count == 0 && modifier == 0)
        {
            error = "Enter at least one die or modifier.";
            return false;
        }

        expression = new DiceExpression(terms, modifier);
        return true;
    }

    public string ToDisplayString()
    {
        var parts = new List<string>();

        foreach (var term in Terms)
        {
            var sign = term.Sign < 0 ? "-" : parts.Count > 0 ? "+" : "";
            var count = term.Count == 1 ? "" : term.Count.ToString();
            var faces = term.Faces == 100 ? "%" : term.Faces.ToString();
            parts.Add($"{sign}{count}d{faces}");
        }

        if (Modifier != 0)
        {
            var sign = Modifier > 0 ? "+" : "-";
            parts.Add($"{sign}{Math.Abs(Modifier)}");
        }

        return parts.Count == 0 ? Modifier.ToString() : string.Concat(parts);
    }

    private static bool ValidateDice(int count, int faces, out string error)
    {
        if (count <= 0)
        {
            error = "Dice count must be at least 1.";
            return false;
        }

        if (faces < 2)
        {
            error = "Dice faces must be at least 2.";
            return false;
        }

        if (faces == 100)
        {
            error = "Use d% for percentile rolls.";
            return false;
        }

        error = "";
        return true;
    }

    private static bool TryParseFaces(string cleaned, ref int index, out int faces, out string error)
    {
        faces = 0;
        error = "";

        if (cleaned[index] != 'd')
        {
            error = "Expected a 'd' in dice expression.";
            return false;
        }

        index++;

        if (index >= cleaned.Length)
        {
            error = "Dice expression is missing faces (e.g., d6).";
            return false;
        }

        if (cleaned[index] == '%')
        {
            faces = 100;
            index++;
            return true;
        }

        if (!char.IsDigit(cleaned[index]))
        {
            error = "Dice faces must be a number (e.g., d6 or d20).";
            return false;
        }

        faces = ParseNumber(cleaned, ref index);
        return true;
    }

    private static int ParseNumber(string cleaned, ref int index)
    {
        var start = index;
        while (index < cleaned.Length && char.IsDigit(cleaned[index]))
            index++;

        var numberText = cleaned[start..index];
        return int.TryParse(numberText, out var value) ? value : 0;
    }
}

public sealed record DiceTerm(int Count, int Faces, int Sign);
