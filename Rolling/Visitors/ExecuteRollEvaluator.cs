﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Rolling.Models;
using Rolling.Models.Definitions;
using Rolling.Models.Rolls;

namespace Rolling.Visitors;

public class ExecuteRollEvaluator : ExpressionEvaluator<RollExpressionResult>
{
    private readonly RollPool _pool;

    public ExecuteRollEvaluator(RollPool pool)
    {
        _pool = pool;
    }

    private static RollExpressionResult Merge(RollExpressionResult left, RollExpressionResult right, char op, Func<int,int,int> calc)
    {
        if (left.Groups.Count != 1 ||
            right.Groups.Count != 1 ||
            left.Groups[0].Tag != right.Groups[0].Tag)
        {
            return new RollExpressionResult(
                calc(left.Value, right.Value),
                left.Groups.AddRange(right.Groups),
                left.Operations + op +right.Operations
            );
        }

        RollResultGroup l = left.Groups[0];
        RollResultGroup r = right.Groups[0];

        var merged = new RollResultGroup(
            l.Tag,
            calc(l.Value, r.Value),
            l.Items.AddRange(r.Items),
            l.Operations + op + r.Operations
        );
            
        // We can combine the bits into a single group
        return new RollExpressionResult(
            calc(left.Value, right.Value),
            ImmutableList.Create(merged),
            ""
        );
    }

    protected override RollExpressionResult VisitDivideExpression(RollExpressionResult left, RollExpressionResult right)
    {
        return Merge(left, right, '/', (a, b) => a / b);
    }

    protected override RollExpressionResult VisitMultiplyExpression(RollExpressionResult left, RollExpressionResult right)
    {
        return Merge(left, right, '*', (a, b) => a * b);
    }

    protected override RollExpressionResult VisitAddExpression(RollExpressionResult left, RollExpressionResult right)
    {
        return Merge(left, right, '+', (a, b) => a + b);
    }

    protected override RollExpressionResult VisitSubtractExpression(RollExpressionResult left, RollExpressionResult right)
    {
        return Merge(left, right, '-', (a, b) => a - b);
    }

    protected override RollExpressionResult VisitConstantExpression(int value)
    {
        return new RollExpressionResult(value);
    }

    protected override RollExpressionResult VisitTaggedExpression(string tag, RollExpressionResult expressionValue)
    {
        return new RollExpressionResult(
            expressionValue.Value,
            expressionValue.Groups.ConvertAll(g => g with { Tag = tag }),
            expressionValue.Operations
        );
    }

    protected override RollExpressionResult VisitDiceRollExpression(DiceSpecification dice)
    {
        List<DieRoll> rolls = new List<DieRoll>();
        for (int i = 0; i < dice.Count; i++)
        {
            rolls.Add(_pool.Take(dice.Sides).Or(DieRoll.Random(dice.Sides)));
        }

        int drop = 0;
        int critSuccessTarget = int.MaxValue;
        int critFailTarget = 0;
        if (dice is {Sides:20, Count:1})
        {
            critSuccessTarget = 20;
            critFailTarget = 1;
        }

        foreach ((DiceModType type, var count) in dice.Modifiers)
        {
            switch (type)
            {
                case DiceModType.Keep:
                    drop = dice.Count - count;
                    break;
                case DiceModType.Drop:
                    drop = count;
                    break;
                case DiceModType.CriticalSuccess:
                    critSuccessTarget = count;
                    break;
                case DiceModType.CriticalFailure:
                    critFailTarget = count;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        HashSet<DieRoll> dropped = rolls.OrderBy(r => r.Result).Take(drop).ToHashSet();
        var res = rolls.Select(r => new AssignedDieRoll(r, dropped.Contains(r))).ToImmutableList();
        bool critSuccesss = false;
        bool critFail = false;
        if (dice.Count - drop == 1)
        {
            // we have exactly 1 die, get it
            var r = res.First(r => !r.Dropped);
            critSuccesss = r.Value.Result >= critSuccessTarget;
            critFail = r.Value.Result <= critFailTarget;
        }

        var value = rolls.Where(r => !dropped.Contains(r)).Sum(r => r.Result);

        return new RollExpressionResult(new SingleRollResult(res, value, critSuccesss, critFail));
    }
}