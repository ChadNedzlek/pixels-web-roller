namespace Rolling.Models.Definitions;

public abstract class DiceExpression
{
    public abstract int Calculate();
    public abstract string DebugString();
}