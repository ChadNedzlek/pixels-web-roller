using System.Collections.Immutable;
using FluentAssertions;
using NUnit.Framework;
using Rolling.Models.Definitions;
using Rolling.Parsing;
using Sprache;

namespace Rolling.Tests;

public class RollParserTests
{
    [Test]
    public void IdentifierTest()
    {
        Grammar.Identifier.Parse("pizza").Should().Be("pizza");
    }
    [Test]
    public void WordsTest()
    {
        Grammar.Words.Parse("some words").Should().Be("some words");
    }
    [Test]
    public void ModListTests()
    {
        Grammar.ModList.Parse("c>19")
            .Should()
            .ContainSingle()
            .Which.Should()
            .Be(new DiceMod(DiceModType.CriticalSuccess, 19));
        Grammar.ModList.Parse("c<3")
            .Should()
            .ContainSingle()
            .Which.Should()
            .Be(new DiceMod(DiceModType.CriticalFailure, 3));
        Grammar.ModList.Parse("kh").Should().ContainSingle().Which.Should().Be(new DiceMod(DiceModType.Keep, 1));
        Grammar.ModList.Parse("k7").Should().ContainSingle().Which.Should().Be(new DiceMod(DiceModType.Keep, 7));
        Grammar.ModList.Parse("d6").Should().ContainSingle().Which.Should().Be(new DiceMod(DiceModType.Drop, 6));
        Grammar.ModList.Parse("c>19c<3k4d5")
            .Should()
            .SatisfyRespectively(
                a => a.Should().Be(new DiceMod(DiceModType.CriticalSuccess, 19)),
                a => a.Should().Be(new DiceMod(DiceModType.CriticalFailure, 3)),
                a => a.Should().Be(new DiceMod(DiceModType.Keep, 4)),
                a => a.Should().Be(new DiceMod(DiceModType.Drop, 5))
            );
    }
    
    [Test]
    public void DiceTests()
    {
        Grammar.Dice.Parse("d20").Should()
            .Be(new DiceSpecification(1, 20, ImmutableList<DiceMod>.Empty));
        Grammar.Dice.Parse("3d4").Should()
            .Be(new DiceSpecification(3, 4, ImmutableList<DiceMod>.Empty));
        Grammar.Dice.Parse("3d4c>19").Should()
            .BeEquivalentTo(new DiceSpecification(3, 4, ImmutableList.Create(new DiceMod(DiceModType.CriticalSuccess, 19))));
    }
    
    [Test]
    public void ExpressionTests()
    {
        Grammar.FinalExpr.Parse("1 + 2 * 3").Calculate().Should().Be(7);
        Grammar.FinalExpr.Parse("2 * 3 + 1").Calculate().Should().Be(7);
        Grammar.FinalExpr.Parse("2 * (3 + 1)").Calculate().Should().Be(8);
    }
    
    [Test]
    public void TaggedExpression()
    {
        Grammar.FinalExpr.Parse("1 tag").DebugString().Should().Be("(1 tag)");
        Grammar.FinalExpr.Parse("1 multi tag").DebugString().Should().Be("(1 multi tag)");
        Grammar.FinalExpr.Parse("1 + 2 multi tag + 3").DebugString().Should().Be("((1 + (2 multi tag)) + 3)");
    }

    [Test]
    public void VariableDef()
    {
        Grammar.VariableDefinition.Parse("A = 5").Name.Should().Be("A");
        Grammar.VariableDefinition.Parse("A = 5").Expression.DebugString().Should().Be("5");
        Grammar.VariableDefinition.Parse("IntMod = 5").Name.Should().Be("IntMod");
        Grammar.VariableDefinition.Parse("IntMod = 5 + @A").Expression.DebugString().Should().Be("(5 + @A)");
    }

    [Test]
    public void MultiVariableDef()
    {
        Grammar.VariableDefinitions.End().Parse("""
            A = 5
            B = 7
            """).Should().HaveCount(2);
    }

    [Test]
    public void RollDefinition()
    {
        DiceRollDefinition twoDFour = Grammar.RollDefinition.End().Parse("2d4");
        twoDFour.Name.Should().BeNone();
        twoDFour.Expression.DebugString().Should().Be("2d4");
    }

    [Test]
    public void ConditionalRollDefinition()
    {
        DiceRollDefinition twoDFour = Grammar.RollDefinition.End().Parse("d20 + 5 => 2d4 + 10");
        twoDFour.Name.Should().BeNone();
        twoDFour.Expression.DebugString().Should().Be("(1d20 + 5)");
        twoDFour.ConditionalExpression.Select(e => e.DebugString()).Should().Be("(2d4 + 10)");
    }

    [Test]
    public void MultipleRolls()
    {
        Grammar.RollDefinitions.End().Parse("""
            2d3
            4d5
            """).Should().HaveCount(2);
    }

    [Test]
    public void NamedRolls()
    {
        Grammar.RollDefinitions.End().Parse("Two Words: 4d5").Should().ContainSingle()
            .Which.Name.Should().Be("Two Words");
        
        Grammar.RollDefinitions.End().Parse("""
            A: 2d3
            Two Words: 4d5
            """).Should().HaveCount(2);
    }

    [Test]
    public void AnonymousSection()
    {
        Grammar.Section.End().Parse("""
            2d3
            4d5
            """).Rolls.Should().HaveCount(2);
    }

    [Test]
    public void NamedSection()
    {
         SheetDefinitionSection equalNamed = Grammar.Section.End().Parse("""
             === A name ===
             2d3
             4d5
             """);
         equalNamed.Rolls.Should().HaveCount(2);
         equalNamed.Name.Should().Be("A name");
         equalNamed.Type.Should().Be(RollSectionType.RepeatDice);
         
         SheetDefinitionSection starNamed = Grammar.Section.End().Parse("""
             *B name*
             2d3

             4d5
             """);
         starNamed.Rolls.Should().HaveCount(2);
         starNamed.Name.Should().Be("B name");
         starNamed.Type.Should().Be(RollSectionType.UniqueDicePerRoll);
        
        SheetDefinitionSection withName = Grammar.Section.End().Parse("""
            === A name ===
            A 1: 2d3
            """);
        withName.Name.Should().Be("A name");
        withName.Type.Should().Be(RollSectionType.RepeatDice);
        withName.Rolls.Should().ContainSingle().Which.Name.Should().Be("A 1");

    }

    [Test]
    public void FullSheet()
    {
        var sheet = Grammar.Sheet.End().Parse("""
A = 5
B = @A + 1d6 + 8

4d6 + 8 fire
8d12 + @B + @A

=== Section A ===
Attack 1: d20 + 5

*** Section B ***
d20 + 50
""");
        sheet.Sections.Should().HaveCount(3);
        sheet.Variables.Should().HaveCount(2);
    }
}