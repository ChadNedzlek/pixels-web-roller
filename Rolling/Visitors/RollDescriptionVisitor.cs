using System;
using System.Text;
using Rolling.Models.Definitions;
using Rolling.Utilities;

namespace Rolling.Visitors;

public class RollDescriptionVisitor : ExpressionVisitor<string>
{
    public override string VisitDivideExpression(string left, string right)
    {
        return $"{left} / {right}";
    }

    public override string VisitMultiplyExpression(string left, string right)
    {
        return $"{left} * {right}";
    }

    public override string VisitAddExpression(string left, string right)
    {
        return $"{left} + {right}";
    }

    public override string VisitSubtractExpression(string left, string right)
    {
        return $"{left} - {right}";
    }

    public override string VisitConstantExpression(int value)
    {
        return value.ToString();
    }

    public override string VisitDiceRollExpression(DiceSpecification dice)
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

    public override string VisitTaggedExpression(string tag, string expressionValue)
    {
        return $"{expressionValue} {tag}";
    }
}