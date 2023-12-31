﻿using System;
using System.Collections.Immutable;
using Rolling.Models;
using Rolling.Models.Definitions;
using Rolling.Models.Rolls;
using Utilities;

namespace Rolling.Visitors;

public class ExecuteRollVisitor : SavingRollVisitor<Maybe<RollExpressionResult>>
{
    private readonly ImmutableList<DieRoll> _rolls;
    private ExecuteRollEvaluator _evaluator;

    public ExecuteRollVisitor(ImmutableList<DieRoll> rolls)
    {
        _evaluator = new ExecuteRollEvaluator(new RollPool());
        _rolls = rolls;
    }

    protected override Maybe<RollExpressionResult> SimplifyValue(Maybe<RollExpressionResult> value)
    {
        return value.Select(
            v =>
            {
                if (v.Groups.Count != 1) return v;
                
                return new RollExpressionResult(v.Value, ImmutableList.Create(RollResultGroup.FromValue(v.Value) with {Tag = v.Groups[0].Tag}), "");
            }
        );
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