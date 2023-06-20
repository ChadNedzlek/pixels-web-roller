using System.Linq;
using Rolling.Models.Definitions;
using Rolling.Utilities;
using Sprache;

namespace Rolling.Parsing;

public class RollParser
{
    public SheetDefinition Parse(string input)
    {
        return Grammar.Sheet.End().Parse(input);
    }

    public Either<SheetDefinition, string> TryParse(string input)
    {
        IResult<SheetDefinition> result = Grammar.Sheet.End().TryParse(input);
        return result.WasSuccessful ? result.Value : result.Message;
    }
}