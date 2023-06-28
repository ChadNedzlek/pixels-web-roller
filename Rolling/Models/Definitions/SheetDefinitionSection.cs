using System.Collections.Immutable;
using Utilities;

namespace Rolling.Models.Definitions;

public record SheetDefinitionSection(
    Maybe<string> Name,
    RollSectionType Type,
    ImmutableList<DiceRollDefinition> Rolls
);