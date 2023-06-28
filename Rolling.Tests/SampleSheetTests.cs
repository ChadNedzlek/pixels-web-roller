using NUnit.Framework;

namespace Rolling.Tests;

public class SampleSheetTests
{
    [Test]
    public void Test()
    {
        SampleSheet.Build("PF-TEST", SampleSheet.Pathfinder1EText);
    }
}