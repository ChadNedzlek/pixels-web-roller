using System;
using System.Collections.Generic;
using System.Text;
using Sprache;

namespace Rolling.Parsing;

public static class ParserExtensions
{
    public static Parser<TInput> ThenDiscard<TInput, TOutput>(this Parser<TInput> t, Parser<TOutput> u)
    {
        return t.Then(x => u.Select(y => x));
    }
    
    public static Parser<TOutput> DiscardThen<TInput, TOutput>(this Parser<TInput> t, Parser<TOutput> u)
    {
        return t.Then(_ => u.Select(y => y));
    }

    public static Parser<(TInput a, TOutput b)> With<TInput, TOutput>(this Parser<TInput> t, Parser<TOutput> u)
    {
        return t.Then(a => u.Select(b => (a, b)));
    }

    public static Parser<TOutput> MapWith<T1, T2, TOutput>(this Parser<(T1 a, T2 b)> input, Func<T1, T2, TOutput> map)
    {
        return input.Select(m => map(m.a, m.b));
    }

    public static Parser<TOutput> MapWith<T1, T2, T3, TOutput>(this Parser<((T1 a, T2 b) l, T3 b)> input,
        Func<T1, T2, T3, TOutput> map)
    {
        return input.Select(m => map(m.l.a, m.l.b, m.b));
    }

    public static Parser<TOutput> MapWith<T1, T2, T3, T4, TOutput>(this Parser<(((T1 a, T2 b) l, T3 c) l, T4 d)> input,
        Func<T1, T2, T3, T4, TOutput> map)
    {
        return input.Select(m => map(m.l.l.a, m.l.l.b, m.l.c, m.d));
    }
    
    public static Parser<T> SpaceAround<T>(this Parser<T> input)
    {
        return Parse.Chars(" \t").Many().DiscardThen(input).ThenDiscard(Parse.Chars(" \t").Many());
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

    public static Parser<string> ManyString(this Parser<char> input, bool trim = false)
    {
        return input.Many().Select(c => string.Join("", c)).Select(s => trim ? s.Trim() : s);
    }
    
    public static Parser<(IEnumerable<TFirst> a, TSecond b)> ManyWith<TFirst, TSecond>(this Parser<TFirst> first, Parser<TSecond> second)
    {
        return input =>
        {
            IResult<TFirst> result = first(input);
            if (!result.WasSuccessful)
                return Result.Failure<(IEnumerable<TFirst> first, TSecond second)>(result.Remainder, result.Message, result.Expectations);
            input = result.Remainder;
            List<TFirst> list = new() { result.Value };
            while (!input.AtEnd)
            {
                var secondResult = second(input);
                if (secondResult.WasSuccessful)
                {
                    return Result.Success(((IEnumerable<TFirst>)list, secondResult.Value), secondResult.Remainder);
                }

                result = first(input);
                if (!result.WasSuccessful)
                {
                    return Result.Failure<(IEnumerable<TFirst> first, TSecond second)>(result.Remainder, result.Message, result.Expectations);
                }

                list.Add(result.Value);
                input = result.Remainder;
            }

            var failedSecond = second(input);
            return Result.Failure<(IEnumerable<TFirst> first, TSecond second)>(
                failedSecond.Remainder,
                failedSecond.Message,
                failedSecond.Expectations
            );
        };
    }
    
    public static Parser<(string a, TSecond b)> ManyStringWith<TSecond>(this Parser<char> first, Parser<TSecond> second)
    {
        return input =>
        {
            IResult<char> result = first(input);
            if (!result.WasSuccessful)
                return Result.Failure<(string first, TSecond second)>(result.Remainder, result.Message, result.Expectations);
            input = result.Remainder;
            StringBuilder s = new StringBuilder();
            s.Append(result.Value);
            while (!input.AtEnd)
            {
                var secondResult = second(input);
                if (secondResult.WasSuccessful)
                {
                    return Result.Success((s.ToString(), secondResult.Value), secondResult.Remainder);
                }

                result = first(input);
                if (!result.WasSuccessful)
                {
                    return Result.Failure<(string first, TSecond second)>(result.Remainder, result.Message, result.Expectations);
                }

                s.Append(result.Value);
                input = result.Remainder;
            }

            var failedSecond = second(input);
            return Result.Failure<(string first, TSecond second)>(
                failedSecond.Remainder,
                failedSecond.Message,
                failedSecond.Expectations
            );
        };
    }
}