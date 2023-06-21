using System.Collections.Immutable;
using Rolling.Models.Definitions;
using Rolling.Models.Rolls;
using Rolling.Utilities;

namespace Rolling.Visitors;

public static class FullSheet
{
    public static EvaluatedSheet<Maybe<RollExpressionResult>> Empty(this SheetDefinition sheet) => sheet.Evaluate(DefaultSavingVisitor<Maybe<RollExpressionResult>>.Instance);
    public static EvaluatedSheet<Maybe<RollExpressionResult>> Roll(this SheetDefinition sheet, ImmutableList<DieRoll> rolls) => sheet.Evaluate(new ExecuteRollVisitor(rolls));
}