using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Rolling.Utilities;

namespace Rolling;

public struct RollMode
{
    public Maybe<int> Keep { get; }
    public Maybe<int> CriticalSuccess { get; }
    public Maybe<int> CriticalFailure { get; }
}

public class RollResultGroup
{
    public Maybe<string> Tag { get; }
    public ImmutableList<DieRoll> Rolls { get; }
    public int Total { get; }

    public RollResultGroup(Maybe<string> tag, ImmutableList<DieRoll> rolls)
    {
        Tag = tag;
        Rolls = rolls;
        Total = rolls.Sum(r => r.Result);
    }
}

public class FullRollResult
{
    public FullRollResult(Maybe<ImmutableList<RollResultGroup>> conditionalRolls, ImmutableList<RollResultGroup> valueRolls)
    {
        ConditionalRolls = conditionalRolls;
        ValueRolls = valueRolls;
    }

    public Maybe<ImmutableList<RollResultGroup>> ConditionalRolls { get; }
    public ImmutableList<RollResultGroup> ValueRolls { get; }
}