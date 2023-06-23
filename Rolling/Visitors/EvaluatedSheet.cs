using System;
using System.Collections.Immutable;
using System.Linq;
using Rolling.Models;
using Rolling.Models.Definitions;

namespace Rolling.Visitors;

public record struct EvaluatedSheet<TValue>(ImmutableList<EvaluatedSection<TValue>> Sections)
{
    public static EvaluatedSheet<TValue> Evaluate(SheetDefinition sheet, SavingRollVisitor<TValue> visitor)
    {
        visitor.Visit(sheet);
        return new EvaluatedSheet<TValue>(
            sheet.Sections.Select(
                    s => new EvaluatedSection<TValue>(
                        s.Name,
                        s.Type,
                        s.Rolls.Select(
                                r =>
                                {
                                    var (value, cond) = visitor.GetValues(r);
                                    return new EvaluatedRoll<TValue>(
                                        r,
                                        value,
                                        cond
                                    );
                                }
                            )
                            .ToImmutableList()
                    )
                )
                .ToImmutableList()
        );
    }
}

public static class EvaluatedSheet
{
    public static EvaluatedSheet<TValue> Evaluate<TValue>(this SheetDefinition sheet, SavingRollVisitor<TValue> eval) => EvaluatedSheet<TValue>.Evaluate(sheet, eval);
}