using System.Collections.Immutable;
using Rolling.Models.Definitions;
using Rolling.Models.Definitions.Expressions;
using Sprache;
using static Sprache.Parse;

namespace Rolling.Parsing;

public static class Grammar
{
    public static readonly Parser<int> Num = Number.Select(int.Parse);
    public static readonly Parser<string> Identifier = Parse.Identifier(
        Letter.Or(Char('_')),
        Letter.Or(Digit).Or(Char('_')).Or(Char('-'))
    );

    public static readonly Parser<string> Words = Letter.Or(Char('_'))
        .With(Letter.Or(Digit).Or(Char('_')).Or(Char('-')).Or(Char(' ')).Many())
        .Select((l, r) => (l + string.Join("", r)).Trim());

    public static readonly Parser<DiceMod> CritSuccessMod = Char('c').Before(Char('>')).FollowedBy(Num).Select(n => new DiceMod(DiceModType.CriticalSuccess, n));
    public static readonly Parser<DiceMod> CritFailMod = Char('c').Before(Char('<')).FollowedBy(Num).Select(n => new DiceMod(DiceModType.CriticalFailure, n));
    public static readonly Parser<DiceMod> KeepMod = Char('k').FollowedBy(Num.Or(Char('h').Return(1))).Select(n => new DiceMod(DiceModType.Keep, n));
    public static readonly Parser<DiceMod> DropMod = Char('d').FollowedBy(Num).Select(n => new DiceMod(DiceModType.Drop, n));

    public static readonly Parser<DiceMod> AllMod = CritSuccessMod
        .Or(CritFailMod)
        .Or(KeepMod)
        .Or(DropMod);
        
    public static readonly Parser<ImmutableList<DiceMod>> ModList = AllMod.Many().Select(d => d.ToImmutableList());

    public static readonly Parser<DiceSpecification> Dice = Num.Optional()
        .Before(Char('d'))
        .With(Num)
        .With(ModList)
        .SpaceAround()
        .Select(
            (n, c, m) => new DiceSpecification(n.Or(1), c, m)
        );
        
    public static readonly Parser<DiceExpression> Reference = Char('@').FollowedBy(Identifier).Select(id => (DiceExpression) new ReferenceExpression(id));
    public static readonly Parser<DiceExpression> Constant = Num.Select(id => (DiceExpression)new ConstantExpression(id));
    public static readonly Parser<DiceExpression> Roll = Dice.Select(d => (DiceExpression)new DiceRollExpression(d));

    public static readonly Parser<DiceExpression> BaseExpression = Reference.Or(Roll).Or(Constant);
        
    public static readonly Parser<DiceExpression> FinalRef = Ref(() => FinalExpr);
    public static readonly Parser<DiceExpression> PrimaryExpression = BaseExpression.Or(Char('(').SpaceAround().FollowedBy(FinalRef).Before(Char(')').SpaceAround()));
    public static readonly Parser<DiceExpression> TaggedExpression =
        PrimaryExpression.Then(e => Words.SpaceAround().Select(w => (DiceExpression)new TaggedExpression(e, w)))
            .Or(PrimaryExpression);
    public static readonly Parser<DiceExpression> MultExpr = ChainOperator(Chars("*/").SpaceAround(), TaggedExpression.SpaceAround(), (o, a, b) => new BinaryDiceExpression(a, b, o));
    public static readonly Parser<DiceExpression> AddExpr = ChainOperator(Chars("-+").SpaceAround(), MultExpr.SpaceAround(), (o, a, b) =>new BinaryDiceExpression(a, b, o));

    public static readonly Parser<DiceExpression> FinalExpr = AddExpr;

    public static readonly Parser<DiceRollDefinition> RollDefinition =
        Words.Before(Char(':').SpaceAround())
            .Optional()
            .With(FinalExpr)
            .With(String("=>").SpaceAround().FollowedBy(FinalExpr).Optional())
            .Select((id, ex, res) => new DiceRollDefinition(id.Maybe(), ex, res.Maybe()))
            .SpaceAround()
            .EndOfLine();
    
    public static readonly Parser<ImmutableList<DiceRollDefinition>> RollDefinitions =
        RollDefinition.Many().Select(d => d.ToImmutableList());

    public static readonly Parser<VariableDefinition> VariableDefinition =
        Identifier
            .Before(Char('=').SpaceAround())
            .With(FinalExpr)
            .Select((id, ex) => new VariableDefinition(id, ex))
            .SpaceAround()
            .EndOfLine();

    public static readonly Parser<ImmutableList<VariableDefinition>> VariableDefinitions =
        VariableDefinition.Many().Select(d => d.ToImmutableList());

    public static readonly Parser<(RollSectionType type, string w)> EqualSectionHeader =
        WhiteSpace.Many()
        .FollowedBy(Char('='))
        .AtLeastOnce()
        .FollowedBy(
            Words.SpaceAround().Select(w => (type: RollSectionType.RepeatDice, w))
        )
        .Before(Char('=').AtLeastOnce())
        .EndOfLine();

    public static readonly Parser<(RollSectionType type, string w)> StarSectionHeader = 
        WhiteSpace.Many()
        .FollowedBy(Char('*'))
        .AtLeastOnce()
        .FollowedBy(
            Words.SpaceAround().Select(w => (type: RollSectionType.UniqueDicePerRoll, w))
        )
        .Before(Char('*').AtLeastOnce())
        .EndOfLine();

    public static readonly Parser<SheetDefinitionSection> Section =
        EqualSectionHeader.Or(StarSectionHeader).Optional()
        .Before(WhiteSpace.Many())
        .With(RollDefinitions.SpaceAround())
        .Select(
            (s, d) => new SheetDefinitionSection(
                s.Maybe().Select(x => x.w),
                s.Maybe().Select(x => x.type).Or(RollSectionType.RepeatDice),
                d
            )
        );

    public static readonly Parser<ImmutableList<SheetDefinitionSection>> Sections =
        Section.AtLeastOnce().Select(s => s.ToImmutableList());

    public static readonly Parser<SheetDefinition> Sheet =
        VariableDefinitions.With(Sections).Select((v, s) => new SheetDefinition(v, s));
}