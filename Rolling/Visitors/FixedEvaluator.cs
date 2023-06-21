using Rolling.Models;

namespace Rolling.Visitors;

internal class FixedEvaluator<TValue> : ExpressionEvaluator<TValue>
{
    private readonly TValue _value;

    public FixedEvaluator(TValue value)
    {
        _value = value;
    }

    public override TValue VisitDivideExpression(TValue left, TValue right) => _value;
    public override TValue VisitMultiplyExpression(TValue left, TValue right) => _value;
    public override TValue VisitAddExpression(TValue left, TValue right) => _value;
    public override TValue VisitSubtractExpression(TValue left, TValue right) => _value;
    public override TValue VisitConstantExpression(int value) => _value;
    public override TValue VisitDiceRollExpression(DiceSpecification dice) => _value;
}