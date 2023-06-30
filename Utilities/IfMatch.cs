using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Utilities;

public static class IfMatchEx
{
    public readonly struct IfMatched
    {
        public string Input { get; }
        public bool WasMatched => Input == null;

        public IfMatched(string input)
        {
            Input = input;
        }
    }
    
    public static IfMatched IfMatch(this string input, [RegexPattern] string pattern,
        Action<string[]> action)
    {
        return IfMatchedImpl(input, pattern, action);
    }
    
    public static IfMatched Or(this IfMatched input, [RegexPattern] string pattern,
        Action<string[]> action)
    {
        if (input.WasMatched)
            return input;

        return IfMatchedImpl(input.Input, pattern, action);
    }

    private static IfMatched IfMatchedImpl(string input, string pattern, Action<string[]> action)
    {
        var m = Regex.Match(input, pattern);
        if (!m.Success)
            return new IfMatched(input);
        action(m.Groups.Cast<Group>().Skip(1).Select(g => g.Value).ToArray());
        return default;
    }

}