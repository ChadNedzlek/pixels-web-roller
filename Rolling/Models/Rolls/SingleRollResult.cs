using System.Collections.Immutable;

namespace Rolling.Models.Rolls;

public record SingleRollResult(
    ImmutableList<AssignedDieRoll> Rolls,
    int Value,
    bool CriticalSuccess,
    bool CriticalFailure) : RollResult(Value);