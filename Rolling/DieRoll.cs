using System;
using Rolling.Utilities;

namespace Rolling;

public record DieRoll(int Result, int Size, Maybe<int> Id)
{
    private static readonly Random _random = new Random();
    private static DieRoll One { get; } = new DieRoll(1, 1, Maybe<int>.None);

    public static DieRoll Random(int size)
    {
        if (size == 1)
            return One;
        
        return new DieRoll(_random.Next(1, size), size, Maybe<int>.None);
    }
}