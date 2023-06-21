using System;
using System.Collections.Immutable;

namespace Rolling.Models.Rolls;

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