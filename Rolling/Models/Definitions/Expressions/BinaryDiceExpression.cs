using System;

namespace Rolling.Models.Definitions.Expressions;

public class BinaryDiceExpression : DiceExpression
{
    public DiceExpression Left { get; }
    public DiceExpression Right { get; }
    public char Operator { get; }

    public BinaryDiceExpression(DiceExpression left, DiceExpression right, char @operator)
    {
        Left = left;
        Right = right;
        Operator = @operator;
    }

    public override int Calculate()
    {
        return Operator switch
        {
            '+' => Left.Calculate() + Right.Calculate(),
            '-' => Left.Calculate() - Right.Calculate(),
            '*' => Left.Calculate() * Right.Calculate(),
            '/' => Left.Calculate() / Right.Calculate(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public override string DebugString() => $"({Left.DebugString()} {Operator} {Right.DebugString()})";
}