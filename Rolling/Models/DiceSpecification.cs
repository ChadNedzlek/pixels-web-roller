using System.Collections.Immutable;
using Rolling.Models.Definitions;

namespace Rolling.Models;

public readonly record struct DiceSpecification(int Count, int Sides, ImmutableList<DiceMod> Modifiers) : IDieExpression
{
    public override string ToString() => $"{Count}d{Sides}";
}