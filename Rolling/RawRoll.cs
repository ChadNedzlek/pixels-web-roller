using System;
using Rolling.Utilities;

namespace Rolling;

public record struct RollState(bool Dropped, bool CriticalSuccess, bool CriticalFailure);

public record RawRoll(int Result, int Size, Maybe<int> Id)
{
    private static readonly Random _random = new Random();
    private static RawRoll One { get; } = new RawRoll(1, 1, Maybe<int>.None);

    public static RawRoll Random(int size)
    {
        if (size == 1)
            return One;
        
        return new RawRoll(_random.Next(1, size), size, Maybe<int>.None);
    }
}

public record UsedRoll(int Result, int Size, Maybe<int> Id, bool Dropped)
{
}