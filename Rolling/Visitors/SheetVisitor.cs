using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Rolling.Models.Definitions;
using Rolling.Utilities;

namespace Rolling.Visitors;

public abstract class SheetVisitor<TValue>
{
    public void Visit(SheetDefinition sheet)
    {
        Dictionary<string, DiceExpression> variables = sheet.Variables.ToDictionary(v => v.Name, v => v.Expression);
        Dictionary<string, TValue> values = new();

        TValue GetValueRec(string name, ImmutableHashSet<string> visited)
        {
            if (visited.Contains(name))
                throw new ArgumentException($"Circular dependency detected in variable '{name}'");

            if (values.TryGetValue(name, out TValue value))
                return value;

            visited = visited.Add(name);
            value = Evaluate(variables[name], n => GetValueRec(n, visited));
            TValue simplified = SimplifyValue(value);
            values.Add(name, simplified);
            return simplified;
        }

        TValue GetValue(string name) => GetValueRec(name, ImmutableHashSet<string>.Empty);

        foreach (var v in variables)
        {
            Evaluate(v.Value, GetValue);
        }

        foreach (var section in sheet.Sections)
        {
            Visit(section, GetValue);
        }
    }

    protected virtual TValue SimplifyValue(TValue value)
    {
        return value;
    }

    protected abstract TValue Evaluate(DiceExpression expression, Func<string, TValue> variableLookup);

    public virtual void Visit(SheetDefinitionSection section, DiceRollDefinition roll, Func<string, TValue> lookup)
    {
        Visit(roll, Evaluate(roll.Expression, lookup), roll.ConditionalExpression.Select(r => Evaluate(r, lookup)));
    }

    public virtual void Visit(SheetDefinitionSection section, Func<string, TValue> lookup)
    {
        foreach (var roll in section.Rolls)
        {
            Visit(section, roll, lookup);
        }
    }

    public virtual void Visit(DiceRollDefinition roll, TValue value, Maybe<TValue> conditionalValue)
    {
    }
}