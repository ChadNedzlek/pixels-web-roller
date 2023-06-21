using Rolling.Models.Definitions;
using Rolling.Utilities;

namespace Rolling.Visitors;

public record struct EvaluatedRoll<TValue>(DiceRollDefinition Definition, TValue Value, Maybe<TValue> ConditionalValue);