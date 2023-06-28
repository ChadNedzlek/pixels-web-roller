using System;
using System.Collections.Generic;
using System.Numerics;

namespace Utilities;

public static class DictionaryExtensions
{
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : new()
    {
        if (dict.TryGetValue(key, out var value))
            return value;
        value = new TValue();
        dict.Add(key, value);
        return value;
    }
    
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue toAdd)
    {
        if (dict.TryGetValue(key, out var value))
            return value;
        dict.Add(key, toAdd);
        return toAdd;
    }
    
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> toAdd)
    {
        if (dict.TryGetValue(key, out var value))
            return value;
        value = toAdd();
        dict.Add(key, value);
        return value;
    }

    public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dict,
        TKey key,
        Func<TValue> add,
        Func<TValue, TValue> update)
    {
        dict[key] = dict.TryGetValue(key, out var value) ? update(value) : add();
    }
    
    public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dict,
        TKey key,
        TValue add,
        Func<TValue, TValue> update)
    {
        dict[key] = dict.TryGetValue(key, out var value) ? update(value) : add;
    }

    public static void Increment<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue add)
        where TValue : IAdditionOperators<TValue, TValue, TValue>
    {
        dict[key] = dict.TryGetValue(key, out var value) ? value + add : add;
    }

    public static void Max<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue other)
        where TValue : INumber<TValue>
    {
        dict[key] = dict.TryGetValue(key, out var value) ? TValue.Max(value, other) : other;
    }
    
    public static void Increment<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        where TValue : IAdditionOperators<TValue, TValue, TValue>, IMultiplicativeIdentity<TValue, TValue>
    {
        var add = TValue.MultiplicativeIdentity;
        dict[key] = dict.TryGetValue(key, out var value) ? value + add : add;
    }
}