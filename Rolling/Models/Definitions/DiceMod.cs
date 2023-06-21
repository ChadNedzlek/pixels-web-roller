using System;

namespace Rolling.Models.Definitions;

public readonly record struct DiceMod(DiceModType Type, int Count)
{
    public override string ToString() => Type switch
    {
        DiceModType.Keep => $"k{Count}",
        DiceModType.Drop => $"d{Count}",
        DiceModType.CriticalSuccess => $"c>{Count}",
        DiceModType.CriticalFailure => $"c<{Count}",
        _ => throw new ArgumentOutOfRangeException()
    };
}