using System.Collections.Immutable;
using Rolling.Utilities;

namespace Rolling.Models.Rolls;

public record RollResultGroup(
    Maybe<string> Tag,
    int Value,
    ImmutableList<RollResult> Items,
    string Operations)
{
    public static RollResultGroup FromRoll(RollResult result)
        => new(
            Maybe<string>.None,
            result.Value,
            ImmutableList.Create(result),
            ""
        );
    
    public static RollResultGroup FromValue(int value)
        => new(
            Maybe<string>.None,
            value,
            ImmutableList.Create(new RollResult(value)),
            ""
        );
}