using System;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Rolling.Models;
using Rolling.Models.Definitions;
using Rolling.Models.Rolls;
using Rolling.Parsing;
using Rolling.Visitors;
using Utilities;

namespace Rolling.Tests;

public class ExecuteRollVisitorTests
{
    [Test]
    public void SingleD20Roll()
    {
        var inputDice = ImmutableList.Create(new DieRoll(3, 20, Maybe<long>.None));
        SheetDefinition sheet = new RollParser().Parse("d20");
        var evaluated = sheet.Evaluate(new ExecuteRollVisitor(inputDice));
        var section = evaluated.Sections.Should().ContainSingle().Subject;
        var result = section.Rolls.Should().ContainSingle().Subject;
        result.ConditionalValue.Should().BeNone();
        var roll = result.Value.Should().BeSet().Subject;
        var group = roll.Groups.Should().ContainSingle().Subject;
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

    [Test]
    public void CombinedRolls()
    {
        var inputDice = ImmutableList.Create(new DieRoll(3, 20, Maybe<long>.None), new DieRoll(5, 10, Maybe<long>.None));
        SheetDefinition sheet = new RollParser().Parse("d20 + d10");
        var evaluated = sheet.Evaluate(new ExecuteRollVisitor(inputDice));
        var roll = evaluated.Sections[0].Rolls[0].Value.OrDefault();
        var group = roll.Groups.Should().ContainSingle().Subject;
        group.Value.Should().Be(8);
        group.Operations.Should().Be("+");
        group.Tag.Should().BeNone();
        group.Items.Should()
            .SatisfyRespectively(
                p =>
                {
                    var part = p.Should().BeOfType<SingleRollResult>().Subject;
                    part.Value.Should().Be(3);
                    part.CriticalSuccess.Should().BeFalse();
                    part.CriticalFailure.Should().BeFalse();
                    var die = part.Rolls.Should().ContainSingle().Subject;
                    die.Dropped.Should().BeFalse();
                    die.Value.Should().BeSameAs(inputDice[0]);
                },
                p =>
                {
                    var part = p.Should().BeOfType<SingleRollResult>().Subject;
                    part.Value.Should().Be(5);
                    part.CriticalSuccess.Should().BeFalse();
                    part.CriticalFailure.Should().BeFalse();
                    var die = part.Rolls.Should().ContainSingle().Subject;
                    die.Dropped.Should().BeFalse();
                    die.Value.Should().BeSameAs(inputDice[1]);
                }
            );
    }
    
    [Test]
    public void KeepHighest()
    {
        var inputDice = ImmutableList.Create(new DieRoll(3, 20, Maybe<long>.None), new DieRoll(7, 20, Maybe<long>.None));
        SheetDefinition sheet = new RollParser().Parse("2d20kh");
        var evaluated = sheet.Evaluate(new ExecuteRollVisitor(inputDice));
        var section = evaluated.Sections.Should().ContainSingle().Subject;
        var result = section.Rolls.Should().ContainSingle().Subject;
        result.ConditionalValue.Should().BeNone();
        var roll = result.Value.Should().BeSet().Subject;
        var group = roll.Groups.Should().ContainSingle().Subject;
        group.Value.Should().Be(7);
        group.Operations.Should().BeEmpty();
        group.Tag.Should().BeNone();
        var part = group.Items.Should().ContainSingle().Which.Should().BeOfType<SingleRollResult>().Subject;
        part.Value.Should().Be(7);
        part.CriticalSuccess.Should().BeFalse();
        part.CriticalFailure.Should().BeFalse();
        part.Rolls.Should()
            .SatisfyRespectively(
                die =>
                {
                    die.Dropped.Should().BeTrue();
                    die.Value.Should().BeSameAs(inputDice[0]);
                },
                die =>
                {
                    die.Dropped.Should().BeFalse();
                    die.Value.Should().BeSameAs(inputDice[1]);
                }
            );
    }
    
    [Test]
    public void Roll5Drop2()
    {
        var inputDice = Enumerable.Range(1,5).Select(v => new DieRoll(v, 6, Maybe<long>.None)).ToImmutableList();
        SheetDefinition sheet = new RollParser().Parse("5d6d2");
        var evaluated = sheet.Evaluate(new ExecuteRollVisitor(inputDice));
        var group = Requirement(
            () => evaluated.Sections[0].Rolls[0].Value.OrDefault().Groups[0]
        );
        group.Value.Should().Be(12);
    }
    
    [Test]
    public void Roll5Keep2()
    {
        var inputDice = Enumerable.Range(1,5).Select(v => new DieRoll(v, 6, Maybe<long>.None)).ToImmutableList();
        SheetDefinition sheet = new RollParser().Parse("5d6k2");
        var evaluated = sheet.Evaluate(new ExecuteRollVisitor(inputDice));
        var group = Requirement(
            () => evaluated.Sections[0].Rolls[0].Value.OrDefault().Groups[0]
        );
        group.Value.Should().Be(9);
    }
    
    [Test]
    public void CriticalD20()
    {
        var inputDice = ImmutableList.Create(new DieRoll(13, 20, Maybe<long>.None));
        SheetDefinition sheet = new RollParser().Parse("d20c>10");
        var evaluated = sheet.Evaluate(new ExecuteRollVisitor(inputDice));
        SingleRollResult part = Requirement(
            () => (SingleRollResult)evaluated.Sections[0].Rolls[0].Value.OrDefault().Groups[0].Items[0]
        );
        part.Value.Should().Be(13);
        part.CriticalSuccess.Should().BeTrue();
        part.CriticalFailure.Should().BeFalse();
        var die = part.Rolls.Should().ContainSingle().Subject;
        die.Dropped.Should().BeFalse();
        die.Value.Should().BeSameAs(inputDice[0]);
    }
    
    [Test]
    public void CriticalFailD20()
    {
        var inputDice = ImmutableList.Create(new DieRoll(13, 20, Maybe<long>.None));
        SheetDefinition sheet = new RollParser().Parse("d20c<15");
        var evaluated = sheet.Evaluate(new ExecuteRollVisitor(inputDice));
        SingleRollResult part = Requirement(
            () => (SingleRollResult)evaluated.Sections[0].Rolls[0].Value.OrDefault().Groups[0].Items[0]
        );
        part.Value.Should().Be(13);
        part.CriticalSuccess.Should().BeFalse();
        part.CriticalFailure.Should().BeTrue();
        var die = part.Rolls.Should().ContainSingle().Subject;
        die.Dropped.Should().BeFalse();
        die.Value.Should().BeSameAs(inputDice[0]);
    }
    
    [Test]
    public void DefaultCriticalD20()
    {
        var inputDice = ImmutableList.Create(new DieRoll(20, 20, Maybe<long>.None));
        SheetDefinition sheet = new RollParser().Parse("d20");
        var evaluated = sheet.Evaluate(new ExecuteRollVisitor(inputDice));
        SingleRollResult part = Requirement(
            () => (SingleRollResult)evaluated.Sections[0].Rolls[0].Value.OrDefault().Groups[0].Items[0]
        );
        part.Value.Should().Be(20);
        part.CriticalSuccess.Should().BeTrue();
        part.CriticalFailure.Should().BeFalse();
        var die = part.Rolls.Should().ContainSingle().Subject;
        die.Dropped.Should().BeFalse();
        die.Value.Should().BeSameAs(inputDice[0]);
    }
    
    [Test]
    public void DefaultCriticalFailD20()
    {
        var inputDice = ImmutableList.Create(new DieRoll(1, 20, Maybe<long>.None));
        SheetDefinition sheet = new RollParser().Parse("d20");
        var evaluated = sheet.Evaluate(new ExecuteRollVisitor(inputDice));

        SingleRollResult part = Requirement(
            () => (SingleRollResult)evaluated.Sections[0].Rolls[0].Value.OrDefault().Groups[0].Items[0]
        );

        part.Value.Should().Be(1);
        part.CriticalSuccess.Should().BeFalse();
        part.CriticalFailure.Should().BeTrue();
    }

    [Test]
    public void TaggedSeparateGroup()
    {
        var inputDice = ImmutableList.Create(new DieRoll(3, 20, Maybe<long>.None), new DieRoll(5, 10, Maybe<long>.None));
        SheetDefinition sheet = new RollParser().Parse("d20 + d10 tagged");
        var evaluated = sheet.Evaluate(new ExecuteRollVisitor(inputDice));
        var roll = Requirement(
            () => evaluated.Sections[0].Rolls[0].Value.OrDefault()
        );
        roll.Operations.Should().Be("+");
        roll.Groups.Should()
            .SatisfyRespectively(
                group =>
                {
                    group.Value.Should().Be(3);
                    group.Tag.Should().BeNone();
                },
                group =>
                {
                    group.Value.Should().Be(5);
                    group.Tag.Should().Be("tagged");
                }
            );
    }
    
    [Test]
    public void ResuseRollSection()
    {
        var inputDice = ImmutableList.Create(new DieRoll(3, 20, Maybe<long>.None), new DieRoll(7, 20, Maybe<long>.None));
        SheetDefinition sheet = new RollParser().Parse("""
            === SECTION ===
            1d20
            1d20
            """);
        var evaluated = sheet.Evaluate(new ExecuteRollVisitor(inputDice));
        var section = evaluated.Sections.Should().ContainSingle().Subject;
        section.Rolls.Should()
            .AllSatisfy(
                result =>
                {
                    var roll = result.Value.Should().BeSet().Subject;
                    var group = roll.Groups.Should().ContainSingle().Subject;
                    group.Value.Should().Be(3);
                    group.Operations.Should().BeEmpty();
                    var part = group.Items.Should().ContainSingle().Which.Should().BeOfType<SingleRollResult>().Subject;
                    var die = part.Rolls.Should().ContainSingle().Subject;
                    die.Dropped.Should().BeFalse();
                    die.Value.Should().BeSameAs(inputDice[0]);
                }
            );
    }

    private TValue Requirement<TValue>(Func<TValue> get)
    {
        try
        {
            return get();
        }
        catch
        {
            Assert.Fail("Basic requirements failed, check other test results");
            throw null; // unreachable
        }
    }
}