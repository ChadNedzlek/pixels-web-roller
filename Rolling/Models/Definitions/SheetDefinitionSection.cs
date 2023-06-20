﻿using System.Collections.Immutable;
using Rolling.Utilities;

namespace Rolling.Models.Definitions;

public record struct SheetDefinitionSection(
    Maybe<string> Name,
    RollSectionType Type,
    ImmutableList<DiceRollDefinition> Rolls
);