using System.Collections.Immutable;
using Rolling.Models.Definitions;
using Rolling.Models.Rolls;
using Utilities;

namespace Rolling.Visitors;

public static class FullSheet
{
    public static EvaluatedSheet<Maybe<RollExpressionResult>> Empty(this SheetDefinition sheet) => sheet.Evaluate(new DefaultSavingVisitor<Maybe<RollExpressionResult>>());
    public static EvaluatedSheet<Maybe<RollExpressionResult>> Roll(this SheetDefinition sheet, ImmutableList<DieRoll> rolls) => sheet.Evaluate(new ExecuteRollVisitor(rolls));
}