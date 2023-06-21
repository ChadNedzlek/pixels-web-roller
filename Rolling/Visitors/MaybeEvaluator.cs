using Rolling.Models;
using Rolling.Utilities;

namespace Rolling.Visitors;

internal class MaybeEvaluator<TValue> : ExpressionEvaluator<Maybe<TValue>>
{
    private readonly ExpressionEvaluator<TValue> _impl;

    public MaybeEvaluator(ExpressionEvaluator<TValue> impl)
    {
        _impl = impl;
    }

    public override Maybe<TValue> VisitTaggedExpression(string tag, Maybe<TValue> expressionValue)
    {
        return _impl.VisitTaggedExpression(tag, expressionValue.OrDefault());
    }
    
    public override Maybe<TValue> VisitDivideExpression(Maybe<TValue> left, Maybe<TValue> right)
    {
        return _impl.VisitDivideExpression(left.OrDefault(), right.OrDefault());
    }

    public override Maybe<TValue> VisitMultiplyExpression(Maybe<TValue> left, Maybe<TValue> right)
    {
        return _impl.VisitMultiplyExpression(left.OrDefault(), right.OrDefault());
    }

    public override Maybe<TValue> VisitAddExpression(Maybe<TValue> left, Maybe<TValue> right)
    {
        return _impl.VisitAddExpression(left.OrDefault(), right.OrDefault());
    }

    public override Maybe<TValue> VisitSubtractExpression(Maybe<TValue> left, Maybe<TValue> right)
    {
        return _impl.VisitSubtractExpression(left.OrDefault(), right.OrDefault());
    }

    public override Maybe<TValue> VisitConstantExpression(int value)
    {
        return _impl.VisitConstantExpression(value);
    }

    public override Maybe<TValue> VisitDiceRollExpression(DiceSpecification dice)
    {
        return _impl.VisitDiceRollExpression(dice);
    }

    public TValue VisitTaggedExpression(string tag, TValue expressionValue)
    {
        return _impl.VisitTaggedExpression(tag, expressionValue);
    }
}