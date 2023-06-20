namespace Rolling.Models.Definitions.Expressions;

public class TaggedExpression : DiceExpression
{
    public TaggedExpression(DiceExpression expression, string tag)
    {
        Expression = expression;
        Tag = tag;
    }

    public DiceExpression Expression { get; }
    public string Tag { get; }
    
    public override int Calculate() => Expression.Calculate();
    public override string DebugString()
    {
        return $"({Expression.DebugString()} {Tag})";
    }
}