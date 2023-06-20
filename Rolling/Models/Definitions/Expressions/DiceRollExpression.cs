using System;

namespace Rolling.Models.Definitions.Expressions;

public class DiceRollExpression : DiceExpression
{
    public DiceRollExpression(DiceSpecification dice)
    {
        Dice = dice;
    }

    public DiceSpecification Dice { get; }
    public override int Calculate()
    {
        throw new NotImplementedException();
    }

    public override string DebugString() => $"{Dice.Count}d{Dice.Sides}{string.Join("",Dice.Modifiers)}";
}