using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Annotations;
using UglyToad.PdfPig.Tokens;

namespace CharacterSheetImporter.Importers;

public class DndBeyondImporter : ISheetImporter
{
    public async Task<SheetImportResult> ImportAsync(Stream stream)
    {
        using var doc = PdfDocument.Open(stream);
        if (!doc.TryGetForm(out var form))
        {
            // return new SheetImportResult(0, null);
        }

        var annotations = doc.GetPage(1).ExperimentalAccess.GetAnnotations();
        Dictionary<string, string> values = new (StringComparer.OrdinalIgnoreCase);
        foreach (var a in annotations)
        {
            if (a.Type != AnnotationType.Widget)
                continue;
            if (!a.AnnotationDictionary.Data.TryGetValue("T", out var typeToken) ||
                typeToken is not StringToken type)
                continue;

            if (!a.AnnotationDictionary.Data.TryGetValue("V", out var valueToken) ||
                valueToken is not IDataToken<string> value)
                continue;
            
            var trimmedType = type.Data.TrimStart('(', ' ').TrimEnd(')', ' ');
            var trimmedValue = value.Data.TrimStart('(', ' ').TrimEnd(')', ' ');
            values.Add(trimmedType, trimmedValue);
        }

        (string name, string abbr)[] attrs = new[]
        {
            ("Strength", "STR"), ("Dexterity", "DEX"), ("Constitution", "CON"),
            ("Intelligence", "INT"), ("Wisdom", "WIS"), ("Charisma", "CHA")
        };

        StringBuilder variables = new StringBuilder();

        variables.AppendLine($"Proficiency = {int.Parse(values.GetValueOrDefault("ProfBonus", "0"))}");
        
        StringBuilder savingThrows = new StringBuilder();
        foreach (var (name, abbr) in attrs)
        {
            bool isProf = !string.IsNullOrEmpty(values.GetValueOrDefault(abbr + "Prof"));
            variables.AppendLine($"{abbr}={values.GetValueOrDefault(abbr + "mod")}");
            variables.AppendLine($"{abbr}MOD=(@{abbr}-10)/2");

            if (isProf)
                savingThrows.AppendLine($"{name}: d20 + @{abbr}MOD + @Proficiency");
            else
                savingThrows.AppendLine($"{name}: d20 + @{abbr}MOD");
        }

        StringBuilder skills = new StringBuilder();
        var skillNames = values.Keys.Where(k => k.EndsWith("Prof")).Select(k => k[..^4]).Except(attrs.Select(a => a.abbr), StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var skill in skillNames)
        {
            var pretty = Regex.Replace(skill, "[a-z][A-Z]", m => m.Value[0] + " " + m.Value[1]);
            var skillKey = skill == "AnimalHandling" ? "Animal" : skill; // for some reason
            string profString = values.GetValueOrDefault(skill + "Prof");
            string attr = values.GetValueOrDefault(skillKey + "Mod");
            skills.AppendLine(
                profString switch
                {
                    "E" => $"{pretty}: d20 + @{attr}MOD + @Proficiency * 2",
                    "P" => $"{pretty}: d20 + @{attr}MOD + @Proficiency",
                    _ => $"{pretty}: d20 + @{attr}MOD ",
                }
            );
        }

        StringBuilder attacks = new StringBuilder();
        for (int iWeapon = 1;; iWeapon++)
        {
            var nameKey = iWeapon == 1 ? "Wpn Name" : $"Wpn Name {iWeapon}";
            if (!values.TryGetValue(nameKey, out var name))
            {
                break;
            }

            string bonus = values.GetValueOrDefault($"Wpn{iWeapon} AtkBonus");
            string damage = values.GetValueOrDefault($"Wpn{iWeapon} Damage");
            var match = Regex.Match(damage, "^[0-9d+-]*");
            string damageRoll = "0";
            if (match.Success)
            {
                string damageType = damage.Substring(match.Length + 1);
                if (string.IsNullOrEmpty(damageType))
                    damageRoll = match.Value;
                else
                    damageRoll = $"({match.Value}) {damageType}";
            }

            attacks.AppendLine($"{name}: d20 {bonus} => {damageRoll}");
        }

        string characterName = values.GetValueOrDefault("CharacterName");

        var sheet = $"""
                     // Imported from dndbeyond PDF
                     // Character: {characterName}

                     {variables}
                     
                     === Attacks ===
                     {attacks}
                     
                     === Saving Throws ===
                     {savingThrows}
                     
                     === Skills ===
                     {skills}
                     
                     === Other ===
                     Initiative: d20 {values.GetValueOrDefault("Init", "+0")}
                     """;

        return new(1, new ($"5e-{characterName}", sheet));
    }
}