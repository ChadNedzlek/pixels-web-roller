using System.Collections.Immutable;
using Rolling.Models.Definitions;
using Rolling.Parsing;
using Rolling.Utilities;

namespace Rolling;

public readonly record struct DiceSpecification(int Count, int Sides, ImmutableList<DiceMod> Modifiers) : IDieExpression
{
    public override string ToString() => $"{Count}d{Sides}";
}

public interface IDieExpression
{
}