using System.Collections.Immutable;
using Rolling.Models;
using Rolling.Utilities;

namespace Rolling.Visitors;

public record struct EvaluatedSection<TValue>(Maybe<string> Name, RollSectionType Type, ImmutableList<EvaluatedRoll<TValue>> Rolls);