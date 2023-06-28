using System.Text.RegularExpressions;
using Rolling.Models.Definitions;
using Sprache;
using Utilities;

namespace Rolling.Parsing;

public class RollParser
{
    private readonly Parser<SheetDefinition> _parser = Grammar.Sheet.End();

    private static string NormalizeInput(string input)
    {
        var normalizedEndings = input.ReplaceLineEndings("\n");
        var multiLine = Regex.Replace(normalizedEndings, @"/\*.*?\*/", "", RegexOptions.Singleline);
        var singleLine = Regex.Replace(multiLine, @"//.*(\n|$)", "");
        return singleLine.TrimStart();
    }

    public SheetDefinition Parse(string input)
    {
        return _parser.Parse(NormalizeInput(input));
    }

    public Either<SheetDefinition, string> TryParse(string input)
    {
        IResult<SheetDefinition> result = _parser.TryParse(NormalizeInput(input));
        return result.WasSuccessful ? result.Value : result.Message;
    }
}