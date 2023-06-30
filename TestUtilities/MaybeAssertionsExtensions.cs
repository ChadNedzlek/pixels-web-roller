using Utilities;

namespace TestUtilities;

public static class MaybeAssertionsExtensions
{
    public static MaybeAssertions<T> Should<T>(this Maybe<T> value) => new MaybeAssertions<T>(value);
}