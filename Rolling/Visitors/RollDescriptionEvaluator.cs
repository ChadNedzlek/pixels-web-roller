using System;
using System.Text;
using Rolling.Models;
using Rolling.Models.Definitions;

namespace Rolling.Visitors;

public class RollDescriptionEvaluator : ExpressionEvaluator<string>
{
    public string Evaluate(DiceExpression expression)
    {
        return Visit(expression, s => $"@{s}");
    }

    protected override string VisitDivideExpression(string left, string right)
    {
        return $"{left} / {right}";
    }

    protected override string VisitMultiplyExpression(string left, string right)
    {
        return $"{left} * {right}";
    }

    protected override string VisitAddExpression(string left, string right)
    {
        return $"{left} + {right}";
    }

    protected override string VisitSubtractExpression(string left, string right)
    {
        return $"{left} - {right}";
    }

    protected override string VisitConstantExpression(int value)
    {
        return value.ToString();
    }

    protected override string VisitDiceRollExpression(DiceSpecification dice)
    {
        StringBuilder b = new StringBuilder();
        if (dice.Count != 1)
            b.Append(dice.Count);
        b.Append('d');
        b.Append(dice.Sides);
        foreach (var mod in dice.Modifiers)
        {
            b.Append(mod.Type switch
                {
                    DiceModType.Keep => "k" + (mod.Count == 1 ? "h" : mod.Count.ToString()),
                    DiceModType.Drop => $"d{mod.Count}",
                    DiceModType.CriticalSuccess => $"c>{mod.Count}",
                    DiceModType.CriticalFailure => $"c<{mod.Count}",
                    _ => throw new ArgumentOutOfRangeException()
                }
            );
        }

        return b.ToString();
    }

    protected override string VisitTaggedExpression(string tag, string expressionValue)
    {
        return $"{expressionValue} {tag}";
    }
}