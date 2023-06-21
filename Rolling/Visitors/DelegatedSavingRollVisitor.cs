using System;
using Rolling.Models.Definitions;

namespace Rolling.Visitors;

public class DelegatedSavingRollVisitor<TValue> : SavingRollVisitor<TValue>
{
    private readonly ExpressionEvaluator<TValue> _evaluator;

    public DelegatedSavingRollVisitor(ExpressionEvaluator<TValue> evaluator)
    {
        _evaluator = evaluator;
    }

    protected override TValue Evaluate(DiceExpression expression, Func<string, TValue> variableLookup)
    {
        return _evaluator.Visit(expression, variableLookup);
    }
}