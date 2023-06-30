using System.Collections.Immutable;
using Rolling.Models;
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
        Letter.Or(Digit).Or(Char('_'))
    );

    public static readonly Parser<string> Words = Letter.Or(Char('_'))
        .With(Letter.Or(Digit).Or(Char('_')).Or(Char('-')).Or(Char(' ')).Many())
        .MapWith((l, r) => (l + string.Join("", r)).Trim());

    public static readonly Parser<DiceMod> CritSuccessMod = Char('c').ThenDiscard(Char('>')).DiscardThen(Num).Select(n => new DiceMod(DiceModType.CriticalSuccess, n));
    public static readonly Parser<DiceMod> CritFailMod = Char('c').ThenDiscard(Char('<')).DiscardThen(Num).Select(n => new DiceMod(DiceModType.CriticalFailure, n));
    public static readonly Parser<DiceMod> KeepMod = Char('k').DiscardThen(Num.Or(Char('h').Return(1))).Select(n => new DiceMod(DiceModType.Keep, n));
    public static readonly Parser<DiceMod> DropMod = Char('d').DiscardThen(Num).Select(n => new DiceMod(DiceModType.Drop, n));

    public static readonly Parser<DiceMod> AllMod = CritSuccessMod
        .Or(CritFailMod)
        .Or(KeepMod)
        .Or(DropMod);
        
    public static readonly Parser<ImmutableList<DiceMod>> ModList = AllMod.Many().Select(d => d.ToImmutableList());

    public static readonly Parser<DiceSpecification> Dice = Num.Optional()
        .ThenDiscard(Char('d'))
        .With(Num)
        .With(ModList)
        .SpaceAround()
        .MapWith(
            (n, c, m) => new DiceSpecification(n.Or(1), c, m)
        );
        
    public static readonly Parser<DiceExpression> Reference = Char('@').DiscardThen(Identifier).Select(id => (DiceExpression) new ReferenceExpression(id));
    public static readonly Parser<DiceExpression> Constant = Num.Select(id => (DiceExpression)new ConstantExpression(id));
    public static readonly Parser<DiceExpression> Roll = Dice.Select(d => (DiceExpression)new DiceRollExpression(d));

    public static readonly Parser<DiceExpression> BaseExpression = Reference.Or(Roll).Or(Constant);
        
    public static readonly Parser<DiceExpression> FinalRef = Ref(() => FinalExpr);
    public static readonly Parser<DiceExpression> PrimaryExpression = BaseExpression.Or(Char('(').SpaceAround().DiscardThen(FinalRef).ThenDiscard(Char(')').SpaceAround()));
    public static readonly Parser<DiceExpression> TaggedExpression =
        PrimaryExpression.Then(e => Words.SpaceAround().Select(w => (DiceExpression)new TaggedExpression(e, w)))
            .Or(PrimaryExpression);
    public static readonly Parser<DiceExpression> MultExpr = ChainOperator(Chars("*/").SpaceAround(), TaggedExpression.SpaceAround(), (o, a, b) => new BinaryDiceExpression(a, b, o));
    public static readonly Parser<DiceExpression> AddExpr = ChainOperator(Chars("-+").SpaceAround(), MultExpr.SpaceAround(), (o, a, b) =>new BinaryDiceExpression(a, b, o));

    public static readonly Parser<DiceExpression> FinalExpr = AddExpr;

    public static readonly Parser<DiceRollDefinition> RollDefinition =
        CharExcept(":\r\n").ManyString(trim: true).ThenDiscard(Char(':').SpaceAround())
            .Optional()
            .With(FinalExpr)
            .With(String("=>").SpaceAround().DiscardThen(FinalExpr).Optional())
            .MapWith((id, ex, res) => new DiceRollDefinition(id.Maybe(), ex, res.Maybe()))
            .SpaceAround()
            .EndOfLine();
    
    public static readonly Parser<ImmutableList<DiceRollDefinition>> RollDefinitions =
        RollDefinition.Many().Select(d => d.ToImmutableList());

    public static readonly Parser<VariableDefinition> VariableDefinition =
        Identifier
            .ThenDiscard(Char('=').SpaceAround())
            .With(FinalExpr)
            .MapWith((id, ex) => new VariableDefinition(id, ex))
            .SpaceAround()
            .EndOfLine();

    public static readonly Parser<ImmutableList<VariableDefinition>> VariableDefinitions =
        VariableDefinition.Many().Select(d => d.ToImmutableList());

    public static readonly Parser<(RollSectionType type, string w)> EqualSectionHeader =
        WhiteSpace.Many()
        .DiscardThen(Char('='))
        .AtLeastOnce()
        .DiscardThen(
            CharExcept("=\r\n").ManyString(trim: true).SpaceAround().Select(w => (type: RollSectionType.RepeatDice, w))
        )
        .ThenDiscard(Char('=').AtLeastOnce())
        .EndOfLine();

    public static readonly Parser<(RollSectionType type, string w)> StarSectionHeader = 
        WhiteSpace.Many()
        .DiscardThen(Char('*'))
        .AtLeastOnce()
        .DiscardThen(
            CharExcept("*\r\n").ManyString(trim: true).SpaceAround().Select(w => (type: RollSectionType.UniqueDicePerRoll, w))
        )
        .ThenDiscard(Char('*').AtLeastOnce())
        .EndOfLine();

    public static readonly Parser<SheetDefinitionSection> Section =
        EqualSectionHeader.Or(StarSectionHeader).Optional()
        .ThenDiscard(WhiteSpace.Many())
        .With(RollDefinitions.SpaceAround())
        .MapWith(
            (s, d) => new SheetDefinitionSection(
                s.Maybe().Select(x => x.w),
                s.Maybe().Select(x => x.type).Or(RollSectionType.RepeatDice),
                d
            )
        );

    public static readonly Parser<ImmutableList<SheetDefinitionSection>> Sections =
        Section.AtLeastOnce().Select(s => s.ToImmutableList());

    public static readonly Parser<SheetDefinition> Sheet =
        VariableDefinitions.With(Sections).MapWith((v, s) => new SheetDefinition(v, s));
}