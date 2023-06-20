using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Rolling.Models.Definitions;
using Rolling.Models.Definitions.Expressions;
using Rolling.Utilities;

namespace Rolling.Visitors;

public abstract class ExpressionVisitor<TValue>
{
    public void Visit(SheetDefinition sheet)
    {
        Dictionary<string, DiceExpression> variables = sheet.Variables.ToDictionary(v => v.Name, v => v.Expression);
        Dictionary<string, TValue> values = new();

        TValue GetValueRec(string name, ImmutableHashSet<string> visited)
        {
            if (visited.Contains(name))
                throw new ArgumentException($"Circular dependency detected in variable '{name}'");

            if (values.TryGetValue(name, out TValue value))
                return value;

            visited = visited.Add(name);
            value = Visit(variables[name], n => GetValueRec(n, visited));
            values.Add(name, value);
            return value;
        }

        TValue GetValue(string name) => GetValueRec(name, ImmutableHashSet<string>.Empty);

        foreach (var v in variables)
        {
            Visit(v.Value, GetValue);
        }

        foreach (var section in sheet.Sections)
        {
            foreach (var roll in section.Rolls)
            {
                VisitRoll(section.Name, section.Type, roll, Visit(roll.Expression, GetValue), roll.ConditionalExpression.Select(r => Visit(r, GetValue)));
            }
        }
    }

    public virtual void VisitRoll(Maybe<string> sectionName,
        RollSectionType sectionType,
        DiceRollDefinition roll,
        TValue value,
        Maybe<TValue> conditionalValue)
    {
    }

    public TValue Visit(DiceExpression expr, Func<string, TValue> lookup)
    {
        return expr switch
        {
            BinaryDiceExpression binaryDiceExpression => VisitBinaryExpression(
                binaryDiceExpression.Operator,
                Visit(binaryDiceExpression.Left, lookup),
                Visit(binaryDiceExpression.Right, lookup)
            ),
            ConstantExpression constantExpression => VisitConstantExpression(constantExpression.Amount),
            DiceRollExpression diceRollExpression => VisitDiceRollExpression(diceRollExpression.Dice),
            ReferenceExpression referenceExpression => lookup(referenceExpression.Name),
            TaggedExpression taggedExpression => VisitTaggedExpression(
                taggedExpression.Tag,
                Visit(taggedExpression.Expression, lookup)
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(expr))
        };
    }

    public virtual TValue VisitBinaryExpression(char op, TValue left, TValue right) =>
        op switch
        {
            '-' => VisitSubtractExpression(left, right),
            '+' => VisitAddExpression(left, right),
            '*' => VisitMultiplyExpression(left, right),
            '/' => VisitDivideExpression(left, right),
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };

    public abstract TValue VisitDivideExpression(TValue left, TValue right);

    public abstract TValue VisitMultiplyExpression(TValue left, TValue right);

    public abstract TValue VisitAddExpression(TValue left, TValue right);

    public abstract TValue VisitSubtractExpression(TValue left, TValue right);

    public abstract TValue VisitConstantExpression(int value);
    
    public abstract TValue VisitDiceRollExpression(DiceSpecification dice);

    public virtual TValue VisitTaggedExpression(string tag, TValue expressionValue)
    {
        return expressionValue;
    }
}