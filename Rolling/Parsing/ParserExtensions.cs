using System;
using Sprache;

namespace Rolling.Parsing;

public static class ParserExtensions
{
    public static Parser<TInput> Before<TInput, TOutput>(this Parser<TInput> t, Parser<TOutput> u)
    {
        return t.Then(x => u.Select(y => x));
    }
    
    public static Parser<TOutput> FollowedBy<TInput, TOutput>(this Parser<TInput> t, Parser<TOutput> u)
    {
        return t.Then(_ => u.Select(y => y));
    }

    public static Parser<(TInput a, TOutput b)> With<TInput, TOutput>(this Parser<TInput> t, Parser<TOutput> u)
    {
        return t.Then(a => u.Select(b => (a, b)));
    }

    public static Parser<TOutput> Select<T1, T2, TOutput>(this Parser<(T1 a, T2 b)> input, Func<T1, T2, TOutput> map)
    {
        return input.Select(m => map(m.a, m.b));
    }

    public static Parser<TOutput> Select<T1, T2, T3, TOutput>(this Parser<((T1 a, T2 b) a, T3 b)> input,
        Func<T1, T2, T3, TOutput> map)
    {
        return input.Select(m => map(m.a.a, m.a.b, m.b));
    }
    
    public static Parser<T> SpaceAround<T>(this Parser<T> input)
    {
        return Parse.Chars(" \t").Many().FollowedBy(input).Before(Parse.Chars(" \t").Many());
    }
    
    public static Parser<TInput> EndOfLine<TInput>(this Parser<TInput> input)
    {
        return i =>
        {
            var res = input(i);
            if (!res.WasSuccessful)
                return res;

            i = res.Remainder;
            
            bool foundEnd = false;
            while (!i.AtEnd)
            {
                if (i.Current == '\n')
                    foundEnd = true;
                if (!char.IsWhiteSpace(i.Current))
                {
                    if (!foundEnd)
                        return Result.Failure<TInput>(i, $"Expected end of line, found {i.Current}", new[] { "\n" });
                    return Result.Success(res.Value, i);
                }
                i = i.Advance();
            }
            return Result.Success(res.Value, i);
        };
    }
}