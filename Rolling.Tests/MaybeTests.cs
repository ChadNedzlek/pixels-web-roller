using System;
using FluentAssertions;
using NUnit.Framework;
using Utilities;
using TestUtilities;

namespace Rolling.Tests;

public class MaybeTests
{
    [Test]
    public void NullThrows()
    {
        var act = () => Maybe.From((string)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void NoneMatchesDefault()
    {
        Maybe<int>.None.Match(x => x, 7).Should().Be(7);
    }
    
    [Test]
    public void ValueMatchesDefault()
    {
        Maybe.From(5).Match(x => x, 7).Should().Be(5);
    }

    [Test]
    public void NoneSelectsNone()
    {
        Maybe<int>.None.Select(x => x * 10).Should().BeNone();
    }

    [Test]
    public void ValueSelectsValue()
    {
        Maybe.From(7).Select(x => x * 10).Should().Be(70);
    }
}