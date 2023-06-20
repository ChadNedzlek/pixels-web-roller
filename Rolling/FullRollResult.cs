using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Rolling.Utilities;

namespace Rolling;

public record struct DieResult(RawRoll Value, bool Dropped);

public record RollResult
(
    int Value
);

public record SingleRollResult(
    ImmutableList<DieResult> Rolls,
    int Value,
    bool CriticalSuccess,
    bool CriticalFailure) : RollResult(Value);

public record RollResultGroup(
    Maybe<string> Tag,
    int Value,
    ImmutableList<RollResult> Items,
    ImmutableList<RollResult> Operations)
{
    public static RollResultGroup FromRoll(RollResult result)
        => new RollResultGroup(
            Maybe<string>.None,
            result.Value,
            ImmutableList.Create(result),
            ImmutableList<RollResult>.Empty
        );
    
    public static RollResultGroup FromValue(int value)
        => new RollResultGroup(
            Maybe<string>.None,
            value,
            ImmutableList.Create(new RollResult(value)),
            ImmutableList<RollResult>.Empty
        );
}

public class RollExpressionResult
{
    public int Value { get; }
    public ImmutableList<RollResultGroup> Groups { get; }
    public ImmutableList<char> Operations { get; }

    public RollExpressionResult(RollResultGroup result) : this(result.Value, ImmutableList.Create(result), ImmutableList<char>.Empty)
    {
    }

    public RollExpressionResult(int value, ImmutableList<RollResultGroup> groups, ImmutableList<char> operations)
    {
        if (groups.Count != operations.Count + 1)
        {
            throw new ArgumentException($"{nameof(groups)} must be one longer than {nameof(operations)}");
        }

        Value = value;
        Groups = groups;
        Operations = operations;
    }

    public RollExpressionResult(int value) : this(RollResultGroup.FromValue(value))
    {
    }
    
    public RollExpressionResult(RollResult roll) : this(RollResultGroup.FromRoll(roll))
    {
    }
}