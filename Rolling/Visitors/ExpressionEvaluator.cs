using System;
using Rolling.Models;
using Rolling.Models.Definitions;
using Rolling.Models.Definitions.Expressions;
using ConstantExpression = Rolling.Models.Definitions.Expressions.ConstantExpression;

namespace Rolling.Visitors;

public abstract class ExpressionEvaluator<TValue>
{
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

    private TValue VisitBinaryExpression(char op, TValue left, TValue right) =>
        op switch
        {
            '-' => VisitSubtractExpression(left, right),
            '+' => VisitAddExpression(left, right),
            '*' => VisitMultiplyExpression(left, right),
            '/' => VisitDivideExpression(left, right),
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };

    protected abstract TValue VisitDivideExpression(TValue left, TValue right);

    protected abstract TValue VisitMultiplyExpression(TValue left, TValue right);

    protected abstract TValue VisitAddExpression(TValue left, TValue right);

    protected abstract TValue VisitSubtractExpression(TValue left, TValue right);

    protected abstract TValue VisitConstantExpression(int value);

    protected abstract TValue VisitDiceRollExpression(DiceSpecification dice);

    protected virtual TValue VisitTaggedExpression(string tag, TValue expressionValue)
    {
        return expressionValue;
    }
}