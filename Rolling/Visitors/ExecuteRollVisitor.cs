using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Rolling.Models;
using Rolling.Models.Definitions;
using Rolling.Utilities;

namespace Rolling.Visitors;

public class ExecuteRollVisitor : ExpressionVisitor<RollExpressionResult>
{
    private readonly ImmutableList<RawRoll> _dice;
    private SheetDefinitionSection _section;
    private RollPool _pool = null;

    public ExecuteRollVisitor(ImmutableList<RawRoll> rolls)
    {
        _dice = rolls;
    }


    protected override void Visit(SheetDefinitionSection section, Func<string, RollExpressionResult> lookup)
    {
        _section = section;
        if (_section.Type == RollSectionType.UniqueDicePerRoll)
            _pool = new RollPool(_dice);
        base.Visit(section, lookup);
        _pool = null;
    }

    protected override void Visit(SheetDefinitionSection section,
        DiceRollDefinition roll,
        Func<string, RollExpressionResult> lookup)
    {
        if (section.Type == RollSectionType.RepeatDice) 
            _pool = new RollPool(_dice);

        base.Visit(section, roll, lookup);
    }

    public override RollExpressionResult VisitDivideExpression(RollExpressionResult left, RollExpressionResult right)
    {
        return new RollExpressionResult(
            left.Value / right.Value,
            left.Groups.AddRange(right.Groups),
            left.Operations.Add('/').AddRange(right.Operations)
        );
    }

    public override RollExpressionResult VisitMultiplyExpression(RollExpressionResult left, RollExpressionResult right)
    {
        return new RollExpressionResult(
            left.Value * right.Value,
            left.Groups.AddRange(right.Groups),
            left.Operations.Add('*').AddRange(right.Operations)
        );
    }

    public override RollExpressionResult VisitAddExpression(RollExpressionResult left, RollExpressionResult right)
    {
        return new RollExpressionResult(
            left.Value + right.Value,
            left.Groups.AddRange(right.Groups),
            left.Operations.Add('+').AddRange(right.Operations)
        );
    }

    public override RollExpressionResult VisitSubtractExpression(RollExpressionResult left, RollExpressionResult right)
    {
        return new RollExpressionResult(
            left.Value - right.Value,
            left.Groups.AddRange(right.Groups),
            left.Operations.Add('-').AddRange(right.Operations)
        );
    }

    public override RollExpressionResult VisitConstantExpression(int value)
    {
        return new RollExpressionResult(value, new ValueGroup(Maybe<string>.None));
    }

    public override RollExpressionResult VisitTaggedExpression(string tag, RollExpressionResult expressionValue)
    {
        return new RollExpressionResult(
            expressionValue.Value,
            expressionValue.Groups.ConvertAll(g => g with { Tag = tag }),
            expressionValue.Operations
        );
    }

    public override RollExpressionResult VisitDiceRollExpression(DiceSpecification dice)
    {
        List<RawRoll> rolls = new List<RawRoll>();
        for (int i = 0; i < dice.Count; i++)
        {
            rolls.Add(_pool.Take(dice.Sides).Or(RawRoll.Random(dice.Sides)));
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
            }
        }

        HashSet<RawRoll> dropped = rolls.OrderBy(r => r.Result).Take(drop).ToHashSet();
        var res = rolls.ConvertAll(r => new DieResult(r, dropped.Contains(r)));
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

        return new RollExpressionResult(value, new RollResultGroup(Maybe<string>.None, res, critSuccesss, critFail));
    }
}