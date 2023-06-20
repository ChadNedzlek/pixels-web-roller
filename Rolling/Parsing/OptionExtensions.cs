using Rolling.Utilities;
using Sprache;

namespace Rolling.Parsing;

public static class OptionExtensions
{
    public static Maybe<T> Maybe<T>(this IOption<T> op)
    {
        return op.IsDefined ? new Maybe<T>(op.Get()) : new Maybe<T>();
    }
    public static T Or<T>(this IOption<T> op, T value)
    {
        return op.IsDefined ? op.Get() : value;
    }
}