using System;

namespace Rolling.Models.Definitions.Expressions;

public class ReferenceExpression : DiceExpression
{
    public string Name { get; }

    public ReferenceExpression(string name)
    {
        Name = name;
    }

    public override int Calculate()
    {
        throw new NotImplementedException();
    }

    public override string DebugString() => $"@{Name}";
}