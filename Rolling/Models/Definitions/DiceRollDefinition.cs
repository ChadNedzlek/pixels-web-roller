using Rolling.Utilities;

namespace Rolling.Models.Definitions;

public class DiceRollDefinition
{
    public DiceRollDefinition(Maybe<string> name, DiceExpression expression, Maybe<DiceExpression> conditionalExpression)
    {
        Name = name;
        Expression = expression;
        ConditionalExpression = conditionalExpression;
    }

    public Maybe<string> Name { get; init; }
    public DiceExpression Expression { get; init; }
    public Maybe<DiceExpression> ConditionalExpression { get; }
}