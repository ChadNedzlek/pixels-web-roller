using System.Collections.Generic;
using Rolling.Models.Definitions;
using Utilities;

namespace Rolling.Visitors;

public abstract class SavingRollVisitor<TValue> : SheetVisitor<TValue>
{
    private readonly Dictionary<DiceRollDefinition, (TValue value, Maybe<TValue> conditionalValue)> _evaluatedRolls = new ();

    public (TValue value, Maybe<TValue> conditionalValue ) GetValues(DiceRollDefinition roll) => _evaluatedRolls[roll];
        
    public override void Visit(DiceRollDefinition roll,
        TValue value,
        Maybe<TValue> conditionalValue)
    {
        _evaluatedRolls.Add(roll, (value, conditionalValue));
        base.Visit(roll, value, conditionalValue);
    }
}

public static class SavingRollVisitor
{
    public static SavingRollVisitor<TValue> ToSavingVisitor<TValue>(this ExpressionEvaluator<TValue> evaluator) =>
        new DelegatedSavingRollVisitor<TValue>(evaluator);
}