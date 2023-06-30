using System.Collections.Generic;
using Rolling.Parsing;
using Sprache;
using Utilities;

using static Sprache.Parse;
using String = System.String;

namespace CharacterSheetImporter.Importers;

public partial class HeroLabStatBlockPathfinder1Importer : StateMachineTextImporter<HeroLabStatBlockPathfinder1Importer.State>
{
    public static class AttackGrammar
    {
        public static readonly Parser<string> SignedNumber = Chars("+-").With(Number).MapWith((s, d) => s + d);

        public static readonly Parser<string> CriticalRange =
            Char('/').DiscardThen(Number).ThenDiscard(Char('-')).ThenDiscard(Number);

        public static readonly Parser<string> CriticalMultiplier = Char('/').ThenDiscard(Chars("Xx×")).DiscardThen(Number);

        public record struct DamageSpec(string Damage,
            Maybe<string> Range,
            Maybe<string> Multiplier,
            Maybe<string> Tag);

        public static readonly Parser<DamageSpec> DamageSpecifier =
            Numeric.Or(Chars("d+-"))
                .ManyString()
                .With(CriticalRange.Optional())
                .With(CriticalMultiplier.Optional())
                .With(String("plus").Token().Not().DiscardThen(Letter.ManyString().Token()).Optional())
                .MapWith((d, r, m, t) => new DamageSpec(d, r.Maybe(), m.Maybe(), t.Maybe()));

        public static readonly Parser<IEnumerable<DamageSpec>> MultipleDamageSpecifier =
            DamageSpecifier.DelimitedBy(String(" plus "));

        public static readonly Parser<IEnumerable<string>> AttackBonuses = SignedNumber.DelimitedBy(Char('/'));

        public record struct SingleAttackSpec(string Name, IEnumerable<string> Bonuses, IEnumerable<DamageSpec> Damage);

        public static readonly Parser<SingleAttackSpec> SingleAttackOption =
            AnyChar.ManyStringWith(
                    AttackBonuses.Token()
                        .With(MultipleDamageSpecifier.Contained(Char('('), Char(')')))
                        .MapWith((bonus, damage) => (bonus, damage))
                )
                .MapWith((n, d) => new SingleAttackSpec(n, d.bonus, d.damage));

        public static readonly Parser<IEnumerable<SingleAttackSpec>> FullAttackSpec =
            SingleAttackOption.Token().DelimitedBy(Char(','));

        public static readonly Parser<IEnumerable<IEnumerable<SingleAttackSpec>>> AllAttackOptions =
            FullAttackSpec.Token().DelimitedBy(String("or"));
    }
}