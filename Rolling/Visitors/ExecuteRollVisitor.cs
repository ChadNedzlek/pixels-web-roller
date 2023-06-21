using System;
using System.Collections.Immutable;
using Rolling.Models;
using Rolling.Models.Definitions;
using Rolling.Models.Rolls;
using Rolling.Utilities;

namespace Rolling.Visitors;

public class ExecuteRollVisitor : SavingRollVisitor<Maybe<RollExpressionResult>>
{
    private readonly ImmutableList<DieRoll> _rolls;
    private ExecuteRollEvaluator _evaluator;

    public ExecuteRollVisitor(ImmutableList<DieRoll> rolls)
    {
        _rolls = rolls;
    }

    public override void Visit(SheetDefinitionSection section, Func<string, Maybe<RollExpressionResult>> lookup)
    {
        if (section.Type == RollSectionType.UniqueDicePerRoll)
            _evaluator = new ExecuteRollEvaluator(new RollPool(_rolls));
        base.Visit(section, lookup);
        _evaluator = null;
    }

    public override void Visit(SheetDefinitionSection section,
        DiceRollDefinition roll,
        Func<string, Maybe<RollExpressionResult>> lookup)
    {
        if (section.Type == RollSectionType.RepeatDice) 
            _evaluator = new ExecuteRollEvaluator(new RollPool(_rolls));

        base.Visit(section, roll, lookup);
    }

    protected override Maybe<RollExpressionResult> Evaluate(DiceExpression expression, Func<string, Maybe<RollExpressionResult>> variableLookup)
    {
        return _evaluator.Visit(expression, s => variableLookup(s).OrDefault());
    }
}