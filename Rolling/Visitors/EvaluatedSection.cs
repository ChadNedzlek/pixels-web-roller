using System.Collections.Immutable;
using Rolling.Utilities;

namespace Rolling.Visitors;

public record struct EvaluatedSection<TValue>(Maybe<string> Name, ImmutableList<EvaluatedRoll<TValue>> Rolls);