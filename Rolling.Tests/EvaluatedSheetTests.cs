using System.Collections.Immutable;
using FluentAssertions;
using NUnit.Framework;
using Rolling.Models.Definitions;
using Rolling.Parsing;
using Rolling.Visitors;

namespace Rolling.Tests;

public class EvaluatedSheetTests
{
    [Test]
    public void VisitBasic()
    {
        SheetDefinition sheetDefinition = new RollParser().Parse(
            """
            A = 2
            B = 3
            
            5d7
            Roll 2: 11d13 + 17
            d19 + @A + @B

            === SECTION ===
            d23 * @A * @B            
            """
        );
        EvaluatedSheet<string> sheet = sheetDefinition.Evaluate(new RollDescriptionVisitor());
        sheet.Sections.Should().SatisfyRespectively(
            s =>
            {
                s.Name.Should().BeNone();
                s.Rolls.Should()
                    .SatisfyRespectively(
                        r =>
                        {
                            r.Definition.Name.Should().BeNone();
                            r.Value.Should().Be("5d7");
                        },
                        r => 
                        {
                            r.Definition.Name.Should().Be("Roll 2");
                            r.Value.Should().Be("11d13 + 17");
                        },
                        r =>
                        {
                            r.Definition.Name.Should().BeNone();
                            r.Value.Should().Be("d19 + 2 + 3");
                        }
                    );
            },
            s => { 
                s.Name.Should().Be("SECTION");
                s.Rolls.Should()
                    .SatisfyRespectively(
                        r =>
                        {
                            r.Definition.Name.Should().BeNone();
                            r.Value.Should().Be("d23 * 2 * 3");
                        }
                    );}
        );
    }
}