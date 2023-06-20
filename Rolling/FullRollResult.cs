using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Rolling.Utilities;

namespace Rolling;

public record struct DieResult(RawRoll Value, bool Dropped);

public record ValueGroup
(
    Maybe<string> Tag
);

public record RollResultGroup(
    Maybe<string> Tag,
    ImmutableList<DieResult> Rolls,
    bool CriticalSuccess,
    bool CriticalFailure) : ValueGroup(Tag);

public class RollExpressionResult
{
    public int Value { get; }
    public ImmutableList<ValueGroup> Groups { get; }
    public ImmutableList<char> Operations { get; }

    public RollExpressionResult(int value, ValueGroup result) : this(value, ImmutableList.Create(result), ImmutableList<char>.Empty)
    {
    }

    public RollExpressionResult(int value, ImmutableList<ValueGroup> groups, ImmutableList<char> operations)
    {
        if (groups.Count != operations.Count + 1)
        {
            throw new ArgumentException($"{nameof(groups)} must be one longer than {nameof(operations)}");
        }

        Value = value;
        Groups = groups;
        Operations = operations;
    }
}