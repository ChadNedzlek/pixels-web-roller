namespace Rolling.Models.Definitions.Expressions;

public class ConstantExpression : DiceExpression
{
    public int Amount { get; }

    public ConstantExpression(int amount)
    {
        Amount = amount;
    }

    public override int Calculate() => Amount;
    public override string DebugString() => Amount.ToString();
}