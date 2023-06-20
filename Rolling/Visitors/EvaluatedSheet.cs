using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Rolling.Models;
using Rolling.Models.Definitions;
using Rolling.Utilities;

namespace Rolling.Visitors;

public record struct EvaluatedSheet<TValue>(ImmutableList<EvaluatedSection<TValue>> Sections)
{
    private class Visitor : ExpressionVisitor<TValue>
    {
        private readonly ExpressionVisitor<TValue> _impl;
        private readonly Dictionary<DiceRollDefinition, TValue> _evaluatedRolls = new Dictionary<DiceRollDefinition,TValue>();
        private readonly Dictionary<DiceRollDefinition, TValue> _evaluatedConditions = new Dictionary<DiceRollDefinition,TValue>();

        public Visitor(ExpressionVisitor<TValue> impl)
        {
            _impl = impl;
        }

        public TValue GetValue(DiceRollDefinition roll) => _evaluatedRolls[roll];

        public Maybe<TValue> GetConditionalValue(DiceRollDefinition roll) =>
            _evaluatedConditions.TryGetValue(roll, out TValue v) ? v : Maybe<TValue>.None;


        public override void VisitRoll(SheetDefinitionSection section,
            DiceRollDefinition roll,
            TValue value,
            Maybe<TValue> conditionalValue)
        {
            _evaluatedRolls.Add(roll, value);
            if (conditionalValue.TryValue(out var cond))
                _evaluatedConditions.Add(roll, cond);
        }

        public override TValue VisitDivideExpression(TValue left, TValue right) => _impl.VisitDivideExpression(left, right);

        public  override TValue VisitMultiplyExpression(TValue left, TValue right) => _impl.VisitMultiplyExpression(left, right);

        public  override TValue VisitAddExpression(TValue left, TValue right) => _impl.VisitAddExpression(left, right);

        public  override TValue VisitSubtractExpression(TValue left, TValue right) => _impl.VisitSubtractExpression(left, right);

        public override  TValue VisitConstantExpression(int value) => _impl.VisitConstantExpression(value);

        public  override TValue VisitDiceRollExpression(DiceSpecification dice) => _impl.VisitDiceRollExpression(dice);

        public override  TValue VisitTaggedExpression(string tag, TValue expressionValue) => _impl.VisitTaggedExpression(tag, expressionValue);
    }

    public static EvaluatedSheet<TValue> Evaluate(SheetDefinition sheet, ExpressionVisitor<TValue> evaluator)
    {
        var visitor = new Visitor(evaluator);
        visitor.Visit(sheet);
        return new EvaluatedSheet<TValue>(
            sheet.Sections.Select(
                    s => new EvaluatedSection<TValue>(
                        s.Name,
                        s.Rolls.Select(
                                r => new EvaluatedRoll<TValue>(r, visitor.GetValue(r), visitor.GetConditionalValue(r))
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
    public static EvaluatedSheet<TValue> Evaluate<TValue>(this SheetDefinition sheet,
        ExpressionVisitor<TValue> eval) => EvaluatedSheet<TValue>.Evaluate(sheet, eval);
}

public record struct EvaluatedSection<TValue>(Maybe<string> Name, ImmutableList<EvaluatedRoll<TValue>> Rolls);
public record struct EvaluatedRoll<TValue>(DiceRollDefinition Definition, TValue Value, Maybe<TValue> ConditionalValue);