using Rolling.Utilities;

namespace Rolling.Tests;

public static class MaybeAssertionsExtensions
{
    public static MaybeAssertions<T> Should<T>(this Maybe<T> value) => new MaybeAssertions<T>(value);
}