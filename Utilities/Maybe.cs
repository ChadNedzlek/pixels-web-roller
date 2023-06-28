using System;
using System.Collections.Generic;

namespace Utilities;

public readonly struct Maybe<T> : IEquatable<Maybe<T>>
{
    public class MaybeComparer : IComparer<Maybe<T>>, IEqualityComparer<Maybe<string>>
    {
        public int Compare(Maybe<T> x, Maybe<T> y)
        {
            int hasValueComparison = x._hasValue.CompareTo(y._hasValue);
            if (hasValueComparison != 0) return hasValueComparison;
            return Comparer<T>.Default.Compare(x._value, y._value);
        }

        public bool Equals(Maybe<string> x, Maybe<string> y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(Maybe<string> obj)
        {
            return obj.GetHashCode();
        }
    }

    private readonly bool _hasValue;
    private readonly T _value;

    public static readonly Maybe<T> None = default;

    public Maybe(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _hasValue = true;
        _value = value;
    }

    public bool IsNone => !_hasValue;
    public static MaybeComparer Comparer { get; } = new MaybeComparer();

    public T Or(T value) => _hasValue ? _value : value;
    public T Or(Func<T> func) => _hasValue ? _value : func();
    public T OrDefault() => _value;

    public bool TryValue(out T value)
    {
        value = _value;
        return _hasValue;
    }

    public TOutput Match<TOutput>(Func<T, TOutput> fromValue, Func<TOutput> noValue)
    {
        if (_hasValue)
            return fromValue(_value);
        return noValue();
    }
    
    public TOutput Match<TOutput>(Func<T, TOutput> fromValue, TOutput noValue)
    {
        if (_hasValue)
            return fromValue(_value);
        return noValue;
    }

    public Maybe<TOutput> Select<TOutput>(Func<T, TOutput> selector)
    {
        if (_hasValue)
            return Maybe.From(selector(_value));
        return Maybe<TOutput>.None;
    }

    public static implicit operator Maybe<T>(T value) => new(value);

    public bool Equals(Maybe<T> other)
    {
        if (!_hasValue) return !other._hasValue;
        return other._hasValue && _value.Equals(other._value);
    }

    public override bool Equals(object obj)
    {
        return obj is Maybe<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _hasValue ? _value.GetHashCode() : 0;
    }

    public static bool operator ==(Maybe<T> left, Maybe<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Maybe<T> left, Maybe<T> right)
    {
        return !left.Equals(right);
    }
}

public static class Maybe
{
    public static Maybe<T> From<T>(T value) => new(value);
}