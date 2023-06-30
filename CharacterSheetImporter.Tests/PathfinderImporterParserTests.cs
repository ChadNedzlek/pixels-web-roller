using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using CharacterSheetImporter.Importers;
using FluentAssertions;
using NUnit.Framework;
using Sprache;
using TestUtilities;

namespace CharacterSheetImporter.Tests;

public class PathfinderImporterParserTests
{
    [Test]
    public void SignedNumber()
    {
        var result = HeroLabStatBlockPathfinder1Importer.AttackGrammar.SignedNumber.End().Parse("+56");
        result.Should().Be("+56");
    }

    [Test]
    public void SingleAttackBonus()
    {
        var result = HeroLabStatBlockPathfinder1Importer.AttackGrammar.AttackBonuses.End().Parse("+56");
        result.Should().ContainSingle().Which.Should().Be("+56");
    }

    [Test]
    public void MultipleAttackBonus()
    {
        var result = HeroLabStatBlockPathfinder1Importer.AttackGrammar.AttackBonuses.End().Parse("+5/+0/-5");
        result.Should()
            .SatisfyRespectively(
                a => a.Should().Be("+5"),
                a => a.Should().Be("+0"),
                a => a.Should().Be("-5")
            );
    }

    [Test]
    public void CriticalRange()
    {
        var result = HeroLabStatBlockPathfinder1Importer.AttackGrammar.CriticalRange.End().Parse("/16-20");
        result.Should().Be("16");
    }

    [Test]
    public void CriticalMultiplier()
    {
        var result = HeroLabStatBlockPathfinder1Importer.AttackGrammar.CriticalMultiplier.End().Parse("/×3");
        result.Should().Be("3");
    }

    [Test]
    public void BasicDamage()
    {
        var result = HeroLabStatBlockPathfinder1Importer.AttackGrammar.DamageSpecifier.End().Parse("1d6");
        result.Damage.Should().Be("1d6");
    }

    [Test]
    public void DamageWithCrits()
    {
        var result = HeroLabStatBlockPathfinder1Importer.AttackGrammar.DamageSpecifier.End().Parse("1d6/16-20/×3");
        result.Damage.Should().Be("1d6");
        result.Range.Should().Be("16");
        result.Multiplier.Should().Be("3");
    }

    [Test]
    public void TaggedDamage()
    {
        var result = HeroLabStatBlockPathfinder1Importer.AttackGrammar.DamageSpecifier.End().Parse("1d6 fire");
        result.Damage.Should().Be("1d6");
        result.Tag.Should().Be("fire");
    }

    [Test]
    public void MultiDamage()
    {
        var result = HeroLabStatBlockPathfinder1Importer.AttackGrammar.MultipleDamageSpecifier.End().Parse("2d8 plus 1d6 fire");
        result.Should()
            .SatisfyRespectively(
                d => { d.Damage.Should().Be("2d8"); },
                d =>
                {
                    d.Damage.Should().Be("1d6");
                    d.Tag.Should().Be("fire");
                }
            );
    }
    
    [Test]
    public void SingleBasicAttack()
    {
        var result = HeroLabStatBlockPathfinder1Importer.AttackGrammar.SingleAttackOption.End().Parse("basic attack +1 (1d6)");
        result.Name.Should().Be("basic attack");
        result.Bonuses.Should().ContainSingle().Which.Should().Be("+1");
        var damage = result.Damage.Should().ContainSingle().Subject;
        damage.Damage.Should().Be("1d6");
        damage.Range.Should().BeNone();
    }

    [Test]
    public void ComplexSingleAttack()
    {
        var result = HeroLabStatBlockPathfinder1Importer.AttackGrammar.SingleAttackOption.End()
            .Parse("+1 basic attack +5/+0/-5 (1d6/16-20/x2 plus 2d6 fire)");
        result.Name.Should().Be("+1 basic attack");
        result.Bonuses.Should()
            .SatisfyRespectively(
                a => a.Should().Be("+5"),
                a => a.Should().Be("+0"),
                a => a.Should().Be("-5")
            );
        result.Damage.Should()
            .SatisfyRespectively(
                damage =>
                {
                    damage.Damage.Should().Be("1d6");
                    damage.Range.Should().Be("16");
                    damage.Multiplier.Should().Be("2");
                    damage.Tag.Should().BeNone();
                },
                damage =>
                {
                    damage.Damage.Should().Be("2d6");
                    damage.Range.Should().BeNone();
                    damage.Multiplier.Should().BeNone();
                    damage.Tag.Should().Be("fire");
                }
            );
    }

    [Test]
    public void NaturalAttack()
    {
        var result = HeroLabStatBlockPathfinder1Importer.AttackGrammar.FullAttackSpec.End()
            .Parse("bite +5 (1d8), claw +6 (2d6)");
        result.Should()
            .SatisfyRespectively(
                a =>
                {
                    a.Name.Should().Be("bite");
                    a.Bonuses.Should().ContainSingle().Which.Should().Be("+5");
                    a.Damage.Should().ContainSingle().Which.Damage.Should().Be("1d8");
                },
                a => {
                    a.Name.Should().Be("claw");
                    a.Bonuses.Should().ContainSingle().Which.Should().Be("+6");
                    a.Damage.Should().ContainSingle().Which.Damage.Should().Be("2d6"); }
            );
    }

    [Test]
    public void AllTogetherNow()
    {
        var result = HeroLabStatBlockPathfinder1Importer.AttackGrammar.AllAttackOptions.End()
            .Parse("bite +5 (1d8), claw +6 (2d6) or +1 sword +5/+0/-5 (1d6/16-20/x2 plus 2d6 fire)");
        result.Should().SatisfyRespectively(
            option =>
            {
                option.Should()
                    .SatisfyRespectively(
                        a =>
                        {
                            a.Name.Should().Be("bite");
                            a.Bonuses.Should().ContainSingle().Which.Should().Be("+5");
                            a.Damage.Should().ContainSingle().Which.Damage.Should().Be("1d8");
                        },
                        a =>
                        {
                            a.Name.Should().Be("claw");
                            a.Bonuses.Should().ContainSingle().Which.Should().Be("+6");
                            a.Damage.Should().ContainSingle().Which.Damage.Should().Be("2d6");
                        }
                    );
            },
            option =>
            {
                var attack = option.Should().ContainSingle().Subject; 
                attack.Name.Should().Be("+1 sword");
                attack.Bonuses.Should()
                    .SatisfyRespectively(
                        a => a.Should().Be("+5"),
                        a => a.Should().Be("+0"),
                        a => a.Should().Be("-5")
                    );
                attack.Damage.Should()
                    .SatisfyRespectively(
                        damage =>
                        {
                            damage.Damage.Should().Be("1d6");
                            damage.Range.Should().Be("16");
                            damage.Multiplier.Should().Be("2");
                            damage.Tag.Should().BeNone();
                        },
                        damage =>
                        {
                            damage.Damage.Should().Be("2d6");
                            damage.Range.Should().BeNone();
                            damage.Multiplier.Should().BeNone();
                            damage.Tag.Should().Be("fire");
                        }
                    );
            }
        );
    }

    [Test]
    public void AttackParserTest()
    {
        var result = HeroLabStatBlockPathfinder1Importer.AttackGrammar.AllAttackOptions.End().Parse("basic attack +1 (1d6)");
        var attack = result.Should().ContainSingle().Subject;
        var option = attack.Should().ContainSingle().Subject;
        option.Name.Should().Be("basic attack");
        option.Bonuses.Should().ContainSingle().Which.Should().Be("+1");
        var damage = option.Damage.Should().ContainSingle().Subject;
        damage.Damage.Should().Be("1d6");
        damage.Range.Should().BeNone();
    }
}