using System.Collections.Immutable;
using FluentAssertions;
using NUnit.Framework;
using Rolling.Models;
using Rolling.Models.Definitions;
using Rolling.Models.Rolls;
using Rolling.Parsing;
using Rolling.Utilities;
using Rolling.Visitors;

namespace Rolling.Tests;

public class ExecuteRollVisitorTests
{
    [Test]
    public void SingleD20Roll()
    {
        var inputDice = ImmutableList.Create(new DieRoll(3, 20, Maybe<int>.None));
        SheetDefinition sheet = new RollParser().Parse("d20");
        var evaluated = sheet.Evaluate(new ExecuteRollVisitor(inputDice));
        var section = evaluated.Sections.Should().ContainSingle().Subject;
        var result = section.Rolls.Should().ContainSingle().Subject;
        result.ConditionalValue.Should().BeNone();
        result.Value.Value.Should().Be(3);
        var group = result.Value.Groups.Should().ContainSingle().Subject;
        group.Value.Should().Be(3);
        group.Operations.Should().BeEmpty();
        group.Tag.Should().BeNone();
        var part = group.Items.Should().ContainSingle().Which.Should().BeOfType<SingleRollResult>().Subject;
        part.Value.Should().Be(3);
        part.CriticalSuccess.Should().BeFalse();
        part.CriticalFailure.Should().BeFalse();
        var die = part.Rolls.Should().ContainSingle().Subject;
        die.Dropped.Should().BeFalse();
        die.Value.Should().BeSameAs(inputDice[0]);
    }
}