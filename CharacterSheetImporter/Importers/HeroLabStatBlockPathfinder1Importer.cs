using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sprache;
using Utilities;

namespace CharacterSheetImporter.Importers;

public partial class HeroLabStatBlockPathfinder1Importer : StateMachineTextImporter<HeroLabStatBlockPathfinder1Importer.State>
{
    public class State
    {
        public string Name { get; set; }
        public string CurrentSection { get; set; }
        public StringBuilder Variables { get; } = new();
        public StringBuilder Saves { get; } = new();
        public StringBuilder Other { get; } = new();
        public StringBuilder Skills { get; } = new();
        public StringBuilder Attacks { get; } = new();
    }

    protected override State InitializeBuilder()
    {
        return new State();
    }

    protected override SheetImportResult Finalize(State builder)
    {
        if (builder.Variables.Length == 0 ||
            builder.Saves.Length == 0 ||
            builder.Skills.Length == 0 ||
            builder.Other.Length == 0)
        {
            // These sections shouldn't be empty, was probably the wrong format
            return new SheetImportResult(0, null);
        }

        var text =  $"""
            // Character Name: {builder.Name}
            // Imported from Pathfinder 1E Hero Lab state block
            {builder.Variables}
            
            {builder.Attacks}
            
            === Saves ===
            {builder.Saves}
            
            === Skills ===
            {builder.Skills}
            
            === Other ===
            {builder.Other}
            """;

        return new SheetImportResult(1, new ImportedSheet("PF1e-" + builder.Name, text));
    }

    protected override StateResult InitialState(string line, State state)
    {
        if (line == null)
            return Failure;
        
        if (IsHeaderBoundary(line))
            return (StateResult)ProcessHeader;
        
        if (state.Name == null)
            state.Name = line;
        else
            line.IfMatch(@"^Init ([+-]\d+); ", m => state.Other.AppendLine($"Initiative: d20 {m[0]}"));

        return default;
    }

    private StateResult ScanForHeader(string line, State state)
    {
        if (line == null)
            return Success;

        if (IsHeaderBoundary(line))
            return (StateResult)ProcessHeader;


        return null;
    }

    private StateResult ProcessHeader(string line, State state)
    {
        if (line == null)
            return Failure;

        state.CurrentSection = line;

        return (StateResult)EndHeader;
    }

    private StateResult EndHeader(string line, State state)
    {
        if (line == null)
            return Failure;

        if (IsHeaderBoundary(line))
        {
            return state.CurrentSection switch {
                "Defense" => (StateResult)DefenseSection,
                "Offense" => (StateResult)OffenseSection,
                "Statistics" => (StateResult)StatisticsSection,
                _ => (StateResult)ScanForHeader
            };
        }

        return null;
    }

    private static bool IsHeaderBoundary(string line)
    {
        return line.Length > 1 && line.All(c => c == '-');
    }

    private StateResult StatisticsSection(string line, State state)
    {
        if (line == null)
            return Failure;

        if (IsHeaderBoundary(line))
            return (StateResult)ProcessHeader;

        line.IfMatch(
                @"^Str (\d+), Dex (\d+), Con (\d+), Int (\d+), Wis (\d+), Cha (\d+)$",
                m =>
                {
                    state.Variables.AppendLine($"STR={m[0]}");
                    state.Variables.AppendLine("STRMOD=(@STR-10)/2");

                    state.Variables.AppendLine($"DEX={m[1]}");
                    state.Variables.AppendLine("DEXMOD=(@DEX-10)/2");

                    state.Variables.AppendLine($"CON={m[2]}");
                    state.Variables.AppendLine("CONMOD=(@CON-10)/2");

                    state.Variables.AppendLine($"INT={m[3]}");
                    state.Variables.AppendLine("INTMOD=(@INT-10)/2");

                    state.Variables.AppendLine($"WIS={m[4]}");
                    state.Variables.AppendLine("WISMOD=(@WIS-10)/2");

                    state.Variables.AppendLine($"CHA={m[5]}");
                    state.Variables.AppendLine("CHAMOD=(@CHA-10)/2");
                }
            )
            .Or(
                @"^Base Atk ([+-]\d+); CMB ([+-]\d+);",
                m =>
                {
                    state.Variables.AppendLine($"BaseAtk={int.Parse(m[0])}");
                    state.Other.AppendLine($"CMB: d20 {m[1]}");
                }
            ).Or("^Skills (.*)", m =>
            {
                var single = System.Text.RegularExpressions.Regex.Match(m[0], @"(?:^|.)\s*([a-zA-Z ()]*?)\s+([+-]\d+)(?:\s+\([^)]+\))?(?:,|;|$)");
                while (single.Success)
                {
                    state.Skills.AppendLine($"{single.Groups[1].Value}: d20 {single.Groups[2].Value}");
                    single = single.NextMatch();
                }
            });

        return null;
    }
    
    private StateResult OffenseSection(string line, State state)
    {
        if (line == null)
            return Failure;

        if (IsHeaderBoundary(line))
            return (StateResult)ProcessHeader;

        bool ProcessAttacks(string type, string s)
        {
            var res = AttackGrammar.AllAttackOptions.End().TryParse(s);
            if (!res.WasSuccessful)
                return false;

            int sequence = 0;
            foreach (IEnumerable<AttackGrammar.SingleAttackSpec> opt in res.Value)
            {
                sequence++;
                state.Attacks.AppendLine($"*** {type} Sequence {sequence} ***");
                foreach (AttackGrammar.SingleAttackSpec attack in opt)
                {
                    var critRange = "";

                    StringBuilder damageString = new StringBuilder();
                    foreach (var damage in attack.Damage)
                    {
                        critRange = damage.Range.Match(r => critRange == "" ? "c>" + r : critRange, critRange);
                        if (damageString.Length != 0)
                            damageString.Append(" + ");
                        if (damage.Tag.TryValue(out var tag) && !string.IsNullOrWhiteSpace(tag))
                            damageString.Append($"({damage.Damage}) {tag}");
                        else
                            damageString.Append(damage.Damage);
                    }

                    foreach (string bonus in attack.Bonuses)
                    {
                        state.Attacks.AppendLine($"{attack.Name}: d20{critRange} {bonus} => {damageString}");
                    }
                }

                state.Attacks.AppendLine();
            }

            return true;
        }

        if (line.StartsWith("Melee "))
        {
            if (!ProcessAttacks("Melee", line[6..]))
            {
                return new StateResult(OffenseSection, line);
            }
        }
        if (line.StartsWith("Ranged "))
        {
            if (!ProcessAttacks("Ranged", line[7..]))
            {
                return new StateResult(OffenseSection, line);
            }
        }

        return null;
    }

    private StateResult DefenseSection(string line, State state)
    {
        if (line == null)
            return Failure;

        if (IsHeaderBoundary(line))
            return (StateResult)ProcessHeader;

        line.IfMatch(@"^Fort ([+-]\d+).*, Ref ([+-]\d+).*, Will ([+-]\d+)", m =>
        {
            state.Saves.AppendLine($"Fortitude: d20 {m[0]}");
            state.Saves.AppendLine($"Reflex: d20 {m[1]}");
            state.Saves.AppendLine($"Will: d20 {m[2]}");
        });

        return null;
    }
}