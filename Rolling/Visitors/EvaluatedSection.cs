using System.Collections.Immutable;
using Rolling.Models;
using Utilities;

namespace Rolling.Visitors;

public class EvaluatedSection<TValue>
{
    public EvaluatedSection(Maybe<string> name, RollSectionType type, ImmutableList<EvaluatedRoll<TValue>> rolls)
    {
        Name = name;
        Type = type;
        Rolls = rolls;
    }

    public Maybe<string> Name { get; }
    public RollSectionType Type { get; }
    public ImmutableList<EvaluatedRoll<TValue>> Rolls { get; }
}