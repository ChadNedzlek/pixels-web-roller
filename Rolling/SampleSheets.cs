using System.Collections.Generic;
using System.Collections.Immutable;
using Rolling.Models.Definitions;
using Rolling.Parsing;

namespace Rolling;

public class SampleSheet
{
    private static readonly RollParser _parser = new RollParser();
    
    public string Name { get; }
    public string Text { get; }
    public SheetDefinition Sheet { get; }

    public static ImmutableDictionary<string, SampleSheet> Available = new Dictionary<string, SampleSheet>
        {
            { "Sample-Pathfinder-1e", Build("Sample-Pathfinder-1e", Pathfinder1EText) }
        }
        .ToImmutableDictionary();

    private SampleSheet(string name, string text, SheetDefinition sheet)
    {
        Name = name;
        Text = text;
        Sheet = sheet;
    }

    public static SampleSheet Build(string name, string text)
    {
        return new SampleSheet(name, text, _parser.Parse(text));
    }

    public static string Pathfinder1EText =>
        """
        Str=12
        StrMod=(@Str-10)/2
        Dex=14
        DexMod=(@Dex-10)/2
        Con=13
        ConMod=(@Con-10)/2
        Int=16
        IntMod=(@Int-10)/2
        Wis=10
        WisMod=(@Wis-10)/2
        Cha=8
        ChaMod=(@Cha-10)/2

        Level=8
        Bab=(@Level * 4) / 3
        MeleeAttack=@Bab+@StrMod

        Will = 2 + @WisMod + (@Level/2)
        Reflex = @DexMod + (@Level/2)
        Fortitude = 2 + @ConMod + (@Level/2)

        === Skills ===
        Climb: d20 + @StrMod + 2
        Knowledge-Arcana: d20 + @IntMod + 5
        Perception: d20 + @WisMod + 2
        SpellCraft: d20 + @IntMod + 5
        User Magic Device: d20 + @ChaMod + 5

        *** Attacks ***
        Spell Strike (Shocking Grasp): d20 + @MeleeAttack => 1d8 + @StrMod + 3d6 electricity
        Attack 2: d20 + @MeleeAttack - 5 => 1d8 + @StrMod + 3d6 electricity 

        === Other ===
        Concentration: d20 + @Level + @IntMod
        Spell Penetration: d20 + @Level
        """;
}