using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using CharacterSheetImporter.Importers;
using FluentAssertions;
using NUnit.Framework;
using Sprache;
using TestUtilities;

namespace CharacterSheetImporter.Tests;

public class PathfinderImporterTests
{
    [Test]
    public async Task FullSheetTest()
    {
        var path = Path.Combine(Assembly.GetExecutingAssembly().Location, "../Files/nikola.test.txt");
        var text = await File.ReadAllTextAsync(path);
        var importer = new HeroLabStatBlockPathfinder1Importer();
        var res = await importer.ImportAsync(text);
        res.Confidence.Should().Be(1);
        res.Sheet.Name.Should().EndWith("Nikola Ag'tharen");
        res.Sheet.SheetText.Should().Contain("Acrobatics: d20 -2");
        res.Sheet.SheetText.Should().Contain("Fortitude: d20 +6");
    }
}