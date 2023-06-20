using Rolling.Parsing;

namespace Rolling.Models.Definitions;

public record struct VariableDefinition(string Name, DiceExpression Expression);