using System.Collections.Immutable;

namespace Rolling.Models.Definitions;

public record struct SheetDefinition(
    ImmutableList<VariableDefinition> Variables,
    ImmutableList<SheetDefinitionSection> Sections
);