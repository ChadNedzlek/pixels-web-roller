using System;
using Rolling.Models.Definitions;

namespace Rolling.Visitors;

public class DefaultSavingVisitor<TValue> : SavingRollVisitor<TValue>
{
    public DefaultSavingVisitor()
    {
    }

    protected override TValue Evaluate(DiceExpression expression, Func<string, TValue> variableLookup)
    {
        return default;
    }
}